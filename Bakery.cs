using System.Numerics;

namespace Match_3;

public static class Bakery
{
    private static Type GetTileTypeTypeByNoise(float noise)
    {
        noise = noise.Trunc(2);

        if (noise is <= 0f or >= 1.0f)
        {
            noise = Utils.Randomizer.NextSingle();
            return GetTileTypeTypeByNoise(noise);
        }

        if (noise <= 0.1)
            noise *= 10;

        (Type, float) ballToNoise = (Type.Empty, -1f);

        var result = noise switch
        {
            > 0.0f  and <= 0.15f => ballToNoise = (Type.Brown, noise),
            > 0.15f  and <= 0.25f => ballToNoise = (Type.Red, noise),
            > 0.25f and <= 0.35f => ballToNoise = (Type.Orange, noise),
            > 0.35f and <= 0.45f => ballToNoise = (Type.Blue, noise),
            > 0.45f and <= 0.55f => ballToNoise = (Type.Green, noise),
            > 0.55f and <= 0.65f => ballToNoise = (Type.Purple, noise),
            > 0.65f and <= 0.75f => ballToNoise = (Type.Violet, noise),
            > 0.75f and <= 1f => ballToNoise = (Type.Yellow, noise),
            _ => ballToNoise = (Type.Empty, noise),
        };
        return result.Item1;
    }

    private static TileShape DefineFrame(float noise)
    {
        TileShape tmp = new()
        {
            Ball = GetTileTypeTypeByNoise(noise)
        };
        
        return tmp.Ball switch
        {
            Type.Green => new()
            {
                Ball = Type.Green,
                FrameLocation = new Vector2(0f, 0f) * ITile.Size, 
                Form = ShapeKind.Circle, 
                Layer = Coat.A
            },
            Type.Purple => new()
            {
                Ball = Type.Purple,
                FrameLocation = new Vector2(1f, 0f) * ITile.Size,
                Form = ShapeKind.Circle,
                Layer = Coat.B
            },
            Type.Orange => new()
            {
                Ball = Type.Orange,
                FrameLocation = new Vector2(2f, 0f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.C
            },
            Type.Yellow => new()
            {
                Ball = Type.Yellow,
                FrameLocation = new Vector2(3f, 0f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.D
            },
            Type.Red => new()
            {
                Ball = Type.Red,
                FrameLocation = new Vector2(0f, 1f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.E
            },
            Type.Blue => new()
            {
                Ball = Type.Blue,
                FrameLocation = new Vector2(1f, 1f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.F
            },
            Type.Brown => new()
            {
                Ball = Type.Brown,
                FrameLocation = new Vector2(2f, 1f) * ITile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.G
            },
            Type.Violet => new()
            {
                Ball = Type.Violet,
                FrameLocation = new Vector2(3f, 1f) * ITile.Size, 
                Form = ShapeKind.Circle, 
                Layer = Coat.H
            },
         
            //DEFAULTS.......
            Type.Length => new() { FrameLocation = -Vector2.One },
            Type.Empty => new() { FrameLocation = -Vector2.One },
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static ITile CreateTile(Vector2 gridPos, float noise)
    {
        var tile = new Tile
        {
            Cell = gridPos, 
            CoordsB4Swap = -Vector2.One,
            Body = DefineFrame(noise),
            State = State.Clean,
            Options = Options.Movable | Options.Shapeable |  Options.Destroyable
        };
        return tile;
    }

    public static EnemyTile Transform(Tile matchTile)
    {
        EnemyTile blockTile = new()
        {
            Cell = matchTile.Cell,
            CoordsB4Swap = matchTile.CoordsB4Swap,
            Body = new TileShape
            {
                Form = ShapeKind.Trapez,
                FrameLocation = matchTile.Body.FrameLocation,
                Ball = matchTile.Body is TileShape c0 ? c0.Ball : Type.Empty,
                Layer = matchTile.Body is TileShape c1 ? c1.Layer : (Coat)(-1)
            },
            State = State.Clean,
            Options = Options.UnMovable | Options.UnShapeable,
        };

        blockTile.Body.Color.AlphaSpeed = 0f;
        return blockTile;
    }
}
