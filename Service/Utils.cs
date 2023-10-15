﻿global using static Raylib_cs.Color;
global using RayColor = Raylib_cs.Color;
global using static Raylib_cs.Raylib;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using DotNext;
using Match_3.Datatypes;
using Match_3.Variables;
using Match_3.Workflow;
using Rectangle = Raylib_cs.Rectangle;


namespace Match_3.Service;

public static class Utils
{
    public static readonly Random Randomizer = new(DateTime.UtcNow.Ticks.GetHashCode());
    public static readonly FastNoiseLite NoiseMaker = new(DateTime.UtcNow.Ticks.GetHashCode());

    private const byte Min = (int)KnownColor.AliceBlue;
    private const byte Max = (int)KnownColor.YellowGreen;
    private const int TrueColorCount = Max - Min;

    private static readonly TileColor[] AllTileColors =
    {
        TileColor.Blue,
        TileColor.Brown,
        TileColor.Green,
        TileColor.Orange,
        TileColor.Purple,
        TileColor.Red,
        TileColor.Violet,
        TileColor.Yellow,
    };

    private static readonly RayColor[] All = new RayColor[TrueColorCount];

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

    public static RayColor GetRndColor() => All[Randomizer.Next(0, TrueColorCount)];

    public static int ToIndex(this TileColor color)
    {
        return color switch
        {
            KnownColor.Blue => 0,
            KnownColor.Brown => 1,
            KnownColor.Green => 2,
            KnownColor.Orange => 3,
            KnownColor.Purple => 4,
            KnownColor.Red => 5,
            KnownColor.Violet => 6,
            KnownColor.Yellow => 7,
            _ => -1
        };
    }

    public static ref readonly TileColor ToColor(this int color)
    {
        if (color < TileColorLen)
        {
            return ref AllTileColors[color];
        }

        throw new IndexOutOfRangeException(nameof(color));
    }

