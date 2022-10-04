using System.Numerics;
using Raylib_CsLo;

namespace Match_3;

public class MatchX
{
    protected readonly ISet<Tile> _matches;
    private Vector2 _first = -Vector2.One;
    private bool _wasRow;
    private Vector2 _placeHere;
    private readonly int _matchCount;

    public bool IsRowBased => _matches.IsRowBased();
    public int Count => _matches.Count;
    public TileShape? Match3Body { get; private set; }
    public Rectangle MapRect { get; private set; }
    public Vector2 Begin
    {
        get
        {
            if (MapRect.IsEmpty())
                return Vector2.Zero;

            //var tmp = MapRect with { x = MapRect.width / Count, y = MapRect.width / Count };
            //the comment above is the actual real code i will use! but its buggy for now!
            var tmp = _matches.ElementAt(0).MapCell / Tile.Size;
            return tmp;
        }
    }
    public MatchX(int matchCount)
    {
        _matchCount = matchCount;
        _matches = new HashSet<Tile>(matchCount);
    }

    /// <summary>
    /// Reorder the Match if it has a structure like: (x0,x1,y2) or similar
    /// </summary>
    public void AsRow()
    {
        
    }
    
    public void Add(Tile matchTile, Grid grid)
    {
        void ForceMatchAxisAligned()
        {
            var current = matchTile.GridCell;
            //Console.WriteLine(matchTile.GridCell);
            _first = _first == -Vector2.One ? current : _first;
            
            
            if (Count == _matchCount)
            {
                if (_first.CompletelyDifferent(current) || _first != (current))
                {
                    _placeHere = _first.GetOpposite(current);
                    var tmpTile = grid[current];
                    grid[_placeHere] = tmpTile;
                }
            }
        }
        
        if (_matches.Add(matchTile))
        {
            //Console.WriteLine("Add() --> " + matchTile.GridCell);
            ForceMatchAxisAligned();
            MapRect = MapRect.Add(matchTile.Bounds);
            Match3Body ??= (matchTile.Body as TileShape)!.Clone() as TileShape;
        }
    }
    public void Empty()
    {
        _wasRow = false;
        _first = -Vector2.One;
        _placeHere = _first;
        MapRect = default;
        _matches.Clear();
    }
    public EnemyMatches AsEnemies(Grid map)
    {
        EnemyMatches list = new(Count);

        foreach (var match in _matches.Order(CellComparer.Singleton))
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
    public EnemyMatches(int matchCount) : base(matchCount)
    {
        //MapRect = default;
    }
    private Rectangle BuildBorder()
    {
        if (_matches.Count == 0)
            throw new MethodAccessException($"This method is accessed even tho {MapRect} seems to be empty");

        Vector2 begin;
        int match3RectWidth;
        int match3RectHeight;
        
        if (IsRowBased)
        {
            //its row based rectangle
            //-----------------|
            // X     Y      Z  |
            //-----------------|
            var first = _matches.OrderBy(x => x.GridCell.X).ElementAt(0);
            begin = first.GridCell - Vector2.One;
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
            var first = _matches.OrderBy(x => x.GridCell.Y).ElementAt(0);
            begin = first.GridCell - Vector2.One;
            match3RectWidth = _matches.Count;
            match3RectHeight = _matches.Count+2;
        }
        return Utils.GetMatch3Rect(begin, match3RectWidth, match3RectHeight);
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