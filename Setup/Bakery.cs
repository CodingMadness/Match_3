using System.Numerics;
using Match_3.DataObjects;

namespace Match_3.Setup;

public static class Bakery
{
    private static RectShape DefineBody(TileColor kind, IGridRect gridBox)
    {
        return new RectShape(gridBox)
        {
            TextureLocation = kind switch
            {
                TileColor.LightBlue => new(1f, 3f),
                TileColor.Turquoise => new Vector2(2f, 1f),
                TileColor.Blue =>new Vector2(3f, 2f),
                TileColor.LightGreen => new Vector2(0f, 3f),
                TileColor.Green =>  new Vector2(3f, 1f),
                TileColor.Brown => new Vector2(0f, 2f),
                TileColor.Orange =>new Vector2(1f, 2f),
                TileColor.Yellow => new Vector2(1f, 1f),
                TileColor.Purple => new Vector2(3f, 0f),
                TileColor.Magenta =>  new Vector2(3f, 3f),
                TileColor.Red => new Vector2(2f, 0f),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "undefined color was passed!")
            },
            TileKind = kind,
        };
    }

    public static Tile CreateTile(CSharpRect gridBox, TileColor kind)
    {
        var tile = new Tile
        {
            Cell = gridBox.Location.ToVector2(),
            CellB4Swap = -Vector2.One,
            State = TileState.UnChanged,
            Body = DefineBody(kind, (SingleCell)gridBox.Location.ToVector2()) 
        };
        return tile;
    }
}