using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using DotNext.Buffers;
using ImGuiNET;

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
    public readonly Vector4 ColorAsVec4;
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
        ColorAsText = code.TrimEnd('\0');
        var color = Enum.Parse<KnownColor>(ColorAsText);
        TileColor = color; 
        ColorAsVec4 = Color.FromKnownColor(color).ToVec4();
        Vector2 offset = Vector2.One * 1.5f;
        TextSize = ImGui.CalcTextSize(slice2Colorize) + offset;
        Variable2Replace = variable2Replace;
    }
    
    public TextInfo(ReadOnlySpan<char> current, Color sysCol, ReadOnlySpan<char> valuePlaceHolder) :
        this(current, sysCol.Name, valuePlaceHolder)
    {
    }

    public override string ToString() => Slice2Colorize.ToString();
    
    [UnscopedRef]
    public WordEnumerator GetEnumerator() => new(this, _separator);
}

public ref struct WordEnumerator
{
    private readonly char _separator;
    private readonly TextInfo _original;
    private TextInfo _tmp;
    private ReadOnlySpan<char> _remainder;

    [UnscopedRef] public ref readonly TextInfo Current => ref _tmp;

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

    public WordEnumerator(in TextInfo original, char separator) : this(original.Slice2Colorize, separator)
    {
        _original = original;
    }

    public bool MoveNext()
    {
        //ReadOnlySpan<char> items = "abc <separator> def <separator> ghi <separator> jkl <separator> mno"
        int idxOfChar = _remainder.IndexOf(_separator);
        ReadOnlySpan<char> word;

        //this separate check serves 2 purposes:
        //1. when "idxOfChar" is -1 and "separator" as well as "_remainder" are empty than indeed
        //its safe to assume that the entire thing is empty or was built up badly...
        if (idxOfChar == -1)
        {
            if (_separator is (char)32 && _remainder.Length == 0)
                return false;

            //the 2. purpose is to determine, when the above if condition is false, that there is really
            //only 1 word left and we have to treat this separately...
            word = _remainder;
            _remainder = [];
        }
        else
        {
            word = _remainder[..idxOfChar];
            _remainder = _remainder[(word.Length + 1)..];
        }

        _tmp = new(word, _original.TileColor.ToString(), []);

        return word.Length > 0;
    }

    [UnscopedRef]
    public WordEnumerator GetEnumerator()
    {
        return this;
    }
}

public ref partial struct FormatTextEnumerator
{
    private readonly bool _skipBlackColor;
    private readonly Span<(int idx, int len)> _colorPositions;
    private readonly ReadOnlySpan<char> _text;
    private TextInfo _current;
    private SpanOwner<(int idx, int len)> _matchPool;
    private int _position;

    public FormatTextEnumerator(ReadOnlySpan<char> text, int nrOfSlices2Format, bool skipBlackColor=false)
    {
        //dont know a value yet for this but we use 15 for now
        _matchPool = new(nrOfSlices2Format, false);
        _position = 0;
        _text = text;
        _skipBlackColor = skipBlackColor;
        //var result = rgx.Split(_text);
        //{Black} This is a {Red} super nice {Green} shiny looking text
        _colorPositions = _matchPool.Span;

        var colorFinder = skipBlackColor
            ? FindNonBlackColorCodes().EnumerateMatches(text)
            : FindAllColorCodes().EnumerateMatches(text);
        
        foreach (var enumerateMatch in colorFinder)
        {
            //reset:
            if (_position < _colorPositions.Length)
            {
                _colorPositions[_position++] = (enumerateMatch.Index, enumerateMatch.Length);
            }
        }

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
                                                  : [];
        
        _current = new(slice2Colorize, color2Use, variable2Replace);
        _position++;

        return slice2Colorize.Length > 0;
    }
    
    private bool GetNextColor()
    {
        if (_position >= _colorPositions.Length)
            return false;

        ref readonly var match = ref _colorPositions[_position];

        var color2Use = _text.Slice(match.idx, match.len);
        int beginOfBlack = match.idx + 1;
        int properStart = match.idx + match.len;
        int relativeEnd = _position < _colorPositions.Length - 1
            ? _text[beginOfBlack..].IndexOf('(') + beginOfBlack
            : _text.Length;
        
        _current = new(_text[properStart..relativeEnd], color2Use, []);
        
        _position++;
        
        return true;
    }
   
    public bool MoveNext()
    {
        return _skipBlackColor 
            ? GetNextNonBlackColor() 
            : GetNextColor();
    }
    
    [UnscopedRef]
    public ref readonly FormatTextEnumerator GetEnumerator()
    {
        return ref this;
    }
    
    public void Dispose()
    {
        _matchPool.Dispose();
    }

    [GeneratedRegex(pattern: @$"\([a-zA-Z0-9\0]+\)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FindAllColorCodes();
    
    [GeneratedRegex(pattern: @"\((?!black\b|Black\b)[A-Za-z]+\)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FindNonBlackColorCodes();
}