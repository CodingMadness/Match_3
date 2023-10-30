using System.Runtime.InteropServices;

namespace Match_3.StateHolder;

[StructLayout(LayoutKind.Auto)]
public readonly ref struct SpanInfo<T>
    where T : unmanaged, IEquatable<T>
{
    public readonly int SrcLength;
    public readonly ReadOnlySpan<T> First, Between, Last;
    public readonly int IndexOfFirst;
    public readonly int IndexOfLast;
    public readonly bool AreSameLength;
    public readonly bool IsLastAtEnd;
    public readonly bool AreXYNext2EachOther;
    public readonly bool IsFirstLargerThanLast;
    public readonly int LengthDiff;
    public readonly Area<T> LargeOneArea, SmallOneArea;
    public readonly bool IsImpossible2Swap;

    public SpanInfo(ReadOnlySpan<T> src, Range x, Range y)
    {
        SrcLength = src.Length;

        Area<T> areaX = new(x, SrcLength);
        Area<T> areaY = new(y, SrcLength);

        IsImpossible2Swap = src == ReadOnlySpan<T>.Empty || areaX == areaY;

        if (IsImpossible2Swap)
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
            
            IsLastAtEnd = SrcLength - y.End.Value == 0;
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
            
            IsLastAtEnd = SrcLength - areaX.End == 0;
        }

      
        //when they are really close and only split by a delimiter from each other
        //then the addition of idxOfFirst + firstLen + sizeof(T) should be same as IndexOfLast
        int endOfFirstOne = IndexOfFirst + First.Length;
        AreXYNext2EachOther = IndexOfLast == endOfFirstOne + 1;
        LengthDiff = Math.Abs(First.Length - Last.Length);
        AreSameLength = LengthDiff == 0;

        Between = AreXYNext2EachOther
            ? src[endOfFirstOne..IndexOfLast]
            : ReadOnlySpan<T>.Empty;
    }
}