using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    public float CurrentAlpha, TargetAlpha, AlphaSpeed, ElapsedTime;
    private FadeableColor(Color color)
    {
        _toWrap = color;
        CurrentAlpha = 1f;
        TargetAlpha = 1f;
        AlphaSpeed = 3f;
    }

    private static float _lerp(float? firstFloat, float secondFloat, float? by)
    {
        return firstFloat ?? (float)(firstFloat * (1 - by) + secondFloat * by);
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

    public string ToReadableString()
    {
        Color compare = _toWrap;
        compare.a = byte.MaxValue;
        return Strings.TryGetValue(compare, out var value) ? value : _toWrap.ToString();
    }

    private Color Lerp()
    {
        _toWrap = ColorAlpha(_toWrap, CurrentAlpha);
        CurrentAlpha = _lerp(CurrentAlpha, TargetAlpha, AlphaSpeed * ElapsedTime);
        return _toWrap;
    }

    public static implicit operator FadeableColor(Color color)
    {
        return new FadeableColor(color);
    }

    public static implicit operator Color(FadeableColor color)
    {
        return color.Lerp();
    }

    public static bool operator ==(FadeableColor c1, FadeableColor c2)
    {
        int bytes_4_c1 = Unsafe.As<Color, int>(ref c1._toWrap);
        int bytes_4_c2 = Unsafe.As<Color, int>(ref c2._toWrap);
        return bytes_4_c1 == bytes_4_c2;//&& Math.Abs(c1.CurrentAlpha - (c2.CurrentAlpha)) < 1e-3; ;
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

//public enum Sweets : sbyte
//{
//    Donut = 0,
//    Cupcake,
//    Bonbon,
//    Cookie,
//    Gummies,
//    Lolipops,
//    Empty = -1,
//    Length = 6
//}


public enum Balls
{
    Red, Blue, Green, Purple, Orange, Yellow, Brown, Violet,
    Length = Violet + 1, Empty = -1,
}

public enum ShapeKind
{
    Circle,
    Quader,
    rectangle,
    Heart,
}

public enum Coat
{
    A, B, C, D, E, F, G,
}

public interface IShape
{
    public ShapeKind Form { get; init; }

    public Vector2 FrameLocation { get; init; }

    public FadeableColor FadeTint { get; set; }

    //protected TShape DetectShapeBySweets(Sweets sweet, float noise)
    //{
    //    noise = MathF.Round(noise, 2, MidpointRounding.ToPositiveInfinity);

    //    if (noise <= 0f)
    //    {
    //        noise += Random.Shared.NextSingle();
    //        return GetTileTypeTypeByNoise(noise);
    //    }

    //    else if (noise <= 0.1)
    //        noise *= 10;

    //    switch (noise)
    //    {
    //        case <= 0.25f:
    //            return Balls.Donut;
    //        case > 0.25f and <= 0.35f:
    //            return Balls.Cookie;
    //        case > 0.35f and <= 0.45f:
    //            return Balls.Cupcake;
    //        case > 0.45f and <= 0.65f:
    //            return Balls.Bonbon;
    //        case > 0.65f and <= 1f:
    //            return Balls.Gummies;
    //        default:
    //            return Balls.Empty;
    //    }
    //}
}

public class CandyShape : IShape, IEquatable<CandyShape>//, IShape<CandyShape>
{
    public CandyShape(Vector2 coord, float noise)
    {
        FadeTint = WHITE;
        Ball = Backery.GetTileTypeTypeByNoise(coord, noise);

        switch (Ball)
        {
            case Balls.Green:
                FrameLocation = new Vector2(0f, 0f) * ITile.Size;
                Form = ShapeKind.Circle;
                Layer = Coat.A;
                break;
            case Balls.Purple:
                FrameLocation = new Vector2(1f, 0f) * ITile.Size;
                Layer = Coat.B;
                break;
            case Balls.Orange:
                FrameLocation = new Vector2(2f, 0f) * ITile.Size;
                Layer = Coat.C;
                break;
            case Balls.Yellow:
                FrameLocation = new Vector2(3f, 0f) * ITile.Size;
                Layer = Coat.D;
                break;
            case Balls.Red:
                FrameLocation = new Vector2(0f, 1f) * ITile.Size;
                Layer = Coat.E;
                break;
            case Balls.Blue:
                FrameLocation = new Vector2(1f, 1f) * ITile.Size;
                Layer = Coat.F;
                break;
            case Balls.Brown:
                FrameLocation = new Vector2(2f, 1f) * ITile.Size;
                Form = ShapeKind.Circle;
                Layer = Coat.A;
                break;
            case Balls.Violet:
                FrameLocation = new Vector2(3f, 1f) * ITile.Size;
                Layer = Coat.G;
                break;

            //DEFAULTS.......
            case Balls.Length:
                FrameLocation = -Vector2.One;
                break;
            case Balls.Empty:
                FrameLocation = -Vector2.One;
                break;
        }
    }

    public Balls Ball { get; init; }
    public Coat Layer { get; init; }
    public FadeableColor FadeTint { get; set; }
    public ShapeKind Form { get; init; }
    public Vector2 FrameLocation { get; init; }

    public bool Equals(CandyShape other) =>
        Ball == other.Ball && Layer == other.Layer;

    public override int GetHashCode()
    {
        return HashCode.Combine(FadeTint, Ball);
    }

    public override string ToString() =>
        $"Tiletype: <{Ball}> with Tint: <{FadeTint}>"; //and Opacitylevel: {FadeTint.CurrentAlpha}";

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

/// <summary>
/// A hardcoded type which is created from a look into the SpriteSheet!
/// </summary>

public interface ITile : IEquatable<ITile>
{
    public Vector2 GridCoords { get; set; }
    public Vector2 CoordsB4Swap { get; set; }
    public static int Size => 64;
    public Vector2 TileSize => (GridCoords * Size) + (Vector2.One * Size);
    public Vector2 ChangeTileSize(float xFactor, float yFactor) =>
        TileSize with { X = TileSize.X * xFactor, Y = TileSize.Y * yFactor };
    public bool Selected { get; set; }
    public void Draw(float elapsedTime);
}

public sealed class Tile : ITile
{
    /// <summary>
    /// Important always is: Match GridCoords with the actual Drawing-Location of the window!
    /// </summary>
    public Vector2 GridCoords { get; set; }

    public Vector2 CoordsB4Swap { get; set; }

    private bool _selected;

    public IShape Shape;

    private FadeableColor _color = WHITE;

    public bool Selected
    {
        get => _selected;

        set
        {
            if (!value)
            {
                _color.AlphaSpeed = 1.5f;
                _color.TargetAlpha = 1f;
            }
            else
            {
                _color.TargetAlpha = _color.CurrentAlpha = 0f;
            }

            _selected = value;
            Shape.FadeTint = _color;
        }
    }
 
    public Tile()
    {
        //we just init the variable with a dummy value to have the error gone, since we will 
        //overwrite the Shape anyway with the Factorymethod "CreateNewTile(..)";
        Shape = new CandyShape(new(0f,0f),0.25f);
    }

    public override string ToString() => $"GridCoords: {GridCoords}; ---- {Shape}";

    public void ChangeTo(FadeableColor color)
    {
        Shape.FadeTint = color;
    }

    public void Draw(float elapsedTime)
    {
        void DrawTextOnTop(in Vector2 worldPosition, bool selected)
        {
            Font copy = AssetManager.WelcomeFont with{baseSize = (int)(64/1.6f)};
            Vector2 drawAt = worldPosition + Vector2.One * 15f - (Vector2.UnitX *6f) + (Vector2.UnitY * 6f);//(this as ITile).TileSize;
           // Console.WriteLine((this as ITile).TileSize);
            GameText coordText = new(copy, (worldPosition / ITile.Size).ToString(), 10f) 
            {
                Begin = drawAt,
                Color = selected ? BLACK : RED,
            };
            coordText.Draw(0.5f);
        }

        //we draw 1*Tilesize because our game-timer occupies an entire row so we begin 1 further down in Y 
        var pos = GridCoords == Vector2.Zero ? GridCoords + (Vector2.UnitY * ITile.Size) : GridCoords * ITile.Size;

        _color = _selected ? BLUE : WHITE;//Shape.FadeTint;
        _color.ElapsedTime = elapsedTime;
        Shape.FadeTint = _color;

        DrawTextureRec(AssetManager.SpriteSheet,
            new(Shape.FrameLocation.X, Shape.FrameLocation.Y, ITile.Size, ITile.Size),
            pos, Shape.FadeTint);

        DrawTextOnTop(GridCoords * ITile.Size, _selected);
    }

    public bool Equals(Tile? other)
    {
        if (Shape is CandyShape c && other is not null && other.Shape is CandyShape d)
            if (c.Equals(d))
                return true;

        return false;
    }

    bool IEquatable<ITile>.Equals(ITile? other)
    {
        return Equals(other as Tile);
    }
}