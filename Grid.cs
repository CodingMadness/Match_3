//using DotNext;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Raylib_cs;

namespace Match_3
{
    public abstract class Grid
    { 
        enum Direction
        {
            PositiveX = 0,
            NegativeX = 1,
            PositiveY = 2,
            NegativeY = 3,
        }

        protected readonly ITile?[,] _bitmap;
        //public double Timer { get; private set; } = 10;
        public readonly int Width;
        public readonly int Height;
        public const int TILE_SIZE = 64;
        private const int MAX_DESTROYABLE_TILES = 3;   
        private bool stopDrawingSameStuff = false;
        public static ITile MatchXTrigger { get; private set; }
        protected  GameTime _gridTimer { get; private set; }
        protected WeightedCellPool CellPool { get; }
        
        protected abstract void CreateMap();
        
        private IEnumerable<Int2> YieldGameWindow()
        {
            for (int x = 0; x < Width - 0; x++)
            {
                for (int y = 0; y < Height - 0; y++)
                {
                    Int2 current = new(x * TILE_SIZE * 1, y * TILE_SIZE * 1);
                    yield return current;
                }
            }
        }
        
        protected Grid(int width, int height, Texture2D sheet)
        {
            Width = width;
            Height = height;
            _bitmap = new ITile?[Width, Height];
            _gridTimer = new();
            CellPool = new WeightedCellPool(YieldGameWindow());
            _gridTimer.StartTimer(10f);

            CreateMap();
        }

        public void Draw()
        {
            /*
            if (stopDrawingSameStuff)
                return;
            
            //Timer -= deltaTime.ElapsedGameTime.TotalSeconds;
            */
            
            _gridTimer.UpdateTimer();
            
            const int timeMax = 10;//<timeMax> seconds for all tiles to appear

            var p = (_gridTimer.ElapsedTime / timeMax);
            var px = p * Width;
            var py = p * Height;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (px < x && py < y)
                    {
                        var tile = this[new(x,y)];

                        tile?.Draw(_gridTimer.ElapsedTime);
                        //stopDrawingSameStuff = x == Width-1 && y == Height-1;
                        //Debug.WriteLine(stopDrawingSameStuff +  "   :  " + "(" + x + "," + y + ")");
                    }
                }
            }
        }

        public ITile? this[Int2 coord]
        {
            get
            {
                if (coord.X >= 0 && coord.Y >= 0 && coord.X  < Width && coord.Y < Height)
                {
                    var tmp = _bitmap[coord.X , coord.Y];
                    return tmp ?? throw new IndexOutOfRangeException("");
                }
                return null;
            }
            set
            {
                if (coord.X >= 0 && coord.Y >= 0 && coord.X  < Width && coord.Y < Height)
                {
                    _bitmap[coord.X, coord.Y] = value;
                }
            }
        }

        public bool MatchInAnyDirection(Int2 clickedCoord, ISet<ITile> matches)
        {
            static bool AddWhenEqual(ITile? first, ITile? next, ISet<ITile> matches)
            {
                if (first is not null &&
                    next is not null &&
                    first.Equals(next))
                {
                    if (matches.Count ==  MAX_DESTROYABLE_TILES)
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

            ITile? first = this[clickedCoord];
            MatchXTrigger = this[clickedCoord];
            
            for (Direction i = 0; (i < lastDir) ; i++)
            {
                /*
                if (matches.Count == MAX_DESTROYABLE_TILES)
                    return true;
                */
                Int2 nextCoords = GetStepsFromDirection(clickedCoord, i);
                ITile? next = this[nextCoords];
                
                while (AddWhenEqual(first, next, matches) /*&& matches.Count < MAX_DESTROYABLE_TILES*/)
                {
                    //compute the proper (x,y) for next round!
                    nextCoords = GetStepsFromDirection(nextCoords, i);
                    next = this[nextCoords];
                }
            }
      
            return matches.Count == MAX_DESTROYABLE_TILES;
        }
        
        public bool TryGetClickedTile([MaybeNullWhen(false)] out ITile? tile)
        {
            tile = null;
            
            if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) 
                return false;
            
            var mouseVec2 = Raylib.GetMousePosition();
            Int2 position = new Int2((int) mouseVec2.X, (int) mouseVec2.Y);
            position /= TILE_SIZE;
            tile = this[position];
            return tile is not null;
        }
    
        public void Swap(ITile? a, ITile? b)
        {
            if (a is null || b is null)
                return;
            
            this[a.Cell] = b;
            this[b.Cell] = a;
            (a.Cell, b.Cell) = (b.Cell, a.Cell);
            (a.CoordsB4Swap, b.CoordsB4Swap) = (b.Cell, a.Cell);
        }
    }

    public class Tilemap : Grid
    {
        protected readonly Texture2D _image;
        
        public Tilemap(Texture2D sheet, int tilesInX, int tilesInY)
            : base(tilesInX, tilesInY, sheet)
        {
            //cellPool = new(YieldGameWindow());
        }
        
        protected override void CreateMap()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var tile = Tile.GetRandomTile(CellPool);
                    tile.Cell = new(x, y);
                    _bitmap[x, y] = tile;
                }
            }
        }
    }

}
