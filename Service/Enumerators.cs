using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using DotNext.Buffers;
using ImGuiNET;
using Match_3.Variables.Extensions;
using Match_3.Workflow;
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
    public readonly ReadOnlySpan<char> Variable2Replace;
    public readonly ReadOnlySpan<char> Slice2Color;
    public readonly Vector2 TextSize;
    public readonly Vector4 ColorV4ToApply;
    // public readonly Color SystemColor;
    public readonly TileColor TileColor;
    private readonly char _separator;
    
    /// <summary>
    /// represents the color we want to convert to a Vector4 type
    /// </summary>
    /// <param name="slice2Color">the slice of a span like: (abc def ghi)</param>
    /// <param name="colorCode">the string colorCode like {Black} or {Red}</param>
    /// <param name="variable2Replace"></param>
    /// <param name="separator">the char which is being used to iterate over each word within the piece</param>
    public TextInfo(ReadOnlySpan<char> slice2Color, ReadOnlySpan<char> colorCode,
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

        Slice2Color = slice2Color;
        _separator = separator;
        //FastEnum.Parse<KnownColor, int>(code); for some reason this does not work.....
        var color = Enum.Parse<KnownColor>(code);
        TileColor = color;//Color.FromKnownColor(color);
        ColorV4ToApply = Color.FromKnownColor(color).ToVec4();
        Vector2 offset = Vector2.One * 1.5f;
        TextSize = ImGui.CalcTextSize(slice2Color) + offset;
        Variable2Replace = variable2Replace;
    }
    
    public TextInfo(ReadOnlySpan<char> current, Color sysCol, ReadOnlySpan<char> valuePlaceHolder) :
        this(current, sysCol.Name, valuePlaceHolder)
    {
    }

    [UnscopedRef]
    public WordEnumerator GetEnumerator() => new(this, _separator);

    public override string ToString() => Slice2Color.ToString();
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

    public WordEnumerator(in TextInfo original, char separator) : this(original.Slice2Color, separator)
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
            _remainder = ReadOnlySpan<char>.Empty;
        }
        else
        {
            word = _remainder[..idxOfChar];
            _remainder = _remainder[(word.Length + 1)..];
        }

        _tmp = new(word, _original.TileColor.ToStringFast(), "---");

        return word.Length > 0;
    }

    public WordEnumerator GetEnumerator()
    {
        return this;
    }
}

public ref partial struct PhraseEnumerator 
{
    private static readonly Regex Rgx = MyRegex();
    private TextInfo _current;
    private readonly Span<(int idx, int len)> colorPositions;
    private int _relativeStart, _relativeEnd;
    private readonly ReadOnlySpan<char> _text;
    private MemoryRental<(int idx, int len)> _matchPool;
    private int _position;

    public PhraseEnumerator(ReadOnlySpan<char> text)
    {
        //dont know a value yet for this but we use 50 for now
        _matchPool = new(15);
        _position = 0;
        _text = text;
        //var result = rgx.Split(_text);
        //{Black} This is a {Red} super nice {Green} shiny looking text
        colorPositions = _matchPool.Span;

        foreach (var enumerateMatch in Rgx.EnumerateMatches(text))
        {
            //reset:
            if (_position < colorPositions.Length)
            {
                colorPositions[_position++] = (enumerateMatch.Index, enumerateMatch.Length);
            }
        }

        colorPositions = colorPositions.Where(x => x is { idx: >= 0, len: > 0 }).CopyInto(colorPositions);
        _position = 0;
    }

    public PhraseEnumerator(StringBuilder text)
    {
        //dont know a value yet for this but we use 50 for now
        _matchPool = new(50);
        _position = 0;
        colorPositions = _matchPool.Span;
        //{Black} This is a {Red} super nice {Green} shiny looking text

        foreach (var chunk in text.GetChunks())
        {
            _text = chunk.Span;

            foreach (var enumerateMatch in Rgx.EnumerateMatches(_text))
            {
                //reset:
                if (_position < colorPositions.Length)
                {
                    colorPositions[_position++] = (enumerateMatch.Index, enumerateMatch.Length);
                }
            }
        }

        colorPositions = colorPositions.Slice(0, _position);
        _position = 0;
    }

    [UnscopedRef] public ref readonly TextInfo Current => ref _current;

    public bool MoveNext()
    {
        if (_position >= colorPositions.Length)
            return false;

        ref readonly var match = ref colorPositions[_position];

        var color2Use = _text.Slice(match.idx, match.len);

        _relativeStart = match.idx;
        int okayBegin = _relativeStart;

        if (_position + 1 < colorPositions.Length)
            _relativeEnd = colorPositions[_position + 1].idx;
        else
        {
            _relativeEnd = _text.Length - match.idx + 1;
            _relativeStart = 1;
        }

        
        var slice2Colorize = _text.Slice(okayBegin, _relativeEnd - _relativeStart)[(match.len + 1)..^1];

        // bool twoSameSpans = QuestBuilder.QuestLog.AsSpan() == _text;
            
        bool isAMemberName = slice2Colorize.Contains('.'); //like: Match.Count and so on...

        var variable2Replace = isAMemberName
                                                  ? slice2Colorize[..slice2Colorize.IndexOf(' ')]
                                                  : ReadOnlySpan<char>.Empty;
        
        _current = new(slice2Colorize, color2Use, variable2Replace);
        _position++;

        return slice2Colorize.Length > 0;
    }

    [UnscopedRef]
    public ref readonly PhraseEnumerator GetEnumerator()
    {
        return ref this;
    }

    public void Dispose() => _matchPool.Dispose();
    [GeneratedRegex("\\([a-zA-Z]+\\)", RegexOptions.Singleline)]
    private static partial Regex MyRegex();
}