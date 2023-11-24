global using static Raylib_cs.Color;
global using RayColor = Raylib_cs.Color;
global using static Raylib_cs.Raylib;
global using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
global using DAMTypes = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DotNext;
using Match_3.DataObjects;
using Match_3.Setup;
using Raylib_cs;
using Color = System.Drawing.Color;


namespace Match_3.Service;

public static class Utils
{
    public static readonly Random Randomizer = new(DateTime.UtcNow.Ticks.GetHashCode());
    public static readonly DotnetNoise.FastNoise NoiseMaker = new(DateTime.UtcNow.Ticks.GetHashCode());
    private static readonly TileColor[] AllTileColors =
    {
        TileColor.SkyBlue,       //--> Hellblau
        TileColor.Turquoise,        //--> Türkis
        TileColor.Blue,             //--> Blau
        TileColor.SpringGreen,      //--> Hellgrün
        TileColor.Green,            //--> Grün
        TileColor.Brown,            //--> Braun
        TileColor.Orange,           //--> Orange
        TileColor.Yellow,           //--> Gelb
        TileColor.MediumVioletRed,  //--> RotPink
        TileColor.BlueViolet,       //--> Rosa
        TileColor.Magenta,          //--> Pink
        TileColor.Red,              //--> Rot
      
    };
    
    public static Vector4 ToVec4(this Color color)
    {
        return new(
            color.R / 255.0f,
            color.G / 255.0f,
            color.B / 255.0f,
            color.A / 255.0f);
    }

    public static Color ToColor(this Vector4 color) =>
        Color.FromArgb((int)(color.W * 255), (int)(color.X * 255), (int)(color.Y * 255), (int)(color.Z * 255));

    public static Vector4 ToVec4(this RayColor color)
    {
        return new(
            color.r / 255.0f,
            color.g / 255.0f,
            color.b / 255.0f,
            color.a / 255.0f);
    }

    public static RayColor AsRayColor(this Color color) => new(color.R, color.G, color.B, color.A);

    public static Color AsSysColor(this RayColor color) => Color.FromArgb(color.a, color.r, color.g, color.b);

    public static int ToIndex(this TileColor color)
    {
        return color switch
        {
            TileColor.SkyBlue => 0,       //--> Hellblau
            TileColor.Turquoise => 1,       //--> Dunkelblau
            TileColor.Blue => 2,             //--> Blau
            TileColor.SpringGreen =>3,      //--> Hellgrün
            TileColor.Green => 4,            //--> Grün
            TileColor.Brown => 5,            //--> Braun
            TileColor.Orange => 6,           //--> Orange
            TileColor.Yellow => 7,           //--> Gelb
            TileColor.MediumVioletRed => 8,  //--> RotPink
            TileColor.BlueViolet => 9,       //--> Rosa
            TileColor.Magenta => 10,          //--> Pink
            TileColor.Red => 11,         
        };
    }

    public static TileColor ToColor(this int color)
    {
        if (color < DataOnLoad.TileColorCount)
        {
            return AllTileColors[color];
        }

        throw new IndexOutOfRangeException(nameof(color));
    }

    //nested helper functions!
    public static Span<char> AsSpan(this StringBuilder self)
    {
        foreach (var chunk in self.GetChunks())
        {
            return chunk.Span.AsWriteable();
        }

        return Span<char>.Empty;
    }

    public static StringBuilder Replace(this StringBuilder input,
        ReadOnlySpan<char> oldValue,
        ReadOnlySpan<char> newValue)
    {
        if (oldValue.Length == 0)
            throw new ArgumentException("Old value could not be found!", nameof(oldValue));

        var span = AsSpan(input);
        int matchIndex;
        int replacementLength = newValue.Length;
        int resultLength = input.Length;
        int searchIndex = 0;

        while ((matchIndex = span.Slice(searchIndex).IndexOf(oldValue)) != -1)
        {
            searchIndex += matchIndex;

            resultLength = resultLength - oldValue.Length + replacementLength;

            if (resultLength > input.Length)
            {
                throw new InvalidOperationException("Resulting span length exceeds input span length.");
            }

            if (replacementLength == 0)
            {
                // Remove the old value
                span.Slice(searchIndex, oldValue.Length)
                    .CopyTo(span[(searchIndex + replacementLength)..]);
            }
            else
            {
                // Replace the old value with the new value
                span[(searchIndex + oldValue.Length)..].CopyTo(span[(searchIndex + replacementLength)..]);
                newValue.CopyTo(span.Slice(searchIndex, replacementLength));
            }
        }

        // var area = span[..resultLength];
        return input;
    }

