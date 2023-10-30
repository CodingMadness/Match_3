global using TileColor = System.Drawing.KnownColor;
using System.Drawing;
using System.Numerics;
using DotNext.Runtime;
using Match_3.Service;
using Match_3.Workflow;


[assembly: FastEnumToString(typeof(TileColor), IsPublic = true, ExtensionMethodNamespace = "Match_3.Variables.Extensions")]
namespace Match_3.StateHolder;

[Flags]
public enum Options
{
    UnDestroyable = 0,
    UnMovable = 1,
    UnShapeable = 2,
    
    Destroyable = 4,
    Movable = 8,
    Shapeable = 16
}

[Flags]
public enum TileState
{
    Disabled=1, Deleted=2, Hidden=4, Selected=8, Clean=16, Pulsate=32
}

public class Shape
{
    public virtual Vector2 AtlasLocation { get; init; }
    public Size Size { get; init; }
    public RectangleF TextureRect => new(AtlasLocation.X, AtlasLocation.Y, Size.Width, Size.Height);
    
    public ScaleableFloat ScaleableFloat;

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
    public TileColor TileColor { get; init; }
    public override Vector2 AtlasLocation { get; init; }
   
    public bool Equals(TileShape? other) =>
        other is not null && TileColor == other.TileColor;
    
    public override int GetHashCode() => HashCode.Combine(FixedWhite, TileColor);

    public override string ToString() => $"Tile type: <{TileColor}> with Tint: <{FixedWhite}>";  

    public object Clone()
    {
        TileShape clone = new()
        {
            TileColor = TileColor,
            AtlasLocation = AtlasLocation,
        };
        return clone;
    }

    public override bool Equals(object? obj) => obj is TileShape shape && Equals(shape);

    public static bool operator ==(TileShape left, TileShape? right) => left.Equals(right);

    public static bool operator !=(TileShape left, TileShape right) => !(left == right);
}

public class Tile(TileShape body) : IEquatable<Tile>
{
    private TileState _current;
    private Quest _quest;
    public EventState EventData = new();
    
    public ref readonly Quest Quest => ref _quest;
    public virtual Options Options { get; set; }
    public TileState TileState
    {
        get => _current;

        set
        {
            if ((value & TileState.Clean) == TileState.Clean)
            {
                _current &= TileState.Selected;
                _current &= TileState.Disabled;
                _current &= TileState.Deleted;
                _current &= TileState.Hidden;
                Body.ToConstColor(WHITE.AsSysColor());
            }

            if ((value & TileState.Pulsate) == TileState.Pulsate)
            {
                _current &= TileState.Selected;
                _current &= TileState.Disabled;
                _current &= TileState.Deleted;
                _current &= TileState.Hidden;
            }

            if ((value & TileState.Selected) == TileState.Selected)
            {
                //if a tile is selected it must also be clean/alive
                _current |= TileState.Clean;
            }
            else if ((value & TileState.Hidden) == TileState.Hidden)
            {
                _current &= TileState.Clean; //remove clean flag from set
                //_current |= TileState.Selected; //remove clean flag from set
                //add disabled flag to set cause when smth is deleted it must be automatically disabled 
                _current &= TileState.Disabled; //operations on that tile with this flag are still possible!
                _current &= TileState.Deleted;
            }
            else if ((value & TileState.Deleted) == TileState.Deleted)
            {
                //remove all flags
                _current &= TileState.Clean;
                _current &= TileState.Selected; //remove clean flag from set
                _current &= TileState.Disabled;
                Body.ToConstColor(WHITE.AsSysColor());
            }
            else if ((value & TileState.Disabled) == TileState.Disabled)
            {
                _current &= TileState.Clean; //remove clean flag from set
                _current &= TileState.Selected; //remove clean flag from set
                _current &= TileState
                    .Deleted; //deleted is reserved as Disabled AND Hidden, so u cannot be both at same time
                Body.ToConstColor(BLACK.AsSysColor());
            }

            _current = value;
        }
    }
    public Vector2 GridCell { get; set; }
    public Vector2 CoordsB4Swap { get; set; }
    public TileShape Body { get; } = body;
    public Vector2 WorldCell => GridCell * Utils.Size;
    public Vector2 End => WorldCell + Vector2.One * Utils.Size;
    public bool IsDeleted => TileState.HasFlag(TileState.Deleted);
    private RectangleF GridBox => new(GridCell.X, GridCell.Y, 1f, 1f);
    public RectangleF MapBox => GridBox.RelativeToMap();

