using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    private bool _allowFading = true;
    public float CurrentAlpha, TargetAlpha, ElapsedTime;
    /// <summary>
    /// The greater this Value, the faster it fades!
    /// </summary>
    public float AlphaSpeed;

    private FadeableColor(Color color)
    {
        _toWrap = color;
        AlphaSpeed = 0.0f; //this basically states that we cannot fade!
        CurrentAlpha = 1.0f;
        TargetAlpha = 0.0f;
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
        if (CurrentAlpha > TargetAlpha)
            CurrentAlpha -= AlphaSpeed * (1f / ElapsedTime);
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
        return bytes4C1 == bytes4C2;//&& Math.Abs(c1.CurrentAlpha - (c2.CurrentAlpha)) < 1e-3; ;
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

    public override string ToString() => ToReadableString();
}

/// <summary>
/// A hardcoded type which is created from a look into the AngryBallsTexture!
/// </summary>
public enum Balls
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
    A, B, C, D, E, F, G,
}

public interface IShape
{
    public ShapeKind Form { get; set; }

    public Vector2 FrameLocation { get; init; }

    public FadeableColor FadeTint { get; set; }
}

public class CandyShape : IShape, IEquatable<CandyShape>//, IShape<CandyShape>
{
    public CandyShape()
    {
        FadeTint = WHITE;
    }
    public Balls Ball { get; init; }
    public Coat Layer { get; init; }
    public FadeableColor FadeTint { get; set; }
    public ShapeKind Form { get; set; }
    public Vector2 FrameLocation { get; init; }
    public bool Equals(CandyShape other) =>
        Ball == other.Ball && Layer == other.Layer;
    public override int GetHashCode()
    {
        return HashCode.Combine(FadeTint, Ball);
    }

    public override string ToString() =>
        $"Tile type: <{Ball}> with Tint: <{FadeTint}>"; //and Opacitylevel: {FadeTint.CurrentAlpha}";

    public override bool Equals(object obj)
    {
        return obj is CandyShape shape && Equals(shape);
    }

    public static bool operator ==(CandyShape left, CandyShape right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CandyShape left, CandyShape right)
    {
        return !(left == right);
    }
}

[Flags]
public enum TileState
{
    UnDestroyable = 0,
    UnMovable = 1,
    UnShapeable = 2,
    
    Destroyable = 4,
    Movable = 8,
    Shapeable = 16
}

public interface ITile : IEquatable<ITile>
{
    public bool IsDeleted { get; set; }
    public TileState State { get; set; }
    
    private static Texture Atlas;

    //public static void SetAtlas(ref Texture tex) => Atlas = tex;
    public static ref Texture GetAtlas() => ref Atlas;
    public Vector2 CurrentCoords { get; set; }
    public Vector2 CoordsB4Swap { get; set; }
    public static int Size => 64;
    public Vector2 TileSize => (CurrentCoords * Size) + (Vector2.One * Size);
    public Vector2 ChangeTileSize(float xFactor, float yFactor) =>
        TileSize with { X = TileSize.X * xFactor, Y = TileSize.Y * yFactor };
    public bool Selected { get; set; }
    public void Draw(float elapsedTime);
}

public class Tile : ITile
{
    public virtual bool IsDeleted { get; set; }
    
    public virtual TileState State { get; set; }

    /// <summary>
    /// Important always is: Match CurrentCoords with the actual Drawing-Location of the window!
    /// </summary>
    public Vector2 CurrentCoords { get; set; }

    public Vector2 CoordsB4Swap { get; set; }

    private bool _selected;

    public IShape Shape { get; init; } = new CandyShape();

    private FadeableColor _color = WHITE;
    
    private Rectangle DestRect => new(Shape.FrameLocation.X, Shape.FrameLocation.Y, ITile.Size, ITile.Size);
    
    public bool Selected
    {
        get => _selected;

        set
        {
            if (value)
            {
                _color.CurrentAlpha = 1f;
                _color.TargetAlpha = 0;
            }
            else
            {
                _color.TargetAlpha = _color.CurrentAlpha = 1f;
            }

            _selected = value;
        }
    }
 
