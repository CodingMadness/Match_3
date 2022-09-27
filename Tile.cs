using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    public float CurrentAlpha, TargetAlpha, ElapsedTime;
    /// <summary>
    /// The greater this Value, the faster it fades!
    /// </summary>
    public float AlphaSpeed;

    private FadeableColor(Color color)
    {
        _toWrap = color;
        AlphaSpeed = 0.5f; //this basically states that we cannot fade!
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
    A, B, C, D, E, F, G, H
}

public abstract class Shape
{
    public virtual ShapeKind Form { get; set; }
    public virtual Vector2 FrameLocation { get; init; }

    private FadeableColor _f;
    //public FadeableColor FadeTint => _f;

    public ref FadeableColor Current() => ref _f;
    public void ChangeColor(Color c, float alphaSpeed, float targetAlpha)
    {
        _f = c;
        _f.AlphaSpeed = alphaSpeed;
        _f.TargetAlpha = targetAlpha;
    }
}

public class CandyShape : Shape, IEquatable<CandyShape>//, IShape<CandyShape>
{
    public CandyShape()
    {
       ChangeColor(WHITE,0f, 1f);
    }
    public Balls Ball { get; init; }
    public Coat Layer { get; init; }
    public override ShapeKind Form { get; set; }
    public override Vector2 FrameLocation { get; init; }
    public bool Equals(CandyShape? other) =>
        other is not null && Ball == other.Ball && Layer == other.Layer;
    public override int GetHashCode()
    {
        return HashCode.Combine(Current(), Ball);
    }

    public override string ToString() =>
        $"Tile type: <{Ball}> with Tint: <{Current()}>"; //and Opacitylevel: {FadeTint.CurrentAlpha}";

    public override bool Equals(object obj)
    {
        return obj is CandyShape shape && Equals(shape);
    }

    public static bool operator ==(CandyShape left, CandyShape? right)
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
    public Vector2 WorldPosition => (CurrentCoords * Size) + (Vector2.One * Size);
    public Vector2 ChangeTileSize(float xFactor, float yFactor) =>
        WorldPosition with { X = WorldPosition.X * xFactor, Y = WorldPosition.Y * yFactor };
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

    public Shape Shape { get; init; }
    
    private Rectangle DestRect => new(Shape.FrameLocation.X, Shape.FrameLocation.Y, ITile.Size, ITile.Size);
    
    public bool Selected
    {
        get => _selected;

        set
        {
            if (value)
            {
                Shape.Current().CurrentAlpha = 1f;
                Shape.Current().TargetAlpha = 0;
                Shape.Current().AlphaSpeed = 0.5f;
            }
            else
            {
                Shape.Current().AlphaSpeed = 0f;
            }

            _selected = value;
        }
    }
 
    public Tile()
    {
        //we just init the variable with a dummy value to have the error gone, since we will 
        //overwrite the Shape anyway with the Factorymethod "CreateNewTile(..)";
        Shape = null!;
    }

    public override string ToString() => 
            $"CurrentCoords: {CurrentCoords}; ---- {Shape}";
    
    public virtual void Draw(float elapsedTime)
    {
        void DrawTextOnTop(in Vector2 worldPosition, bool selected)
        {
            Font copy = AssetManager.WelcomeFont with{ baseSize = 1024 };
            /*Vector2 drawAt = worldPosition + Vector2.One *
                             15f - (Vector2.UnitX *6f) + 
                             (Vector2.UnitY * 6f);*/
            var begin = (this as ITile).WorldPosition;
            float halfSize = ITile.Size * 0.5f;
            begin = begin with { X = begin.X - halfSize - halfSize * 0.3f, Y = (begin.Y - halfSize - halfSize * 0.3f) };

            GameText coordText = new(copy, (worldPosition / ITile.Size).ToString(), 8.5f) 
            {
                Begin = begin,
                Color = selected ? RED : BLACK,
            };
            
            coordText.Color.AlphaSpeed = 0f;
            coordText.ScaleText();
            //DrawLine((int)coordText.Begin.X, (int)coordText.Begin.Y, 512, 0, RED);
            //DrawCircle((int)begin.X, (int)begin.Y, 3.5f, BLACK);
            coordText.Draw(2f);
            Console.WriteLine(begin);
        }

        //we draw 1*Tilesize in Y-Axis,
        //because our game-timer occupies an entire row so we begin 1 further down in Y 
        //var pos = CurrentCoords == Vector2.Zero ? CurrentCoords + Vector2.UnitY * ITile.Size : CurrentCoords * ITile.Size;
        Shape.Current().ElapsedTime = elapsedTime;
        DrawTextureRec(ITile.GetAtlas(), DestRect, CurrentCoords * ITile.Size, Shape.Current().Apply());
        DrawTextOnTop(CurrentCoords * ITile.Size, _selected);
    }

    public bool Equals(Tile? other)
    {
        return Shape switch
        {
            CandyShape c when other?.Shape is CandyShape d   && 
                                                !IsDeleted   && 
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

    public void DisableSwapForNeighbors(Grid map)
    {
        static bool IsOnlyDefaultTile(ITile? current) => 
            current is Tile and not MatchBlockTile;
        
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
            Vector2 coordA = CurrentCoords;

            for (Grid.Direction i = 0; i < lastDir; i++)
            {
                if (IsOnlyDefaultTile(map[coordA]))
                {
                    var t = map[coordA] as Tile;
                    t!.Shape.ChangeColor(BLACK, 0f, 1f);
                    t.State = TileState.UnMovable;
                    Console.WriteLine($"{t}  IS BLOCKED NOW!");
                }
                coordA = GetStepsFromDirection(coordA, i);
            }
        }
    }

    public override void Draw(float elapsedTime)
    {
        Selected = false;
        base.Draw(elapsedTime);
    }
}