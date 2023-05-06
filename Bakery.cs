namespace Match_3;

public static class Bakery
{
    private static TileType GetTileTypeTypeByNoise(float noise)
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
            > 0.0f  and <= 0.15f =>  (TileType.Brown, noise),
            > 0.15f  and <= 0.25f => (TileType.Red, noise),
            > 0.25f and <= 0.35f =>  (TileType.Orange, noise),
            > 0.35f and <= 0.45f =>  (TileType.Blue, noise),
            > 0.45f and <= 0.55f =>  (TileType.Green, noise),
            > 0.55f and <= 0.65f =>  (TileType.Purple, noise),
            > 0.65f and <= 0.75f =>  (TileType.Violet, noise),
            > 0.75f and <= 1f =>     (TileType.Yellow, noise),
            _ => (TileType.Empty, noise),
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
            TileType.Green => new()
            {
                TileType = TileType.Green,
                AtlasLocation = new Vector2(0f, 0f) * Tile.Size, 
                Size = new(Tile.Size, Tile.Size),
                Form = ShapeKind.Circle, 
                Layer = Coat.A,
                Scale = 1f,
            },
            TileType.Purple => new()
            {
                TileType = TileType.Purple,
                AtlasLocation = new Vector2(1f, 0f) * Tile.Size,
                Size = new(Tile.Size, Tile.Size),
                Form = ShapeKind.Circle,
                Layer = Coat.B,
                Scale = 1f,
            },
            TileType.Orange => new()
            {
                TileType = TileType.Orange,
                AtlasLocation = new Vector2(2f, 0f) * Tile.Size,
                Size = new(Tile.Size, Tile.Size),
                Form = ShapeKind.Circle, 
                Layer = Coat.C,
                Scale = 1f,
            },
            TileType.Yellow => new()
            {
                TileType = TileType.Yellow,
                AtlasLocation = new Vector2(3f, 0f) * Tile.Size,
                Size = new(Tile.Size, Tile.Size),
                Form = ShapeKind.Circle, 
                Layer = Coat.D,
                Scale = 1f,
            },
            TileType.Red => new()
            {
                TileType = TileType.Red,
                AtlasLocation = new Vector2(0f, 1f) * Tile.Size,
                Size = new(Tile.Size, Tile.Size),
                Form = ShapeKind.Circle, 
                Layer = Coat.E,
                Scale = 1f,
            },
            TileType.Blue => new()
            {
                TileType = TileType.Blue,
                AtlasLocation = new Vector2(1f, 1f) * Tile.Size,
                Size = new(Tile.Size, Tile.Size),
                Form = ShapeKind.Circle, 
                Layer = Coat.F,
                Scale = 1f,
            },
            TileType.Brown => new()
            {
                TileType = TileType.Brown,
                AtlasLocation = new Vector2(2f, 1f) * Tile.Size,
                Size = new(Tile.Size, Tile.Size),
                Form = ShapeKind.Circle, 
                Layer = Coat.G,
                Scale = 1f,
            },
            TileType.Violet => new()
            {
                TileType = TileType.Violet,
                AtlasLocation = new Vector2(3f, 1f) * Tile.Size, 
                Size = new(Tile.Size, Tile.Size),
                Form = ShapeKind.Circle, 
                Layer = Coat.H,
                Scale = 1f,
            },
            
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
            TileState = TileState.Clean,
            Options = Options.Movable | Options.Shapeable |  Options.Destroyable
        };
        return tile;
    }

    private static EnemyTile AsEnemy(Tile matchTile)
    {
        EnemyTile blockTile = new()
        {
            GridCell = matchTile.GridCell,
            CoordsB4Swap = matchTile.GridCell,
            Body = new TileShape
            {
                Scale = new(0.7f, 1.15f)
                {
                    ElapsedTime = 0f, Speed = 0.2f
                },
                Form = ShapeKind.Trapez,
                AtlasLocation = matchTile.Body.AtlasLocation,
                Size = new(Tile.Size, Tile.Size),
                TileType = matchTile.Body is { } c0 ? c0.TileType : TileType.Empty,
                Layer = matchTile.Body is { } c1 ? c1.Layer : (Coat)(-1)
            },
            TileState = TileState.Clean,
            Options = Options.UnMovable | Options.UnShapeable,
        };
        return blockTile;
    }
    
    public static event Grid.GridAction OnEnemyTileCreated;

    public static EnemyMatches AsEnemies(Grid map, MatchX match)
    {
        EnemyMatches list = new();
         
        for (int i = 0; i <  match.Count; i++)
        {
            //var gridCell = match.Move(i) ?? throw new ArgumentOutOfRangeException("bla bla");
            var gridCell = match[(i)].GridCell;
            //--!--
            map[gridCell] = AsEnemy(map[gridCell]!);
            EnemyTile e = (EnemyTile)map[gridCell]!;
            GameState.Current = e;
            OnEnemyTileCreated(Span<byte>.Empty);
            //e.BlockSurroundingTiles(map, true);
            list.Add(e);
        }
        match.Clear(); 
        //now match has become an enemy match and we dont need the other
        //one anymore
        return list;
    }
}
