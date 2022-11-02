global using System.Drawing;
global using System.Numerics;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using DotNext;
global using ImGuiNET;
global using SysColor = System.Drawing.Color;
global using System.Diagnostics.CodeAnalysis;
global using static Raylib_CsLo.Raylib;
global using Color = Raylib_CsLo.Color;
global using Rectangle = Raylib_CsLo.Rectangle;
global using System.Text.RegularExpressions;
global using FastEnumUtility;
global using NoAlloq;

namespace Match_3;

public ref struct SpanEnumerator<TItem>
{
    private ref TItem _currentItem;
    private readonly ref TItem _lastItemOffsetByOne;
    
    public SpanEnumerator(ReadOnlySpan<TItem> span) 
        : this(ref MemoryMarshal.GetReference(span), span.Length)
    { }

    public SpanEnumerator(Span<TItem> span) : 
        this(ref MemoryMarshal.GetReference(span), span.Length)
    { }

    private SpanEnumerator(ref TItem item, nint length)
    {
        _currentItem = ref Unsafe.Subtract(ref item, 1);
        _lastItemOffsetByOne = ref Unsafe.Add(ref item, length);
        Length = (int)length;
    }
    [UnscopedRef] public ref TItem Current => ref _currentItem;
    public int Counter { get; private set; } = 0;

    public readonly int Length;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool MoveNext()
    {
        _currentItem = ref Unsafe.Add(ref _currentItem, 1);
        /*
        bool equalityCheck = EqualityComparer<TItem>.Default.Equals(_currentItem, default);
        return shallSkipDefault ? !equalityCheck && !Unsafe.AreSame(ref _currentItem, ref _lastItemOffsetByOne) 
                                : !Unsafe.AreSame(ref _currentItem, ref _lastItemOffsetByOne);*/
        Counter++;
        return !Unsafe.AreSame(ref _currentItem, ref _lastItemOffsetByOne);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
   
    [UnscopedRef]
    public ref SpanEnumerator<TItem> GetEnumerator()
    {
        return ref this;
    }
}
public ref struct WordEnumerator
{
    //private SpanEnumerator<TextChunk> _iterator;
    private readonly char _separator;
    private int _startIdx = 0;
    
    private ReadOnlySpan<char> _current;
    private TextChunk _chunk;
    private ReadOnlySpan<char> _remainder;
        
    //[UnscopedRef]public ref readonly ReadOnlySpan<char> Current => ref _current;
    
    [UnscopedRef]public ref readonly TextChunk Current => ref _chunk;
    public int Counter { get; private set; } 
    /// <summary>
    /// An Enumerator who iterates over a string[] stored as ROS
    /// </summary>
    /// <param name="stringArray">the ROS which is interpreted as a string[]</param>
    /// <param name="separator">the character who will be used to split the ROS</param>
    public WordEnumerator(ReadOnlySpan<char> stringArray, char separator)
    {
        _separator = separator;
        
        if (stringArray.IndexOf(separator) == 0)
            _remainder = stringArray[1..];

        else
            _remainder = stringArray;


        if (!stringArray.Contains(separator))
            throw new ArgumentException(
                "The Enumerator expects a char which shall function as line splitter! If there is none" +
                "it cannot slice the ROS which shall be viewed as string[]");
    }

    public WordEnumerator(in TextChunk chunk, char separator) : this(chunk.Piece, separator)
    {
        _chunk = chunk;
    }
    
    public bool MoveNext()
    {
        //ReadOnlySpan<char> items = "abc, def, ghi, jkl, mno"
        int idxOfChar = _remainder.IndexOf(_separator);
            
        if (idxOfChar < 0)
            return false;
            
        var firstBlock = _remainder[..idxOfChar];
        _remainder = _remainder[(firstBlock.Length + 1)..];
        _current = firstBlock;
        _chunk = new(_current, _chunk.SystemColor);
        Counter++;
        return _current.Length > 0;
    }
    
    public WordEnumerator GetEnumerator()
    {
        return this;
    }
}

public ref struct TextStyleEnumerator
{
    private static readonly Regex rgx = new(@"\([a-zA-Z]+\)", RegexOptions.Singleline);
    private TextChunk _current;
    private Span<(int idx, int len)> matchData;
    private int relativeStart, relativeEnd;
    private readonly ReadOnlySpan<char> _text;
    private StackBuffer _stackPool;
    private int _position;

    public TextStyleEnumerator(ReadOnlySpan<char> text)
    {
        _stackPool = new();
        _position = 0;
        _text = text;
        //var result = rgx.Split(_text);
        //{Black} This is a {Red} super nice {Green} shiny looking text
        matchData = _stackPool.Slice<(int idx, int len)>(0, 5);
    
        foreach (var enumerateMatch in rgx.EnumerateMatches(text))
        {
            reset:
            if (_position < matchData.Length)
            {
                matchData[_position++] = (enumerateMatch.Index, enumerateMatch.Length);
            }
            else
            {
                //_stackPool.Store<(int idx, int len)>(matchData, 0);
                matchData = _stackPool.Slice<(int idx, int len)>(0, matchData.Length*2);
                goto reset;
            }
        }

        matchData = matchData.Where(x => x.idx >= 0 && x.len > 0).CopyInto(matchData).Slice(0);
        _position = 0;
    }
    
    [UnscopedRef]public ref readonly TextChunk Current => ref _current;
    
    public bool MoveNext()
    {
        if (_position >= matchData.Length)
            return false;

        ref readonly var match = ref matchData[_position];

        ReadOnlySpan<char> colorCode = _text.Slice(match.idx, match.len);

        relativeStart = match.idx;
        int okayBegin = relativeStart;
        
        if (_position + 1 < matchData.Length)
            relativeEnd = matchData[_position + 1].idx;
        else
        {
            relativeEnd = _text.Length - match.idx + 1;
            relativeStart = 1;
        }
        //part0: (Black) You have to collect (Red) xxx
        var tmp = _text.Slice(okayBegin, relativeEnd - relativeStart);
        tmp = tmp[match.len..^1];
        _current = new(tmp, colorCode, (match.idx + match.len, tmp.Length));
        _position++;
        
        return tmp.Length > 0;
    }

    [UnscopedRef]
    public ref readonly TextStyleEnumerator GetEnumerator()
    {
        return ref this;
    }
}

public readonly ref struct TextChunk
{
    public readonly Vector2 TextSize;
    public readonly ReadOnlySpan<char> Piece;
    public readonly Vector4 ImGuiColor;
    public readonly SysColor SystemColor;
    public readonly (int idx, int len) Occurence;
    /// <summary>
    /// represents the color we want to convert to a Vector4 type
    /// </summary>
    /// <param name="piece"></param>
    /// <param name="colorCode">the string colorname like {Black} or {Red}</param>
    public TextChunk(ReadOnlySpan<char> piece, ReadOnlySpan<char> colorCode, 
        (int idx, int len) occurence)
    {
        var colName = colorCode[1..^1];
        Piece = piece;
        Occurence = occurence;
        SystemColor = SysColor.FromName(colName.ToString());
        ImGuiColor = ImGui.ColorConvertU32ToFloat4((uint)SystemColor.ToArgb());
        Vector2 offset = Vector2.One * 1.5f;
        TextSize = ImGui.CalcTextSize(Piece.ToString()) + offset;// make improvement PR to accept ROS instead of string
    }

    public TextChunk(ReadOnlySpan<char> piece, SysColor color)
    {
        Piece = piece;
        SystemColor = color;
        TextSize = ImGui.CalcTextSize(piece.ToString());
        ImGuiColor = ImGui.ColorConvertU32ToFloat4((uint)SystemColor.ToArgb());
        Occurence = (-1, -1);
    }
}

public static class Utils
{
    public static  readonly Random Randomizer =  new(DateTime.UtcNow.Ticks.GetHashCode());
    public static readonly FastNoiseLite NoiseMaker = new(DateTime.UtcNow.Ticks.GetHashCode());
    public static readonly IReadOnlyList<TileType> ValidItems = FastEnum.GetValues<TileType>();
    
    static void DoWork()
    {
    }
    
    public static Vector4 AsVec4(Color color)
    {
        Vector4 v4Color = default;
        const int max = 255; //100%

        for (byte i = 0; i < 4; i++)
        {
            byte colorValue = Unsafe.Add(ref Unsafe.AsRef(color.r), i);
            ref float v4Value = ref Unsafe.Add(ref Unsafe.AsRef(v4Color.X), i);
            float percentage = colorValue / (float)max;
            v4Value = percentage;
        }
        
        return v4Color;
    }

    public static Color FromSysColor(SysColor color)
    {
        return new(color.R, color.G, color.B, color.A);
    }
    
    public static Color GetRndColor( )
    {
        int max = (int)KnownColor.YellowGreen;
        Span<Color> all = stackalloc Color[max];
        
        for (int i = 0; i < max; i++)
        {
            all[i] = FromSysColor(SysColor.FromKnownColor((KnownColor)i));
        }
        all.Shuffle(Randomizer);
        return all[Randomizer.Next(0, max)];
    }
    
    static Utils()
    {
        NoiseMaker.SetFrequency(25f);
        NoiseMaker.SetFractalType(FastNoiseLite.FractalType.Ridged);
        NoiseMaker.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    }
    
    public static float Trunc(this float value, int digits)
    {
        float mult = MathF.Pow(10.0f, digits);
        float result = MathF.Truncate(mult * value) / mult;
        return result < 0 ? -result : result;
    }
    
    public static Vector2 GetScreenCoord() => new(GetScreenWidth(), GetScreenHeight());
    
    public static bool RollADice()
    {
        var val = Randomizer.NextSingle();
        return val.GreaterOrEqual(0.50f, 0.001f);
    }
    
    public  static bool IsEmpty(this Rectangle rayRect) => 
       /* rayRect.x == 0 && rayRect.y == 0 &&*/ rayRect.width == 0 && rayRect.height == 0;

    public static readonly Rectangle InvalidRect = new(-1f, -1f, 0f, 0f);
    public static Vector2 InvalidCell => -Vector2.One;
    public static void Add(ref this Rectangle a, Rectangle b)
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
        float width = a.width;
        float height = a.height;
        
        //we know that: a) the direction and b)
        if (pair.isRow)
        {
            //a=10, b=10, result= a + b * 1
            width = (a.width + b.width);
        }
        else
        {
            height = (a.height + b.height);
        }

        a = new(first.X, first.Y, width, height);
    }
    public static string ToStr(this Rectangle rayRect)
        => $"x:{rayRect.x} y:{rayRect.y}  width:{rayRect.width}  height:{rayRect.height}";
    public static Rectangle ToWorldBox(this Rectangle cellRect)
    {
        //rayrect 
        return new(cellRect.x * Tile.Size, 
            cellRect.y * Tile.Size,
            cellRect.width * Tile.Size,
            cellRect.height * Tile.Size);
    }
    public static Rectangle ToGridBox(this Rectangle worldRect)
    {
        //rayrect 
        return new(worldRect.x / Tile.Size, 
            worldRect.y / Tile.Size,
            worldRect.width / Tile.Size,
            worldRect.height / Tile.Size);
    }
    public static Rectangle DoScale(this Rectangle rayRect, Scale factor)
    {
        //rayrect 
        return new(rayRect.x, rayRect.y, rayRect.width * factor.GetFactor(), rayRect.height * factor.GetFactor());
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
    private static Vector2 GetWorldPos(this Rectangle a) => new((int)a.x, (int)a.y);
    public static Vector2 GetCellPos(this Rectangle a) => GetWorldPos(a) / Tile.Size;
    public static void SetMouseToWorldPos(Vector2 position, int scale = Tile.Size)
    {
        SetMousePosition((int)position.X * scale, (int)position.Y * scale);
    }
    public static unsafe nint GetAddrOfObject<TObjectT>(this TObjectT @object) where TObjectT: class
    {
        return (nint) Unsafe.AsPointer(ref Unsafe.As<StrongBox<byte>>(@object).Value);
    }
    public static Rectangle NewWorldRect(Vector2 begin, int width, int height)
    {
        return new((int)begin.X * Tile.Size,
            (int)begin.Y * Tile.Size,
            width * Tile.Size,
            height* Tile.Size);
    }
}
