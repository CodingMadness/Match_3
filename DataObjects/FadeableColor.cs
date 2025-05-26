using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.Service;

namespace Match_3.DataObjects;

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    public float CurrentAlpha, TargetAlpha;
    private float _currSeconds;

    /// <summary>
    /// The greater this Value, the faster it fades!
    /// </summary>
    public float AlphaSpeed;

    public void AddTime(float currSeconds)
    {
        _currSeconds = !currSeconds.Equals(0f, 0.001f) ? currSeconds : 1f;
    }

    private FadeableColor(Color toWrap)
    {
        _toWrap = toWrap;
        AlphaSpeed = 0.5f;
        CurrentAlpha = 1.0f;
        TargetAlpha = 0.0f;
        _currSeconds = 1f;
    }

    private static readonly Dictionary<RayColor, string> ColorsAsText = new()
    {
        {Black, "Black"},
        {Blue, "Blue"},
        {Brown, "Brown"},
        {DarkGray, "DarkGray"},
        {Gold, "Gold"},
        {Gray, "Gray"},
        {Green, "Green"},
        {LightGray, "LightGray"},
        {Magenta, "Magenta"},
        {Maroon, "Maroon"},
        {Orange, "Orange"},
        {Pink, "Pink"},
        {Purple, "Purple"},
        {RayWhite, "RayWhite"},
        {Red, "Red"},
        {SkyBlue, "SkyBlue"},
        {Violet, "Violet"},
        {White, "White"},
        {Yellow, "Yellow"}
    };

    private readonly string ToReadableString()
    {
        RayColor compare = AsRayColor();
        return ColorsAsText.TryGetValue(compare, out var value) ? value : _toWrap.ToString();
    }

    private void Lerp()
    {
        //if u wanna maybe stop fading at 0.5f so we explicitly check if currAlpha > Target-Alpha
        if (CurrentAlpha > TargetAlpha)
            CurrentAlpha -= AlphaSpeed * (1f / _currSeconds);
    }

    public FadeableColor Apply()
    {
        Lerp();
        return Fade(AsRayColor(), CurrentAlpha);         
    }

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
         TileColor.MediumVioletRed,  //--> RotPink
         TileColor.Purple,       //--> Rosa
         TileColor.Magenta,          //--> Pink
         TileColor.Red,              //--> Rot
   
     ];

    public readonly Vector4 ToVec4()
    {
        return new(
            _toWrap.R / 255.0f,
            _toWrap.G / 255.0f,
            _toWrap.B / 255.0f,
            _toWrap.A / 255.0f);
    }

    public static void Fill(Span<TileColor> toFill)
    {
        for (int i = 0; i < Config.TileColorCount; i++)
            toFill[i] = AllTileColors[i];
    }

    public readonly RayColor AsRayColor() => new(_toWrap.R, _toWrap.G, _toWrap.B, _toWrap.A);

    public readonly Color AsSysColor() => Color.FromArgb(_toWrap.A, _toWrap.R, _toWrap.G, _toWrap.B);

    public static int ToIndex(TileColor _toWrap)
    {
        return _toWrap switch
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
            _ => throw new ArgumentOutOfRangeException(nameof(_toWrap), _toWrap, "No other _toWrap is senseful since we do not need other or more colors!")
        };
    }

    public static implicit operator RayColor(FadeableColor toWrap) => toWrap.AsRayColor();

    public static implicit operator Color(FadeableColor toWrap) => toWrap.AsSysColor();

    public static implicit operator FadeableColor(Color toWrap) => new(toWrap);

    public static implicit operator FadeableColor(RayColor toWrap) => new(Color.FromArgb(toWrap.R, toWrap.G, toWrap.B));

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

    public override readonly bool Equals(object? obj) => obj is FadeableColor other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(_toWrap, CurrentAlpha);

    public static bool operator !=(FadeableColor c1, FadeableColor c2) => !(c1 == c2);

    public override readonly string ToString() => ToReadableString();
}