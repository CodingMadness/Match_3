using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Raylib_cs;

namespace Match_3;

public enum Direction
{
    PositiveX = 0,
    NegativeX = 1,
    PositiveY = 2,
    NegativeY = 3,
}

public class TileMap
{
    private readonly Tile?[,] _tiles;
    public int Width { get; }
    public int Height { get; }  
    
    public TileMap(int width, int height)
    {
        _tiles = new Tile[width, height];
        Width = width;
        Height = height;
        Fill();
    }

    public void Swap(Tile? a, Tile? b)
    {
        if (a is null || b is null)
            return;
        
        this[a.CurrentCoords] = b;
        this[b.CurrentCoords] = a;
        (a.CurrentCoords, b.CurrentCoords) = (b.CurrentCoords, a.CurrentCoords);
        (a.CoordsB4Swap, b.CoordsB4Swap) = (b.CurrentCoords, a.CurrentCoords);
    }
 
    public Tile? this[IntVector2 coord]
    {
        get
        {
            if (TryGetTile(coord, out var tile))
                return tile;
            {
                /*
                if (coord.X >= Width)
                    coord.X = Width - 1;
                else if (coord.Y >= Height)
                    coord.Y = Height - 1;

                return this[coord];
                */
                return null;
            }
        }
        set => _tiles[coord.X, coord.Y] = value;
    }
    
    private void Fill()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var tile = Tile.GetRandomTile();
                tile.CurrentCoords = new IntVector2(x, y);
                _tiles[x, y] = tile;
            }
        }
    }

    public void Draw(float deltaTime)
    {
        for (int x = 0; x < Width; x += 1)
        {
            for (int y = 0; y < Height; y += 1)
            {
                var currentTile = _tiles[x, y];
                currentTile?.Draw(deltaTime);
            }
        }
    }

    public bool Match3InAnyDirection(IntVector2 clickedCoord, ref HashSet<Tile> rowOf3)
    {
        static bool AddWhenEqual(Tile? first, Tile? next, Direction direction, HashSet<Tile> rowOf3)
        {
            if (first is not null &&
                next is not null &&
                first.Equals(next))
            {
                rowOf3.Add(first);
                rowOf3.Add(next);
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
        
        for (Direction i = 0; (i < lastDir) ; i++)
        {
            if (rowOf3.Count == 3)
                return true;
            
            IntVector2 nextCoords = GetStepsFromDirection(clickedCoord, i);
            Tile? next = this[nextCoords];
            
            while (AddWhenEqual(first, next, i, rowOf3) && rowOf3.Count < 3)
            {
                //compute the proper (x,y) for next round!
                nextCoords = GetStepsFromDirection(nextCoords, i);
                next = this[nextCoords];
            }
        }
/*
        var only3 = rowOf3.Take(3);
        rowOf3 = only3.ToHashSet();*/
        return rowOf3.Count >= 3;
    }
    
    public bool TryGetClickedTile([MaybeNullWhen(false)] out Tile? tile)
    {
        tile = null;
        
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) 
            return false;
        
        var mouseVec2 = Raylib.GetMousePosition();

        IntVector2 position = new IntVector2((int) mouseVec2.X, (int) mouseVec2.Y);
        position /= Program.TileSize;
        return TryGetTile(position, out tile);
    }

    private bool TryGetTile(IntVector2 position, [MaybeNullWhen(false)] out Tile? tile)
    {
        if (position.X < 0 || position.X >= Width
                           || position.Y < 0 || position.Y >= Height)
        {
            tile = null;
            return false;
        }

        tile = _tiles[position.X, position.Y];
        return tile is not null;
    }
}