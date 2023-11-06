global using TileColor = System.Drawing.KnownColor;
using System.Drawing;
using System.Numerics;
using DotNext.Runtime;
using Match_3.Service;
using Match_3.Workflow;

[assembly: FastEnumToString(typeof(TileColor), IsPublic = true, ExtensionMethodNamespace = "Match_3.Variables.Extensions")]
namespace Match_3.DataObjects;

[Flags]
public enum TileState
{
    Disabled=1, NotRendered=4, Selected=8, UnChanged=16, Pulsate=32
}

public class Shape
{
    public virtual Vector2 AtlasLocation { get; init; }
    public Size Size { get; init; }
    public RectangleF TextureRect => new(AtlasLocation.X, AtlasLocation.Y, Size.Width, Size.Height);
    
    public ScaleableFloat ScaleFactor;

    private FadeableColor _color; 
    public ref readonly  FadeableColor Color => ref _color;
    public ref readonly FadeableColor Fade(Color c, float targetAlpha, float elapsedTime)
    {
        _color = c;
        _color.CurrentAlpha = 1f;
        _color.AlphaSpeed = 0.5f;
        _color.TargetAlpha = targetAlpha;
        _color.AddTime(elapsedTime);
        _color = _color.Apply();
        return ref _color;
    }
    public ref readonly FadeableColor Fade(Color c, float elapsedTime) => ref Fade(c, 0f, elapsedTime);
    public ref readonly FadeableColor ToConstColor(Color c) => ref Fade(c, 1f, 1f);
    public ref readonly FadeableColor FixedWhite => ref ToConstColor(WHITE.AsSysColor());
}

public class TileShape : Shape, IEquatable<TileShape>, ICloneable
{
    public TileColor TileKind { get; init; }
    public override Vector2 AtlasLocation { get; init; }
   
    public bool Equals(TileShape? other) =>
        other is not null && TileKind == other.TileKind;
    
    public override int GetHashCode() => HashCode.Combine(FixedWhite, TileKind);

    public override string ToString() => $"Tile type: <{TileKind}>";  

    public object Clone()
    {
        TileShape clone = new()
        {
            TileKind = TileKind,
            AtlasLocation = AtlasLocation,
        };
        return clone;
    }

    public override bool Equals(object? obj) => obj is TileShape shape && Equals(shape);

    public static bool operator ==(TileShape left, TileShape? right) => left.Equals(right);

    public static bool operator !=(TileShape left, TileShape right) => !(left == right);
}

public class Tile(TileShape body)
{
    private TileState _current;
   
    public TileState State
    {
        get => _current;

        set
        {
            if ((value & TileState.UnChanged) == TileState.UnChanged)
            {
                _current = TileState.UnChanged;
                Body.ToConstColor(WHITE.AsSysColor());
            }
            
            _current = value;
        }
    }
    
    public Vector2 GridCell { get; set; }
  
    public Vector2 CoordsB4Swap { get; set; }
   
    public TileShape Body { get; } = body;
    
    public Vector2 WorldCell => GridCell * Utils.Size;
    
    public Vector2 End => WorldCell + Vector2.One * Utils.Size;
    
    public bool IsDeleted => State.HasFlag(TileState.Disabled) && 
                             State.HasFlag(TileState.NotRendered);
    private RectangleF GridBox => new(GridCell.X, GridCell.Y, 1f, 1f);
   
    public RectangleF MapBox => GridBox.RelativeToMap();
    
    public override string ToString() => $"Cell: {GridCell}; ---- {Body}";
    
    public void Disable(bool shallDelete=false)
    {
        Body.Fade(BLACK.AsSysColor(), 0f, 1f);
        State = !shallDelete ? TileState.Disabled : (TileState.Disabled | TileState.NotRendered);
    }
   
    public void Enable()
    {
        Body.Fade(WHITE.AsSysColor(), 0f, 1f);
        State = TileState.UnChanged;
    }
}

