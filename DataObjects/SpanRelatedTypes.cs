using System.Runtime.InteropServices;

namespace Match_3.DataObjects;

public record Distance(Range Breadth, int SourceLength)
{
    private (int offset, int length) tplRange => Breadth.GetOffsetAndLength(SourceLength);

    public int Start => tplRange.offset;
    public int End => Start + tplRange.length;

    public Distance(int Start, int Length, int SourceLength)
        : this(new Range(Start, Start + Length), SourceLength)
    {

    }

    public Distance GetSlice(Distance other)
    {
        int diff = Math.Abs(SourceLength - other.SourceLength);
        Distance newOne = this - diff;
        return newOne;
    }

    public static Distance operator +(Distance self, int incRangeBy)
    {
        var copy = new Distance(self.Start..(self.End + incRangeBy), self.SourceLength);
        return copy;
    }

    public static Distance operator -(Distance self, int incRangeBy)
    {
        var copy = new Distance(self.Start..(self.End - incRangeBy), self.SourceLength);
        return copy;
    }

    public static bool operator >(Distance self, Distance other)
        => Math.Abs(self.Start - self.End) > Math.Abs(other.Start - other.End);

    public static bool operator <(Distance self, Distance other) => !(self > other || self == other);

    public static implicit operator Range(Distance self) => self.Start..self.End;

    public int Overlaps(Distance other)
    {
        bool isOverlap = Start < other.End && other.Start < other.End;
        return isOverlap ? other.End - other.Start : 0;
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
    public readonly Distance LargeOneArea = null!, SmallOneArea = null!;

    public SpanInfo(ReadOnlySpan<T> src, Range x, Range y)
    {
        var srcLength = src.Length;

        Distance areaX = new(x, srcLength);
        Distance areaY = new(y, srcLength);

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