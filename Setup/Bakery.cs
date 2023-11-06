using System.Numerics;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Workflow;

namespace Match_3.Setup;

public static class Bakery
{
    private static readonly EnemyMatches Enemies = new();

    private static TileShape DefineFrame(TileColor kind)
    {
        TileShape tmp = new()
        {
            TileKind = kind
        };

        return tmp.TileKind switch
        {
            TileColor.SkyBlue => new()
            {
                TileKind = TileColor.SkyBlue,
                AtlasLocation = new Vector2(1f, 3f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.Turquoise => new()
            {
                TileKind = TileColor.Turquoise,
                AtlasLocation = new Vector2(2f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.Blue => new()
            {
                TileKind = TileColor.Blue,
                AtlasLocation = new Vector2(3f, 2f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.SpringGreen => new()
            {
                TileKind = TileColor.SpringGreen,
                AtlasLocation = new Vector2(0f, 3f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.Green => new()
            {
                TileKind = TileColor.Green,
                AtlasLocation = new Vector2(3f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.Brown => new()
            {
                TileKind = TileColor.Brown,
                AtlasLocation = new Vector2(0f, 2f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.Orange => new()
            {
                TileKind = TileColor.Orange,
                AtlasLocation = new Vector2(1f, 2f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.Yellow => new()
            {
                TileKind = TileColor.Yellow,
                AtlasLocation = new Vector2(1f, 1f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.MediumVioletRed => new()
            {
                TileKind = TileColor.MediumVioletRed,
                AtlasLocation = new Vector2(1f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.BlueViolet => new()
            {
                TileKind = TileColor.BlueViolet,
                AtlasLocation = new Vector2(3f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.Magenta => new()
            {
                TileKind = TileColor.Magenta,
                AtlasLocation = new Vector2(3f, 3f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            TileColor.Red => new()
            {
                TileKind = TileColor.Red,
                AtlasLocation = new Vector2(2f, 0f) * Utils.Size,
                Size = new(Utils.Size, Utils.Size),
                ScaleFactor = 1f,
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static Tile CreateTile(Vector2 gridPos, TileColor kind)
    {
        var tile = new Tile(DefineFrame(kind))
        {
            GridCell = gridPos,
            CoordsB4Swap = -Vector2.One,
            State = TileState.UnChanged,
        };
        return tile;
    }

    private static EnemyTile AsEnemy(Tile matchTile)
    {
        var body = new TileShape
        {
            ScaleFactor = new(0.7f, 1.15f)
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
            State = TileState.UnChanged,
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