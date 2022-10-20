using System.Collections;
using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;

namespace Match_3;

public class MatchX
{
    protected readonly SortedSet<Tile> Matches;

    private Vector2 _direction;
    private Rectangle _worldRect;
    public TimeOnly DeletedAt { get; private set; }
    public TimeOnly CreatedAt { get; private set; }
    public bool IsRowBased { get; private set; }
    public int Count => Matches.Count;
    public bool IsMatchActive => Count == Level.MAX_TILES_PER_MATCH;
    public TileShape? Body { get; private set; }
    public Rectangle WorldBox => _worldRect;
    public Vector2 WorldPos { get; private set; }

    public MatchX()
    {
        Matches = new(CellComparer.Singleton);
    }

    public Tile this[int index] => Matches.ElementAt(index);
    public Tile this[Index index] => Matches.ElementAt(index);
    /// <summary>
    /// investigate this function cause this shall be the one at how i will iterate thru the tiles!
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public Vector2? Move(int i = 0)
    {
        if (i < 0 || i > Count-1 || _worldRect.IsEmpty())
            return null;

        var pos = WorldPos / Tile.Size;
        
        if (IsRowBased)
        {
            return pos with { X = pos.X + (i  *  _direction).X };
        }
        else
        {
            return pos with { Y = pos.Y + (i  *  _direction).Y };
        }
    }
    /// <summary>
    /// Reorder the Matched if it has a structure like: (x0,x1,y2) or similar
    /// </summary>
    public void Add(Tile matchTile)
    {
        if (Matches.Add(matchTile) && !IsMatchActive)
        {
            if (Count is > 1 and < 3)
            {
                //INSPECT THIS, so that I can use Move(x) instead of ElementAt(0)
                var cell0 = Matches.ElementAt(0).GridCell;
                var cell1 = Matches.ElementAt(1).GridCell;
                var dir = cell0.GetDirectionTo(cell1);
                _direction = dir.Direction;
                IsRowBased = dir.isRow;
            }
            
            Body ??= matchTile.Body.Clone() as TileShape;
            _worldRect.Add(matchTile.WorldBounds);
        }
       
        else if (IsMatchActive)
        {
            var cell0 = Matches.ElementAt(0);
            var cellLast = Matches.ElementAt(^1);
            
            if (IsRowBased)
                if (cell0.GridCell != cellLast.GridCell)
                {
                    var cellRight = cell0.GridCell - Vector2.UnitX;
                    
                }
            
            WorldPos = cell0.WorldCell;

            CreatedAt = TimeOnly.FromDateTime(DateTime.UtcNow);
        }
    }
    
    public void Clear()
    {
        _worldRect = Utils.InvalidRect;
        IsRowBased = false;
        Matches.Clear();
        Body = null;
        DeletedAt = TimeOnly.FromDateTime(DateTime.UtcNow);
    }
}

public class EnemyMatches : MatchX
{
    private Rectangle _border;
    private Rectangle BuildBorder()
    {
        if (Matches.Count == 0)
            return new(0f,0f,0f,0f);
            
        int match3RectWidth;
        int match3RectHeight;
        var firstSlot = WorldBox.GetCellPos();
        var next = firstSlot - Vector2.One;
        
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
            if (_border.IsEmpty())
            {
                _border = BuildBorder();
                return _border;
            }
            else
            {
                return _border;
            }
        }
    }
}