global using DynMembers = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
global using DynMemberTypes = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
global using System.Drawing;
global using System.Numerics;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using DotNext;
global using ImGuiNET;
global using static Raylib_cs.Color;
global using RayColor = Raylib_cs.Color;
global using static Raylib_cs.Raylib;
global using System.Text.RegularExpressions;
global using NoAlloq;
using DotNext.Collections.Generic;
using Match_3.GameTypes;
using Rectangle = Raylib_cs.Rectangle;


namespace Match_3;

public static class Utils
{
    public static readonly Random Randomizer = new(DateTime.UtcNow.Ticks.GetHashCode());
    public static readonly FastNoiseLite NoiseMaker = new(DateTime.UtcNow.Ticks.GetHashCode());

    private const byte Min = (int)KnownColor.AliceBlue;
    private const byte Max = (int)KnownColor.YellowGreen;
    private const int TrueColorCount = Max - Min;

    private static readonly RayColor[] All = new RayColor[TrueColorCount];

    public static Vector4 ToVec4(this Color color)
    {
        return new(
            color.R / 255.0f,
            color.G / 255.0f,
            color.B / 255.0f,
            color.A / 255.0f);
    }


    
    public static Color ToColor(this Vector4 color) => 
        Color.FromArgb((int)(color.W * 255), (int)(color.X * 255), (int)(color.Y * 255), (int)(color.Z * 255));

    public static Vector4 ToVec4(this RayColor color)
    {
        return new(
            color.r / 255.0f,
            color.g / 255.0f,
            color.b / 255.0f,
            color.a / 255.0f);
    }

    public static RayColor AsRayColor(this Color color) => new(color.R, color.G, color.B, color.A);

    public static Color AsSysColor(this RayColor color) => Color.FromArgb(color.a, color.r, color.g, color.b);

    public static RayColor GetRndColor() => All[Randomizer.Next(0, TrueColorCount)];

    public static Rectangle AsIntRayRect(this RectangleF floatBox) =>
        new(floatBox.X, floatBox.Y, floatBox.Width, floatBox.Height);

    static Utils()
    {
        All.AsSpan().Shuffle(Randomizer);

        NoiseMaker.SetFrequency(25f);
        NoiseMaker.SetFractalType(FastNoiseLite.FractalType.Ridged);
        NoiseMaker.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    }

    public static Span<T> Writable<T>(this ReadOnlySpan<T> readOnlySpan) =>
        MemoryMarshal.CreateSpan(ref Unsafe.AsRef(readOnlySpan[0]), readOnlySpan.Length);
    
    public static float Trunc(this float value, int digits)
    {
        float mult = MathF.Pow(10.0f, digits);
        float result = MathF.Truncate(mult * value) / mult;
        return result < 0 ? -result : result;
    }

    public static Vector2 GetScreenCoord() => new(GetScreenWidth(), GetScreenHeight());

    public static bool IsMoreThanHalf()
    {
        var val = Randomizer.NextSingle();
        return val.GreaterOrEqual(0.50f, 0.001f);
    }

    public static Span<T> TakeRndItemsAtRndPos<T>(this Span<T> items)
        where T : unmanaged
    {
        int offset = Randomizer.Next(0, items.Length - 1);
        int len = items.Length;
        int amount2Take = Game.Level.ID switch
        {
            0 => Randomizer.Next(2, 4),
            1 => Randomizer.Next(5, 7),
            2 => Randomizer.Next(7, 10),
            _ => throw new ArgumentOutOfRangeException(nameof(Game.Level.ID))
        };

        return offset + amount2Take < len
            ? items.Slice(offset, amount2Take)
            : items[offset..^1];
    }

    private static bool IsEmpty(this RectangleF rayRect) =>
        /* rayRect.x == 0 && rayRect.y == 0 &&*/ rayRect is { Width: 0, Height: 0 };

    public static readonly RectangleF InvalidRect = new(-1, -1, 0, 0);
    public static Vector2 InvalidCell { get; } = -Vector2.One; //this will be computed only once!

    public static void Add(ref this RectangleF a, RectangleF b)
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
        float width = a.Width;
        float height = a.Height;

        //we know that: a) the direction and b)
        if (pair.isRow)
        {
            //a=10, b=10, result= a + b * 1
            width = (a.Width + b.Width);
        }
        else
        {
            height = (a.Height + b.Height);
        }

        a = new(first.X, first.Y, width, height);
    }

    public static string ToStr(this RectangleF rayRect)
        => $"x:{rayRect.X} y:{rayRect.Y}  width:{rayRect.Width}  height:{rayRect.Height}";

    public static RectangleF RelativeToMap(this RectangleF cellRect)
    {
        return new(cellRect.X * Tile.Size,
            cellRect.Y * Tile.Size,
            cellRect.Width * Tile.Size,
            cellRect.Height * Tile.Size);
    }

    public static RectangleF RelativeToGrid(this RectangleF worldRect)
    {
        return new(worldRect.X / Tile.Size,
            worldRect.Y / Tile.Size,
            worldRect.Width / Tile.Size,
            worldRect.Height / Tile.Size);
    }

    public static RectangleF DoScale(this RectangleF rayRect, Scale factor)
    {
        return rayRect with
        {
            Width = (rayRect.Width * factor.GetFactor()),
            Height = (rayRect.Height * factor.GetFactor())
        };
    }

    public static bool Equals(this float x, float y, float tolerance)
    {
        var diff = MathF.Abs(x - y);
        return diff <= tolerance ||
               diff <= MathF.Max(MathF.Abs(x), MathF.Abs(y)) * tolerance;
    }

    public static bool Equals(this int x, int y, int tolerance)
    {
        var diff = MathF.Abs(x - y);
        return diff <= tolerance ||
               diff >= Math.Max(Math.Abs(x), Math.Abs(y));
    }

    private static bool GreaterOrEqual(this float x, float y, float tolerance)
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

    private static Vector2 GetWorldPos(this RectangleF a) => new(a.X, a.Y);
    public static Vector2 GetCellPos(this RectangleF a) => GetWorldPos(a) / Tile.Size;

    public static void SetMouseToWorldPos(Vector2 position, int scale = Tile.Size)
    {
        SetMousePosition((int)position.X * scale, (int)position.Y * scale);
    }

    public static RectangleF NewWorldRect(Vector2 begin, int width, int height)
    {
        return new(begin.X * Tile.Size,
            begin.Y * Tile.Size,
            width * Tile.Size,
            height * Tile.Size);
    }
}