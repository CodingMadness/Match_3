using System.Globalization;
using System.Numerics;
using Raylib_CsLo;

namespace Match_3;

public class MatchX
{
    protected readonly ISet<Tile> Matches;
    private IOrderedEnumerable<Tile>? _orderedSet;
    private Vector2 _first = -Vector2.One;
    private bool _wasRow;
    private Vector2 _placeHere;
    public readonly int AllowedMatchCount;
    private Rectangle _worldRect;
    
    public bool IsRowBased => _wasRow;
    public int Count => Matches.Count;
    public bool IsMatch => Count == AllowedMatchCount;
    public TileShape? Body { get; private set; }
    public Rectangle WorldBox
    {
        get
        {
            if (!_worldRect.IsEmpty())
                return _worldRect;

            if (Matches.Count == 0)
                throw new ArgumentException("empty matchlist, add tiles to it b4 u call a member!");
            
            _orderedSet ??= Matches.Order(CellComparer.Singleton);
            
            foreach (var tile in _orderedSet)
            {
                _worldRect.Add(tile.WorldBounds);
            }
            return _worldRect;
        }
    }
    public Vector2 BeginInWorld
    {
        get
        {
            if (!_worldRect.IsEmpty())
            {
                return _worldRect.GetBeginInWorld();
            }
            if (_orderedSet is null || Matches.Count == 0)
            {
                return Utils.INVALID_CELL;
            }
            else
            {
                var result = _orderedSet ??= Matches.Order(CellComparer.Singleton);
                return result.ElementAt(0).WorldCell;
            }
        }
    }
    public MatchX(int allowedMatchCount)
    {
        _worldRect = default;
        AllowedMatchCount = allowedMatchCount;
        Matches = new HashSet<Tile>(allowedMatchCount);
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
      
        if (Matches.Add(matchTile))
        {
            if (Count is > 1 and < 3)
            {
                var cell0 = Matches.ElementAt(0).GridCell;
                var cell1 = Matches.ElementAt(1).GridCell;
                _wasRow = cell0.GetDirectionTo(cell1).isRow;
            }
            Body ??= (matchTile.Body as TileShape)!.Clone() as TileShape;
            Body.Color = Raylib.GREEN;
            Body.Color.AlphaSpeed = 0.5f;
        }
    }
    public void Clear()
    {
        _worldRect = Utils.INVALID_RECT;
        _wasRow = false;
        _first = -Vector2.One;
        _placeHere = _first;
        Matches.Clear();
        _orderedSet = null;
    }
    public EnemyMatches AsEnemies(Grid map)
    {
        EnemyMatches list = new(Count);

        _orderedSet ??= Matches.Order(CellComparer.Singleton);
        
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