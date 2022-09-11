//using DotNext;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Raylib_cs;

namespace Match_3
{
    public struct GameTime
    {
        public float ElapsedTime { get; set; }

        public void StartTimer(float lifetime)
        { 
            ElapsedTime = lifetime;
        }
        
        //update a timer with the current frame time
        public void UpdateTimer()
        {
            // subtract this frame from the timer if it's not allready expired
            if (ElapsedTime > 0)
                ElapsedTime -= Raylib.GetFrameTime();
        }

        // check if a timer is done.
        public bool TimerDone()
        {
            return ElapsedTime <= 0;
        }
    }
    
    public abstract class Grid
    { 
        enum Direction
        {
            PositiveX = 0,
            NegativeX = 1,
            PositiveY = 2,
            NegativeY = 3,
        }

        public const int TILE_SIZE = 64;
        private const int MAX_DESTROYABLE_TILES = 3;
        
        //public double Timer { get; private set; } = 10;
        public readonly int _tileCountInX;
        public readonly int _tileCountInY;
        protected readonly Tile?[,] _bitmap;
        protected readonly Texture2D _image;
        public static Tile MatchXTrigger { get; private set; }

        private bool stopDrawingSameStuff = false;
        
        protected  GameTime _gridTimer { get; private set; }

        protected Grid(int tileCountInX, int tileCountInY, Texture2D sheet)
        {
            _tileCountInX = tileCountInX;
            _tileCountInY = tileCountInY;
            _bitmap = new Tile[_tileCountInX, _tileCountInY];
            _image = sheet;
            _gridTimer.StartTimer(10f);
        }

        public abstract void CreateMap();

        public void Draw()
        {
            if (stopDrawingSameStuff)
                return;
            
            //Timer -= deltaTime.ElapsedGameTime.TotalSeconds;
            _gridTimer.UpdateTimer();
            
            const int timeMax = 10;//<timeMax> seconds for all tiles to appear

            var p = (_gridTimer.ElapsedTime / timeMax);
            var px = p * _tileCountInX;
            var py = p * _tileCountInY;

            for (int x = 0; x < _tileCountInX; x++)
            {
                for (int y = 0; y < _tileCountInY; y++)
                {
                    if (px < y && py < y)
                    {
                        var tile = this[new(x,y)];

                        tile?.Draw(_gridTimer.ElapsedTime);
                        //stopDrawingSameStuff = x == _tileCountInX-1 && y == _tileCountInY-1;
                        //Debug.WriteLine(stopDrawingSameStuff +  "   :  " + "(" + x + "," + y + ")");
                    }
                }
            }
        }

        public Tile? this[IntVector2 coord]
        {
            get
            {
                if (coord.X >= 0 && coord.Y >= 0 && coord.X  < _tileCountInX && coord.Y < _tileCountInY)
                {
                    var tmp = _bitmap[coord.X , coord.Y];
                    return tmp ?? throw new IndexOutOfRangeException("");
                }
                return null;
            }
            set
            {
                if (coord.X >= 0 && coord.Y >= 0 && coord.X  < _tileCountInX && coord.Y < _tileCountInY)
                {
                    _bitmap[coord.X, coord.Y] = value;
                }
            }
        }

        public bool MatchInAnyDirection(IntVector2 clickedCoord, ref HashSet<Tile> matches)
        {
            static bool AddWhenEqual(Tile? first, Tile? next, HashSet<Tile> matches)
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
        
            static IntVector2 GetStepsFromDirection(IntVector2 input, Direction direction)
            {
                var tmp = direction switch
                {
                    Direction.NegativeX => new IntVector2(input.X - 1, input.Y),
                    Direction.PositiveX => new IntVector2(input.X + 1, input.Y),
                    Direction.NegativeY => new IntVector2(input.X, input.Y - 1),
                    Direction.PositiveY => new IntVector2(input.X, input.Y + 1),
                    _ => IntVector2.Zero
                };

                return tmp;
            }

            const Direction lastDir = (Direction)4;

            Tile? first = this[clickedCoord];
            MatchXTrigger = this[clickedCoord];
            
            for (Direction i = 0; (i < lastDir) ; i++)
            {
                /*
                if (matches.Count == MAX_DESTROYABLE_TILES)
                    return true;
                */
                IntVector2 nextCoords = GetStepsFromDirection(clickedCoord, i);
                Tile? next = this[nextCoords];
                
                while (AddWhenEqual(first, next, matches) /*&& matches.Count < MAX_DESTROYABLE_TILES*/)
                {
                    //compute the proper (x,y) for next round!
                    nextCoords = GetStepsFromDirection(nextCoords, i);
                    next = this[nextCoords];
                }
            }
      
            return matches.Count == MAX_DESTROYABLE_TILES;
        }
        
        public bool TryGetClickedTile([MaybeNullWhen(false)] out Tile? tile)
    {
        tile = null;
        
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) 
            return false;
        
        var mouseVec2 = Raylib.GetMousePosition();
        IntVector2 position = new IntVector2((int) mouseVec2.X, (int) mouseVec2.Y);
        position /= Program.TileSize;
        return this[position] is not null;
    }
    
        public void Swap(Tile? a, Tile? b)
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
        private readonly WeightedCellPool cellPool;

        private IEnumerable<IntVector2> YieldGameWindow()
        {
            for (int x = 1; x < _tileCountInX - 0; x++)
            {
                for (int y = 1; y < _tileCountInY - 0; y++)
                {
                    IntVector2 current = new(x * TILE_SIZE * 1, y * TILE_SIZE * 1);
                    yield return current;
                }
            }
        }

        public Tilemap(Texture2D sheet, int tilesInX, int tilesInY)
            : base(tilesInX, tilesInY, sheet)
        {
            cellPool = new(YieldGameWindow());
        }
        
        public override void CreateMap()
        {
            for (int x = 0; x < _tileCountInX; x++)
            {
                for (int y = 0; y < _tileCountInY; y++)
                {
                    var tile = Tile.GetRandomTile(cellPool);
                    _bitmap[x, y] = tile;
                }
            }
        }
    }

}
