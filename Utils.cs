using System.Drawing;
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

    
    public  static bool IsEmpty(this Rectangle rayRect) => 
        rayRect.x < 0 && rayRect.y < 0 && rayRect.width == 0 && rayRect.height == 0;

    public static readonly Rectangle EMPTY = new(-1f, -1f, 0f, 0f); 
    public static Rectangle Add(this Rectangle a, Rectangle b)
    {
        if (a.IsEmpty())
        {
            a = b;
            return a;
        }
        if (b.IsEmpty())
        {
            b = a;
            return b;
        }
    
        Vector2 first = a.GetBegin();
        Vector2 other = b.GetBegin();
        (Vector2 Direction, bool isRow) pair = first.GetDirectionTo(other);
        float width = a.width;
        float height = a.height;
        
        //we know that: a) the direction and b)
        if (pair.isRow)
        {
            //a=10, b=10, result= a + b * 1
            width = ((a.width + b.width) * pair.Direction).X;
        }
        else
        {
            height = ((a.height + b.height) * pair.Direction).Y;
        }

        return new(first.X, first.Y, width, height);
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

    public static (Vector2 Direction, bool isRow) GetDirectionTo(this Vector2 first, Vector2 next)
    {
        bool sameRow = (int)first.Y == (int)next.Y;
        
        //switch on direction
        if (sameRow)
        {
            //the difference is positive
            if (first.X < next.X)
                return (Vector2.UnitX, sameRow);
            
            if (first.X > next.X)
                return (-Vector2.UnitX, sameRow);
        }
        //switch on direction
        else
        {
            //the difference is positive
            if (first.Y < next.Y)
                return (Vector2.UnitY, sameRow);
            
            if (first.Y > next.Y)
                return (-Vector2.UnitY, sameRow);
        }

        return (-Vector2.One, false);
    }

    public static Vector2 GetOpposite(this Vector2 a, Vector2 b)
    {
        var pair = a.GetDirectionTo(b);
        
        if (pair.isRow)
        {
            if (pair.Direction == -Vector2.UnitX)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return a + Vector2.UnitX;
            }
            if (pair.Direction == Vector2.UnitX)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return a - Vector2.UnitX;
            }
        }
        else 
        {
            if (pair.Direction == -Vector2.UnitY)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return a + Vector2.UnitY;
            }
            if (pair.Direction == Vector2.UnitY)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return a - Vector2.UnitY;
            }
        }

        throw new ArgumentException("this line should never be reached!");
    }

    public static bool CompletelyDifferent(this Vector2 a, Vector2 b)
    {
        return  ((int)a.X != (int)b.X && (int)a.Y != (int)b.Y) ;
    }
    
    public static Vector2 GetBegin(this Rectangle rayRect) => new(rayRect.x, rayRect.y);

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
        var isColumnBased = items.Count(x => (int)x.GridCell.Y == (int)cmpr.GridCell.Y) == items.Count;
        return isColumnBased;
    }
}
