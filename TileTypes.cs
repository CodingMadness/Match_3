using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3;
 
public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    public float CurrentAlpha, TargetAlpha;
    public float ElapsedTime;
    /// <summary>
    /// The greater this Value, the faster it fades!
    /// </summary>
    public float AlphaSpeed;

    private FadeableColor(Color color)
    {
        _toWrap = color;
        AlphaSpeed = 0.5f; 
        CurrentAlpha = 1.0f;
        TargetAlpha = 0.0f;
        ElapsedTime = 1f;
    }
    
    private static readonly Dictionary<Color, string> Strings = new()
    {
        {BLACK, "Black"},
        {BLUE, "Blue"},
        {BROWN, "Brown"},
        {DARKGRAY, "DarkGray"},
        {GOLD, "Gold"},
        {GRAY, "Gray"},
        {GREEN, "Green"},
        {LIGHTGRAY, "LightGray"},
        {MAGENTA, "Magenta"},
        {MAROON, "Maroon"},
        {ORANGE, "Orange"},
        {PINK, "Pink"},
        {PURPLE, "Purple"},
        {RAYWHITE, "RayWhite"},
        {RED, "Red"},
        {SKYBLUE, "SkyBlue"},
        {VIOLET, "Violet"},
        {WHITE, "White"},
        {YELLOW, "Yellow"}
    };
    
    private void _Lerp()
    {
        
        //if u wana maybe stop fading at 0.5f so we explicitly check if currALpha > targetalpha
        if (CurrentAlpha > TargetAlpha)  
            CurrentAlpha -= AlphaSpeed * (1f / ElapsedTime);

       // CurrentAlpha = CurrentAlpha < 0f ? 0f : CurrentAlpha;
    }

    public string? ToReadableString()
    {
        Color compare = _toWrap;
        compare.a = byte.MaxValue;
        return Strings.TryGetValue(compare, out var value) ? value : _toWrap.ToString();
    }

    public FadeableColor Apply()
    {
        _Lerp();
        return this with { _toWrap = ColorAlpha(_toWrap, CurrentAlpha) };
    }

    public static implicit operator FadeableColor(Color color)
    {
        return new FadeableColor(color);
    }
    
    public static implicit operator Color(FadeableColor color)
    {
        return color._toWrap;
    }

    public static bool operator ==(FadeableColor c1, FadeableColor c2)
    {
        int bytes4C1 = Unsafe.As<Color, int>(ref c1._toWrap);
        int bytes4C2 = Unsafe.As<Color, int>(ref c2._toWrap);
        return bytes4C1 == bytes4C2;
    }

    public bool Equals(FadeableColor other)
    {
        return this == other;
    }

    public override bool Equals(object? obj)
    {
        return obj is FadeableColor other && this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_toWrap, CurrentAlpha);
    }

    public static bool operator !=(FadeableColor c1, FadeableColor c2) => !(c1 == c2);

    public override string ToString() => ToReadableString()!;
}

/// <summary>
/// A hardcoded type which is created from a look into the AngryBallsTexture!
/// </summary>
public enum Type
{
    Red, Blue, Green, Purple, Orange, Yellow, Brown, Violet,
    Length = Violet + 1, Empty = -1,
}

public enum ShapeKind
{
    Circle,
    Quader,
    Rectangle,
    Heart,
    Trapez
}

public enum Coat
{
    A, B, C, D, E, F, G, H
}

public abstract class Shape
{
    public virtual ShapeKind Form { get; set; }
    public virtual Vector2 AtlasLocation { get; init; }
    public Rectangle Rect => new(AtlasLocation.X, AtlasLocation.Y, Tile.Size, Tile.Size);
    
    private FadeableColor _f;
    public ref FadeableColor Color => ref _f;
    public void ChangeColor(Color c, float alphaSpeed, float targetAlpha)
    {
        _f = c;
        _f.AlphaSpeed = alphaSpeed;
        _f.TargetAlpha = targetAlpha;
    }
}

public class TileShape : Shape, IEquatable<TileShape>, ICloneable
{
    public TileShape()
    {
       ChangeColor(WHITE,0f, 1f);
    }
    public Type Ball { get; init; }
    public Coat Layer { get; init; }
    public override ShapeKind Form { get; set; }
    public override Vector2 AtlasLocation { get; init; }
    public bool Equals(TileShape? other) =>
        other is not null && Ball == other.Ball && Layer == other.Layer;
    public override int GetHashCode()
    {
        return HashCode.Combine(Color, Ball);
    }

    public override string ToString() =>
        $"Tile type: <{Ball}> with Tint: <{Color}>"; //and Opacitylevel: {FadeTint.CurrentAlpha}";

    public object Clone()
    {
        TileShape clone = new()
        {
            Ball = Ball,
            Color = Color ,
            Form = Form,
            Layer = Layer,
            AtlasLocation = AtlasLocation,
        };
        return clone;
    }

    public override bool Equals(object? obj)
    {
        return obj is TileShape shape && Equals(shape);
    }

