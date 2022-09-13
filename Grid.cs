//using DotNext;

using Raylib_cs;

namespace Match_3
{
    public sealed class Grid<TTile> where TTile : ITile
    {
        enum Direction
        {
            PositiveX = 0,
            NegativeX = 1,
            PositiveY = 2,
            NegativeY = 3,
        }

        private readonly TTile?[,] _bitmap;

        //public double Timer { get; private set; } = 10;
        public readonly int TileWidth;
        public readonly int TileHeight;
        public const int TileSize = 64;
        private const int MaxDestroyableTiles = 3;
        //private bool _stopDrawingSameStuff = false;

        public static TTile? MatchXTrigger { get; private set; }
        private GameTime _gridTimer;

        private bool isDrawn = true;

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
                    
                    if (noise < 0f)
                        noise -= noise;
                    else if (noise == 0f)
                        Console.WriteLine(noise);
                    //    noise = noiseMaker.GetNoise(x, y);

                    _bitmap[x, y] = (TTile?)Tile.Create(new(x, y), noise);
                }
            }
        }

        public Grid(int tileWidth, int tileHeight, in GameTime gridTimer)
        {
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            _bitmap = new TTile?[TileWidth, TileHeight];
            _gridTimer = gridTimer;
            _gridTimer = GameTime.GetTimer(5*60);
            CreateMap();
        }

        public void Draw()
        {
            //Do this Draw second per second OLNLY ONCE
            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 0; y < TileHeight; y++ )
                {
                    var tile = _bitmap[x, y];
                    
                    if (!isDrawn)
                    {
                        _gridTimer.UpdateTimer();
                        Console.WriteLine(_gridTimer.ElapsedSeconds);
                        if (_gridTimer.TimerDone())
                        {
                            tile?.Draw(new(x,y));
                            //Console.WriteLine(x + ":  " + "  " + y);
                            _gridTimer.Reset();
                        }
                    }
                    else
                    {
                        if ((x, y) == (0, 0))
                            continue;
                        
                        tile?.Draw(new(x,y));
                        //Draw normally!
                    }
                    //Console.WriteLine(x + ":  " + "  " + y);
                }
            }

            //isDrawn = true;
            //Console.WriteLine("ITERATION OVER FOR THIS DRAW-CALL!");
        }

        public TTile? this[Int2 coord]
        {
            get
            {
                if (coord.X >= 0 && coord.X < TileWidth 
                                 &&coord.Y >= 0 && coord.Y < TileHeight)
                {
                    //its within bounds!
                    var tmp = _bitmap[coord.X, coord.Y];
                    return tmp;
                }

                return default;
                //throw new Exception("ck.ysdnjlfdnajöilsfshfl");
            }
            set
            {
                if (coord.X >= 0 && coord.Y >= 0 && coord.X < TileWidth && coord.Y < TileHeight)
                {
                    _bitmap[coord.X, coord.Y] = value;
                }
            }
        }

        public bool MatchInAnyDirection(Int2 clickedCoord, ISet<TTile> matches)
        {
            static bool AddWhenEqual(TTile? first, TTile? next, ISet<TTile> matches)
            {
                if (first is not null &&
                    next is not null &&
                    first.Equals(next))
                {
                    if (matches.Count == MaxDestroyableTiles)
                        return false;

                    matches.Add(first);
                    matches.Add(next);
                    return true;
                }

                return false;
            }

            static Int2 GetStepsFromDirection(Int2 input, Direction direction)
            {
                var tmp = direction switch
                {
                    Direction.NegativeX => new Int2(input.X - 1, input.Y),
                    Direction.PositiveX => new Int2(input.X + 1, input.Y),
                    Direction.NegativeY => new Int2(input.X, input.Y - 1),
                    Direction.PositiveY => new Int2(input.X, input.Y + 1),
                    _ => Int2.Zero
                };

                return tmp;
            }

            const Direction lastDir = (Direction)4;

            TTile? first = this[clickedCoord];
            MatchXTrigger = this[clickedCoord];

            for (Direction i = 0; (i < lastDir); i++)
            {
                /*
                if (matches.Count == MAX_DESTROYABLE_TILES)
                    return true;
                */
                Int2 nextCoords = GetStepsFromDirection(clickedCoord, i);
                TTile? next = this[nextCoords];

                while (AddWhenEqual(first, next, matches) /*&& matches.Count < MAX_DESTROYABLE_TILES*/)
                {
                    //compute the proper (x,y) for next round!
                    nextCoords = GetStepsFromDirection(nextCoords, i);
                    next = this[nextCoords];
                }
            }

            return matches.Count == MaxDestroyableTiles;
        }

        public bool TryGetClickedTile(out TTile? tile)
        {
            tile = default;

            if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
                return false;

            var mouseVec2 = Raylib.GetMousePosition();
            Int2 position = new Int2((int)mouseVec2.X, (int)mouseVec2.Y);
            position /= TileSize;
            tile = this[position];
            return tile is not null;
        }

        public void Swap(TTile? a, TTile? b)
        {
            if (a is null || b is null)
                return;

            this[a.Cell] = b;
            this[b.Cell] = a;
            (a.Cell, b.Cell) = (b.Cell, a.Cell);
            (a.CoordsB4Swap, b.CoordsB4Swap) = (b.Cell, a.Cell);
        }
    }
}