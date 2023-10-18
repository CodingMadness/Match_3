using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using DotNext.Buffers;
using ImGuiNET;
using NoAlloq;

namespace Match_3.Service;

/// <summary>
/// A faster span enumerator than .NET currently provides
/// </summary>
/// <typeparam name="TItem"></typeparam>
public ref struct FastSpanEnumerator<TItem>
{
    private ref TItem _currentItem;
    private readonly ref TItem _lastItemOffsetByOne;

    public FastSpanEnumerator(Span<TItem> span) :
        this(ref MemoryMarshal.GetReference(span), span.Length)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private FastSpanEnumerator(ref TItem item, nint length)
    {
        //we store 1 address BEHIND '_currentItem' in order to have an 'invalid' address, so that our "MoveNext()"
        //func can do + 1 and be at the 0t index! and vice versa with '_lastItemOffsetByOne'
        _currentItem = ref Unsafe.Subtract(ref item, 1);
        _lastItemOffsetByOne = ref Unsafe.Add(ref _currentItem, length + 1);
    }

    [UnscopedRef] public ref TItem Current => ref _currentItem;


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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [UnscopedRef]
    public ref FastSpanEnumerator<TItem> GetEnumerator()
    {
        return ref this;
    }
}

public readonly ref struct TextInfo
{
    public readonly ReadOnlySpan<char> Variable2Replace, Slice2Colorize, ColorAsText;
    public readonly Vector2 TextSize;
    public readonly Vector4 ColorV4ToApply;
    // public readonly Color SystemColor;
    public readonly TileColor TileColor;
    private readonly char _separator;
    
    /// <summary>
    /// represents the color we want to convert to a Vector4 type
    /// </summary>
    /// <param name="slice2Colorize">the slice of a span like: (abc def ghi)</param>
    /// <param name="colorCode">the string colorCode like {Black} or {Red}</param>
    /// <param name="variable2Replace"></param>
    /// <param name="separator">the char which is being used to iterate over each word within the piece</param>
    public TextInfo(ReadOnlySpan<char> slice2Colorize, ReadOnlySpan<char> colorCode,
                    ReadOnlySpan<char> variable2Replace, char separator = ' ')
    {
        ReadOnlySpan<char> code;

        if (colorCode == ReadOnlySpan<char>.Empty)
        {
            code = "Black";
        }
        else if (!colorCode.Contains('('))
        {
            code = colorCode;
        }
        else
        {
            code = colorCode[1..^1];
        }

        Slice2Colorize = slice2Colorize;
        _separator = separator;
        //FastEnum.Parse<KnownColor, int>(code); for some reason this does not work.....
        ColorAsText = code;
        var color = Enum.Parse<KnownColor>(code);
        TileColor = color; 
        ColorV4ToApply = Color.FromKnownColor(color).ToVec4();
        Vector2 offset = Vector2.One * 1.5f;
        TextSize = ImGui.CalcTextSize(slice2Colorize) + offset;
        Variable2Replace = variable2Replace;
    }
    
    public TextInfo(ReadOnlySpan<char> current, Color sysCol, ReadOnlySpan<char> valuePlaceHolder) :
        this(current, sysCol.Name, valuePlaceHolder)
    {
    }

    public override string ToString() => Slice2Colorize.ToString();
}

public ref partial struct PhraseEnumerator
{
    private TextInfo _current;
    private readonly Span<(int idx, int len)> _colorPositions;
    private readonly ReadOnlySpan<char> _text;
    private MemoryRental<(int idx, int len)> _matchPool;
    private int _position;
    private readonly bool _skipBlackColor;

    public PhraseEnumerator(ReadOnlySpan<char> text, bool skipBlackColor=false)
    {
        //dont know a value yet for this but we use 15 for now
        _matchPool = new(5, false);
        
        _position = 0;
        _text = text;
        _skipBlackColor = skipBlackColor;
        //var result = rgx.Split(_text);
        //{Black} This is a {Red} super nice {Green} shiny looking text
        _colorPositions = _matchPool.Span;

        //slice from 0..7 should be (Black), always!
        var colorFinder = skipBlackColor ? FindNonBlackColorCodes() : FindColorCodes();
        
        foreach (var enumerateMatch in colorFinder.EnumerateMatches(text))
        {
            //reset:
            if (_position < _colorPositions.Length)
            {
                _colorPositions[_position++] = (enumerateMatch.Index, enumerateMatch.Length);
            }
        }

        //ArrayPool<char>.Shared.Return(,);
        int d=1;
        _colorPositions = _colorPositions[.._position];
        _position = 0;
    }
    
    [UnscopedRef] public ref readonly TextInfo Current => ref _current;

    private bool GetNextNonBlackColor()
    {
        if (_position == _colorPositions.Length)
            return false;
        
        ref var match = ref _colorPositions[_position];
        var color2Use = _text.Slice(match.idx, match.len);
        int startOfSlice2Color = match.idx + match.len + 1;
        int lengthTilNextColor = _position < _colorPositions.Length - 1
            ? _text[startOfSlice2Color..].IndexOf('(')
            : _text.Length - startOfSlice2Color;
        var slice2Colorize = _text.Slice(startOfSlice2Color, lengthTilNextColor);
        bool isAMemberName = slice2Colorize.Contains('.'); //like: Match.Count and so on...
        var variable2Replace = isAMemberName                  
                                                  ? slice2Colorize[..slice2Colorize.IndexOf(' ')] 
                                                  : ReadOnlySpan<char>.Empty;
        
        _current = new(slice2Colorize, color2Use, variable2Replace);
        _position++;

        return slice2Colorize.Length > 0;
    }
    
    public bool GetNextColor()
    {
        if (_position >= _colorPositions.Length)
            return false;

        ref readonly var match = ref _colorPositions[_position];

        var color2Use = _text.Slice(match.idx, match.len);

        int relativeEnd;
        int relativeStart = match.idx;
        int okayBegin = relativeStart;

        if (_position + 1 < _colorPositions.Length)
            relativeEnd = _colorPositions[_position + 1].idx;
        else
        {
            relativeEnd = _text.Length - match.idx + 1;
            relativeStart = 1;
        }
        
        var slice2Colorize = _text.Slice(okayBegin, relativeEnd - relativeStart)[(match.len + 1)..^1];

        bool isAMemberName = slice2Colorize.Contains('.'); //like: Match.Count and so on...

        var variable2Replace = isAMemberName
            ? slice2Colorize[..slice2Colorize.IndexOf(' ')]
            : ReadOnlySpan<char>.Empty;
        
        _current = new(slice2Colorize, color2Use, variable2Replace);
        _position++;

        return slice2Colorize.Length > 0;
    }
    public bool MoveNext()
    {
        return _skipBlackColor 
            ? GetNextNonBlackColor() 
            : GetNextColor();
    }
    
    [UnscopedRef]
    public ref readonly PhraseEnumerator GetEnumerator()
    {
        return ref this;
    }

    public void Dispose()
    {
        // _matchPool.Span.Clear();
        _matchPool.Dispose();
    }

    [GeneratedRegex(pattern: @"\([a-zA-Z]+\)", RegexOptions.Singleline)]
    private static partial Regex FindColorCodes();
    
    [GeneratedRegex(pattern: @"\((?!black\b|Black\b)[A-Za-z]+\)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FindNonBlackColorCodes();
}