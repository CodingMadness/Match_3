using System.Numerics;
using System.Runtime.InteropServices;
using DotNext.Runtime;

namespace Match_3.Datatypes;

[StructLayout(LayoutKind.Auto)]
public readonly unsafe ref struct SpanInfo<T>
    where T : unmanaged, IEquatable<T>, IComparable<T>, INumber<T>
{
    private readonly int SrcLength;
    public readonly ReadOnlySpan<T> First, Between, Last;
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
        static T GetEmptyValue()
        {
            T tmp = default;

            return tmp switch
            {
                char => T.CreateSaturating((char)32),
                _ => tmp
            };
        }
        
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
        nint adrOfFirst = Intrinsics.AddressOf(First[0]);
        nint adrOfLast = Intrinsics.AddressOf(Last[0]);

        IsFirstLargerThanLast = First.Length > Last.Length;

        IndexOfFirst = (int)Math.Abs(adrOfAbsFirst - adrOfFirst) / sizeof(T);
        IndexOfLast = (int)Math.Abs(adrOfAbsFirst - adrOfLast) / sizeof(T);

        //when they are really close and only split by a delimiter from each other
        //then the addition of idxOfFirst + firstLen + sizeof(T) should be same as IndexOfLast
        AreXYNext2EachOther = IndexOfLast == IndexOfFirst + First.Length + sizeof(T) * 1;
        LengthDiff = Math.Abs(First.Length - Last.Length);
        AreSameLength = LengthDiff == 0;
        int endOfFirstOne = IndexOfFirst + First.Length;
        /*            
            int startOfNextOne = src[endOfFirstOne..IndexOfLast].IndexOf(GetEmptyValue()) + 1;
        */
        Between = AreXYNext2EachOther
            ? src[endOfFirstOne..IndexOfLast]
            : ReadOnlySpan<T>.Empty; //src.Slice(endOfFirstOne, startOfNextOne);

        if (First.Length < Last.Length)
        {
            SmallOne = IndexOfFirst..(IndexOfFirst + First.Length);
            LargeOne = IndexOfLast..(IndexOfLast + Last.Length);
        }
        else
        {
            LargeOne = IndexOfFirst..(IndexOfFirst + First.Length);
            SmallOne = IndexOfLast..(IndexOfLast + Last.Length);
        }
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