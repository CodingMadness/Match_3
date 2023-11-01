using System.Numerics;
using Match_3.Service;
using Match_3.StateHolder;
using Match_3.Workflow;

namespace Match_3.Setup;

public static class Bakery
{
    private static readonly EnemyMatches Enemies = new();
    
    private static TileColor GetTileTypeTypeByNoise(float noise)
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
            > 0.0f and <= 0.15f => (TileColor.Brown, noise),
            > 0.15f and <= 0.25f => (TileColor.Red, noise),
            > 0.25f and <= 0.35f => (TileColor.Orange, noise),
            > 0.35f and <= 0.45f => (TileColor.Blue, noise),
            > 0.45f and <= 0.55f => (TileColor.Green, noise),
            > 0.55f and <= 0.65f => (TileColor.Purple, noise),
            > 0.65f and <= 0.75f => (TileColor.Violet, noise),
            > 0.75f and <= 1f => (TileColor.Yellow, noise),
        };
        return result.Item1;
    }

    private static TileShape DefineFrame(float noise)
    {
        TileShape tmp = new()
        {
            TileColor = GetTileTypeTypeByNoise(noise)
        };

        return tmp.TileColor switch
        {
            TileColor.Green => new()
            {
                TileColor = TileColor.Green,
                AtlasLocation = new Vector2(0f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableFloat = 1f,
            },
            TileColor.Purple => new()
            {
                TileColor = TileColor.Purple,
                AtlasLocation = new Vector2(1f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableFloat = 1f,
            },
            TileColor.Orange => new()
            {
                TileColor = TileColor.Orange,
                AtlasLocation = new Vector2(2f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableFloat = 1f,
            },
            TileColor.Yellow => new()
            {
                TileColor = TileColor.Yellow,
                AtlasLocation = new Vector2(3f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableFloat = 1f,
            },
            TileColor.Red => new()
            {
                TileColor = TileColor.Red,
                AtlasLocation = new Vector2(0f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableFloat = 1f,
            },
            TileColor.Blue => new()
            {
                TileColor = TileColor.Blue,
                AtlasLocation = new Vector2(1f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableFloat = 1f,
            },
            TileColor.Brown => new()
            {
                TileColor = TileColor.Brown,
                AtlasLocation = new Vector2(2f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableFloat = 1f,
            },
            TileColor.Violet => new()
            {
                TileColor = TileColor.Violet,
                AtlasLocation = new Vector2(3f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableFloat = 1f,
            },

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static Tile CreateTile(Vector2 gridPos, float noise)
    {
        var tile = new Tile(DefineFrame(noise))
        {
            GridCell = gridPos,
            CoordsB4Swap = -Vector2.One,
            TileState = TileState.Clean,
            Options = Options.Movable | Options.Shapeable | Options.Destroyable
        };
        return tile;
    }

    private static EnemyTile AsEnemy(Tile matchTile)
    {
        var body = new TileShape
        {
            ScaleableFloat = new(0.7f, 1.15f)
            {
                ElapsedTime = 0f, Speed = 0.2f
            },
            AtlasLocation = matchTile.Body.AtlasLocation,
            Size = new(Utils.Size, Utils.Size),
            TileColor = matchTile.Body is { } c0 ? c0.TileColor : TileColor.Transparent,
        };
            
        EnemyTile blockTile = new(body)
        {
            GridCell = matchTile.GridCell,
            CoordsB4Swap = matchTile.GridCell,
            TileState = TileState.Clean,
            Options = Options.UnMovable | Options.UnShapeable,
        };
        return blockTile;
    }
    
    public static EnemyMatches AsEnemies(MatchX match)
    {
        var currData = GameState.CurrData!;
        
        for (int i = 0; i < match.Count; i++)
        {
            //var gridCell = match.Move(i) ?? throw new ArgumentOutOfRangeException("bla bla");
            var gridCell = match[i].GridCell;
            var tile = Grid.GetTile(gridCell)!;
            var enemyTile = AsEnemy(tile);
            Grid.SetTile(enemyTile);
            currData.Matches = Enemies;
            //OnEnemyTileCreated(Span<byte>.Empty);
            //e.BlockSurroundingTiles(map, true);
            Enemies.Add(enemyTile);
        }

        match.Clear();
        //now match has become an enemy match and we dont need the other
        //one anymore
        return Enemies;
    }
}