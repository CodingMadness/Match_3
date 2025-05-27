using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.Service;

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
    
    private FadeableColor(Color toWrap)
    {
        _toWrap = toWrap;
        _alphaSpeed = 0.5f;
        _currentAlpha = 1.0f;
        _targetAlpha = 0.0f;
        _currSeconds = 1f;
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

    public readonly string Name => _toWrap.Name;

    private static readonly TileColor[] AllTileColors =
    [
        TileColor.LightBlue,          //--> Hellblau
         TileColor.Turquoise,        //--> Türkis
         TileColor.Blue,             //--> Blau
         TileColor.LightGreen,      //--> Hellgrün
         TileColor.Green,            //--> Grün
         TileColor.Brown,            //--> Braun
         TileColor.Orange,           //--> Orange
         TileColor.Yellow,           //--> Gelb
         TileColor.Purple,       //--> Rosa
         TileColor.Magenta,          //--> Pink
         TileColor.Red,              //--> Rot
   
     ];

    public static Vector4 ToVec4(TileColor colorKind)
    {
        Color systemColor = Color.FromKnownColor(colorKind);

        return new(
            systemColor.R / 255.0f,
            systemColor.G / 255.0f,
            systemColor.B / 255.0f,
            systemColor.A / 255.0f);
    }

    public static void Fill(Span<TileColor> toFill)
    {
        for (int i = 0; i < Config.TileColorCount; i++)
            toFill[i] = AllTileColors[i];
    }

    private readonly Raylib_cs.Color AsRayColor() => new(_toWrap.R, _toWrap.G, _toWrap.B, _toWrap.A);

    private readonly Color AsSysColor() => Color.FromArgb(_toWrap.A, _toWrap.R, _toWrap.G, _toWrap.B);

    public static int ToIndex(TileColor toWrap)
    {
        return toWrap switch
        {
            TileColor.LightBlue => 0,           //--> Hellblau
            TileColor.Turquoise => 1,           //--> Dunkelblau
            TileColor.Blue => 2,                //--> Blau
            TileColor.LightGreen => 3,           //--> Hellgrün
            TileColor.Green => 4,               //--> Grün
            TileColor.Brown => 5,               //--> Braun
            TileColor.Orange => 6,              //--> Orange
            TileColor.Yellow => 7,              //--> Gelb
            TileColor.MediumVioletRed => 8,     //--> RotPink
            TileColor.Purple => 9,              //--> Rosa
            TileColor.Magenta => 10,            //--> Pink
            TileColor.Red => 11,
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