    public static StringBuilder Replace(this StringBuilder input,
        ReadOnlySpan<char> oldValue,
        ReadOnlySpan<char> newValue)
    {
        //nested helper functions!
        static Span<char> GetSpan(StringBuilder self)
        {
            foreach (var chunk in self.GetChunks())
            {
                var span = chunk.Span;
                return span.AsWriteable();
            }

            return Span<char>.Empty;
        }


        if (oldValue.Length == 0)
            throw new ArgumentException("Old value could not be found!", nameof(oldValue));

        var span = GetSpan(input);
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
        int matchIndex;
        int newValueLen = newValue.Length;
        int resultLength = input.Length;
        int searchIndex = 0;

        resultLength = resultLength - oldValueLen + newValueLen;

        while ((matchIndex = span.Slice(searchIndex).IndexOf(oldValue)) != -1)
        {
            searchIndex += matchIndex;

            // Replace the old value with the new value
            span[(searchIndex + oldValueLen)..].CopyTo(span[(searchIndex + newValueLen)..]);
            newValue.CopyTo(span.Slice(searchIndex, newValueLen));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Move2<T>(this ReadOnlySpan<T> input, Range sliceToMove, int start)
        where T : struct, IEquatable<T>
    {
        var r = sliceToMove.GetOffsetAndLength(input.Length);
        var areaToCopyInto = input.Slice(start, r.Length);
        input[sliceToMove].CopyTo(areaToCopyInto.AsWriteable());
    }

    /// <summary>
    /// Moves a slice within the input span by "moveBy" steps, if that value is > 0
    /// it moves to the right, else to the left 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="area2Move"></param>
    /// <param name="moveBy"></param>
    /// <param name="fillEmpties"></param>
    /// <typeparam name="T"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MoveBy<T>(this ReadOnlySpan<T> input,
        Range area2Move, int moveBy,
        T fillEmpties = default)
        where T : struct, IEquatable<T>
    {
        var r = area2Move.GetOffsetAndLength(input.Length);
        int newOffset = r.Offset + moveBy;
        Range areaToCopyInto = newOffset..(r.Length + newOffset);
        input[area2Move].CopyTo(input[areaToCopyInto].AsWriteable());

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
            begin2Clear = r.Offset;
            endOfArea2Move = begin2Clear + moveBy;
        }
        
        Range area2Clear = begin2Clear..endOfArea2Move;
        input[area2Clear].AsWriteable().Fill(fillEmpties);
        
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
    private static void Swap<T>(this scoped ReadOnlySpan<T> input, 
                                scoped ReadOnlySpan<T> x,
                                scoped ReadOnlySpan<T> y,
                                T delimiter = default)
        where T : unmanaged, IEquatable<T>, IComparable<T>, INumber<T>
    {
        //get all the needed information about the occuring spans here!
        scoped var info = new SpanInfo<T>(input, x, y);
        
        //use the "info" type for the future in this function!.....
        int diffToMove = info.LengthDiff;
        scoped ReadOnlySpan<T> first = info.First, 
                               last = info.Last;

        if (info.AreSameLength)
        {
            //store a copy of the smaller one
            scoped Span<T> lastCopy = stackalloc T[last.Length];
            last.CopyTo(lastCopy);

            first.CopyTo(input.Slice(info.IndexOfLast, first.Length).AsWriteable());
            lastCopy.CopyTo(input.Slice(info.IndexOfFirst, lastCopy.Length).AsWriteable());
            
            return;
        }

        if (info.AreXYNext2EachOther)
        {
            if (info.IsFirstLargerThanLast)
            {
                //first > last ===> first=largeOne; last=smallOne
                (_, _, int endOfLargeOne) = info.DeconstructLargeOne();
                (_, int smallOneLen, int endOfSmallOne) = info.DeconstructSmallOne();
               
                //TEMP-STEPS:
                    //slice "smallOne" 
                    var smallOne = last.AsWriteable();
                    //slice "largeOne"
                    var largeOne = first.AsWriteable();
                    //make a copy of "largeOne[0..smallOneLen]
                    Span<T> sliceOfLargeOne = stackalloc T[smallOneLen];
                    largeOne[..smallOneLen].CopyTo(sliceOfLargeOne);
                    //slice the "remainder" from "largeOne"
                    var remainderOfLargeOne = largeOne[smallOneLen..];
                    //make a copy of that "remainder" locally	
                    Span<T> remainderCopy = stackalloc T[remainderOfLargeOne.Length];
                    remainderOfLargeOne.CopyTo(remainderCopy);
                    //define the range of " smallOne"
                    Range areaOfSmallOne = endOfLargeOne..endOfSmallOne;  
                        
                //MUTATING-STEPS:
                    //copy "smallOne" to "largeOne"
                    smallOne.CopyTo(largeOne);
                    //copy the prev copied "largeOne[smallOneLen..]" to "smallOne"
                    sliceOfLargeOne.CopyTo(smallOne);
                    //clear the "remainder" from "input"	
                    remainderOfLargeOne.Fill(delimiter);
                    //move only " smallOne" by "remainder.Length" back, so we pass "-remainder.Length"
                    int idxOfConcat = input[..endOfSmallOne].MoveBy(areaOfSmallOne, -remainderCopy.Length);
                    //concat back the "remainder" to the "largeOne"
                    remainderCopy.CopyTo(input[idxOfConcat..].AsWriteable());
            }
            else
            {
                //first < last ===> first=smallOne; last=largeOne
                (_, int smallOneLen, int endOfSmallOne) = info.DeconstructSmallOne();
                (int startOfLargeOne, _, int endOfLargeOne) = info.DeconstructLargeOne();
               
                
                //TEMP-instructions:
                    //slice "smallOne"
                    var smallOne = first;
                    //slice "largeOne"
                    var largeOne = last;
                    //slice "smallOne" from "largeOne"
                    var slice = largeOne[..smallOneLen];
                    //copy "smallOne" locally  
                    Span<T> copyOfSmallOne = stackalloc T[smallOneLen]; 
                    smallOne.CopyTo(copyOfSmallOne);
                    //compute the last index of slice from the largeOne
                    int idxOfSlice = startOfLargeOne + smallOneLen;
                    //compute Range from where it shall move to another location
                    int len2Copy = Math.Abs(endOfSmallOne - idxOfSlice);
                    //Define the remaining area from "largeOne"
                    Range remainder2Move = idxOfSlice..endOfLargeOne;    
                    
                //MUTATING instructions:
                    //swap instantly a subset of largeOne with smallOne
                    input.Swap(smallOne, slice, delimiter);
                    //move remainder to the "endOfSmallOne" 
                    int idxOfSmallOne = input.MoveBy(remainder2Move, -len2Copy, delimiter) + 1; //+1 for delimiter
                    //copy back the "smallOne" to the end of "largeOne"
                    copyOfSmallOne.CopyTo(input[idxOfSmallOne..].AsWriteable());
            }
        }
        else
        {
            //compare the length and decide which one to copy where!
            if (info.IsFirstLargerThanLast)
            {
                //first > last ===> first=largeOne; last=smallOne
                
                (int startOfSmallOne, int smallOneLen, int endOfSmallOne) = info.DeconstructSmallOne();
                (int startOfLargeOne, int largeOneLen, int endOfLargeOne) = info.DeconstructLargeOne();
                
                //store a copy of the 'smallOne'
                scoped Span<T> smallOne = stackalloc T[smallOneLen];
                last.CopyTo(smallOne);

                //store a copy of the larger one
                scoped Span<T> largeOne = stackalloc T[largeOneLen];
                first.CopyTo(largeOne);

                //we have to clear the 'smallOne' from the input, before we can copy in order for .IndexOf() 
                //to find the match of the "largeOne" because otherwise there will be 2x

                T invalid = default;
                //we clear the area where the 'largeOne' resides!
                input.Slice(startOfLargeOne, largeOneLen).AsWriteable().Fill(invalid);
                //copy the 'smallOne' into the area of the 'largeOne'
                smallOne.CopyTo(input.Slice(startOfLargeOne, smallOneLen).AsWriteable());
                //'minStart' => is the first <empty> (in case of char, its ' ') value which comes
                // RIGHT AFTER the new position of 'smallOne' value, so the 
                //'end'     => is the last <empty> (in case of char, its ' ') value which comes
                // RIGHT AFTER the new position of 'largeOne' value, so the

                //'area2MoveBack' begins where 'largeOne' ENDS til the end of 'smallOne'
                // endOfSmallOne begins at 'startOfSmallOne' + 'largeOneLen' because we have to consider 
                // the area of 'smallerOne' is filled by the length of the 'largerOne'
                Range area2MoveBack = endOfLargeOne..endOfSmallOne;
 
                //Now we move the area to where the 'largerOne' is but only 'smallOneLen' further, because
                //we wanna override the remaining 'null'(\0) values! 
                input.Move2(area2MoveBack, startOfLargeOne + smallOneLen);

                //Finally we copy the 'largeOne' back to  
                largeOne.CopyTo(input.Slice(startOfSmallOne - diffToMove, largeOne.Length).AsWriteable());
            }
            else
            {
                //first < last ===> first=smallOne  && last=largeOne
                (int startOfSmallOne, int smallOneLen, int endOfSmallOne) = info.DeconstructSmallOne();
                (int startOfLargeOne, int largeOneLen, int endOfLargeOne) = info.DeconstructLargeOne();

                /*store a copy of the smaller one*/
                scoped Span<T> smallerOne = stackalloc T[smallOneLen];
                first.CopyTo(smallerOne);

                /*store a copy of the largerOne*/
                scoped Span<T> largerOne = stackalloc T[largeOneLen];
                last.CopyTo(largerOne);

                /*step1: copy 'largeOne' into 'smallOne' until 'smallOne' is filled*/
                largerOne[..smallOneLen].CopyTo(input.Slice(startOfSmallOne, smallOneLen).AsWriteable());

                /*step1.5: copy 'smallerOne' into 'largerOne' until 'smallOne' is filled* /*/
                smallerOne.CopyTo(input.Slice(startOfLargeOne, largeOneLen).AsWriteable());

                /*step2: compute the difference in length between large and small
                           as well as the 'correctStart' and 'end'*/
                int afterSmallOne = endOfSmallOne + 1;
                int endOfGreaterOne = startOfLargeOne + smallOneLen;
                //step3: create a range from AFTER 'smallOne' to END of 'largerOne' minus 'diffToMove'
                Range area2Move = afterSmallOne..endOfGreaterOne;

                //step4: copy the 'remaining' parts of 'largerOne' locally
                var remainderSlice = input.Slice(startOfLargeOne + smallOneLen, diffToMove);
                Span<T> remainderCopy = stackalloc T[remainderSlice.Length];
                remainderSlice.CopyTo(remainderCopy);
                
                //step5: Move now the slice by "remainder.Length" towards the end  
                input.MoveBy(area2Move, diffToMove, delimiter); //---->this here breaks somehow!! investigate!

                //step6: Copy now the "remainder" to the end of "smallOne"
                Range area2CopyRemainInto = startOfSmallOne..(startOfSmallOne + largeOneLen);
                Range remainingArea2Copy = ^diffToMove..;
                smallerOne = input[area2CopyRemainInto].AsWriteable();
                remainderCopy.CopyTo(smallerOne[remainingArea2Copy]);
                
                //step 7: replace the '\0' with ''=(char)32 because 0s are not recognized in a string
                //and are as such removed from it, so we dont have empty spaces in a string

                if (delimiter is char c)
                {
                    
                }
            }
        }
    }
    
    public static void Swap(this ReadOnlySpan<char> input, 
        scoped ReadOnlySpan<char> x,
        scoped ReadOnlySpan<char> y)
        => input.Swap(x, y, (char)32);

    public static void TestSwap()
    {
        //Works in all fashions EXCEPT when there are <special> chars in it, like:
        //!,.'
        var text = "Hello you world damn but so cool and adventures crazy".AsSpan();
        var x = text.Slice(text.IndexOf("you"), "you".Length);
        var y = text.Slice(text.IndexOf("adventures"), "adventures".Length);
        text.Swap(x, y);
    }
    
    public static Rectangle AsIntRayRect(this RectangleF floatBox) =>
        new(floatBox.X, floatBox.Y, floatBox.Width, floatBox.Height);

    static Utils()
    {
        All.AsSpan().Shuffle(Randomizer);

        NoiseMaker.SetFrequency(25f);
        NoiseMaker.SetFractalType(FastNoiseLite.FractalType.Ridged);
        NoiseMaker.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    }

    public static Span<T> AsWriteable<T>(this ReadOnlySpan<T> readOnlySpan) =>
        MemoryMarshal.CreateSpan(ref Unsafe.AsRef(readOnlySpan[0]), readOnlySpan.Length);

    public static float Trunc(this float value, int digits)
    {
        float mult = MathF.Pow(10.0f, digits);
        float result = MathF.Truncate(mult * value) / mult;
        return result < 0 ? -result : result;
    }

    public static Vector2 GetScreenCoord() => new(GetScreenWidth(), GetScreenHeight());

    public static bool CoinFlip()
    {
        var val = Randomizer.NextSingle();
        return val.GreaterOrEqual(0.50f, 0.001f);
    }

    public static Span<T> TakeRndItemsAtRndPos<T>(this Span<T> items)
        where T : unmanaged
    {
        int offset = Randomizer.Next(0, items.Length - 1);
        int len = items.Length;
        int amount2Take = Game.Level.Id switch
        {
            0 => Randomizer.Next(2, 4),
            1 => Randomizer.Next(5, 7),
            2 => Randomizer.Next(7, 10),
            _ => throw new ArgumentOutOfRangeException(nameof(Game.Level.Id))
        };

        return offset + amount2Take < len
            ? items.Slice(offset, amount2Take)
            : items[offset..^1];
    }

    private static bool IsEmpty(this RectangleF rayRect) =>
        /* rayRect.x == 0 && rayRect.y == 0 &&*/ rayRect is { Width: 0, Height: 0 };

    public static readonly RectangleF InvalidRect = new(-1, -1, 0, 0);

    public static void Add(ref this RectangleF a, RectangleF b)
    {
        if (a.IsEmpty())
        {
            a = b;
            return;
        }

        if (b.IsEmpty())
        {
            return;
        }

        Vector2 first = a.GetWorldPos();
        Vector2 other = b.GetWorldPos();
        (Vector2 Direction, bool isRow) pair = first.GetDirectionTo(other);
        float width = a.Width;
        float height = a.Height;

        //we know that: a) the direction and b)
        if (pair.isRow)
        {
            //a=10, b=10, result= a + b * 1
            width = a.Width + b.Width;
        }
        else
        {
            height = a.Height + b.Height;
        }

        a = new(first.X, first.Y, width, height);
    }

    public static string ToStr(this RectangleF rayRect)
        => $"x:{rayRect.X} y:{rayRect.Y}  width:{rayRect.Width}  height:{rayRect.Height}";

    public static RectangleF RelativeToMap(this RectangleF cellRect)
    {
        return new(cellRect.X * Size,
            cellRect.Y * Size,
            cellRect.Width * Size,
            cellRect.Height * Size);
    }

    public static RectangleF RelativeToGrid(this RectangleF worldRect)
    {
        return new(worldRect.X / Size,
            worldRect.Y / Size,
            worldRect.Width / Size,
            worldRect.Height / Size);
    }

    public static RectangleF DoScale(this RectangleF rayRect, ScaleableFloat factor)
    {
        return rayRect with
        {
            Width = (rayRect.Width * factor.GetFactor()),
            Height = (rayRect.Height * factor.GetFactor())
        };
    }

    public static bool Equals(this float x, float y, float tolerance)
    {
        var diff = MathF.Abs(x - y);
        return diff <= tolerance ||
               diff <= MathF.Max(MathF.Abs(x), MathF.Abs(y)) * tolerance;
    }

    public static bool Equals(this int x, int y, int tolerance)
    {
        var diff = MathF.Abs(x - y);
        return diff <= tolerance ||
               diff >= Math.Max(Math.Abs(x), Math.Abs(y));
    }

    private static bool GreaterOrEqual(this float x, float y, float tolerance)
    {
        var diff = MathF.Abs(x - y);
        return diff > tolerance ||
               diff > MathF.Max(MathF.Abs(x), MathF.Abs(y)) * tolerance;
    }

    public static int CompareTo(this Vector2 a, Vector2 b)
    {
        var pair = a.GetDirectionTo(b);
        return pair.isRow ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
    }

    public static (Vector2 Direction, bool isRow) GetDirectionTo(this Vector2 first, Vector2 next)
    {
        bool sameRow = (int)first.Y == (int)next.Y;

        //switch on direction
        if (sameRow)
        {
            //the difference is positive
            if (first.X < next.X)
                return (Vector2.UnitX, sameRow);

            if (first.X > next.X)
                return (-Vector2.UnitX, sameRow);
        }
        //switch on direction
        else
        {
            //the difference is positive
            if (first.Y < next.Y)
                return (Vector2.UnitY, sameRow);

            if (first.Y > next.Y)
                return (-Vector2.UnitY, sameRow);
        }

        return (-Vector2.One, false);
    }

    public static Vector2 GetOpposite(this Vector2 a, Vector2 b)
    {
        var pair = a.GetDirectionTo(b);

        if (pair.isRow)
        {
            if (pair.Direction == -Vector2.UnitX)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return a + Vector2.UnitX;
            }

            if (pair.Direction == Vector2.UnitX)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return a - Vector2.UnitX;
            }
        }
        else
        {
            if (pair.Direction == -Vector2.UnitY)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return a + Vector2.UnitY;
            }

            if (pair.Direction == Vector2.UnitY)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return a - Vector2.UnitY;
            }
        }

        throw new ArgumentException("this line should never be reached!");
    }

    private static Vector2 GetWorldPos(this RectangleF a) => new(a.X, a.Y);

    public static Vector2 GetCellPos(this RectangleF a) => GetWorldPos(a) / Size;

    public static void SetMouseToWorldPos(Vector2 position, int scale = Size)
    {
        SetMousePosition((int)position.X * scale, (int)position.Y * scale);
    }

    public static RectangleF NewWorldRect(Vector2 begin, int width, int height)
    {
        return new(begin.X * Size,
            begin.Y * Size,
            width * Size,
            height * Size);
    }

    public const int TileColorLen = 8;
    public const int Size = Level.TileSize;
    public static readonly Vector2 InvalidCell = -Vector2.One; //this will be computed only once!
}