    public static bool operator ==(TileShape left, TileShape? right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TileShape left, TileShape right)
    {
        return !(left == right);
    }
}

 

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
public enum State
{
    Disabled=1, Deleted=2, Hidden=4, Selected=8, Clean=16
}

public class Tile
{
    private State _current;
    
    public virtual Options Options { get; set; }
    
    public State State 
    {
        get => _current;

        set
        {
            if ((value & State.Clean) == State.Clean)
            {
                _current &= State.Selected;
                _current &= State.Disabled;
                _current &= State.Deleted;
                _current &= State.Hidden;

                Body.Color = WHITE;
                Body.Color.CurrentAlpha = 1f;
                Body.Color.TargetAlpha = 1f;
                Body.Color.AlphaSpeed = 0f;
            }
            if ((value & State.Selected) == State.Selected)
            {
                Body.Color.CurrentAlpha = 1f;
                Body.Color.AlphaSpeed = 0.75f;
                Body.Color.TargetAlpha = 0.35f;
                
                //if a tile is selected it must also be clean/alive
                _current |= State.Clean;
            }
            else if ((value & State.Hidden) == State.Hidden)
            {
                _current &= State.Clean;    //remove clean flag from set
                //_current |= State.Selected; //remove clean flag from set
                //add disabled flag to set cause when smth is deleted it must be automatically disabled 
                _current &= State.Disabled; //operations on that tile with this flag are still possible!
                _current &= State.Deleted;
                //Body.Color.AlphaSpeed = 0f;
            }
            else if ((value & State.Deleted) == State.Deleted)
            {
                //remove all flags
                _current &= State.Clean; 
                _current &= State.Selected; //remove clean flag from set
                _current &= State.Disabled;
                Body.Color = WHITE;
                Body.Color.CurrentAlpha = 0f;
                //Body.Color.AlphaSpeed = 0.75f;
                //Body.Color.TargetAlpha = 1f;
            }
            else if ((value & State.Disabled) == State.Disabled)
            {
                _current &= State.Clean; //remove clean flag from set
                _current &= State.Selected; //remove clean flag from set
                _current &= State.Deleted; //deleted is reserved as Disabled AND Hidden, so u cannot be both at same time
                
                Body.Color = BLACK;
                Body.Color.CurrentAlpha = 1f;
                Body.Color.AlphaSpeed = 0.0f;
                //Body.Color.TargetAlpha = 1f;
            }
            _current = value;
        }
    }
    public Vector2 GridCell { get; set; }
    public Vector2 CoordsB4Swap { get; set; }
    public Shape Body { get; init; }
    /// <summary>
    /// WorldCell in WorldCoordinates
    /// </summary>
    public Vector2 WorldCell => (GridCell * Size);
    /// <summary>
    /// End in WorldCoordinates
    /// </summary>
    public Vector2 End => WorldCell + (Vector2.One * Size); 
    public bool IsDeleted => (State & State.Deleted) == State.Deleted;
    public Rectangle GridBounds => new(GridCell.X, GridCell.Y, 1f, 1f);
    public Rectangle WorldBounds => GridBounds.ToWorldBox();

    public const int Size = 64;
    public static bool IsOnlyDefaultTile(Tile? current) =>
        current is not null and not EnemyTile;
    public Rectangle DestRect => new(Body.AtlasLocation.X, Body.AtlasLocation.Y, Size, Size);
    public Tile()
    {
        //we just init the variable with a dummy value to have the error gone, since we will 
        //overwrite the Body anyway with the Factorymethod "CreateNewTile(..)";
        Body = null!;
    }
    public override string ToString() => $"GridCell: {GridCell}; ---- {Body}";
    public void Disable(bool shallDelete)
    {
        //Body.ChangeColor(BLACK, 0f, 1f);
        Options = Options.UnMovable | Options.UnShapeable;
        State = !shallDelete ? State.Disabled : State.Deleted;
    }
    public void Enable()
    {
        //draw from whatever was the 1. sprite-atlas 
        //Body.ChangeColor(WHITE, 0f, 1f);
        Options = Options.Movable | Options.Shapeable;
        State = State.Clean;
    }
}

public class EnemyTile : Tile
{
    public override Options Options => Options.UnMovable;
    
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
            else
            {
                return direction switch
                {
                    Grid.Direction.NegativeX => GridCell - Vector2.One,
                    Grid.Direction.PositiveX => GridCell + Vector2.One,
                    Grid.Direction.NegativeY => GridCell with { X = GridCell.X + 1, Y = GridCell.Y - 1},
                    Grid.Direction.PositiveY => GridCell with { X = GridCell.X - 1, Y = GridCell.Y + 1},
                    _ => Vector2.Zero
                };
            }
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
        
        //if (map[GridCell] is not null)
        {
            for (Grid.Direction i = 0; i < lastDir; i++)
            {
                RepeatLoop(ref i, false);
                
                Vector2 next = NextCell(i);

                if (IsOnlyDefaultTile(map[next]))
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
}