using System.Diagnostics.CodeAnalysis;
using Raylib_cs;

namespace Match_3;

public enum Direction
{
    PositiveX = 0,
    NegativeX = 1,
    
    NegativeY = 2,
    PositiveY = 3,
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

    public void Swap(Tile a, Tile b)
    {
        this[a.CurrentCoords] = b;
        this[b.CurrentCoords] = a;
        (a.CurrentCoords, b.CurrentCoords) = (b.CurrentCoords, a.CurrentCoords);
        (a.PreviewCoords, b.PreviewCoords) = (b.CurrentCoords, a.CurrentCoords);
    }

    public void Delete(IntVector2 coord)
    {
        
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
        private set => _tiles[coord.X, coord.Y] = value;
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

    public bool TryGetClickedTile([MaybeNullWhen(false)] out Tile tile)
    {
        tile = null;
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) 
            return false;
        
        var mouseVec2 = Raylib.GetMousePosition();

        IntVector2 position = new IntVector2((int) mouseVec2.X, (int) mouseVec2.Y);
        position /= Program.TileSize;
        return TryGetTile(position, out tile);

    }

    private bool TryGetTile(IntVector2 position, [MaybeNullWhen(false)] out Tile tile)
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