    public void UpdateGoal(EventType eventType, in Quest aQuest)
    {
        _quest = eventType switch
        {
            EventType.Swapped => _quest with { Swap = aQuest.Swap },
            EventType.Matched => _quest with { Match = aQuest.Match },
            //EventType.RePainted => _goal with { RePaint = aGoal.RePaint },
            //EventType.Destroyed => _goal with { Destroyed = aGoal.Destroyed },
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
        };
    }
    
    public override string ToString() => $"Cell: {GridCell}; ---- {Body}";
    
    public void Disable(bool shallDelete)
    {
        Body.Fade(BLACK.AsSysColor(), 0f, 1f);
        Options = Options.UnMovable | Options.UnShapeable;
        TileState = !shallDelete ? TileState.Disabled : TileState.Deleted;
    }
   
    public void Enable()
    {
        Body.Fade(WHITE.AsSysColor(), 0f, 1f);
        Options = Options.Movable | Options.Shapeable;
        TileState = TileState.Clean;
    }

    public bool Equals(Tile? other) => StateAndBodyComparer.Singleton.Equals(other, this);

    public override bool Equals(object? obj) => Equals(obj as Tile);

    public override int GetHashCode() => Body.GetHashCode();
}

public class EnemyTile(TileShape body) : Tile(body)
{
    public override Options Options => Options.UnMovable;
    
    public RectangleF Pulsate(float elapsedTime)
    {
        if (elapsedTime <= 0f)
            return Body.TextureRect;

        if (Body.ScaleableFloat.Speed == 0f)
            Body.ScaleableFloat.Speed = 20.25f;
        
        var rect = Body.TextureRect.DoScale(Body.ScaleableFloat.GetFactor());
                
        Body.ScaleableFloat.ElapsedTime = elapsedTime;
        return rect with { X = WorldCell.X, Y = (int)WorldCell.Y };
    }
    
    public void BlockSurroundingTiles(Grid map, bool disable)
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

            if (Intrinsics.IsExactTypeOf<Tile>(map[next]))
            {
                var t = map[next];

                if (disable)
                    t!.Disable(false);

                else
                    t!.Enable();
            }
        }
    }
}

public class MatchX
{
    protected readonly SortedSet<Tile> Matches = new(CellComparer.Singleton);

    private Vector2 _direction;
    private RectangleF _worldRect;
    public TimeOnly DeletedAt { get; private set; }
    public TimeOnly CreatedAt { get; private set; }
    protected bool IsRowBased { get; private set; }
    public int Count => Matches.Count;
    public bool IsMatchActive => Count == Level.MaxTilesPerMatch;
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

public sealed class StateAndBodyComparer : EqualityComparer<Tile>
{
    public override bool Equals(Tile? x, Tile? y)
    {
        if (x is not { }) return false;
        if (y is not { }) return false;
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        if ((x.TileState & TileState.Deleted) == TileState.Deleted ||
            (x.TileState & TileState.Disabled) == TileState.Disabled) return false;
        
        return x.Body.Equals(y.Body);
    }
    public override int GetHashCode(Tile obj)
    {
        return HashCode.Combine((int)obj.TileState, obj.Body);
    }
    public static StateAndBodyComparer Singleton => new();
}

public sealed class CellComparer : EqualityComparer<Tile>, IComparer<Tile>
{
    public override bool Equals(Tile? x, Tile? y)
    {
        return Compare(x, y) == 0;
    }
    public override int GetHashCode(Tile obj)
    {
        return obj.GridCell.GetHashCode();
    }
    public int Compare(Tile? a, Tile? b)
    {
        if (a is null) return -1;
        if (a == b) return 0;
        if (b is null) return 1;
        return a.GridCell.CompareTo(b.GridCell);
    }
    public static CellComparer Singleton => new();
}
