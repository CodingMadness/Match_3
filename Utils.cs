﻿using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Raylib_CsLo.Raylib;
using Rectangle = Raylib_CsLo.Rectangle;

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

    public static Rectangle Union(this Rectangle rayRect, Rectangle otherRayRect)
    {
        var rect1 = new System.Drawing.Rectangle((int)rayRect.x, (int)rayRect.y, (int)rayRect.width, (int)rayRect.height);
        var rect2 = new System.Drawing.Rectangle((int)otherRayRect.x, (int)otherRayRect.y, (int)otherRayRect.width, (int)otherRayRect.height);
        var union = RectangleF.Union(rect1, rect2);
        return new Rectangle(union.X, union.Y, union.Width, union.Height);
    }

    private static void Clean(ref this Rectangle rayRect, ref Rectangle otherRayRect)
    {
        float firstWidth = rayRect.width == 0 ? otherRayRect.width : rayRect.width;
        float otherWidth = otherRayRect.width == 0 ? rayRect.width : otherRayRect.width;
        
        float firstHeight = rayRect.height == 0 ? otherRayRect.height : rayRect.height;
        float otherHeight = otherRayRect.height == 0 ? rayRect.height : otherRayRect.height;
        
        float firstX = rayRect.x == 0 ? otherRayRect.x : rayRect.x;
        float otherX = otherRayRect.x == 0 ? rayRect.x : otherRayRect.x;
        
        float firstY = rayRect.y == 0 ? otherRayRect.y : rayRect.y;
        float otherY = otherRayRect.y == 0 ? rayRect.y : otherRayRect.y;

        rayRect = new(firstX, firstY, firstWidth, firstY);
        otherRayRect = new(otherX, otherY, otherWidth, otherHeight);

    }
    
    public  static bool IsEmpty(this Rectangle rayRect) => 
        rayRect.x == 0 && rayRect.y == 0 && rayRect.width == 0 && rayRect.height == 0; 
    
    public static Rectangle Add(this Rectangle rayRect, Rectangle otherRayRect)
    {
        if (rayRect.IsEmpty())
        {
            rayRect = otherRayRect;
            return rayRect;
        }
        if (otherRayRect.IsEmpty())
        {
            otherRayRect = rayRect;
            return otherRayRect;
        }
    
        Vector2 first = rayRect.ToWorldCoord();
        Vector2 other = otherRayRect.ToWorldCoord();
        bool sameRow = first.IsSameRow(other);
        float x = sameRow ? rayRect.x + otherRayRect.width : rayRect.x;
        float y = sameRow ? rayRect.y : rayRect.y + otherRayRect.height;
        float width = sameRow ? rayRect.width + otherRayRect.width : rayRect.width;
        float height = sameRow ? rayRect.height : otherRayRect.height + rayRect.height;
        return new Rectangle(x, y, width, height);
    }
    
    public static string ToStr(this Rectangle rayRect)
        => $"x:{rayRect.x} y:{rayRect.y}  width:{rayRect.width}  height:{rayRect.height}";
    public static Rectangle Divide(this Rectangle rayRect, int divisor)
    {
        //rayrect 
        return new(rayRect.x, rayRect.y, rayRect.width / divisor, rayRect.height / divisor);
    }
    
    public static Rectangle Move(this Rectangle rayRect, bool xDirection, int steps=1)
    {
        if (steps < 2)
            return rayRect;
        
        Rectangle tmp;
        
        if (xDirection)
        {
            //{0,0, 64, 64}  ---> {64 ,0, 64, 64}  ---> {128 ,0, 64, 64}  
            tmp = new(rayRect.x + rayRect.width * steps, rayRect.y, rayRect.width, rayRect.height);
        }
        else
        {
            //{0,0, 64, 64}  ---> {0 ,64, 64, 64}  ---> {0 ,128, 64, 64}  
            tmp = new(rayRect.x, rayRect.y + rayRect.width * steps, rayRect.width, rayRect.height);
        }

        return tmp;
    }

    public static bool IsSameRow(this Vector2 first, Vector2 next)
    {
        return (int)first.Y == (int)next.Y;
    }
    
    public static Vector2 ToWorldCoord(this Rectangle rayRect) => new(rayRect.x, rayRect.y);

    public static void SetMousePos(Vector2 position, int scale = Tile.Size)
    {
        SetMousePosition((int)position.X * scale, (int)position.Y * scale);
    }
    
    public static unsafe nint GetAddrOfObject<ObjectT>(this ObjectT Object) where ObjectT: class
    {
        return (nint) Unsafe.AsPointer(ref Unsafe.As<StrongBox<byte>>(Object).Value);
    }

    public static Rectangle GetMatch3Rect(Vector2 begin, int width, int height)
    {
        return new((int)begin.X * Tile.Size,
            (int)begin.Y * Tile.Size,
            width * Tile.Size,
            height* Tile.Size);
    }

    public static bool IsRowBased(this ISet<Tile> items) 
    {
        Tile cmpr = items.ElementAt(0);
        var isColumnBased = items.Count(x => (int)x.Cell.Y == (int)cmpr.Cell.Y) == items.Count;
        return isColumnBased;
    }
}
