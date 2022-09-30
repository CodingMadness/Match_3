//using DotNext;

using System.Drawing;
using System.Numerics;
using Match_3.GameTypes;
using Microsoft.Toolkit.HighPerformance;
using Raylib_CsLo;

namespace Match_3
{
    public sealed class Grid
    {
        public enum Direction : byte
        {
            PositiveX = 0,
            NegativeX = 1,
            PositiveY = 2,
            NegativeY = 3,
        }
      
        private readonly ITile?[,] _bitmap;

        //public double Timer { get; private set; } = 10;
        public readonly int TileWidth;
        public readonly int TileHeight;

        public static event Action<int[]> NotifyOnGridCreationDone;

        private const int MaxDestroyableTiles = 3;

        public static ITile? LastMatchTrigger { get; private set; }
        private GameTime _gridTimer;
        private readonly bool isDrawn = true;
        private byte _match3FuncCounter;

        private void CreateMap()
        {
            Span<int> counts = stackalloc int[(int)Balls.Length];
            ITile.GetAtlas() = AssetManager.DefaultTileAtlas;

            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 0; y < TileHeight; y++)
                {
                    Vector2 current = new(x, y);
                    float noise = Utils.NoiseMaker.GetNoise(x * -0.5f, y * -0.5f);
                    _bitmap[x, y] = Bakery.CreateTile(current, noise);
                    var kind = _bitmap[x, y] is Tile { Body: CandyShape c } ? c.Ball : Balls.Empty;
                    counts[(int)kind]++;
                }
            }
            
            
            NotifyOnGridCreationDone(counts.ToArray());
        }

        public (int start, int end) MakePlaceForTimer()
        {
            float beginX = (TileWidth - 1) / 2f;
            this[new(beginX-1, 0)] = null;
            this[new(beginX-0, 0)] = null;
            this[new(beginX+1, 0)] = null;
            this[new(beginX+2, 0)] = null;
            
            int start = (int)(beginX-1) * ITile.Size;
            int end = (int)(beginX+3) * ITile.Size;
            return (start, end);
        }
        
        public Grid(Level current)
        {
            TileWidth = current.TilemapWidth;
            TileHeight = current.TilemapHeight;
            _bitmap = new ITile?[TileWidth, TileHeight];
            _gridTimer = GameTime.GetTimer(5 * 60);
            NotifyOnGridCreationDone += GameRuleManager.SetCountPerBall;
            CreateMap();
        }

        public void Draw(float elapsedTime)
        {
            //Do this Draw second per second ONLY ONCE
            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 0; y < TileHeight; y++)
                {
                    ITile? basicTile = _bitmap[x, y];

                    if (basicTile is not null && !basicTile.IsDeleted)
                    {
                        ITile.GetAtlas() = (basicTile is EnemyTile)
                            ? AssetManager.EnemyAtlas
                            : AssetManager.DefaultTileAtlas;

                        basicTile.Draw(elapsedTime);
                    }
                }
            }

            //isDrawn = true;
            //Console.WriteLine("ITERATION OVER FOR THIS DRAW-CALL!");
        }

        public ITile? this[Vector2 coord]
        {
            get
            {
                if (coord.X >= 0 && coord.X < TileWidth
                                 && coord.Y >= 0 && coord.Y < TileHeight)
                {
                    //its within bounds!
                    var tmp = _bitmap[(int)coord.X, (int)coord.Y];
                    return tmp;
                }

                return default;
            }
            set
            {
                if (coord.X >= 0 && coord.Y >= 0 && coord.X < TileWidth && coord.Y < TileHeight)
                {
                    _bitmap[(int)coord.X, (int)coord.Y] = value;
                }
            }
        }

        public bool WasAMatchInAnyDirection(ITile? match3Trigger, ISet<ITile?> matches)
        {
            static bool AddWhenEqual(ITile? first, ITile? next, ISet<ITile?> matches)
            {
                if (first is Tile f &&
                    next is Tile n &&
                    f.Equals(n))
                {
                    if (matches.Count == MaxDestroyableTiles)
                        return false;

                    matches.Add(first);
                    matches.Add(next);
                    return true;
                }
                return false;
            }

            static Vector2 NextFrom(Vector2 input, Direction direction)
            {
                var tmp = direction switch
                {
                    Direction.NegativeX => input with { X = input.X - 1 },
                    Direction.PositiveX => input with { X = input.X + 1 },
                    Direction.NegativeY => input with { Y = input.Y - 1 },
                    Direction.PositiveY => input with { Y = input.Y + 1 },
                    _ => Vector2.Zero
                };

                return tmp;
            }

            const Direction lastDir = (Direction)4;

            LastMatchTrigger = match3Trigger;

            if (LastMatchTrigger is not null)
            {
                for (Direction i = 0; i < lastDir; i++)
                {
                    Vector2 nextCoords = NextFrom(LastMatchTrigger.Cell, i);
                    var next = this[nextCoords];

                    while (AddWhenEqual(LastMatchTrigger, next, matches))
                    {
                        //compute the proper (x,y) for next round, because
                        //we found a match between a -> b, now we check
                        //a -> c and so on
                        nextCoords = NextFrom(nextCoords, i);
                        next = this[nextCoords];
                    }
                }
                //it is kinda working, but depending on game logic, I would like to be able to
                //potentially swap endlessly the same matching-tiles...
                if (matches.Count < MaxDestroyableTiles && ++_match3FuncCounter <= 1)
                {
                    matches.Clear();
                    return WasAMatchInAnyDirection(this[LastMatchTrigger.CoordsB4Swap], matches);
                }
            }
            if (_match3FuncCounter >= 1)
            {
                _match3FuncCounter = 0;
            }
            return matches.Count == MaxDestroyableTiles;
        }

        public bool TryGetClickedTile(out ITile? tile)
        {
            tile = default!;

            if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
                return false;

            var mouseVec2 = Raylib.GetMousePosition();
            Vector2 gridPos = new Vector2((int)mouseVec2.X, (int)mouseVec2.Y);
            gridPos /= ITile.Size;
            tile = this[gridPos];
            return tile is not null;
        }

        public bool Swap(ITile? a, ITile? b)
        {
            if (a is null || b is null || a.IsDeleted || b.IsDeleted)
            {
                return false;
            }
            if ((a.State & TileState.UnMovable) == TileState.UnMovable ||
                (b.State & TileState.UnMovable) == TileState.UnMovable)
                return false;
            
            this[a.Cell] = b;
            this[b.Cell] = a;
            (a.Cell, b.Cell) = (b.Cell, a.Cell);
            (a.CoordsB4Swap, b.CoordsB4Swap) = (b.Cell, a.Cell);
            return true;
        }

        public void Delete(Vector2 coord)
        {
            var tile = this[coord];

            if (tile is Tile mapTile)
            {
                //we mark the tile as kind of "deleted"
                //by making it invisible and disallowing any
                //movement to be happening
                mapTile.IsDeleted = true;
                mapTile.Disable();
            }
        }
    }
}