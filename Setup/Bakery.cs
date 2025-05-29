using System.Drawing;
using System.Numerics;
using Match_3.DataObjects;

namespace Match_3.Setup;

public static class Bakery
{
    private static RectShape DefineBody(KnownColor kind, IGridRect gridBox)
    {
        return new RectShape(gridBox)
        {
            TextureLocation = kind switch
            {
                KnownColor.LightBlue => new(1f, 3f),
                KnownColor.Turquoise => new Vector2(2f, 1f),
                KnownColor.Blue =>new Vector2(3f, 2f),
                KnownColor.LightGreen => new Vector2(0f, 3f),
                KnownColor.Green =>  new Vector2(3f, 1f),
                KnownColor.Brown => new Vector2(0f, 2f),
                KnownColor.Orange =>new Vector2(1f, 2f),
                KnownColor.Yellow => new Vector2(1f, 1f),
                KnownColor.Purple => new Vector2(3f, 0f),
                KnownColor.Magenta =>  new Vector2(3f, 3f),
                KnownColor.Red => new Vector2(2f, 0f),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "undefined color was passed!")
            },
            Colour = Color.FromKnownColor(kind)
        };
    }

    public static Tile CreateTile(CSharpRect gridBox, KnownColor kind)
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