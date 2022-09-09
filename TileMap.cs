using System.Diagnostics.CodeAnalysis;
using Raylib_cs;

namespace Match_3;

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
        SetTile(a.Coords, b);
        SetTile(b.Coords, a);
        
        (a.Coords, b.Coords) = (b.Coords, a.Coords);
    }
    
    public void SetTile(Int2 position, Tile tile)
    {
        _tiles[position.X, position.Y] = tile;
    }

    private void Fill()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var tile = Tile.GetRandomTile();
                tile.Coords = new Int2(x, y);
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

        Int2 position = new Int2((int) mouseVec2.X, (int) mouseVec2.Y);
        position /= Program.TileSize;
        return TryGetTile(position, out tile);

    }

    public bool TryGetTile(Int2 position, [MaybeNullWhen(false)] out Tile tile)
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