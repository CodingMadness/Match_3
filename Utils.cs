using System.Numerics;
using Raylib_CsLo;

namespace Match_3;

public static class Utils
{
    /// <summary>
    /// Rounds a random number to a value which has no remainder
    /// </summary>
    /// <param name="rnd"></param>
    /// <param name="r"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static int Round(Random rnd, Range r, int divisor)
    {
        if (r.Start.Value > r.End.Value)
        {
            r = new Range(r.End, r.Start);
        }

        int value = rnd.Next(r.Start.Value, r.End.Value);
        value = value % divisor == 0 ? value : ((int)MathF.Round(value / divisor)) * divisor;
        return value;
    }

    private static FastNoiseLite noiseMaker = new(DateTime.UtcNow.GetHashCode());

    static Utils()
    {
        noiseMaker.SetFrequency(25f);
        noiseMaker.SetFractalType(FastNoiseLite.FractalType.Ridged);
        noiseMaker.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    }

    public static FastNoiseLite NoiseMaker => noiseMaker;

    public static float Trunc(this float value, int digits)
    {
        float mult = MathF.Pow(10.0f, digits);
        float result = MathF.Truncate(mult * value) / mult;
        return result < 0 ? -result : result;
        ;
    }

    public static Vector2 GetScreenCoord() => new(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
}
