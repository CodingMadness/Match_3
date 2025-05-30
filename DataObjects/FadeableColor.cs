using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Match_3.DataObjects;

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    private float _currentAlpha;
    private readonly float _targetAlpha, _currSeconds;
    /// <summary>
    /// The greater this Value, the faster it fades!
    /// </summary>
    private readonly float _alphaSpeed;

    public readonly TileColorTypes Type;
    public readonly string Name;
    public readonly Vector4 Vector;
    
    private FadeableColor(Color toWrap)
    {
        _toWrap = toWrap;
        _alphaSpeed = 0.5f;
        _currentAlpha = 1.0f;
        _targetAlpha = 0.0f;
        _currSeconds = 1f;
        Type = toWrap.ToKnownColor();
        Name =  toWrap.Name;
        Vector = ToVec4(Type);
    }

    private void Lerp()
    {
        //if you want to maybe stop fading at 0.5f so we explicitly check if currAlpha > Target-Alpha
        if (_currentAlpha > _targetAlpha)
            _currentAlpha -= _alphaSpeed * (1f / _currSeconds);
    }

    public FadeableColor Apply()
    {
        Lerp();
        return Fade(AsRayColor(), _currentAlpha);         
    }

    private static readonly TileColorTypes[] AllTileColors =
    [
        TileColorTypes.LightBlue,          //--> Hellblau
         TileColorTypes.Turquoise,        //--> Türkis
         TileColorTypes.Blue,             //--> Blau
         TileColorTypes.LightGreen,      //--> Hellgrün
         TileColorTypes.Green,            //--> Grün
         TileColorTypes.Brown,            //--> Braun
         TileColorTypes.Orange,           //--> Orange
         TileColorTypes.Yellow,           //--> Gelb
         TileColorTypes.Purple,       //--> Rosa
         TileColorTypes.Magenta,          //--> Pink
         TileColorTypes.Red,              //--> Rot
   
     ];

    public static Vector4 ToVec4(TileColorTypes colorTypesKind)
    {
        Color systemColor = Color.FromKnownColor(colorTypesKind);

        return new(
            systemColor.R / 255.0f,
            systemColor.G / 255.0f,
            systemColor.B / 255.0f,
            systemColor.A / 255.0f);
    }

    public static void Fill(Span<TileColorTypes> toFill)
    {
        for (int i = 0; i < Config.TileColorCount; i++)
            toFill[i] = AllTileColors[i];
    }

    private readonly Raylib_cs.Color AsRayColor() => new(_toWrap.R, _toWrap.G, _toWrap.B, _toWrap.A);

    private readonly Color AsSysColor() => Color.FromArgb(_toWrap.A, _toWrap.R, _toWrap.G, _toWrap.B);

    public static int ToIndex(TileColorTypes toWrap)
    {
        return toWrap switch
        {
            TileColorTypes.LightBlue => 0,           //--> Hellblau
            TileColorTypes.Turquoise => 1,           //--> Dunkelblau
            TileColorTypes.Blue => 2,                //--> Blau
            TileColorTypes.LightGreen => 3,           //--> Hellgrün
            TileColorTypes.Green => 4,               //--> Grün
            TileColorTypes.Brown => 5,               //--> Braun
            TileColorTypes.Orange => 6,              //--> Orange
            TileColorTypes.Yellow => 7,              //--> Gelb
            TileColorTypes.MediumVioletRed => 8,     //--> RotPink
            TileColorTypes.Purple => 9,              //--> Rosa
            TileColorTypes.Magenta => 10,            //--> Pink
            TileColorTypes.Red => 11,
            _ => throw new ArgumentOutOfRangeException(nameof(toWrap), toWrap, "No other _toWrap is senseful since we do not need other or more colors!")
        };
    }
    public static implicit operator Raylib_cs.Color(FadeableColor toWrap) => toWrap.AsRayColor();

    public static implicit operator Color(FadeableColor toWrap) => toWrap.AsSysColor();

    public static implicit operator FadeableColor(Color toWrap) => new(toWrap);

    public static implicit operator FadeableColor(Raylib_cs.Color toWrap) => new(Color.FromArgb(toWrap.R, toWrap.G, toWrap.B));

    public static bool operator ==(FadeableColor c1, FadeableColor c2)
    {
        int bytes4C1 = Unsafe.As<Color, int>(ref c1._toWrap);
        int bytes4C2 = Unsafe.As<Color, int>(ref c2._toWrap);
        return bytes4C1 == bytes4C2;
    }

    public readonly bool Equals(FadeableColor other)
    {
        return this == other;
    }

    public readonly override bool Equals(object? obj) => obj is FadeableColor other && this == other;

    public readonly override int GetHashCode() => HashCode.Combine(_toWrap, _currentAlpha);

    public static bool operator !=(FadeableColor c1, FadeableColor c2) => !(c1 == c2);

    public readonly override string ToString() => _toWrap.Name;
}