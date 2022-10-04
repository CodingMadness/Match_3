using System.Numerics;
using Raylib_CsLo;

namespace Match_3;

public class MatchX
{
    protected readonly ISet<Tile> _matches;
    private IOrderedEnumerable<Tile>? _orderedSet;
    private Vector2 _first = -Vector2.One;
    private bool _wasRow;
    private Vector2 _placeHere;
    public readonly int AllowedMatchCount;
    private Rectangle _worldRect;
    
    public bool IsRowBased => _matches.IsRowBased();
    public int Count => _matches.Count;

    public bool IsMatch => Count == AllowedMatchCount;
    
    public TileShape? Match3Body { get; private set; }
    public Rectangle WorldBox
    {
        get
        {
            if (!_worldRect.IsEmpty())
                return _worldRect;

            _orderedSet ??= _matches.Order(CellComparer.Singleton);
            
            foreach (var tile in _orderedSet)
            {
                _worldRect.Add(tile.WorldBounds);
            }
            return _worldRect;
        }
    }
    public Vector2 Begin
    {
        get
        {
            if (_worldRect.IsEmpty())
                return Utils.INVALID_CELL;

            return _worldRect.GetBegin();
        }
    }
    public MatchX(int allowedMatchCount)
    {
        _worldRect = default;
        AllowedMatchCount = allowedMatchCount;
        _matches = new HashSet<Tile>(allowedMatchCount);
    }

    /// <summary>
    /// Reorder the Match if it has a structure like: (x0,x1,y2) or similar
    /// </summary>
    public void Add(Tile matchTile, Grid grid)
    {
        //this entire algorithm only works for a Match3.. rework at some later stag
        void ForceMatchAxisAligned()
        {
            var current = matchTile.GridCell;
            _first = _first == -Vector2.One ? current : _first;

            if (_first != current && _placeHere == default)
            {
                _placeHere = _first.GetOpposite(current);
            }

            if (Count == AllowedMatchCount) 
            {
                if (_first.CompletelyDifferent(current) || _first != (current))
                {
                    //DOES NOT WORK YET!......
                    grid[_placeHere] = grid[current];
                    grid[current] = Bakery.CreateTile(current, 0.45f);
                    //grid.Swap(grid[_placeHere], grid[current]);
                }
            }
        }
      
        if (_matches.Add(matchTile))
        {
            Match3Body ??= (matchTile.Body as TileShape)!.Clone() as TileShape;
        }
    }
    public void Empty()
    {
        _worldRect = Utils.INVALID_RECT;
        _wasRow = false;
        _first = -Vector2.One;
        _placeHere = _first;
        _matches.Clear();
    }
    public EnemyMatches AsEnemies(Grid map)
    {
        EnemyMatches list = new(Count);

        _orderedSet ??= _matches.Order(CellComparer.Singleton);
        
        foreach (var match in _orderedSet)
        {
            map[match.GridCell] = Bakery.AsEnemy(match);
            EnemyTile e = (EnemyTile)map[match.GridCell]!;
            e.BlockSurroundingTiles(map, true);
            list.Add(e, map);
        }
        return list;
    }
}

public class EnemyMatches : MatchX
{
    private Rectangle _border;
    public EnemyMatches(int allowedMatchCount) : base(allowedMatchCount)
    {
        //WorldBox = default;
    }
    private Rectangle BuildBorder()
    {
        if (_matches.Count == 0)
            throw new MethodAccessException($"This method is accessed even tho {WorldBox} seems to be empty");

        Vector2 begin;
        int match3RectWidth;
        int match3RectHeight;
        
        if (IsRowBased)
        {
            //its row based rectangle
            //-----------------|
            // X     Y      Z  |
            //-----------------|
            var firstSlot = Begin / Tile.Size;
            begin = firstSlot - Vector2.One;
            match3RectWidth = _matches.Count + 2;
            match3RectHeight = _matches.Count;
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
            var firstSlot = Begin / Tile.Size;
            begin = firstSlot - Vector2.One;
            match3RectWidth = _matches.Count;
            match3RectHeight = _matches.Count+2;
        }
        return Utils.NewWorldRect(begin, match3RectWidth, match3RectHeight);
    }
    public Rectangle Border
    {
        get
        {
            if (_border.x == 0 && _border.y == 0)
                _border = BuildBorder();

            return _border;
        }
    }
}