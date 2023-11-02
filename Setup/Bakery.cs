﻿using System.Numerics;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Workflow;

namespace Match_3.Setup;

public static class Bakery
{
    private static readonly EnemyMatches Enemies = new();
    
    private static TileColor GetTileKindByNoise(float noise)
    {
        var normalizedNoise = noise.Trunc(2);
        
        var result = normalizedNoise switch
        {
            >= 0.00f and < 0.125f => (TileColor.Brown, noise),
            >= 0.125f and < 0.250f => (TileColor.Red, noise),
            >= 0.250f and < 0.375f => (TileColor.Orange, noise),
            >= 0.375f and < 0.500f => (TileColor.Blue, noise),
            >= 0.500f and < 0.625f => (TileColor.Green, noise),
            >= 0.625f and < 0.750f => (TileColor.Purple, noise),
            >= 0.750f and < 0.875f => (TileColor.Violet, noise),
            >= 0.875f and <= 1.00f => (TileColor.Yellow, noise),
        };
        return result.Item1;
    }

    private static TileShape DefineFrame(float noise)
    {
        TileShape tmp = new()
        {
            TileKind = GetTileKindByNoise(noise)
        };

        return tmp.TileKind switch
        {
            TileColor.Green => new()
            {
                TileKind = TileColor.Green,
                AtlasLocation = new Vector2(0f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableSize = 1f,
            },
            TileColor.Purple => new()
            {
                TileKind = TileColor.Purple,
                AtlasLocation = new Vector2(1f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableSize = 1f,
            },
            TileColor.Orange => new()
            {
                TileKind = TileColor.Orange,
                AtlasLocation = new Vector2(2f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableSize = 1f,
            },
            TileColor.Yellow => new()
            {
                TileKind = TileColor.Yellow,
                AtlasLocation = new Vector2(3f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableSize = 1f,
            },
            TileColor.Red => new()
            {
                TileKind = TileColor.Red,
                AtlasLocation = new Vector2(0f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableSize = 1f,
            },
            TileColor.Blue => new()
            {
                TileKind = TileColor.Blue,
                AtlasLocation = new Vector2(1f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableSize = 1f,
            },
            TileColor.Brown => new()
            {
                TileKind = TileColor.Brown,
                AtlasLocation = new Vector2(2f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableSize = 1f,
            },
            TileColor.Violet => new()
            {
                TileKind = TileColor.Violet,
                AtlasLocation = new Vector2(3f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleableSize = 1f,
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
            ScaleableSize = new(0.7f, 1.15f)
            {
                ElapsedTime = 0f, Speed = 0.2f
            },
            AtlasLocation = matchTile.Body.AtlasLocation,
            Size = new(Utils.Size, Utils.Size),
            TileKind = matchTile.Body is { } c0 ? c0.TileKind : TileColor.Transparent,
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