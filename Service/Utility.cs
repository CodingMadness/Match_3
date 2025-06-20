global using static Raylib_cs.Color;
global using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
global using DAMTypes = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNext.Runtime.InteropServices;
using Match_3.DataObjects;

namespace Match_3.Service;

public static class SpanUtility
{
    public static readonly Random Randomizer = new(DateTime.UtcNow.Ticks.GetHashCode());

    public static void Replace(this ReadOnlySpan<char> input,
        ReadOnlySpan<char> oldValue,
        ReadOnlySpan<char> newValue)
    {
        int oldValueLen = oldValue.Length;

        if (oldValueLen == 0)
            throw new ArgumentException("Old value could not be found!", nameof(oldValue));

        var span = input.Mutable();
        var oldVal = oldValue.Mutable();
        var newVal = newValue.Mutable();
        int newValueLen = newVal.Length;
        int searchIndex = 0;

        if (oldValueLen >= newValueLen)
        {
            int matchIndex;

            while ((matchIndex = span[searchIndex..].IndexOf(oldValue)) != -1)
            {
                searchIndex += matchIndex;
                var tmpOld = span.Slice(searchIndex, oldVal.Length);
                //copy parts of newValue  to oldValue

                newVal.CopyTo(tmpOld[..newValueLen]);
                tmpOld[newValueLen..].Clear();
            }
        }
    }

    public static Span<T> Mutable<T>(this scoped in ReadOnlySpan<T> readOnlySpan) =>
        MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in readOnlySpan[0]), readOnlySpan.Length);

    public static Span<T> Mutable<T>(this in View<T> readOnlySpan) => ((ReadOnlySpan<T>)readOnlySpan).Mutable();

    public static ref T RefValue<T>(in T? nullable) where T : struct =>
        ref Unsafe.AsRef(in Nullable.GetValueRefOrDefaultRef(in nullable));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Move2<T>(this scoped ReadOnlySpan<T> input, Range slice2Move, int newPos)
        where T : unmanaged, IEquatable<T>
    {
        var (_, length) = slice2Move.GetOffsetAndLength(input.Length);
        var areaToCopyInto = input.Slice(newPos, length);
        input[slice2Move].CopyTo(areaToCopyInto.Mutable());
        return newPos + length;
    }

    private static int Internal_MoveBy<T>(this scoped ReadOnlySpan<T> input,
        Range area2Move, int moveBy,
        T fillEmpties = default)
        where T : struct, IEquatable<T>
    {
        var source = input.Mutable();
        var (offset, length) = area2Move.GetOffsetAndLength(source.Length);
        int newOffset = offset + moveBy;
        Range areaToCopyInto = newOffset..(length + newOffset);
        source[area2Move].CopyTo(source[areaToCopyInto]);

        int endOfArea2Move;
        int begin2Clear;

        if (moveBy < 0)
        {
            endOfArea2Move = offset + length;
            //go "moveBy" back
            begin2Clear = endOfArea2Move + moveBy;
        }
        else
        {
            //go "moveBy" forward
            begin2Clear = offset;
            endOfArea2Move = begin2Clear + moveBy;
        }

        Range area2Clear = begin2Clear..endOfArea2Move;
        source[area2Clear].Fill(fillEmpties);

        return newOffset + length;
    }

    public static Span<T> TakeRndItemsAtRndPos<T>(this Span<T> items, int leveliD) where T : unmanaged
    {
        if (items.Length < 2)
            throw new ArgumentException("Span must have at least 2 items", nameof(items));

        var random = new Random(DateTime.UtcNow.Ticks.GetHashCode());

        // Clamp levelID to ensure it doesn't force a slice bigger than the span
        leveliD = Math.Max(1, Math.Min(leveliD, items.Length / 2));

        // Higher levelID = bigger slice (but never more than half the span)
        int maxPossibleTake = Math.Max(2, Math.Min(items.Length / 2, 2 + leveliD));
        int takeAmount = random.Next(2, maxPossibleTake + 1);

        // Ensure we don't go out of bounds when choosing a start position
        int maxStart = items.Length - takeAmount;
        int startPos = random.Next(0, maxStart + 1);

        return items.Slice(startPos, takeAmount);
    }

    public static void Shuffle<T>(this Span<T> span)
    {
    }

    public static unsafe string FirstLetter2Upper(this string input)
    {
        fixed (char* p = input)
        {
            *p = char.ToUpper(*p);
        }

        return input;
    }
}

public static class BaseTypeUtility
{

    public static readonly Random Randomizer = new(DateTime.UtcNow.Ticks.GetHashCode());

    public static bool Equals(this float x, float y, float tolerance)
    {
        var diff = MathF.Abs(x - y);
        return diff <= tolerance ||
               diff <= MathF.Max(MathF.Abs(x), MathF.Abs(y)) * tolerance;
    }

    public static float Trunc(this float value, int digits)
    {
        float mult = MathF.Pow(10.0f, digits);
        float result = MathF.Truncate(mult * value) / mult;
        return result < 0 ? -result : result;
    }

    //Need to have this method because we cannot modify a return value of a tuple directly!
    public static void SetCount(this (int, float) tuple, int value)
    {
        tuple.Item1 = value;
    }

    public static void IncCount(this (int, float) tuple, int value2IncreaseBy)
    {
        tuple.Item1 += value2IncreaseBy;
    }
 
}