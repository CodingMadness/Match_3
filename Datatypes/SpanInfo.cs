using System.Numerics;
using System.Runtime.InteropServices;
using DotNext.Runtime;

namespace Match_3.Datatypes;

/// <summary>
/// write a custom struct which has 2 spans as fields and have the ability to:
///  * compare the 2 spans
///  * can get back the relative order of both
///  * can check if they point within the same memory block
///  * can get back who is larger than the other, in length
///  * can retrieve for both the actual 
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly unsafe ref struct SpanInfo<T>
    where T : unmanaged, IEquatable<T>, IComparable<T>, INumber<T>
{
    private readonly int SrcLength;
    public readonly ReadOnlySpan<T> First;
    public readonly ReadOnlySpan<T> Last;
    public readonly int IndexOfFirst;
    public readonly int IndexOfLast;
    public readonly bool AreXYNext2EachOther;
    public readonly bool AreSameLength;
    public readonly bool IsFirstLargerThanLast;
    public readonly int LengthDiff;

    public readonly Range LargeOne, SmallOne;

    public SpanInfo(scoped in ReadOnlySpan<T> src,
        scoped in ReadOnlySpan<T> x,
        scoped in ReadOnlySpan<T> y)
    {
        //1 of these at least is empty which is bad!
        if (src == ReadOnlySpan<T>.Empty || x == ReadOnlySpan<T>.Empty || y == ReadOnlySpan<T>.Empty)
            throw new ArgumentException("ALL of the spans MUST be valid");

        SrcLength = src.Length;

        nint adrOfX = Intrinsics.AddressOf(x[0]);
        nint adrOfY = Intrinsics.AddressOf(y[0]);
        long x2Y = Math.Abs(adrOfX - adrOfY) / sizeof(T);

        //its same or invalid address
        if (x2Y <= 0)
            throw new ArgumentException("spans cannot be the same or empty");

        nint adrOfAbsFirst = Intrinsics.AddressOf(src[0]);
        nint adrOfAbsLast = Intrinsics.AddressOf(src[^1]);
        long totalLen = Math.Abs(adrOfAbsFirst - adrOfAbsLast) / sizeof(T);

        //check if just 1 is within same range!, if not,
        //then the entire method based on this struct will be fruitless
        var sameMemory = (uint)x2Y <= (uint)totalLen;

        if (!sameMemory)
            throw new ArgumentException("x and y are not pointing to the same memory region!");

        First = adrOfX < adrOfY ? x : y;
        Last = adrOfX < adrOfY ? y : x;

        IsFirstLargerThanLast = First.Length > Last.Length;
        
        IndexOfFirst = (int)Math.Abs(adrOfAbsFirst - adrOfX) / sizeof(T);
        IndexOfLast = (int)Math.Abs(adrOfAbsFirst - adrOfY) / sizeof(T);
        //when they are really close and only split by a delimiter from each other
        //then the addition of idxOfFirst + firstLen + sizeof(T) should be same as IndexOfLast 
        AreXYNext2EachOther = IndexOfLast == IndexOfFirst + First.Length + sizeof(T) * 1;
        LengthDiff = Math.Abs(First.Length - Last.Length);
        AreSameLength = LengthDiff == 0;

        SmallOne = First.Length < Last.Length
            ? IndexOfFirst..(IndexOfFirst + First.Length)
            : IndexOfLast..(IndexOfLast + Last.Length);

        LargeOne = First.Length > Last.Length
            ? IndexOfFirst..(IndexOfFirst + First.Length)
            : IndexOfLast..(IndexOfLast + Last.Length);
    }

    public (int start, int len, int end) DeconstructLargeOne()
    {
        var r = LargeOne.GetOffsetAndLength(SrcLength);
        return (r.Offset, r.Length, r.Offset + r.Length);
    }

    public (int start, int len, int end) DeconstructSmallOne()
    {
        var r = SmallOne.GetOffsetAndLength(SrcLength);
        return (r.Offset, r.Length, r.Offset + r.Length);
    }
}