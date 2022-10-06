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
        
        var result = noise switch
        {
            > 0.0f  and <= 0.15f =>  (Type.Brown, noise),
            > 0.15f  and <= 0.25f => (Type.Red, noise),
            > 0.25f and <= 0.35f =>  (Type.Orange, noise),
            > 0.35f and <= 0.45f =>  (Type.Blue, noise),
            > 0.45f and <= 0.55f =>  (Type.Green, noise),
            > 0.55f and <= 0.65f =>  (Type.Purple, noise),
            > 0.65f and <= 0.75f =>  (Type.Violet, noise),
            > 0.75f and <= 1f =>     (Type.Yellow, noise),
            _ => (Type.Empty, noise),
        };
        return result.Item1;
    }

    private static TileShape DefineFrame(float noise)
    {
        TileShape tmp = new()
        {
            TileType = GetTileTypeTypeByNoise(noise)
        };
        
        return tmp.TileType switch
        {
            Type.Green => new()
            {
                TileType = Type.Green,
                AtlasLocation = new Vector2(0f, 0f) * Tile.Size, 
                Form = ShapeKind.Circle, 
                Layer = Coat.A
            },
            Type.Purple => new()
            {
                TileType = Type.Purple,
                AtlasLocation = new Vector2(1f, 0f) * Tile.Size,
                Form = ShapeKind.Circle,
                Layer = Coat.B
            },
            Type.Orange => new()
            {
                TileType = Type.Orange,
                AtlasLocation = new Vector2(2f, 0f) * Tile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.C
            },
            Type.Yellow => new()
            {
                TileType = Type.Yellow,
                AtlasLocation = new Vector2(3f, 0f) * Tile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.D
            },
            Type.Red => new()
            {
                TileType = Type.Red,
                AtlasLocation = new Vector2(0f, 1f) * Tile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.E
            },
            Type.Blue => new()
            {
                TileType = Type.Blue,
                AtlasLocation = new Vector2(1f, 1f) * Tile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.F
            },
            Type.Brown => new()
            {
                TileType = Type.Brown,
                AtlasLocation = new Vector2(2f, 1f) * Tile.Size,
                Form = ShapeKind.Circle, 
                Layer = Coat.G
            },
            Type.Violet => new()
            {
                TileType = Type.Violet,
                AtlasLocation = new Vector2(3f, 1f) * Tile.Size, 
                Form = ShapeKind.Circle, 
                Layer = Coat.H
            },
         
            //DEFAULTS.......
            Type.Length => new() { AtlasLocation = -Vector2.One },
            Type.Empty => new() { AtlasLocation = -Vector2.One },
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static Tile CreateTile(Vector2 gridPos, float noise)
    {
        var tile = new Tile
        {
            GridCell = gridPos, 
            CoordsB4Swap = -Vector2.One,
            Body = DefineFrame(noise),
            State = State.Clean,
            Options = Options.Movable | Options.Shapeable |  Options.Destroyable
        };
        return tile;
    }

    public static EnemyTile AsEnemy(Tile matchTile)
    {
        EnemyTile blockTile = new()
        {
            GridCell = matchTile.GridCell,
            CoordsB4Swap = matchTile.GridCell,
            Body = new TileShape
            {
                Form = ShapeKind.Trapez,
                AtlasLocation = matchTile.Body.AtlasLocation,
                TileType = matchTile.Body is TileShape c0 ? c0.TileType : Type.Empty,
                Layer = matchTile.Body is TileShape c1 ? c1.Layer : (Coat)(-1)
            },
            State = State.Clean,
            Options = Options.UnMovable | Options.UnShapeable,
        };

        blockTile.Body.Color.AlphaSpeed = 0f;
        return blockTile;
    }
}