public class EnemyTile(TileShape body) : Tile(body)
{
    public RectangleF Pulsate(float elapsedTime)
    {
        if (elapsedTime <= 0f)
            return Body.TextureRect;

        if (Body.ScaleFactor.Speed == 0f)
            Body.ScaleFactor.Speed = 20.25f;
        
        var rect = Body.TextureRect.DoScale(Body.ScaleFactor.GetFactor());
                
        Body.ScaleFactor.ElapsedTime = elapsedTime;
        return rect with { X = WorldCell.X, Y = (int)WorldCell.Y };
    }
    
    public void BlockSurroundingTiles(bool disable)
    {
        bool goDiagonal = false;
        const Grid.Direction lastDir = (Grid.Direction)4;

        Vector2 NextCell(Grid.Direction direction)
        {
            if (!goDiagonal)
            {
                return direction switch
                {
                    /* direction inside screen:
                     *    -X => <-----
                     *    +X => ----->
                     *
                     *    -Y => UP
                     *    +Y => DOWN
                     * 
                     */
                    Grid.Direction.NegativeX => GridCell with { X = GridCell.X - 1 },
                    Grid.Direction.PositiveX => GridCell with { X = GridCell.X + 1 },
                    Grid.Direction.NegativeY => GridCell with { Y = GridCell.Y - 1 },
                    Grid.Direction.PositiveY => GridCell with { Y = GridCell.Y + 1 },
                    _ => Vector2.Zero
                };
            }

            return direction switch
            {
                Grid.Direction.NegativeX => GridCell - Vector2.One,
                Grid.Direction.PositiveX => GridCell + Vector2.One,
                Grid.Direction.NegativeY => GridCell with { X = GridCell.X + 1, Y = GridCell.Y - 1},
                Grid.Direction.PositiveY => GridCell with { X = GridCell.X - 1, Y = GridCell.Y + 1},
                _ => Vector2.Zero
            };
        }

        void RepeatLoop(ref Grid.Direction i, bool shallDoRepeat)
        {
            if (!shallDoRepeat)
                return;
                
            if (i == lastDir - 1) //&& goDiagonal == false)
            {
                goDiagonal = true; //set this back to true!
                i = 0;
            }
        }

        for (Grid.Direction i = 0; i < lastDir; i++)
        {
            RepeatLoop(ref i, false);

            Vector2 next = NextCell(i);
            var neighborTile = Grid.GetTile(next);
            
            if (Intrinsics.IsExactTypeOf<Tile>(neighborTile))
            {
                if (disable)
                    neighborTile!.Disable(false);

                else
                    neighborTile!.Enable();
            }
        }
    }
}

public class MatchX
{
    protected readonly SortedSet<Tile> Matches = new(Comparer.CellComparer.Singleton);

    private Vector2 _direction;
    private RectangleF _worldRect;
    public TimeOnly DeletedAt { get; private set; }
    public TimeOnly CreatedAt { get; private set; }
    protected bool IsRowBased { get; private set; }
    public int Count => Matches.Count;
    public bool IsMatchActive => Count >= DataOnLoad.MaxTilesPerMatch;
    public TileShape? Body { get; private set; }
    public RectangleF WorldBox => _worldRect;
    public Vector2 WorldPos { get; private set; }

    public Tile this[int index] => Matches.ElementAt(index);
    public Tile this[Index index] => Matches.ElementAt(index);
    /// <summary>
    /// investigate this function cause this shall be the one at how i will iterate thru the tiles!
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public Vector2? Move(int i = 0)
    {
        if (i < 0 || i > Count-1 || _worldRect.IsEmpty)
            return null;

        var pos = WorldPos / Utils.Size;

        return IsRowBased 
            ? pos with { X = pos.X + (i * _direction).X }
            : pos with { Y = pos.Y + (i * _direction).Y };
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
            _worldRect.Add(matchTile.MapBox);
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
    private RectangleF _border;
    
    private RectangleF BuildBorder()
    {
        if (Matches.Count == 0)
            return new(0,0,0,0);
            
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
   
    public RectangleF Border
    {
        get
        {
            if (_border.IsEmpty)
            {
                _border = BuildBorder();
                return _border;
            }

            return _border;
        }
    }
}

