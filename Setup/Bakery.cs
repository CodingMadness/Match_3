﻿using System.Drawing;
using Match_3.DataObjects;

namespace Match_3.Setup;

public static class Bakery
{
    private static ConcreteRectangularBody DefineBody(TileColorTypes kind, IGridRect gridBox)
    {
        return new ConcreteRectangularBody(gridBox)
        {
            TextureLocation = kind switch
            {
                TileColorTypes.LightBlue => new(1f, 3f),
                TileColorTypes.Turquoise => new Vector2(2f, 1f),
                TileColorTypes.Blue =>new Vector2(3f, 2f),
                TileColorTypes.LightGreen => new Vector2(0f, 3f),
                TileColorTypes.Green =>  new Vector2(3f, 1f),
                TileColorTypes.Brown => new Vector2(0f, 2f),
                TileColorTypes.Orange =>new Vector2(1f, 2f),
                TileColorTypes.Yellow => new Vector2(1f, 1f),
                TileColorTypes.Purple => new Vector2(3f, 0f),
                TileColorTypes.Magenta =>  new Vector2(3f, 3f),
                TileColorTypes.Red => new Vector2(2f, 0f),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "undefined color was passed!")
            },
            Colour = Color.FromKnownColor(kind)
        };
    }

    public static Tile CreateTile(in SingleCell cell, TileColorTypes kind)
    {
        var tile = new Tile
        {
            Cell = cell,
            CellB4Swap = -Vector2.One,
            State = TileState.UnChanged,
            Body = DefineBody(kind,cell) 
        };
        
        return tile;
    }
}