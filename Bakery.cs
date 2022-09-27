using System.Numerics;

namespace Match_3;

public static class Bakery
{
    private static Balls GetTileTypeTypeByNoise(float noise)
    {
        noise = noise.Trunc(2);

        if (noise is <= 0f or >= 1.0f)
        {
            noise = Utils.Randomizer.NextSingle();
            return GetTileTypeTypeByNoise(noise);
        }

        if (noise <= 0.1)
            noise *= 10;

        (Balls, float) ballToNoise = (Balls.Empty, -1f);

        var result = noise switch
        {
            > 0.0f  and <= 0.15f => ballToNoise = (Balls.Brown, noise),
            > 0.15f  and <= 0.25f => ballToNoise = (Balls.Red, noise),
            > 0.25f and <= 0.35f => ballToNoise = (Balls.Orange, noise),
            > 0.35f and <= 0.45f => ballToNoise = (Balls.Blue, noise),
            > 0.45f and <= 0.55f => ballToNoise = (Balls.Green, noise),
            > 0.55f and <= 0.65f => ballToNoise = (Balls.Purple, noise),
            > 0.65f and <= 0.75f => ballToNoise = (Balls.Violet, noise),
            > 0.75f and <= 1f => ballToNoise = (Balls.Yellow, noise),
            _ => ballToNoise = (Balls.Empty, noise),
        };
        return result.Item1;
    }

    private static CandyShape DefineFrame(float noise)
    {
        CandyShape tmp = new()
        {
            Ball = GetTileTypeTypeByNoise(noise)
        };
        
        return tmp.Ball switch
        {
            Balls.Green => new()
            {
                Ball = Balls.Green,
                FrameLocation = new Vector2(0f, 0f) * ITile.Size, 
                Form = ShapeKind.Circle, 
                Layer = Coat.A
            },
            Balls.Purple => new()
            {
                Ball = Balls.Purple,
                FrameLocation = new Vector2(1f, 0f) * ITile.Size,
                Form = ShapeKind.Circle,
                Layer = Coat.B
            },
            Balls.Orange => new()
            {
                Ball = Balls.Orange,
                FrameLocation = new Vector2(2f, 0f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.C
            },
            Balls.Yellow => new()
            {
                Ball = Balls.Yellow,
                FrameLocation = new Vector2(3f, 0f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.D
            },
            Balls.Red => new()
            {
                Ball = Balls.Red,
                FrameLocation = new Vector2(0f, 1f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.E
            },
            Balls.Blue => new()
            {
                Ball = Balls.Blue,
                FrameLocation = new Vector2(1f, 1f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.F
            },
            Balls.Brown => new()
            {
                Ball = Balls.Brown,
                FrameLocation = new Vector2(2f, 1f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.G
            },
            Balls.Violet => new()
            {
                Ball = Balls.Violet,
                FrameLocation = new Vector2(3f, 1f) * ITile.Size, 
                Form = ShapeKind.Circle, 
                Layer = Coat.H
            },
         
            //DEFAULTS.......
            Balls.Length => new() { FrameLocation = -Vector2.One },
            Balls.Empty => new() { FrameLocation = -Vector2.One },
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static ITile CreateTile(Vector2 gridPos, float noise)
    {
        var tile = new Tile
        {
            CurrentCoords = gridPos, 
            CoordsB4Swap = -Vector2.One,
            Shape = DefineFrame(noise),
            Selected = false
        };
        return tile;
    }

    public static MatchBlockTile Transform(Tile other, Grid map)
    {
        MatchBlockTile blockTile = new()
        {
            CurrentCoords = other.CurrentCoords,
            CoordsB4Swap = other.CoordsB4Swap,
            IsDeleted = false,
            State = other.State,
            
            Shape = new CandyShape
            {
                Form = ShapeKind.Trapez,
                FrameLocation = other.Shape.FrameLocation,
                Ball = other.Shape is CandyShape c0 ? c0.Ball : Balls.Empty,
                Layer = other.Shape is CandyShape c1 ? c1.Layer : (Coat)(-1)
            },
            Selected = false,
        };

        blockTile.Shape.Current().AlphaSpeed = 0f;
        return blockTile;
    }
}
