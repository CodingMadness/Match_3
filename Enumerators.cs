using System.Diagnostics.CodeAnalysis;
using DotNext.Buffers;
using FastEnumUtility;

namespace Match_3;

/// <summary>
/// A faster span enumerator than .NET currently provides
/// </summary>
/// <typeparam name="TItem"></typeparam>
public ref struct FastSpanEnumerator<TItem>
{
    private ref TItem _currentItem;
    private readonly ref TItem _lastItemOffsetByOne;

    public FastSpanEnumerator(ReadOnlySpan<TItem> span)
        : this(ref MemoryMarshal.GetReference(span), span.Length)
    {
    }

    public FastSpanEnumerator(Span<TItem> span) :
        this(ref MemoryMarshal.GetReference(span), span.Length)
    {
    }

    private FastSpanEnumerator(ref TItem item, nint length)
    {
        _currentItem = ref Unsafe.Subtract(ref item, 1);
        _lastItemOffsetByOne = ref Unsafe.Add(ref _currentItem, length + 1);
    }

    [UnscopedRef] public ref TItem Current => ref _currentItem;

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool MoveNext()
    {
        _currentItem = ref Unsafe.Add(ref _currentItem, 1);
        return Unsafe.IsAddressLessThan(ref _currentItem, ref _lastItemOffsetByOne);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool MoveBack()
    {
        _currentItem = ref Unsafe.Subtract(ref _currentItem, 1);
        return Unsafe.IsAddressLessThan(ref _currentItem, ref _lastItemOffsetByOne);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [UnscopedRef]
    public ref FastSpanEnumerator<TItem> GetEnumerator()
    {
        return ref this;
    }
}

public ref struct WordEnumerator
{
    //private SpanEnumerator<TextChunk> _iterator;
    private readonly char _separator;
    private readonly TextChunk _original;
    private TextChunk _tmp;
    private ReadOnlySpan<char> _remainder;

    //[UnscopedRef]public ref readonly ReadOnlySpan<char> Current => ref _current;

    [UnscopedRef] public ref readonly TextChunk Current => ref _tmp;

    /// <summary>
    /// An Enumerator who iterates over a string[] stored as ROS
    /// </summary>
    /// <param name="stringArray">the ROS which is interpreted as a string[]</param>
    /// <param name="separator">the character who will be used to split the ROS</param>
    private WordEnumerator(ReadOnlySpan<char> stringArray, char separator)
    {
        _separator = separator;

        _remainder = stringArray.Contains(separator) ? stringArray[1..] : stringArray;

        if (!stringArray.Contains(separator))
            throw new ArgumentException(
                "The Enumerator expects a char which shall function as line splitter! If there is none" +
                "it cannot slice the ROS which shall be viewed as string[]");
    }

    public WordEnumerator(in TextChunk original, char separator) : this(original.Piece, separator)
    {
        _original = original;
    }

    public bool MoveNext()
    {
        //ReadOnlySpan<char> items = "abc <separator> def <separator> ghi <separator> jkl <separator> mno"
        int idxOfChar = _remainder.IndexOf(_separator);

        if (idxOfChar < 0)
            return false;

        var word = _remainder[..idxOfChar];
        _remainder = _remainder[(word.Length + 1)..];
        var (idx, len) = (0, 0);

        idx = _original.Piece.IndexOf(word, StringComparison.OrdinalIgnoreCase);
        len = word.Length;

        _tmp = new(word, _original.SystemColor, (idx, len));
        string debug = _tmp.Piece.ToString();

        return word.Length > 0;
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
    private MemoryRental<(int idx, int len)> _matchPool;
    private int _position;

    public TextStyleEnumerator(ReadOnlySpan<char> text)
    {
        //dont know a value yet for this but we use 50 for now
        _matchPool = new(50);
        _position = 0;
        _text = text;
        //var result = rgx.Split(_text);
        //{Black} This is a {Red} super nice {Green} shiny looking text
        matchData = _matchPool.Span;

        foreach (var enumerateMatch in rgx.EnumerateMatches(text))
        {
            //reset:
            if (_position < matchData.Length)
            {
                matchData[_position++] = (enumerateMatch.Index, enumerateMatch.Length);
            }
            // else
            // {
            //     matchData = _stackPool.Slice<(int idx, int len)>(0, matchData.Length*2);
            //     goto reset;
            // }
        }

        matchData = matchData.Where(x => x is { idx: >= 0, len: > 0 }).CopyInto(matchData);
        _position = 0;
    }

    [UnscopedRef] public ref readonly TextChunk Current => ref _current;

    public bool MoveNext()
    {
        if (_position >= matchData.Length)
            return false;

        ref readonly var match = ref matchData[_position];

        var colorCode = _text.Slice(match.idx, match.len);

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

    public void Dispose() => _matchPool.Dispose();
}

public readonly ref struct TextChunk
{
    public readonly Vector2 TextSize;
    public readonly ReadOnlySpan<char> Piece;
    public readonly Vector4 ImGuiColor;
    public readonly SysColor SystemColor;
    public readonly (int idx, int len) RelativeLocation;
    private readonly char _separator;

    /// <summary>
    /// represents the color we want to convert to a Vector4 type
    /// </summary>
    /// <param name="piece"></param>
    /// <param name="colorCode">the string colorname like {Black} or {Red}</param>
    /// <param name="relativeLocation"></param>
    /// <param name="separator"></param>
    public TextChunk(ReadOnlySpan<char> piece, ReadOnlySpan<char> colorCode, (int idx, int len) relativeLocation,
        char separator = ' ')
    {
        ReadOnlySpan<char> code;

        if (colorCode == ReadOnlySpan<char>.Empty)
        {
            code = "Black";
        }

        else if (colorCode.IndexOf('(') == -1)
        {
            code = colorCode;
        }
        else
        {
            code = colorCode[1..^1];
        }

        Piece = piece;
        RelativeLocation = relativeLocation;
        _separator = separator;
        var color = FastEnum.Parse<KnownColor, int>(code, true); //Enum.Parse<KnownColor>(code);
        Console.WriteLine(code.ToString());
        SystemColor = SysColor.FromKnownColor(color);
        ImGuiColor = ImGui.ColorConvertU32ToFloat4((uint)SystemColor.ToArgb());
        Vector2 offset = Vector2.One * 1.5f;
        TextSize = ImGui.CalcTextSize(Piece.ToString()) + offset;
    }

    public TextChunk(ReadOnlySpan<char> piece, SysColor color)
    {
        Piece = piece;
        SystemColor = color;
        TextSize = ImGui.CalcTextSize(piece.ToString());
        ImGuiColor = ImGui.ColorConvertU32ToFloat4((uint)SystemColor.ToArgb());
        RelativeLocation = (-1, -1);
    }

    public TextChunk(ReadOnlySpan<char> piece, (int idx, int len) occurence)
        : this(piece, ReadOnlySpan<char>.Empty, occurence)
    {
    }

    public TextChunk(ReadOnlySpan<char> current, SysColor sysCol,
        (int idx, int len) relativeLocation) : this(current, sysCol.Name, relativeLocation)
    {
    }

    [UnscopedRef]
    public WordEnumerator GetEnumerator() => new(this, _separator);
}