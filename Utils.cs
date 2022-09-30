﻿using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

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

    public static  readonly Random Randomizer =  new(DateTime.UtcNow.Ticks.GetHashCode());
    private static readonly FastNoiseLite noiseMaker = new(DateTime.UtcNow.Ticks.GetHashCode());

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

    public static Vector2 GetScreenCoord() => new(GetScreenWidth(), GetScreenHeight());
    
    public static unsafe nint GetAddrOfObject<ObjectT>(this ObjectT Object) where ObjectT: class
    {
        return (nint) Unsafe.AsPointer(ref Unsafe.As<StrongBox<byte>>(Object).Value);
    }

    public static Rectangle GetMatch3Rect(Vector2 begin, int width, int height)
    {
        return new((int)begin.X, (int)begin.Y, width, height);
    }

    public static void SetMousePos(Vector2 position)
    {
        SetMousePosition((int)position.X * ITile.Size, (int)position.Y* ITile.Size);
    }
    
    public static bool IsRowBased<T>(this ISet<T?> items) where T: ITile
    {
        T? cmpr = items.ElementAt(0);

        var isColumnBased = items.Count(x => x.Cell.Y == cmpr.Cell.Y) == items.Count;
        return isColumnBased;
    }
}
