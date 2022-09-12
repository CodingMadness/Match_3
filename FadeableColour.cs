using Raylib_cs;

namespace Match_3;

public struct FadeableColour : IEquatable<FadeableColour>
{
    private Color toWrapp;
    public float CurrentAlpha, TargetALpha, AlphaSpeed, ElapsedTime;
    
    private FadeableColour(Color color)
    {
        toWrapp = color;
        CurrentAlpha = 0f;
        TargetALpha = 0f;
        AlphaSpeed = 0f;
        ElapsedTime = 0f;
    }
    
    private static float Lerp(float? firstFloat, float secondFloat, float? by)
    {
        return firstFloat ?? (float)(firstFloat * (1 - by) + secondFloat * by);
    }

    public string ToReadableString()
    {
        //......
    }
    
    private Color Lerp(float degree)
    {
        toWrapp = Raylib.ColorAlpha(toWrapp, degree);
        CurrentAlpha = Lerp(CurrentAlpha, TargetALpha, AlphaSpeed * ElapsedTime);
        return toWrapp;
    }

    public static implicit operator FadeableColour(Color color)
    {
        return new FadeableColour(color);
    }

    public static implicit operator Color(FadeableColour color)
    {
        return color.Lerp(color.ElapsedTime);
    }

    public static bool operator ==(FadeableColour c1, FadeableColour c2) =>
        c1.toWrapp.a == c2.toWrapp.a && 
        c1.toWrapp.b == c2.toWrapp.b &&
        c1.toWrapp.g == c2.toWrapp.g &&
        c1.toWrapp.r == c2.toWrapp.r;

    public bool Equals(FadeableColour other)
    {
        return this == other &&
               Math.Abs(CurrentAlpha - (other.CurrentAlpha)) < 1e-3;
    }

    public override bool Equals(object? obj)
    {
        return obj is FadeableColour other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(toWrapp, CurrentAlpha);
    }
    
    public static bool operator !=(FadeableColour c1, FadeableColour c2) => !(c1 == c2);
    
    public override string ToString() => nameof(toWrapp);
}