using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;

namespace Match_3;

public class MatchX
{
    protected readonly ISet<Tile> Matches;
    private IOrderedEnumerable<Tile>? _orderedSet;
    private bool _wasRow;
    private Rectangle _worldRect;
    public TimeOnly DeletedAt { get; private set; }
    public TimeOnly CreatedAt { get; private set; }
    public bool IsRowBased => _wasRow;
    public int Count => Matches.Count;
    public bool IsMatchActive => Count == Level.MAX_TILES_PER_MATCH;
    public TileShape? Body { get; private set; }
    public Rectangle WorldBox
    {
        get
        {
            if (!_worldRect.IsEmpty())
                return _worldRect;

            if (Matches.Count == 0)
                return new(0f, 0f, 0f, 0f);
            
            _orderedSet ??= Matches.Order(CellComparer.Singleton);
            
            foreach (var tile in _orderedSet)
            {
                _worldRect.Add(tile.WorldBounds);
            }
            return _worldRect;
        }
    }

    public Vector2 WorldPos { get; private set; } = -Vector2.One;

    public MatchX()
    {
        _worldRect = default;
        Matches = new HashSet<Tile>(Level.MAX_TILES_PER_MATCH);
    }

    /// <summary>
    /// Reorder the Match if it has a structure like: (x0,x1,y2) or similar
    /// </summary>
    public void Add(Tile matchTile)
    {
        if (Matches.Add(matchTile) && !IsMatchActive)
        {
            WorldPos = WorldPos != -Vector2.One ? matchTile.WorldCell : WorldPos;
            
            if (Count is > 1 and < 3)
            {
                var cell0 = Matches.ElementAt(0).GridCell;
                var cell1 = Matches.ElementAt(1).GridCell;
                _wasRow = cell0.GetDirectionTo(cell1).isRow;
            }
            
            Body ??= (matchTile.Body as TileShape)!.Clone() as TileShape;
            Body!.Color = Raylib.GREEN;
            Body.Color.AlphaSpeed = 0.5f;
        }
        else if (!IsMatchActive)
            CreatedAt = TimeOnly.FromDateTime(DateTime.UtcNow);
    }
    public void Clear()
    {
        _worldRect = Utils.INVALID_RECT;
        _wasRow = false;
        WorldPos = -Vector2.One;
        Matches.Clear();
        DeletedAt = TimeOnly.FromDateTime(DateTime.UtcNow);
        _orderedSet = null;
    }
    public EnemyMatches AsEnemies(Grid map)
    {
        EnemyMatches list = new();

        _orderedSet ??= Matches.Order(CellComparer.Singleton);
        
        foreach (var match in _orderedSet)
        {
            map[match.GridCell] = Bakery.AsEnemy(match);
            EnemyTile e = (EnemyTile)map[match.GridCell]!;
            e.BlockSurroundingTiles(map, true);
           // e.Pulsate();
            list.Add(e);
        }
        return list;
    }
}

public class EnemyMatches : MatchX
{
    private Rectangle _border;
    private Rectangle BuildBorder()
    {
        if (Matches.Count == 0)
            throw new MethodAccessException($"This method is accessed even tho {WorldBox} seems to be empty");

        Vector2 next;
        int match3RectWidth;
        int match3RectHeight;
        var firstSlot = WorldBox.GetBeginInGrid();
        next = firstSlot - Vector2.One;
        if (IsRowBased)
        {
            //its row based rectangle
            //-----------------|
            // X     Y      Z  |
            //-----------------|
            match3RectWidth = Count + 2;
            match3RectHeight = Count;
        }
        else
        {
            //its column based rectangle
            //-*--*--*--|
            // *  X  *  |
            // *  Y  *  |
            // *  Z  *  |
            // *  *  *  |
            //----------|
            match3RectWidth = Count;
            match3RectHeight = Count+2;
        }
        return Utils.NewWorldRect(next, match3RectWidth, match3RectHeight);
    }
    public Rectangle Border
    {
        get
        {
            if (_border.x == 0 && _border.y == 0)
            {
                _border = BuildBorder();
            }

            return _border;
        }
    }
}