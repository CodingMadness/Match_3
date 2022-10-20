using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Raylib_CsLo.Raylib;
using Rectangle = Raylib_CsLo.Rectangle;

namespace Match_3;


public ref struct SpanEnumerator<TItem>
{
    public ref TItem CurrentItem;

    public readonly ref TItem LastItemOffsetByOne;

    public SpanEnumerator(ReadOnlySpan<TItem> Span) : this(ref MemoryMarshal.GetReference(Span), Span.Length)
    {
    }

    public SpanEnumerator(Span<TItem> Span) : this(ref MemoryMarshal.GetReference(Span), Span.Length)
    {
    }

    public SpanEnumerator(ref TItem Item, nint Length)
    {
        CurrentItem = ref Unsafe.Subtract(ref Item, 1);

        LastItemOffsetByOne = ref Unsafe.Add(ref Item, Length);
    }

    [UnscopedRef] public ref TItem Current => ref CurrentItem;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool MoveNext()
    {
        CurrentItem = ref Unsafe.Add(ref CurrentItem, 1);

        return !Unsafe.AreSame(ref CurrentItem, ref LastItemOffsetByOne);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public SpanEnumerator<TItem> GetEnumerator()
    {
        return this;
    }
}


public static class Utils
{
    
    public static  readonly Random Randomizer =  new(DateTime.UtcNow.Ticks.GetHashCode());
    public static readonly FastNoiseLite NoiseMaker = new(DateTime.UtcNow.Ticks.GetHashCode());

    static Utils()
    {
        NoiseMaker.SetFrequency(25f);
        NoiseMaker.SetFractalType(FastNoiseLite.FractalType.Ridged);
        NoiseMaker.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    }
    
    public static float Trunc(this float value, int digits)
    {
        float mult = MathF.Pow(10.0f, digits);
        float result = MathF.Truncate(mult * value) / mult;
        return result < 0 ? -result : result;
        ;
    }

    public static Rectangle ScreenRect = new(0f, 0f, GetScreenWidth(), GetScreenHeight());

    public static Vector2 GetScreenCoord() => new(GetScreenWidth(), GetScreenHeight());

    public static Rectangle Union(this Rectangle rayRect, Rectangle otherRayRect)
    {
        var rect1 = new System.Drawing.Rectangle((int)rayRect.x, (int)rayRect.y, (int)rayRect.width, (int)rayRect.height);
        var rect2 = new System.Drawing.Rectangle((int)otherRayRect.x, (int)otherRayRect.y, (int)otherRayRect.width, (int)otherRayRect.height);
        var union = RectangleF.Union(rect1, rect2);
        return new Rectangle(union.X, union.Y, union.Width, union.Height);
    }

    public static bool RollADice()
    {
        var val = Randomizer.NextSingle();
        return val.GreaterOrEqual(0.50f, 0.001f);
    }
    
    public  static bool IsEmpty(this Rectangle rayRect) => 
       /* rayRect.x == 0 && rayRect.y == 0 &&*/ rayRect.width == 0 && rayRect.height == 0;

    public static readonly Rectangle InvalidRect = new(-1f, -1f, 0f, 0f);
    public static Vector2 InvalidCell => -Vector2.One;
    
    public static void Add(ref this Rectangle a, Rectangle b)
    {
        if (a.IsEmpty())
        {
            a = b;
            return;
        }
        if (b.IsEmpty())
        {
            return;
        }
    
        Vector2 first = a.GetWorldPos();
        Vector2 other = b.GetWorldPos();
        (Vector2 Direction, bool isRow) pair = first.GetDirectionTo(other);
        float width = a.width;
        float height = a.height;
        
        //we know that: a) the direction and b)
        if (pair.isRow)
        {
            //a=10, b=10, result= a + b * 1
            width = (a.width + b.width);
        }
        else
        {
            height = (a.height + b.height);
        }

        a = new(first.X, first.Y, width, height);
    }
    public static string ToStr(this Rectangle rayRect)
        => $"x:{rayRect.x} y:{rayRect.y}  width:{rayRect.width}  height:{rayRect.height}";
    public static Rectangle ToWorldBox(this Rectangle cellRect)
    {
        //rayrect 
        return new(cellRect.x * Tile.Size, 
            cellRect.y * Tile.Size,
            cellRect.width * Tile.Size,
            cellRect.height * Tile.Size);
    }
    public static Rectangle ToGridBox(this Rectangle worldRect)
    {
        //rayrect 
        return new(worldRect.x / Tile.Size, 
            worldRect.y / Tile.Size,
            worldRect.width / Tile.Size,
            worldRect.height / Tile.Size);
    }
    public static Rectangle DoScale(this Rectangle rayRect, Scale factor)
    {
        //rayrect 
        return new(rayRect.x, rayRect.y, rayRect.width * factor.GetFactor(), rayRect.height * factor.GetFactor());
    }
    public static Rectangle SliceBy(this Rectangle rayRect, int factor)
    {
        //rayrect 
        return new(rayRect.x, rayRect.y, rayRect.width * factor, rayRect.height * factor);
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
    
    public static bool Equals(this float x, float y, float tolerance)
    {
        var diff = MathF.Abs(x - y);
        return diff <= tolerance ||
               diff <= MathF.Max(MathF.Abs(x), MathF.Abs(y)) * tolerance;
    }

    public static bool GreaterOrEqual(this float x, float y, float tolerance)
    {
        var diff = MathF.Abs(x - y);
        return diff > tolerance ||
               diff > MathF.Max(MathF.Abs(x), MathF.Abs(y)) * tolerance;
    }

    public static int CompareTo(this Vector2 a, Vector2 b)
    {
        var pair = a.GetDirectionTo(b);
        return pair.isRow ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
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
    public static Vector2 GetWorldPos(this Rectangle a) => new((int)a.x, (int)a.y);
    public static Vector2 GetCellPos(this Rectangle a) => GetWorldPos(a) / Tile.Size;
    public static void SetMouseToWorldPos(Vector2 position, int scale = Tile.Size)
    {
        SetMousePosition((int)position.X * scale, (int)position.Y * scale);
    }
    public static unsafe nint GetAddrOfObject<TObjectT>(this TObjectT @object) where TObjectT: class
    {
        return (nint) Unsafe.AsPointer(ref Unsafe.As<StrongBox<byte>>(@object).Value);
    }
    public static Rectangle NewWorldRect(Vector2 begin, int width, int height)
    {
        return new((int)begin.X * Tile.Size,
            (int)begin.Y * Tile.Size,
            width * Tile.Size,
            height* Tile.Size);
    }
}
