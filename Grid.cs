//using DotNext;

using Match_3.GameTypes;
using Raylib_cs;
using System.Numerics;

namespace Match_3
{
    public sealed class Grid
    {
        enum Direction
        {
            PositiveX = 0,
            NegativeX = 1,
            PositiveY = 2,
            NegativeY = 3,
        }

        private readonly ITile[,] _bitmap;

        //public double Timer { get; private set; } = 10;
        public readonly int TileWidth;
        public readonly int TileHeight;
        private const int MaxDestroyableTiles = 3;
        //private bool _stopDrawingSameStuff = false;

        public static ITile? MatchXTrigger { get; private set; }
        private GameTime _gridTimer;

        private readonly bool isDrawn = true;

        private void CreateMap()
        {
            FastNoiseLite noiseMaker = new(DateTime.UtcNow.GetHashCode());
            noiseMaker.SetFrequency(10f);
            noiseMaker.SetFractalType(FastNoiseLite.FractalType.PingPong);
            noiseMaker.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 0; y < TileHeight; y++)
                {
                    float noise = noiseMaker.GetNoise(x,y);
                    Vector2 current = new(x, y);

                    if (noise < 0f)
                        noise = -noise;

                    else if (noise == 0f) 
                        noise = noiseMaker.GetNoise(x, y);

                    _bitmap[x, y] = Backery.CreateTile_1(current, noise);
                    Console.WriteLine("NOISE: " + noise);
                }
            }
        }

        public Grid(GameState current)
        {
            TileWidth = current.TilemapWidth;
            TileHeight = current.TilemapHeight;
            _bitmap = new ITile[TileWidth, TileHeight];
            _gridTimer = GameTime.GetTimer(5*60);
            CreateMap();
        }

        public void Draw(float elapsedTime)
        {
            //Do this Draw second per second OLNLY ONCE
            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 1; y < TileHeight; y++ )
                {
                    var tile = _bitmap[x, y];
                    
                    if (!isDrawn)
                    {
                        _gridTimer.UpdateTimer();
                        Console.WriteLine(_gridTimer.ElapsedSeconds);

                        if (_gridTimer.Done())
                        {
                            //tile?.Draw(new(x,y));
                            //Console.WriteLine(x + ":  " + "  " + y);
                            _gridTimer.Reset(null);
                        }
                    }
                    else
                    {
                        if ((x, y) == (0, 0))
                            continue;
                        
                        tile?.Draw(new(x,y), elapsedTime);
                        //Draw normally!
                    }
                    //Console.WriteLine(x + ":  " + "  " + y);
                }
            }

            //isDrawn = true;
            //Console.WriteLine("ITERATION OVER FOR THIS DRAW-CALL!");
        }

        public ITile this[Vector2 coord]
        {
            get
            {
                if (coord.X >= 0 && coord.X < TileWidth 
                                 &&coord.Y >= 0 && coord.Y < TileHeight)
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

        public bool MatchInAnyDirection(Vector2 clickedCoord, ISet<ITile> matches)
        {
            static bool AddWhenEqual(ITile first, ITile next, ISet<ITile> matches)
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

            ITile first = this[clickedCoord];
            MatchXTrigger = this[clickedCoord];

            for (Direction i = 0; (i < lastDir); i++)
            {
                /*
                if (matches.Count == MAX_DESTROYABLE_TILES)
                    return true;
                */
                Vector2 nextCoords = GetStepsFromDirection(clickedCoord, i);
                ITile next = this[nextCoords];

                while (AddWhenEqual(first, next, matches) /*&& matches.Count < MAX_DESTROYABLE_TILES*/)
                {
                    //compute the proper (x,y) for next round!
                    nextCoords = GetStepsFromDirection(nextCoords, i);
                    next = this[nextCoords];
                }
            }

            return matches.Count == MaxDestroyableTiles;
        }

        public bool TryGetClickedTile(out ITile tile)
        {
            tile = default!;

            if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
                return false;

            var mouseVec2 = Raylib.GetMousePosition();
            Vector2 position = new Vector2((int)mouseVec2.X, (int)mouseVec2.Y);
            position /= ITile.Size;
            tile = this[position];
            return tile is not null;
        }

        public void Swap(ITile a, ITile b)
        {
            if (a is null || b is null)
                return;

            this[a.Current] = b;
            this[b.Current] = a;
            (a.Current, b.Current) = (b.Current, a.Current);
            (a.CoordsB4Swap, b.CoordsB4Swap) = (b.Current, a.Current);
        }

        public void Delete(Vector2 coord) => this[coord] = default;
    }
}