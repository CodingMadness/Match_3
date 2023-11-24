using System.Numerics;
using Match_3.DataObjects;

namespace Match_3.Setup;

public static class Bakery
{
    private static Shape DefineBody(TileColor kind)
    {
        return kind switch
        {
            TileColor.SkyBlue => new()
            {
                TileKind = TileColor.SkyBlue,
                AtlasLocation = new Vector2(1f, 3f),
            },
            TileColor.Turquoise => new()
            {
                TileKind = TileColor.Turquoise,
                AtlasLocation = new Vector2(2f, 1f),
            },
            TileColor.Blue => new()
            {
                TileKind = TileColor.Blue,
                AtlasLocation = new Vector2(3f, 2f),
            },
            TileColor.SpringGreen => new()
            {
                TileKind = TileColor.SpringGreen,
                AtlasLocation = new Vector2(0f, 3f),
            },
            TileColor.Green => new()
            {
                TileKind = TileColor.Green,
                AtlasLocation = new Vector2(3f, 1f),
            },
            TileColor.Brown => new()
            {
                TileKind = TileColor.Brown,
                AtlasLocation = new Vector2(0f, 2f),
            },
            TileColor.Orange => new()
            {
                TileKind = TileColor.Orange,
                AtlasLocation = new Vector2(1f, 2f),
            },
            TileColor.Yellow => new()
            {
                TileKind = TileColor.Yellow,
                AtlasLocation = new Vector2(1f, 1f),
            },
            TileColor.MediumVioletRed => new()
            {
                TileKind = TileColor.MediumVioletRed,
                AtlasLocation = new Vector2(1f, 0f),
            },
            TileColor.BlueViolet => new()
            {
                TileKind = TileColor.BlueViolet,
                AtlasLocation = new Vector2(3f, 0f),
            },
            TileColor.Magenta => new()
            {
                TileKind = TileColor.Magenta,
                AtlasLocation = new Vector2(3f, 3f),
            },
            TileColor.Red => new()
            {
                TileKind = TileColor.Red,
                AtlasLocation = new Vector2(2f, 0f),
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static Tile CreateTile(Vector2 gridPos, TileColor kind)
    {
        var tile = new Tile(DefineBody(kind))
        {
            Cell = gridPos,
            CellB4Swap = -Vector2.One,
            State = TileState.UnChanged,
        };
        return tile;
    }
}