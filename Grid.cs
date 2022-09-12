//using DotNext;

using System.Diagnostics.CodeAnalysis;
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
        public const int TILE_SIZE = 64;
        private const int MAX_DESTROYABLE_TILES = 3;
        private bool stopDrawingSameStuff = false;

        public static TTile MatchXTrigger { get; private set; }
        private GameTime _gridTimer;
        private WeightedCellPool CellPool { get; }

        private void CreateMap(bool shuffle)
        {
            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 0; y < TileHeight; y++)
                {
                    _bitmap[x, y] = (TTile)TTile.Create(shuffle ? CellPool.GetNext() : new(x, y));
                }
            }
        }

        private IEnumerable<Int2> YieldGameWindow()
        {
            for (int x = 0; x < TileWidth - 0; x++)
            {
                for (int y = 0; y < TileHeight - 0; y++)
                {
                    Int2 current = new(x * TILE_SIZE * 1, y * TILE_SIZE * 1);
                    yield return current;
                }
            }
        }

        public Grid(int tileWidth, int tileHeight, bool shuffle)
        {
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            _bitmap = new TTile?[TileWidth, TileHeight];
            _gridTimer = new();
            CellPool = new WeightedCellPool(YieldGameWindow());
            _gridTimer.StartTimer(10f);
            CreateMap(shuffle);
        }

        public void Draw()
        {
            _gridTimer.UpdateTimer();

            const int timeMax = 10; //<timeMax> seconds for all tiles to appear

            var p = (_gridTimer.ElapsedTime / timeMax);
            var px = p * TileWidth;
            var py = p * TileHeight;

            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 0; y < TileHeight; y++)
                {
                    if (px < x && py < y)
                    {
                        var tile = this[new(x, y)];

                        tile?.Draw(_gridTimer.ElapsedTime);
                        //stopDrawingSameStuff = x == TileWidth-1 && y == TileHeight-1;
                        //Debug.WriteLine(stopDrawingSameStuff +  "   :  " + "(" + x + "," + y + ")");
                    }
                }
            }
        }

        public TTile? this[Int2 coord]
        {
            get
            {
                if (coord.X >= 0 && coord.Y >= 0 && coord.X < TileWidth && coord.Y < TileHeight)
                {
                    var tmp = _bitmap[coord.X, coord.Y];
                    return tmp ?? throw new IndexOutOfRangeException("");
                }

                return default;
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
                    if (matches.Count == MAX_DESTROYABLE_TILES)
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

            return matches.Count == MAX_DESTROYABLE_TILES;
        }

        public bool TryGetClickedTile(out TTile? tile)
        {
            tile = default;

            if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
                return false;

            var mouseVec2 = Raylib.GetMousePosition();
            Int2 position = new Int2((int)mouseVec2.X, (int)mouseVec2.Y);
            position /= TILE_SIZE;
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