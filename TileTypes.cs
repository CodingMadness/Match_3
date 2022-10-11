using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.GameTypes;
using static Raylib_CsLo.Raylib;
using Color = Raylib_CsLo.Color;
using Rectangle = Raylib_CsLo.Rectangle;

namespace Match_3;

public struct Scale
{
    private readonly float _minScale;
    private readonly float _maxScale;
    private float _direction;
    private float _finalScaleFactor;

    public float Speed;
    public float ElapsedTime;

    public Scale(float minScale, float maxScale)
    {
        _minScale = minScale;
        _maxScale = maxScale;
         Speed = 0f;
        _direction = -1f;
        _finalScaleFactor = minScale.Equals(maxScale, 0.1f) ? minScale : 1f;
    }
    
    public float GetFactor()
    {
        if (ElapsedTime <= 0f)
            return _finalScaleFactor;
        
        if (_finalScaleFactor.Equals(_minScale, 0.1f) || 
            _finalScaleFactor.Equals(_maxScale, 0.1f))
        {
            //so we start at scale1: then it scaled slowly down to "_minScale" and then from there
            //we change the multiplier to now ADD the x to the scale, so we scale back UP
            //this created this scaling flow
            _direction *= -1;  
        }
        float x = Speed * (1 / ElapsedTime); 
        return _finalScaleFactor += (_direction * x);
    }
    
    public static implicit operator Scale(float size) => new(size, size)
    {
        ElapsedTime = 0f,
        //_direction = 0,
    };
}

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
        return this with { _toWrap = Fade(_toWrap, CurrentAlpha) };
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
/// DefaultTile hardcoded type which is created from a look into the AngryBallsTexture!
/// </summary>
public enum Type : short
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

public class Shape
{
    public virtual ShapeKind Form { get; set; }
    public virtual Vector2 AtlasLocation { get; init; }
    public Size Size { get; init; }
    public Rectangle TextureRect => new(AtlasLocation.X, AtlasLocation.Y, Size.Width, Size.Height);
    public Scale Scale;
    public FadeableColor Color;
    
    public void ChangeColor(Color c, float alphaSpeed, float targetAlpha)
    {
        Color = c;
        Color.AlphaSpeed = alphaSpeed;
        Color.TargetAlpha = targetAlpha;
    }
}

public class TileShape : Shape, IEquatable<TileShape>, ICloneable
{
    public TileShape()
    {
       ChangeColor(WHITE,0f, 1f);
    }
    public Type TileType { get; init; }
    public Coat Layer { get; init; }
    public override ShapeKind Form { get; set; }
    public override Vector2 AtlasLocation { get; init; }
    public bool Equals(TileShape? other) =>
        other is not null && TileType == other.TileType && Layer == other.Layer;
    public override int GetHashCode()
    {
        return HashCode.Combine(Color, TileType);
    }

    public override string ToString() =>
        $"Tile type: <{TileType}> with Tint: <{Color}>"; //and Opacitylevel: {FadeTint.CurrentAlpha}";

    public object Clone()
    {
        TileShape clone = new()
        {
            TileType = TileType,
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
public enum TileState
{
    Disabled=1, Deleted=2, Hidden=4, Selected=8, Clean=16, Pulsate=32
}

public class Tile
{
    private TileState _current;
    
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

                Body.Color.CurrentAlpha = 1f;
                Body.Color.TargetAlpha = 1f;
                Body.Color.AlphaSpeed = 0f;
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
                _current &= TileState.Clean;    //remove clean flag from set
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
                Body.Color = WHITE;
                Body.Color.CurrentAlpha = 0f;
                //Body.Color.AlphaSpeed = 0.75f;
                //Body.Color.TargetAlpha = 1f;
            }
            else if ((value & TileState.Disabled) == TileState.Disabled)
            {
                _current &= TileState.Clean; //remove clean flag from set
                _current &= TileState.Selected; //remove clean flag from set
                _current &= TileState.Deleted; //deleted is reserved as Disabled AND Hidden, so u cannot be both at same time
                
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

    public bool IsDeleted => (TileState & TileState.Deleted) == TileState.Deleted;
    
    public Rectangle GridBounds => new(GridCell.X, GridCell.Y, 1f, 1f);
    public Rectangle WorldBounds => GridBounds.ToWorldBox();
    
    public const int Size = Level.TILE_SIZE;
    public static bool IsOnlyDefaultTile(Tile? current) =>
        current is not null and not EnemyTile;
    public Tile()
    {
        //we just init the variable with a dummy value to have the error gone, since we will 
        //overwrite the Body anyway with the Factorymethod "CreateNewTile(..)";
        Body = null!;
    }
    public override string ToString() => $"GridCell: {GridCell}; ---- {Body}";
    public void Select()
    {
        Body.ChangeColor(BLACK, 0f, 1f);
        TileState |= TileState.Selected;
    }
    public void DeSelect()
    {
        Body.ChangeColor(WHITE, 0f, 1f);
        TileState &= TileState.Selected;
    }
    public void Disable(bool shallDelete)
    {
        Body.ChangeColor(BLACK, 0f, 1f);
        Options = Options.UnMovable | Options.UnShapeable;
        TileState = !shallDelete ? TileState.Disabled : TileState.Deleted;
    }
    public void Enable()
    {
        Body.ChangeColor(WHITE, 0f, 1f);
        Options = Options.Movable | Options.Shapeable;
        TileState = TileState.Clean;
    }
}

public class EnemyTile : Tile
{
    public override Options Options => Options.UnMovable;
    
    public Rectangle Pulsate(float elapsedTime)
    {
        if (elapsedTime <= 0f)
            return Body.TextureRect;

        if (Body.Scale.Speed == 0f)
            Body.Scale.Speed = 20.25f;
        
        var rect = Body.TextureRect.DoScale(Body.Scale.GetFactor());
                
        Body.Scale.ElapsedTime = elapsedTime;
        return rect with { X = WorldCell.X, Y = WorldCell.Y };
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