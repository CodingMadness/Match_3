using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.Service;

namespace Match_3.Datatypes;

/// <summary>
/// write a custom struct which has 2 spans as fields and have the ability to:
///  * compare the 2 spans
///  * can get back the relative order of both
///  * can check if they point within the same memory block
///  * can get back who is larger than the other, in length
///  * can retrieve for both the actual 
/// </summary>
public readonly unsafe ref struct SpanInfo<T>
    where T : unmanaged, IEquatable<T>, IComparable<T>, INumber<T>
{
    public SpanInfo(scoped in ReadOnlySpan<T> src, 
                    scoped in ReadOnlySpan<T> x,
                    scoped in ReadOnlySpan<T> y)
    {
        //1 of these at least is empty which is bad!
        if (src == ReadOnlySpan<T>.Empty || x == ReadOnlySpan<T>.Empty || y == ReadOnlySpan<T>.Empty)
           throw new ArgumentException("ALL of the spans MUST be valid");

        nint adrOfX = (nint)Unsafe.AsPointer(ref x.AsWriteable()[0]);
        nint adrOfY = (nint)Unsafe.AsPointer(ref y.AsWriteable()[0]);
        long x2Y = Math.Abs(adrOfX - adrOfY) / sizeof(T);

        //its same or invalid address
        if (x2Y <= 0)
            throw new ArgumentException("spans cannot be the same or empty");

        nint adrOfFirst = (nint)Unsafe.AsPointer(ref src.AsWriteable()[0]);
        nint adrOfLast = (nint)Unsafe.AsPointer(ref src.AsWriteable()[^1]);
        long totalLen = Math.Abs(adrOfFirst - adrOfLast) / sizeof(T);

        //check if just 1 is within same range!, if not,
        //then the entire method based on this struct will be fruitless
        bool sameRange = adrOfX <= totalLen;

        First = sameRange && adrOfX < adrOfY ? x : y;
        Last = sameRange && adrOfX < adrOfY ? y : x;

        IndexOfFirst = (int)(adrOfFirst / sizeof(T));
        IndexOfLast = (int)(adrOfLast / sizeof(T));

        //when they are really close and only split by a delimiter from each other
        //then the addition of idxOfFirst + firstLen + sizeof(T) should be same as IndexOfLast 
        AreNext2EachOther = IndexOfLast == IndexOfFirst + First.Length + sizeof(T) * 1;
    }
    
    public ReadOnlySpan<T> First { get; private init; }
    public ReadOnlySpan<T> Last { get; private init; }

    public int IndexOfFirst { get; } 
    public int IndexOfLast { get; } 
    
    public bool AreNext2EachOther { get; }
}