using System.Diagnostics.CodeAnalysis;
using DotNext.Buffers;

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

public readonly ref struct TextChunk
{
    public readonly ReadOnlySpan<char> Piece;
    public readonly Vector2 TextSize;
    public readonly Vector4 ImGuiColorAsVec4;
    public readonly SysColor SystemColor;
    private readonly char _separator;

    private static Vector4 ToVector4(SysColor color)
    {
        return new (
            color.R / 255.0f,
            color.G / 255.0f,
            color.B / 255.0f,
            color.A / 255.0f);
    }

    /// <summary>
    /// represents the color we want to convert to a Vector4 type
    /// </summary>
    /// <param name="piece">the slice of a span like: (abc def ghi)</param>
    /// <param name="colorCode">the string colorCode like {Black} or {Red}</param>
    /// <param name="separator">the char which is being used to iterate over each word within the piece</param>
    public TextChunk(ReadOnlySpan<char> piece, ReadOnlySpan<char> colorCode,
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
        _separator = separator;
        //FastEnum.Parse<KnownColor, int>(code); for some reason this does not work.....
        var color = Enum.Parse<KnownColor>(code); 
        SystemColor = SysColor.FromKnownColor(color);
        ImGuiColorAsVec4 = ToVector4(SystemColor);
        Vector2 offset = Vector2.One * 1.5f;
        TextSize = ImGui.CalcTextSize(Piece) + offset;
    }

    public TextChunk(ReadOnlySpan<char> current, SysColor sysCol) :
        this(current, sysCol.Name)
    {
    }

    [UnscopedRef]
    public WordEnumerator GetEnumerator() => new(this, _separator);
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
    /// An Enumerator who iterates over an array of string interpreted as an array of words, stored as ROS
    /// </summary>
    /// <param name="stringArray">the ROS which is interpreted as an array of words</param>
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
        //var (idx, len) = (0, 0);
        // idx = _original.Piece.IndexOf(word, StringComparison.OrdinalIgnoreCase);
        // len = word.Length;

        _tmp = new(word, _original.SystemColor);

        return word.Length > 0;
    }

    public WordEnumerator GetEnumerator()
    {
        return this;
    }
}

public ref struct TextStyleEnumerator
{
    private static readonly Regex Rgx = new(@"\([a-zA-Z]+\)", RegexOptions.Singleline);
    private TextChunk _current;
    private readonly Span<(int idx, int len)> _matchData;
    private int _relativeStart, _relativeEnd;
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
        _matchData = _matchPool.Span;
        
        foreach (var enumerateMatch in Rgx.EnumerateMatches(text))
        {
            //reset:
            if (_position < _matchData.Length)
            {
                _matchData[_position++] = (enumerateMatch.Index, enumerateMatch.Length);
            }
        }

        _matchData = _matchData.Where(x => x is { idx: >= 0, len: > 0 }).CopyInto(_matchData);
        _position = 0;
    }

    [UnscopedRef] public ref readonly TextChunk Current => ref _current;

    public bool MoveNext()
    {
        if (_position >= _matchData.Length)
            return false;

        ref readonly var match = ref _matchData[_position];

        var colorCode = _text.Slice(match.idx, match.len);

        _relativeStart = match.idx;
        int okayBegin = _relativeStart;

        if (_position + 1 < _matchData.Length)
            _relativeEnd = _matchData[_position + 1].idx;
        else
        {
            _relativeEnd = _text.Length - match.idx + 1;
            _relativeStart = 1;
        }

        //part0: (Black) You have to collect (Red) xxx
        var tmp = _text.Slice(okayBegin, _relativeEnd - _relativeStart);
        tmp = tmp[match.len..^1];
        _current = new(tmp, colorCode);
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