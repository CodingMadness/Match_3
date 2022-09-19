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
        return bytes_4_c1 == bytes_4_c2 && Math.Abs(c1.CurrentAlpha - (c2.CurrentAlpha)) < 1e-3; ;
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

public enum Sweets : sbyte
{
    Donut = 0,
    Cupcake,
    Bonbon,
    Cookie,
    Gummies,
    Lolipops,
    Empty = -1,
    Length = 6
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
    public Sweets Sweet => Sweets.Donut;

    public ShapeKind Form { get; init; }

    public Coat Layer { get; init; }

    public Vector2 FrameLocation { get; init; }

    public FadeableColor FadeTint { get; set; }

    //protected TShape DetectShapeBySweets(Sweets sweet, float noise)
    //{
    //    noise = MathF.Round(noise, 2, MidpointRounding.ToPositiveInfinity);

    //    if (noise <= 0f)
    //    {
    //        noise += Random.Shared.NextSingle();
    //        return GetSweetTypeByNoise(noise);
    //    }

    //    else if (noise <= 0.1)
    //        noise *= 10;

    //    switch (noise)
    //    {
    //        case <= 0.25f:
    //            return Sweets.Donut;
    //        case > 0.25f and <= 0.35f:
    //            return Sweets.Cookie;
    //        case > 0.35f and <= 0.45f:
    //            return Sweets.Cupcake;
    //        case > 0.45f and <= 0.65f:
    //            return Sweets.Bonbon;
    //        case > 0.65f and <= 1f:
    //            return Sweets.Gummies;
    //        default:
    //            return Sweets.Empty;
    //    }
    //}
}

public struct BaseShape : IShape, IEquatable<BaseShape>//, IShape<BaseShape>
{   
    public BaseShape(float noise)
    {
        FadeTint = Raylib.WHITE;
        Sweet = SweetsFactory.GetSweetTypeByNoise(noise);
    }

    public readonly Sweets Sweet { get; }

    public FadeableColor FadeTint { get; set; }
    public ShapeKind Form { get; init; }
    public Coat Layer { get; init; }
    public Vector2 FrameLocation { get; init; }

    public bool Equals(BaseShape other) =>
        Sweet == other.Sweet && FadeTint == other.FadeTint;

    public override int GetHashCode()
    {
        return HashCode.Combine(FadeTint, Sweet);
    }

    public override string ToString() =>
        $"TileShape: {Sweet} with MainColor: {FadeTint}"; //and Opacitylevel: {FadeTint.CurrentAlpha}";

    public override bool Equals(object obj)
    {
        return obj is BaseShape shape && Equals(shape);
    }

    public static BaseShape Create(Vector2 posWithinSpriteSheet)
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(BaseShape left, BaseShape right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BaseShape left, BaseShape right)
    {
        return !(left == right);
    }
}

/// <summary>
/// A hardcoded type which is created from a look into the SpriteSheet!
/// </summary>
public struct Donut : IShape
{  
    public Donut()
    {
       
    }

    public FadeableColor FadeTint { get; set; }
    public readonly Sweets Sweet => Sweets.Donut;
    public readonly ShapeKind Form { get; init; }
    public readonly Coat Layer { get; init; }
    public readonly Vector2 FrameLocation { get; init; }
}

public interface ITile// : IEquatable<ITile>
{
    public Vector2 Current { get; set; }
    public Vector2 CoordsB4Swap { get; set; }
    public int Size { get; }
    public bool Selected { get; set; }
    public void Draw(Vector2 position, float elapsedTime);

    public static ITile Create(Vector2 position, float noise) 
    {
        return SweetsFactory.CreateTile(position, noise);
    }

    //public new bool Equals(ITile other);
}

public sealed class Tile : ITile
{
    public Vector2 Current { get; set; }

    public int Size => Grid.TileSize;

    public Vector2 CoordsB4Swap { get; set; }

    private bool _selected;

    private IShape  _tileShape;

    private FadeableColor _color;

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
            _tileShape.FadeTint = _color;
        }
    }

    public IShape  TileShape
    {
        get => _tileShape;
        init => _tileShape = value;
    }

    public Tile()
    {
        //we just init the variable with a dummy value to have the error gone, since we will 
        //overwrite the _tileShape anyway with the Factorymethod "CreateNewTile(..)";
        _tileShape = new BaseShape(0.25f);
    }
       
    public override string ToString() => $"Current Position: {Current};  {TileShape}";

    public void ChangeTo(FadeableColor color)
    {
        _tileShape.FadeTint = color;
    }

    public void Draw(Vector2 position, float elapsedTime)
    {
        void DrawTextOnTop(in Vector2 worldPosition, bool selected)
        {
            float xCenter = worldPosition.X + Size / 4.3f;
            float yCenter = worldPosition.Y < 128f ? worldPosition.Y + Size / 2.5f :
                worldPosition.Y >= 128f ? (worldPosition.Y + Size / 2f) - 5f : 0f;

            Vector2 drawPos = new(xCenter - 10f, yCenter);
            FadeableColor drawColor = selected ? Raylib.BLACK : Raylib.WHITE;
            Raylib.DrawTextEx(AssetManager.DebugFont, worldPosition.ToString(), drawPos,
                14f, 1f, selected ? Raylib.BLACK : drawColor);
        }
        position *= Size;

        _tileShape.FadeTint = _selected ? Raylib.BLUE : Raylib.WHITE;//_tileShape.FadeTint;
        _color.ElapsedTime = elapsedTime;


        Raylib.DrawTextureRec(AssetManager.SpriteSheet,
            new(_tileShape.FrameLocation.X, _tileShape.FrameLocation.Y, Size, Size),
            position,
            _tileShape.FadeTint);

        DrawTextOnTop(position, _selected);
    }

    public bool Equals(ITile? other)
    {
        return other is Tile d && TileShape.Equals(d.TileShape);
    }
}