    public static void Replace(this ReadOnlySpan<char> input,
        ReadOnlySpan<char> oldValue,
        ReadOnlySpan<char> newValue)
    {
        int oldValueLen = oldValue.Length;

        if (oldValueLen == 0)
            throw new ArgumentException("Old value could not be found!", nameof(oldValue));

        var span = input.AsWriteable();
        var old = oldValue.AsWriteable();
        var next = newValue.AsWriteable();
        int newValueLen = next.Length;
        int searchIndex = 0;

        if (oldValueLen >= newValueLen)
        {
            int matchIndex;

            while ((matchIndex = span.Slice(searchIndex).IndexOf(oldValue)) != -1)
            {
                searchIndex += matchIndex;
                var tmpOld = span.Slice(searchIndex, old.Length);
                //copy parts of newValue  to oldValue

                next.CopyTo(tmpOld[..newValueLen]);
                tmpOld[newValueLen..].Fill(default);
            }
        }
    }

    public static Span<T> AsWriteable<T>(this scoped ReadOnlySpan<T> readOnlySpan) =>
        MemoryMarshal.CreateSpan(ref Unsafe.AsRef(readOnlySpan[0]), readOnlySpan.Length);

    public static void WriteBin(int v)
    {
        const byte maxBits = 32;
        var padLeft = Convert.ToString(v, 2).PadLeft(maxBits, '0');
        string res = "";
        const byte fourByteBlocks = 4;
        byte blockCounter = 0;

        for (byte i = 0; i < maxBits; i++)
        {
            if (i % 8 == 0 && blockCounter++ < fourByteBlocks)
            {
                var byteBlock = padLeft.AsSpan(i, 8);
                res += byteBlock.ToString() + "_";
            }
        }

        res.AsSpan().AsWriteable()[^1] = default;
        Console.WriteLine(res);
        Console.WriteLine(new string('-', maxBits));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Move2<T>(this scoped ReadOnlySpan<T> input, Range slice2Move, int newPos)
        where T : unmanaged, IEquatable<T>
    {
        var (_, length) = slice2Move.GetOffsetAndLength(input.Length);
        var areaToCopyInto = input.Slice(newPos, length);
        input[slice2Move].CopyTo(areaToCopyInto.AsWriteable());
        return newPos + length;
    }

    /// <summary>
    /// Moves a slice within the input span by "moveBy" steps, 
    /// if that value is > 0, it moves to the RIGHT, else to the LEFT 
    /// </summary>
    public static Area<T>? MoveBy<T>(this scoped ReadOnlySpan<T> input,
        Range area2Move, int moveBy, bool shallAdjustInput,
        T fillEmpties = default)
        where T : unmanaged, IEquatable<T>
    {
        if (moveBy == 0)
            return null;

        var source = input.AsWriteable();
        int srcLen = source.Length;
        Area<T> moveableArea = new(area2Move, srcLen);
        Area<T> area2CopyInto, area2Clear;
        (int startOfMoveArea, int length2Move, _) = moveableArea.Deconstruct();
        int newOffset = startOfMoveArea + moveBy;

        if (moveBy < 0)
        {
            newOffset = newOffset < 0 ? 0 : newOffset;
            area2CopyInto = new(newOffset, length2Move);

            source[area2Move].CopyTo(source[(Range)area2CopyInto]);

            area2Clear = area2CopyInto.Overlaps(moveableArea) > 0
                //(-moveBy) to turn it positive, since its already < 0 !
                ? new Area<T>(area2CopyInto.End, -moveBy)
                : new Area<T>(area2Move.Start..area2Move.End, srcLen);
        }
        else
        {
            bool doesExceedLength = (newOffset > srcLen) || (newOffset + length2Move) >= srcLen;

            area2CopyInto = doesExceedLength
                ? new(^length2Move.., srcLen)
                : new(newOffset, length2Move);

            source[area2Move].CopyTo(source[(Range)area2CopyInto]);

            area2Clear = area2CopyInto.Overlaps(moveableArea) > 0
                ? new(startOfMoveArea, doesExceedLength ? length2Move : moveBy)
                : new(area2Move.Start..area2Move.End, srcLen);
        }

        source[(Range)area2Clear].Fill(fillEmpties);

        if (shallAdjustInput)
        {
            //step1: move the "newArea" now by "moveBy" BACK/TOP
            moveBy = area2Clear.Length;
            var adjustableArea = area2CopyInto;
            var adjustedArea = input.MoveBy(adjustableArea, -moveBy, false, fillEmpties)!.Value;
            int startOfRemain2Move = adjustedArea.End;
            Area<T> remain = new(startOfRemain2Move.., srcLen);
            input.MoveBy(remain, -1, false, fillEmpties);
            return null;
        }

        return area2Clear;
    }

    private static int Internal_MoveBy<T>(this scoped ReadOnlySpan<T> input,
        Range area2Move, int moveBy,
        T fillEmpties = default)
        where T : struct, IEquatable<T>
    {
        var source = input.AsWriteable();
        var r = area2Move.GetOffsetAndLength(source.Length);
        int newOffset = r.Offset + moveBy;
        Range areaToCopyInto = newOffset..(r.Length + newOffset);
        source[area2Move].CopyTo(source[areaToCopyInto]);

        int endOfArea2Move;
        int begin2Clear;

        if (moveBy < 0)
        {
            endOfArea2Move = r.Offset + r.Length;
            //go "moveBy" back
            begin2Clear = endOfArea2Move + moveBy;
        }
        else
        {
            //go "moveBy" forward
            begin2Clear = r.Offset;
            endOfArea2Move = begin2Clear + moveBy;
        }

        Range area2Clear = begin2Clear..endOfArea2Move;
        source[area2Clear].Fill(fillEmpties);

        return newOffset + r.Length;
    }

    /// <summary>
    /// Swaps 2 different slices within the same span!
    /// </summary>
    /// <param name="input">the base span where the swap is reflected </param>
    /// <param name="x">the 1. span to swap with the 2. one</param>
    /// <param name="y">the 2. span to swap with the 1. one</param>
    /// <param name="delimiter">a value which functions as a delimiter to say when a new block of an arbitrary numeric value begins and ends..</param>
    /// <typeparam name="T">The type of the span</typeparam>
    private static void Swap<T>(this scoped ReadOnlySpan<T> input, Range x, Range y, T delimiter = default)
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

        var source = input.AsWriteable();

        //NOTE: We have to +1 to every "endOf...." variable because it is EXCLUSIVE inside the "Range", which means 
        //      that END value is excluded from the span, so in order to include it into the range,
        //      we do +1 to get that value 

        if (info.AreSameLength)
        {
            //store a copy of the smaller one
            using var copyBuffer = new SpanQueue<T>(last.Length);
            var lastCopy = copyBuffer.CoreEnqueue(last);

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
                var smallOne = last.AsWriteable();
                //slice "largeOne"
                var largeOne = first.AsWriteable();
                //make the nessecary slice from the largeOne
                var sliceOfLargeOne = largeOne[..smallOneLen];
                //make the nessecary slice to get the remainder
                var remainderOfLargeOne = largeOne[smallOneLen..];
                //define the range of " smallOne"
                Range areaOfSmallOne = (endOfLargeOne)..(endOfSmallOne);

                //This is a copy-buffer which holds enough space to store the nessecary parts needed for the swap!
                using var copyBuffer = new SpanQueue<T>(smallOneLen + remainderOfLargeOne.Length);
                var sliceOfLargeOneCopy = copyBuffer.CoreEnqueue(sliceOfLargeOne);
                var remainderCopy = copyBuffer.CoreEnqueue(remainderOfLargeOne);

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
                var sliceFromLargeOne = info.LargeOneArea.Slice(info.SmallOneArea);
                //This is a copy-buffer which holds enough space to store the nessecary parts, in this case (smallOne + between) needed for the swap!
                using var copyBuffer = new SpanQueue<T>(smallOneLen + between.Length);
                var smallOneCopy = copyBuffer.CoreEnqueue(first);
                var betweenCopy = copyBuffer.CoreEnqueue(between);
                //compute the last index of smallOne from the largeOne
                int idxOfSlice = startOfLargeOne + smallOneLen;
                //compute Range from where it shall move to another location
                int len2Copy = Math.Abs((endOfSmallOne) - idxOfSlice);
                //Define the remaining area from "largeOne"
                Range remainder2Move = idxOfSlice..(endOfLargeOne);

                //MUTATING instructions:
                //swap instantly a subset of largeOne with smallOne
                input.Swap((Range)info.SmallOneArea, (Range)sliceFromLargeOne, delimiter);
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
                using var copyBuffer = new SpanQueue<T>(smallOneLen + largeOneLen);
                var smallOneCopy = copyBuffer.CoreEnqueue(last);
                var largeOneCopy = copyBuffer.CoreEnqueue(first);

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
                using var copyBuffer = new SpanQueue<T>(smallOneLen + largeOneLen + diffInLength);
                var smallOneCopy = copyBuffer.CoreEnqueue(first);
                var largeOneCopy = copyBuffer.CoreEnqueue(last);

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
                var remainderCopy = copyBuffer.CoreEnqueue(remainderSlice);

                //step5: Move now the slice by "remainder.Length" towards the end  
                int _ = input.Internal_MoveBy(area2Move, diffInLength, delimiter);

                //step6: slice from "smallOne" the data PLUS "smallOneLen" more `default` values out and
                //copy now the "remainder" to the end of "smallOne"
                Range area2CopyRemainderInto = startOfSmallOne..(startOfSmallOne + largeOneLen);
                Range remainingArea2Copy = ^diffInLength..;

                //get what is left and store that to the "smallOneCopy"
                smallOneCopy = source[area2CopyRemainderInto];
                remainderCopy.CopyTo(smallOneCopy[remainingArea2Copy].AsWriteable());
            }
        }
    }

    public static void Swap(this scoped ReadOnlySpan<char> input, Range x, Range y) => input.Swap(x, y, ' ');
    
    public static Span<T> TakeRndItemsAtRndPos<T>(this Span<T> items) where T : unmanaged
    {
        int len = items.Length;
        int m = len / 2;
        float distribution = Randomizer.NextSingle();
        int levelId = GameState.Lvl.Id;
        
        int amount2Take = levelId switch
        {
            0 => Randomizer.Next(3, 4),
            1 => Randomizer.Next(5, 6),
            2 => Randomizer.Next(7, 7),
            _ => throw new ArgumentOutOfRangeException(nameof(levelId))
        };
        
        Range r = distribution switch
        {
            >= 0.0f and < 0.33f => ..amount2Take,
            >= 0.33f and < 0.66f => m..,
            >= 0.66f and < 1.00f => ^amount2Take..
        };

        return items[r];
    }

    public static void Shuffle<T>(this Span<T> span) => span.Shuffle(Randomizer);
    
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
    
    private static bool GreaterOrEqual(this float x, float y, float tolerance)
    {
        var diff = MathF.Abs(x - y);
        return diff > tolerance ||
               diff > MathF.Max(MathF.Abs(x), MathF.Abs(y)) * tolerance;
    }
    
    public static void Fill(Span<TileColor> toFill)
    {
        for (int i = 0; i < DataOnLoad.TileColorCount; i++)
            toFill[i] = i.ToColor();
    }

    public static void SetMouseToWorldPos(Vector2 position, int scale = DataOnLoad.TileSize)
    {
        SetMousePosition((int)position.X * scale, (int)position.Y * scale);
    }

    public static void UpdateShader<T>(int locInShader, T value) where T : unmanaged
    {
        switch (value)
        {
            case int or bool or long:
                SetShaderValue(AssetManager.WobbleEffect, locInShader, value, ShaderUniformDataType.SHADER_UNIFORM_INT);
                break;

            case float or double or Half:
                SetShaderValue(AssetManager.WobbleEffect, locInShader, value,
                    ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
                break;

            case Vector2:
                SetShaderValue(AssetManager.WobbleEffect, locInShader, value,
                    ShaderUniformDataType.SHADER_UNIFORM_VEC2);
                break;
        }
    }
}