using System.Numerics;

namespace Match_3;

public struct IntVector2
{
    public int X { get; set; }
    public int Y { get; set; }

    public IntVector2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public IntVector2(int v)
    {
        X = v;
        Y = v;
    }

    public static bool operator ==(IntVector2 left, IntVector2 right)
    {
        return left.X == right.X && left.Y == right.Y;
    }

    public static bool operator !=(IntVector2 left, IntVector2 right)
    {
        return left.X != right.X || left.Y != right.Y;
    }

    public static IntVector2 operator +(IntVector2 left, IntVector2 right)
    {
        return new IntVector2(left.X + right.X, left.Y + right.Y);
    }

    public static IntVector2 operator -(IntVector2 left, IntVector2 right)
    {
        return new IntVector2(left.X - right.X, left.Y - right.Y);
    }

    public static IntVector2 operator *(IntVector2 left, int right)
    {
        return new IntVector2(left.X * right, left.Y * right);
    }

    public static IntVector2 operator /(IntVector2 left, int right)
    {
        return new IntVector2(left.X / right, left.Y / right);
    }

    public static IntVector2 operator %(IntVector2 left, int right)
    {
        return new IntVector2(left.X % right, left.Y % right);
    }

    public static IntVector2 operator +(IntVector2 left, int right)
    {
        return new IntVector2(left.X + right, left.Y + right);
    }

    public static IntVector2 operator -(IntVector2 left, int right)
    {
        return new IntVector2(left.X - right, left.Y - right);
    }

    public static IntVector2 operator *(IntVector2 left, IntVector2 right)
    {
        return new IntVector2(left.X * right.X, left.Y * right.Y);
    }

    public static IntVector2 operator /(IntVector2 left, IntVector2 right)
    {
        return new IntVector2(left.X / right.X, left.Y / right.Y);
    }

    public static IntVector2 operator -(IntVector2 value)
    {
        return new IntVector2(-value.X, -value.Y);
    }

    public static IntVector2 One => new(1);
    public static IntVector2 Zero => new(0);
    public static IntVector2 UnitX => new(1, 0);
    public static IntVector2 UnitY => new(0, 1);
    public static IntVector2 MinValue => new(int.MinValue);
    public static IntVector2 MaxValue => new(int.MaxValue);

    public static IntVector2 Abs(IntVector2 value)
    {
        return new IntVector2(Math.Abs(value.X), Math.Abs(value.Y));
    }

    public static IntVector2 Min(IntVector2 value1, IntVector2 value2)
    {
        return new IntVector2(Math.Min(value1.X, value2.X), Math.Min(value1.Y, value2.Y));
    }

    public static IntVector2 Max(IntVector2 value1, IntVector2 value2)
    {
        return new IntVector2(Math.Max(value1.X, value2.X), Math.Max(value1.Y, value2.Y));
    }

    public static Vector2 operator *(Vector2 left, IntVector2 right)
    {
        return new Vector2(left.X * right.X, left.Y * right.Y);
    }

    private static Dictionary<IntVector2, string> _stringCache = new();
    public override string ToString()
    {
        if (!_stringCache.TryGetValue(this, out var result))
        {
            result = $"<{X}, {Y}>";
            _stringCache.Add(this, result);
        }

        return result;
    }

    public bool Equals(IntVector2 other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is IntVector2 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}