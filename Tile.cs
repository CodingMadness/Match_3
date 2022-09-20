using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_CsLo;

using Color = Raylib_CsLo.Color;

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
        {Raylib.BLACK, "Black"},
        {Raylib.BLUE, "Blue"},
        {Raylib.BROWN, "Brown"},
        {Raylib.DARKGRAY, "DarkGray"},
        {Raylib.GOLD, "Gold"},
        {Raylib.GRAY, "Gray"},
        {Raylib.GREEN, "Green"},
        {Raylib.LIGHTGRAY, "LightGray"},
        {Raylib.MAGENTA, "Magenta"},
        {Raylib.MAROON, "Maroon"},
        {Raylib.ORANGE, "Orange"},
        {Raylib.PINK, "Pink"},
        {Raylib.PURPLE, "Purple"},
        {Raylib.RAYWHITE, "RayWhite"},
        {Raylib.RED, "Red"},
        {Raylib.SKYBLUE, "SkyBlue"},
        {Raylib.VIOLET, "Violet"},
        {Raylib.WHITE, "White"},
        {Raylib.YELLOW, "Yellow"}
    };

    public string ToReadableString()
    {
        Color compare = _toWrap;
        compare.a = byte.MaxValue;
        return Strings.TryGetValue(compare, out var value) ? value : _toWrap.ToString();
    }

    private Color Lerp()
    {
        _toWrap = Raylib.ColorAlpha(_toWrap, CurrentAlpha);
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
    Red, Blue, Green, Purple, Orange, Yellow, Violet,
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
    public CandyShape(float noise)
    {
        FadeTint = Raylib.WHITE;
        Ball = Backery.GetTileTypeTypeByNoise(noise);

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
            default:
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
    public Vector2 GridPos { get; set; }
    public Vector2 CoordsB4Swap { get; set; }
    public static int Size => 64;
    public bool Selected { get; set; }
    public void Draw(float elapsedTime);
}

public sealed class Tile : ITile
{
    /// <summary>
    /// Important always is: Match GridPos with the actual Drawing-Location of the window!
    /// </summary>
    public Vector2 GridPos { get; set; }

    public Vector2 CoordsB4Swap { get; set; }

    private bool _selected;

    public IShape TileShape;

    private FadeableColor _color = Raylib.WHITE;

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
            TileShape.FadeTint = _color;
        }
    }
 
    public Tile()
    {
        //we just init the variable with a dummy value to have the error gone, since we will 
        //overwrite the TileShape anyway with the Factorymethod "CreateNewTile(..)";
        TileShape = new CandyShape(0.25f);
    }

    public override string ToString() => $"GridPos: {GridPos}; ---- {TileShape}";

    public void ChangeTo(FadeableColor color)
    {
        TileShape.FadeTint = color;
    }

    public void Draw(float elapsedTime)
    {
        void DrawTextOnTop(in Vector2 worldPosition, bool selected)
        {
            float xCenter = worldPosition.X + ITile.Size / 4.3f;
            float yCenter = worldPosition.Y < 128f ? worldPosition.Y + ITile.Size / 2.5f :
                worldPosition.Y >= 128f ? (worldPosition.Y + ITile.Size / 2f) - 5f : 0f;

            Vector2 drawPos = new(xCenter - 10f, yCenter);
            FadeableColor drawColor = selected ? Raylib.BLACK : Raylib.WHITE;
            Raylib.DrawTextEx(AssetManager.DebugFont, worldPosition.ToString(), drawPos,
                14f, 1f, selected ? Raylib.BLACK : drawColor);
        }

        var pos = GridPos == Vector2.Zero ? GridPos + (Vector2.UnitY * ITile.Size) : GridPos * ITile.Size;

        _color = _selected ? Raylib.BLUE : Raylib.WHITE;//TileShape.FadeTint;
        _color.ElapsedTime = elapsedTime;
        TileShape.FadeTint = _color;

        Raylib.DrawTextureRec(AssetManager.SpriteSheet,
            new(TileShape.FrameLocation.X, TileShape.FrameLocation.Y, ITile.Size, ITile.Size),
            pos, TileShape.FadeTint);

        DrawTextOnTop(GridPos, _selected);
    }

    public bool Equals(Tile? other)
    {
        if (TileShape is CandyShape c && other is not null && other.TileShape is CandyShape d)
            if (c.Equals(d))
                return true;

        return false;
    }

    bool IEquatable<ITile>.Equals(ITile? other)
    {
        return Equals(other as Tile);
    }
}