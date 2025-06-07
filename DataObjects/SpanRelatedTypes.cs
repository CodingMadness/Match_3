using System.Runtime.InteropServices;

namespace Match_3.DataObjects;

public readonly struct Distance<T> : IEquatable<Distance<T>>, IComparable<Distance<T>>
    where T : unmanaged, IEquatable<T>
{
    public override int GetHashCode() => Start.GetHashCode();

    private readonly int _srcLen;
    public readonly int Start;
    private readonly int _length;
    public readonly int End;

    public Distance(Range r, int srcLen)
    {
        _srcLen = srcLen;
        (int offset, int len) = r.GetOffsetAndLength(srcLen);
        Start  = offset;
        _length = len;
        End    = offset + len;
    }

    public Distance<T> GetSlice(Distance<T> other)
    {
        int diff = Math.Abs(_length - other._length);
        Distance<T> newOne = this - diff;
        return newOne;
    }

    public (int start, int length, int end) Deconstruct() => (Start, _length, End);

    public bool Equals(Distance<T> other) => Start == other.Start;

    public int CompareTo(Distance<T> other) => Start.CompareTo(other.Start);

    public override string ToString() => ((Range)this).ToString();

    public static Distance<T> operator +(Distance<T> self, int incRangeBy)
    {
        var copy = new Distance<T>(self.Start..(self.End + incRangeBy), self._srcLen);
        return copy;
    }

    public static Distance<T> operator -(Distance<T> self, int incRangeBy)
    {
        var copy = new Distance<T>(self.Start..(self.End - incRangeBy), self._srcLen);
        return copy;
    }

    public static bool operator ==(Distance<T> self, Distance<T> other) => other.Equals(self);

    public static bool operator !=(Distance<T> self, Distance<T> other) => !(self == other);

    public static bool operator >(Distance<T> self, Distance<T> other) => self.CompareTo(other) == 1;

    public static bool operator <(Distance<T> self, Distance<T> other) => !(self > other || self == other);

    public static implicit operator Range(Distance<T> self) => self.Start..self.End;

    public int Overlaps(Distance<T> other)
    {
        (int startOther, _, int endOther) = other.Deconstruct();

        bool isOverlap = Start < endOther && startOther < endOther;
        return isOverlap ? endOther - startOther : 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is Distance<T> slice && Equals(slice);
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
    public readonly Distance<T> LargeOneArea, SmallOneArea;

    public SpanInfo(ReadOnlySpan<T> src, Range x, Range y)
    {
        var srcLength = src.Length;

        Distance<T> areaX = new(x, srcLength);
        Distance<T> areaY = new(y, srcLength);

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