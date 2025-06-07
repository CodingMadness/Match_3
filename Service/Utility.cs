global using static Raylib_cs.Color;
global using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
global using DAMTypes = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    public static ref T RefValue<T>(in T? nullable) where T:  struct =>
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

    /// <summary>
    /// Swaps 2 different slices within the same span!
    /// </summary>
    /// <param name="input">the base span where the swap is reflected </param>
    /// <param name="x">the 1. span to swap with the 2. one</param>
    /// <param name="y">the 2. span to swap with the 1. one</param>
    /// <param name="delimiter">a value which functions as a delimiter to say when a new block of an arbitrary numeric value begins and ends..</param>
    /// <typeparam name="T">The type of the span</typeparam>
    public static void Swap<T>(this scoped ReadOnlySpan<T> input, Range x, Range y, T delimiter = default)
        where T : unmanaged, IEquatable<T>
    {
        //get all the needed information about the occuring spans here!
        scoped var info = new SpanInfo<T>(input, x, y);

        //use the "info" type for the future in this function!.....
        int diffInLength = info.LengthDiff;

        scoped ReadOnlySpan<T>
            first = info.First,
            between = info.Between,
            last = info.Last;

        var source = input.Mutable();

        //NOTE: We have to +1 to every "endOf...." variable because it is EXCLUSIVE inside the "Range", which means 
        //      that END value is excluded from the span, so in order to include it into the range,
        //      we do +1 to get that value 

        if (info.AreSameLength)
        {
            //store a copy of the smaller one
            using var copyBuffer = new SpanPool<T>(last.Length, null);
            var lastCopy = copyBuffer.CorePush(last);

            first.CopyTo(source.Slice(info.IndexOfLast, first.Length));
            lastCopy.CopyTo(source.Slice(info.IndexOfFirst, lastCopy.Length));

            return;
        }

        if (info.AreXYNext2EachOther)
        {
            if (info.IsFirstLargerThanLast)
            {
                //first > last ===> first=largeOne; last=smallOne
                (_, _, int endOfLargeOne) = info.LargeOneArea.Deconstruct();
                (_, int smallOneLen, int endOfSmallOne) = info.SmallOneArea.Deconstruct();

                //TEMP-STEPS:
                //slice "smallOne" 
                var smallOne = last.Mutable();
                //slice "largeOne"
                var largeOne = first.Mutable();
                //make the nessecary slice from the largeOne
                var sliceOfLargeOne = largeOne[..smallOneLen];
                //make the nessecary slice to get the remainder
                var remainderOfLargeOne = largeOne[smallOneLen..];
                //define the range of " smallOne"
                Range areaOfSmallOne = (endOfLargeOne)..(endOfSmallOne);

                //This is a copy-buffer which holds enough space to store the nessecary parts needed for the swap!
                using var copyBuffer = new SpanPool<T>(smallOneLen + remainderOfLargeOne.Length, null);
                var sliceOfLargeOneCopy = copyBuffer.CorePush(sliceOfLargeOne);
                var remainderCopy = copyBuffer.CorePush(remainderOfLargeOne);

                //MUTATING-STEPS:
                //copy "smallOne" to "largeOne"
                smallOne.CopyTo(largeOne);
                //copy the prev copied "largeOne[smallOneLen..]" to "smallOne"
                sliceOfLargeOneCopy.CopyTo(smallOne);
                //clear the "remainder" from "input"	
                remainderOfLargeOne.Fill(delimiter);
                //move only " smallOne" by "remainder.Length" back, so we pass "-remainder.Length"
                int idxOfConcat = input.Internal_MoveBy(areaOfSmallOne, -remainderCopy.Length);
                //concat back the "remainder" to the "largeOne"
                remainderCopy.CopyTo(source[idxOfConcat..]);
            }
            else
            {
                //first < last ===> first=smallOne; last=largeOne
                (_, int smallOneLen, int endOfSmallOne) = info.SmallOneArea.Deconstruct();
                (int startOfLargeOne, _, int endOfLargeOne) = info.LargeOneArea.Deconstruct();

                //TEMP-instructions:
                //calculate a slice-range from "LargeOne" by "SmallOne.length" 
                var sliceFromLargeOne = info.LargeOneArea.GetSlice(info.SmallOneArea);
                //This is a copy-buffer which holds enough space to store the nessecary parts, in this case (smallOne + between) needed for the swap!
                using var copyBuffer = new SpanPool<T>(smallOneLen + between.Length, null);
                var smallOneCopy = copyBuffer.CorePush(first);
                var betweenCopy = copyBuffer.CorePush(between);
                //compute the last index of smallOne from the largeOne
                int idxOfSlice = startOfLargeOne + smallOneLen;
                //compute Range from where it shall move to another location
                int len2Copy = Math.Abs((endOfSmallOne) - idxOfSlice);
                //Define the remaining area from "largeOne"
                Range remainder2Move = idxOfSlice..(endOfLargeOne);

                //MUTATING instructions:
                //swap instantly a subset of largeOne with smallOne
                input.Swap(info.SmallOneArea, sliceFromLargeOne, delimiter);
                //move remainder to the "endOfSmallOne" 
                int idxOfSmallOne = input.Internal_MoveBy(remainder2Move, -len2Copy, delimiter);
                //before we can copy the smallOne where it belongs
                //we must copy "between" first, to ensure proper order
                betweenCopy.CopyTo(source[idxOfSmallOne..]);
                //update the "idxOfSmallOne" by between.Length then
                //copy back the "smallOne" to the end of "largeOne"
                idxOfSmallOne += betweenCopy.Length;
                smallOneCopy.CopyTo(source[idxOfSmallOne..]);
            }
        }
        else
        {
            //is FirstInOrder > Last, in terms of Length!
            if (info.IsFirstLargerThanLast)
            {
                //first > last ===> first=largeOne; last=smallOne

                //Here we get all the nessecary data back from each of the Areas, x and y.
                (int startOfSmallOne, int smallOneLen, _) = info.SmallOneArea.Deconstruct();
                (int startOfLargeOne, int largeOneLen, int endOfLargeOne) = info.LargeOneArea.Deconstruct();

                //This is a copy-buffer which holds enough space to store the nessecary parts,
                //in this case (last + first) in order to not overwrite these needed memories!
                using var copyBuffer = new SpanPool<T>(smallOneLen + largeOneLen, null);
                var smallOneCopy = copyBuffer.CorePush(last);
                var largeOneCopy = copyBuffer.CorePush(first);

                //FirstInOrder we copy what we have from "SmallOne" 
                smallOneCopy.CopyTo(source.Slice(startOfLargeOne, smallOneLen));
                //Then we delete only what was left from "LargeOne", this is more performant than to 1. delete ALL
                //of "LargeOne" and just THEN copy, this way we only have to delete a decent portion, not everything..
                source.Slice(startOfLargeOne + smallOneLen, diffInLength).Fill(delimiter);

                //this area we have to move 'behind', "large=first; small=last;"
                //int extraSpace = endOfSmallOne == info.SrcLength ? 1 : 0;
                Index properEnd = info.IsLastAtEnd ? ^smallOneLen : startOfSmallOne;
                Range area2MoveBack = (endOfLargeOne)..properEnd;

                //Now we move the area to where the 'largerOne' is but only 'smallOneLen' further, because
                //we wanna override the remaining 'null'(\0) values! 
                int newIdxOfLargeOne = input.Move2(area2MoveBack, startOfLargeOne + smallOneLen);

                //Finally we copy the 'largeOne' back to  
                largeOneCopy.CopyTo(source.Slice(newIdxOfLargeOne, largeOneLen));
            }
            else
            {
                //first < last ===> first=smallOne  && last=largeOne
                (int startOfSmallOne, int smallOneLen, int endOfSmallOne) = info.SmallOneArea.Deconstruct();
                (int startOfLargeOne, int largeOneLen, int _) = info.LargeOneArea.Deconstruct();

                //This is a copy-buffer which holds enough space to store the nessecary parts,
                //in this case (last + first) needed for the swap!
                using var copyBuffer = new SpanPool<T>(smallOneLen + largeOneLen + diffInLength, null);
                var smallOneCopy = copyBuffer.CorePush(first);
                var largeOneCopy = copyBuffer.CorePush(last);

                /*step1: copy 'largeOne' into 'smallOne' until 'smallOne' is filled*/
                largeOneCopy[..smallOneLen].CopyTo(source.Slice(startOfSmallOne, smallOneLen));

                /*step1.5: copy 'smallerOne' into 'largerOne' until 'smallOne' is filled* /*/
                smallOneCopy.CopyTo(source.Slice(startOfLargeOne, largeOneLen));

                /*step2: define the start..end for the area we shall move around*/
                //step3: create a range which reflects the part which has to be moved to the right-side,
                //considering that we already flipped parts of "SmallOne" with "largeOne"
                Range area2Move = endOfSmallOne..(startOfLargeOne + smallOneLen);

                //step4: slice and copy the 'remaining' parts of 'largerOne' locally
                var remainderSlice = input.Slice(startOfLargeOne + smallOneLen, diffInLength);
                var remainderCopy = copyBuffer.CorePush(remainderSlice);

                //step5: Move now the slice by "remainder.Length" towards the end  
                int _ = input.Internal_MoveBy(area2Move, diffInLength, delimiter);

                //step6: slice from "smallOne" the data PLUS "smallOneLen" more `default` values out and
                //copy now the "remainder" to the end of "smallOne"
                Range area2CopyRemainderInto = startOfSmallOne..(startOfSmallOne + largeOneLen);
                Range remainingArea2Copy = ^diffInLength..;

                //get what is left and store that to the "smallOneCopy"
                smallOneCopy = source[area2CopyRemainderInto];
                remainderCopy.CopyTo(smallOneCopy[remainingArea2Copy].Mutable());
            }
        }
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

    public static unsafe Segment* GetPtr(this in Segment segment)
    {
        return (Segment*)Unsafe.AsPointer(ref Unsafe.AsRef(in segment));
    }
}