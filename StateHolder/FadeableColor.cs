using System.Drawing;
using System.Runtime.CompilerServices;
using Match_3.Service;

namespace Match_3.StateHolder;

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    public float CurrentAlpha, TargetAlpha;
    private float _elapsedTime;
   
    /// <summary>
    /// The greater this Value, the faster it fades!
    /// </summary>
    public float AlphaSpeed;

    public void AddTime(float elapsedTime)
    {
        _elapsedTime = !elapsedTime.Equals(0f, 0.001f) ? elapsedTime : 1f;
    }
    
    private FadeableColor(Color color)
    {
        _toWrap = color;
        AlphaSpeed = 0.5f; 
        CurrentAlpha = 1.0f;
        TargetAlpha = 0.0f;
        _elapsedTime = 1f;
    }
    
    private static readonly Dictionary<RayColor, string> Strings = new()
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
    
    private string ToReadableString()
    {
        RayColor compare = _toWrap.AsRayColor();
        return Strings.TryGetValue(compare, out var value) ? value : _toWrap.ToString();
    }

    private void _Lerp()
    {
        //if u wanna maybe stop fading at 0.5f so we explicitly check if currAlpha > Target-Alpha
        if (CurrentAlpha > TargetAlpha)  
            CurrentAlpha -= AlphaSpeed * (1f / _elapsedTime);
    }
    
    public FadeableColor Apply()
    {
        _Lerp();
        return this with { _toWrap = Fade(_toWrap.AsRayColor(), CurrentAlpha).AsSysColor() };
    }

    public static implicit operator RayColor(FadeableColor color) => color._toWrap.AsRayColor();
    public static implicit operator FadeableColor(Color color) => new(color);
    
    public static implicit operator FadeableColor(RayColor color) => new(color.AsSysColor());
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

    public override bool Equals(object? obj) => obj is FadeableColor other && this == other;

    public override int GetHashCode() => HashCode.Combine(_toWrap, CurrentAlpha);

    public static bool operator !=(FadeableColor c1, FadeableColor c2) => !(c1 == c2);

    public override string ToString() => ToReadableString();
}