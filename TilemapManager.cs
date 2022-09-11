//using DotNext;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Raylib_cs;

namespace Match_3
{
    public abstract class Grid
    {
        public const int FLOORTILE_WIDTH = 64;
        public const int FLOORTILE_HEIGHT = 64;

        public double Timer { get; private set; } = 10;
        public readonly int _tileCountInX;
        public readonly int _tileCountInY;
        protected readonly Tile[,] _bitmap;
        protected readonly Texture2D _image;

        private bool stopDrawingSameStuff = false;

        protected Grid(int tileCountInX, int tileCountInY, Texture2D img)
        {
            _tileCountInX = tileCountInX;
            _tileCountInY = tileCountInY;
            _bitmap = new Tile[_tileCountInX, _tileCountInY];
            _image = img;
        }

        protected abstract void internalDraw(SpriteBatch sb, Tile current, SpriteFont font);

        public abstract void CreateMap();

        public void Draw(SpriteBatch sb, GameTime deltaTime, SpriteFont font)
        {
            if (stopDrawingSameStuff)
                return;
            //–
            Timer -= deltaTime.ElapsedGameTime.TotalSeconds;
            // Debug.WriteLine("Timer: " + Timer);
            const int timeMax = 10;//11 seconds for all tiles to appear

            var p = (Timer / timeMax);
            var px = p * _tileCountInX;
            var py = p * _tileCountInY;

            for (int x = 0; x < _tileCountInX; x++)
            {
                for (int y = 0; y < _tileCountInY; y++)
                {
                    //if (px < y && py < y)
                    {
                        var tile = _bitmap[x, y];

                        if (tile != null)
                        {
                            internalDraw(sb, tile, font);
                            //Debug.WriteLine("I DRAWED SMTH!");
                        }
                        //stopDrawingSameStuff = x == _tileCountInX-1 && y == _tileCountInY-1;
                        //Debug.WriteLine(stopDrawingSameStuff +  "   :  " + "(" + x + "," + y + ")");
                    }
                }
            }

        }

        public Tile this[int r, int c]
        {
            get
            {
                if (r >= 0 && c >= 0 && r < _tileCountInX && c < _tileCountInY)
                {
                    var tmp = _bitmap[r, c];
                    return tmp ?? throw new IndexOutOfRangeException("");
                }
                return null;
            }
            set
            {
                if (r >= 0 && c >= 0 && r < _tileCountInX && c < _tileCountInY)
                {
                    _bitmap[r, c] = value;
                }
            }
        }
    }

    public class BorderMap : Grid
    {
        public BorderMap(int width, int height, Texture2D single) : base(width, height, single)
        {

        }

        public override void CreateMap()
        {
            //Here x fill the entire LEFT-ROW and RIGHT-ROW
            int start = 0;
            //_bitmap[spalte=0, zeile=y]
            for (int x = 0; x < _tileCountInY; x++)
            {
                _bitmap[start, x] = Tile.CreateTile(0, start, x, BORDERTILE_WIDTH, BORDERTILE_HEIGHT, _image, Tile.TileKind.Border);

                if (x == _tileCountInY - 1)
                {
                    x = -1;
                    start = _tileCountInX - 1;

                    if (_bitmap[start, x + 1] != null)
                        break;
                }
            }

            start = 0;
            //_bitmap[spalte=0, zeile=y]
            for (int x = 0; x < _tileCountInX; x++)
            {
                _bitmap[x, start] = Tile.CreateTile(0, x, start, BORDERTILE_WIDTH, BORDERTILE_HEIGHT, _image, Tile.TileKind.Border);

                if (x == _tileCountInX - 1)
                {
                    x = -1;
                    start = _tileCountInY - 1;

                    if (_bitmap[x + 2, start] != null)
                        break;
                }
            }
        }

        protected override void internalDraw(SpriteBatch sb, Tile current, SpriteFont font)
        {
            sb.Draw(current.TileImg, current.DrawableRect, Color.White);
            //sb.DrawString(font, $"{current.Cell.X} {current.Cell.Y}", current.Cell, Color.Black);
        }
    }

    public class FloorMap : Grid
    {
        public FloorMap(int width, int height, Texture2D single) : base(width, height, single)
        {

        }

        public override void CreateMap()
        {
            for (int x = 1; x < (_tileCountInX); x++)
            {
                for (int y = 1; y < (_tileCountInX); y++)
                {
                    _bitmap[x, y] = Tile.CreateTile(1, x, y, FLOORTILE_WIDTH, FLOORTILE_HEIGHT, _image, Tile.TileKind.Floor);
                }
            }
        }

        protected override void internalDraw(SpriteBatch sb, Tile current, SpriteFont font)
        {
            sb.Draw(current.TileImg, current.Cell, Color.White);
            sb.DrawString(font, $"{current.Cell.X} {current.Cell.Y}", current.Cell, Color.Red);
        }
    }

    public class PickupsMap : Grid
    {
        private readonly FloorMap pickupGround;
        private readonly WeightedCellPool cellPool;

        private IEnumerable<Vector2> YieldGameWindow()
        {
            for (int x = 1; x < pickupGround._tileCountInX - 0; x++)
            {
                for (int y = 1; y < pickupGround._tileCountInY - 0; y++)
                {
                    Vector2 current = new(x * PICKUPTILE_WIDTH * 2, y * PICKUPTILE_HEIGHT * 2);
                    //if (x % 2 == 0)
                    yield return current;
                }
            }
        }

        public PickupsMap(in FloorMap groundForPickups, Texture2D sheet)
            : base(groundForPickups._tileCountInX, groundForPickups._tileCountInY, sheet)
        {
            pickupGround = groundForPickups;
            //cellPool = new(YieldGameWindow());
        }

        public override void CreateMap()
        {
            for (int x = 1; x < (_tileCountInX) - 1; x++)
            {
                Vector2 tmp = new(2 * FLOORTILE_WIDTH, 2 * FLOORTILE_HEIGHT);
                _bitmap[1, 1] = Tile.CreateRndTileFromSheet(2, tmp/*pickupGround[1, 1].Cell*/, PICKUPTILE_WIDTH,
                                                                PICKUPTILE_HEIGHT,
                                                                _image, Tile.TileKind.Pickup);
                //for (int y = 1; y < (_tileCountInX); y++)
                //{
                //    _bitmap[x, y] = Tile.CreateRndTileFromSheet(pickupGround[x,y].Cell, PICKUPTILE_WIDTH, 
                //                                                PICKUPTILE_HEIGHT, 
                //                                                _image, Tile.TileKind.Pickup);
                //}
            }
        }

        protected override void internalDraw(SpriteBatch sb, Tile current, SpriteFont font)
        {
            if (current.Cell != default)
                sb.Draw(current.TileImg, current.Cell, current.DrawableRect, Color.White);
            //WORKS! //sb.DrawString(font, $"{current.Cell.X} {current.Cell.Y}", current.Cell, Color.Red);
        }      
    }

    //public sealed class CollisionPool : ICollidable
    //{
    //    readonly ICollidable[] _maps;

    //    public CollisionPool(params ICollidable[] maps)
    //    {
    //        _maps = maps;
    //    }

    //    public bool CheckIfPlayerCollidesWithMe(in Rectangle srcToCheck, Direction dir)
    //    {
    //        for (int i = 0; i < _maps.Length; i++)
    //        {
    //            if (_maps[i].CheckIfPlayerCollidesWithMe(srcToCheck, dir))
    //            {
    //                return true;
    //            }
    //        }
    //        return false;
    //    }
    //}
}
