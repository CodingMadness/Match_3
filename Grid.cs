﻿//using DotNext;

using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;

namespace Match_3
{
    public sealed class Grid
    {
        public enum Direction
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
            ITile.GetAtlas() = AssetManager.Default;

            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 1; y < TileHeight; y++)
                {
                    Vector2 current = new(x, y);
                    float noise = Utils.NoiseMaker.GetNoise(x * -0.5f, y * -0.5f);
                    _bitmap[x, y] = Bakery.CreateTile(current, noise);
                    var kind = _bitmap[x, y] is Tile { Shape: CandyShape c } ? c.Ball : Balls.Empty;
                    counts[(int)kind]++;
                }
            }
            NotifyOnGridCreationDone(counts.ToArray());
        }

        public Grid(Level current)
        {
            TileWidth = current.TilemapWidth;
            TileHeight = current.TilemapHeight;
            _bitmap = new ITile?[TileWidth, TileHeight];
            _gridTimer = GameTime.GetTimer(5 * 60);
            NotifyOnGridCreationDone += GameRuleManager.DefineMatch3Quest;
            CreateMap();
        }

        public void Draw(float elapsedTime)
        {
            //Do this Draw second per second ONLY ONCE
            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 1; y < TileHeight; y++)
                {
                    ITile? basicTile = _bitmap[x, y];
                    
                    var matchBlockAtlas = (basicTile.State == TileState.UnMovable
                        ? AssetManager.MatchBlockAtlas
                        : AssetManager.Default);

                     ITile.GetAtlas() = matchBlockAtlas;
                    
                    if (basicTile is not null)
                    {
                        
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

        public bool MatchInAnyDirection(ITile? match3Trigger, ISet<ITile?> matches)
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

            static Vector2 GetStepsFromDirection(Vector2 input, Direction direction)
            {
                var tmp = direction switch
                {
                    Direction.NegativeX => new Vector2(input.X - 1, input.Y),
                    Direction.PositiveX => new Vector2(input.X + 1, input.Y),
                    Direction.NegativeY => new Vector2(input.X, input.Y - 1),
                    Direction.PositiveY => new Vector2(input.X, input.Y + 1),
                    _ => Vector2.Zero
                };

                return tmp;
            }

            const Direction lastDir = (Direction)4;

            LastMatchTrigger = match3Trigger;

            if (LastMatchTrigger is not null)
            {
                var coordA = LastMatchTrigger.CurrentCoords;
                var coordB = LastMatchTrigger.CoordsB4Swap;

                for (Direction i = 0; i < lastDir; i++)
                {
                    Vector2 nextCoords = GetStepsFromDirection(coordA, i);
                    var next = this[nextCoords];

                    while (AddWhenEqual(LastMatchTrigger, next, matches))
                    {
                        //compute the proper (x,y) for next round!
                        nextCoords = GetStepsFromDirection(nextCoords, i);
                        next = this[nextCoords];
                    }
                }
                //it is kinda working, but depending on game logic, I would like to be able to
                //potentially swap endlessly the same matching-tiles...
                if (matches.Count < MaxDestroyableTiles && ++_match3FuncCounter <= 1)
                {
                    matches.Clear();
                    return MatchInAnyDirection(this[coordB], matches);
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

        public void Swap(ITile? a, ITile? b)
        {
            if (a is null || b is null)
            {
                if (a is MatchBlockTile a0 && a0.State == TileState.UnMovable ||
                    b is MatchBlockTile b0 && b0.State == TileState.UnMovable)
                    return;

                return;
            }
            this[a.CurrentCoords] = b;
            this[b.CurrentCoords] = a;
            (a.CurrentCoords, b.CurrentCoords) = (b.CurrentCoords, a.CurrentCoords);
            (a.CoordsB4Swap, b.CoordsB4Swap) = (b.CurrentCoords, a.CurrentCoords);
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
                mapTile.State = TileState.UnMovable | TileState.UnShapeable;
                FadeableColor fc = Raylib.WHITE;
                fc.CurrentAlpha = 0f;
                mapTile.Shape.FadeTint = fc.Apply();
            }
        }
    }
}