    public Tile()
    {
        //we just init the variable with a dummy value to have the error gone, since we will 
        //overwrite the Shape anyway with the Factorymethod "CreateNewTile(..)";
        _color.AlphaSpeed = 0.65f;
    }

    public override string ToString() => $"CurrentCoords: {CurrentCoords}; ---- {Shape}";

    public void ChangeTo(FadeableColor color)
    {
        Shape.FadeTint = color;
    }
    
    public virtual void Draw(float elapsedTime)
    {
        void DrawTextOnTop(in Vector2 worldPosition, bool selected)
        {
            Font copy = AssetManager.WelcomeFont with{baseSize = (int)(64/1.6f)};
            Vector2 drawAt = worldPosition + Vector2.One *
                             15f - (Vector2.UnitX *6f) + 
                             (Vector2.UnitY * 6f);
            GameText coordText = new(copy, (worldPosition / ITile.Size).ToString(), 10f) 
            {
                Begin = drawAt,
                Color = selected ? BLACK : RED,
            };
            coordText.Draw(0.5f);
        }

        //we draw 1*Tilesize in Y-Axis,
        //because our game-timer occupies an entire row so we begin 1 further down in Y 
        var pos = CurrentCoords == Vector2.Zero ? CurrentCoords + Vector2.UnitY * ITile.Size : CurrentCoords * ITile.Size;
        _color.ElapsedTime = elapsedTime; 
        DrawTextureRec(ITile.GetAtlas(), DestRect, pos, _color.Apply());
        DrawTextOnTop(CurrentCoords * ITile.Size, _selected);
        ChangeTo(_color);
    }

    public bool Equals(Tile? other)
    {
        return Shape switch
        {
            CandyShape c when other?.Shape is CandyShape d && 
                                                !IsDeleted && 
                                                c.Equals(d) => true,
            _ => false
        };
    }

    public static bool operator ==(Tile? a, Tile? b)
    {
        return a?.Equals(b) ?? false;
    }

    public static bool operator !=(Tile? a, Tile? b)
    {
        return !(a == b);
    }

    bool IEquatable<ITile>.Equals(ITile? other)
    {
        return Equals(other as Tile);
    }
}

public class MatchBlockTile : Tile
{
    public override TileState State => TileState.UnMovable;
    
    /// <summary>
    /// While a matchblocking tile is "IsBlocking" you dont get a match3 and hence
    /// no points
    /// </summary>
    private readonly Tile[] BlockedNeighbors;
    
    public MatchBlockTile() : base()
    {
        BlockedNeighbors = new Tile[4];
    }

    public void DisableMoveForNeighbors(Grid map)
    {
        static Vector2 GetStepsFromDirection(Vector2 input, Grid.Direction direction)
        {
            var tmp = direction switch
            {
                Grid.Direction.NegativeX => new Vector2(input.X - 1, input.Y),
                Grid.Direction.PositiveX => new Vector2(input.X + 1, input.Y),
                Grid.Direction.NegativeY => new Vector2(input.X, input.Y - 1),
                Grid.Direction.PositiveY => new Vector2(input.X, input.Y + 1),
                _ => Vector2.Zero
            };

            return tmp;
        }
            
        const Grid.Direction lastDir = (Grid.Direction)4;

        if (map[CurrentCoords] is not null)
        {
            Vector2 coordA = map[CurrentCoords]!.CurrentCoords;

            for (Grid.Direction i = 0; i < lastDir; i++)
            {
                Vector2 nextCoords = GetStepsFromDirection(coordA, i);
                map[nextCoords]!.State = TileState.UnMovable;
                BlockedNeighbors[(int)i] = (Tile)map[nextCoords]!;
                var color = BlockedNeighbors[(int)i].Shape.FadeTint;
                color.CurrentAlpha = 1f;
                color.AlphaSpeed = 0.75f;
                color.TargetAlpha = 0.33f;
                BlockedNeighbors[(int)i].Shape.FadeTint = (color);
                coordA = GetStepsFromDirection(nextCoords, i);
            }
        }
    }
}