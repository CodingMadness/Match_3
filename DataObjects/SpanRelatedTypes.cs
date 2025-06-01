using System.Runtime.InteropServices;

namespace Match_3.DataObjects;

public readonly struct Slice<T> : IEquatable<Slice<T>>, IComparable<Slice<T>>
    where T : unmanaged, IEquatable<T>
{
    public override int GetHashCode() => Start.GetHashCode();

    private readonly int _srcLen;
    public readonly int Start;
    private readonly int _length;
    public readonly int End;

    public Slice(Range r, int srcLen)
    {
        _srcLen = srcLen;
        (int offset, int len) = r.GetOffsetAndLength(srcLen);
        Start  = offset;
        _length = len;
        End    = offset + len;
    }

    public Slice<T> GetSlice(Slice<T> other)
    {
        int diff = Math.Abs(_length - other._length);
        Slice<T> newOne = this - diff;
        return newOne;
    }

    public (int start, int length, int end) Deconstruct() => (Start, _length, End);

    public bool Equals(Slice<T> other) => Start == other.Start;

    public int CompareTo(Slice<T> other) => Start.CompareTo(other.Start);

    public override string ToString() => ((Range)this).ToString();

    public static Slice<T> operator +(Slice<T> self, int incRangeBy)
    {
        var copy = new Slice<T>(self.Start..(self.End + incRangeBy), self._srcLen);
        return copy;
    }

    public static Slice<T> operator -(Slice<T> self, int incRangeBy)
    {
        var copy = new Slice<T>(self.Start..(self.End - incRangeBy), self._srcLen);
        return copy;
    }

    public static bool operator ==(Slice<T> self, Slice<T> other) => other.Equals(self);

    public static bool operator !=(Slice<T> self, Slice<T> other) => !(self == other);

    public static bool operator >(Slice<T> self, Slice<T> other) => self.CompareTo(other) == 1;

    public static bool operator <(Slice<T> self, Slice<T> other) => !(self > other || self == other);

    public static implicit operator Range(Slice<T> self) => self.Start..self.End;

    public int Overlaps(Slice<T> other)
    {
        (int startOther, _, int endOther) = other.Deconstruct();

        bool isOverlap = Start < endOther && startOther < endOther;
        return isOverlap ? endOther - startOther : 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is Slice<T> slice && Equals(slice);
    }
}

[StructLayout(LayoutKind.Auto)]
public readonly ref struct SpanInfo<T> where T : unmanaged, IEquatable<T>
{
    public readonly ReadOnlySpan<T> First, Between, Last;
    public readonly int IndexOfFirst;
    public readonly int IndexOfLast;
    public readonly bool AreSameLength;
    public readonly bool IsLastAtEnd;
    public readonly bool AreXYNext2EachOther;
    public readonly bool IsFirstLargerThanLast;
    public readonly int LengthDiff;
    public readonly Slice<T> LargeOneArea, SmallOneArea;

    public SpanInfo(ReadOnlySpan<T> src, Range x, Range y)
    {
        var srcLength = src.Length;

        Slice<T> areaX = new(x, srcLength);
        Slice<T> areaY = new(y, srcLength);

        var isImpossible2Swap = src == ReadOnlySpan<T>.Empty || areaX == areaY;

        if (isImpossible2Swap)
            return;

        if (areaX < areaY)
        {
            First = src[x];
            Last = src[y];

            if (First.Length < Last.Length)
            {
                IsFirstLargerThanLast = false;
                LargeOneArea = areaY;
                SmallOneArea = areaX;
            }
            else if (First.Length > Last.Length)
            {
                IsFirstLargerThanLast = true;
                LargeOneArea = areaX;
                SmallOneArea = areaY;
            }

            IndexOfFirst = areaX.Start;
            IndexOfLast = areaY.Start;

            IsLastAtEnd = srcLength - y.End.Value == 0;
        }

        if (areaX > areaY)
        {
            First = src[y];
            Last = src[x];

            if (First.Length < Last.Length)
            {
                IsFirstLargerThanLast = false;
                LargeOneArea = areaX;
                SmallOneArea = areaY;
            }
            else if (First.Length > Last.Length)
            {
                IsFirstLargerThanLast = true;
                LargeOneArea = areaY;
                SmallOneArea = areaX;
            }

            IndexOfFirst = areaY.Start;
            IndexOfLast = areaX.Start;

            IsLastAtEnd = srcLength - areaX.End == 0;
        }

        //when they are really close and only split by a delimiter from each other
        //then the addition of idxOfFirst + firstLen + sizeof(T) should be same as IndexOfLast
        int endOfFirstOne = IndexOfFirst + First.Length;
        AreXYNext2EachOther = IndexOfLast == endOfFirstOne + 1;
        LengthDiff = Math.Abs(First.Length - Last.Length);
        AreSameLength = LengthDiff == 0;

        Between = AreXYNext2EachOther
            ? src[endOfFirstOne..IndexOfLast]
            : [];
    }
}