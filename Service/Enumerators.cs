using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Text.RegularExpressions;

using DotNext.Buffers;
using ImGuiNET;
using Match_3.DataObjects;

namespace Match_3.Service;

public readonly ref struct TextInfo
{
    public readonly ReadOnlySpan<char> MemberName2Replace, Text, TextWithColorCode, ColorCode;
    public readonly Vector4 ColorAsVec4;
    public readonly TileColor ColorKind;
    public readonly (int spanIdx, int spanLen) Occurence;
    
    /// <summary>
    /// represents the color we want to convert to a Vector4 type
    /// </summary>
    /// <param name="slice2Colorize">the slice of a span like: (abc def ghi)</param>
    /// <param name="colorCode">the string colorCode like {Black} or {Red}</param>
    /// <param name="memberName2Replace">The member identifier inside a Class, like "nameof(Animal.Age)"</param>
    /// <param name="textWithColorCode">The color code with the corresponding text</param>
    /// <param name="occurence"></param>
    public TextInfo(ReadOnlySpan<char> slice2Colorize, ReadOnlySpan<char> colorCode,
        ReadOnlySpan<char> memberName2Replace, ReadOnlySpan<char> textWithColorCode,
        (int spanIdx, int spanLen) occurence)
    {
        ReadOnlySpan<char> code = [];

        if (colorCode == ReadOnlySpan<char>.Empty)
        {
            code = "(Black)";
        }
        else if (!colorCode.Contains('('))
        {
            code = colorCode;
            ColorCode = code;
        }
        else
        {
            ColorCode = colorCode;       //(Color)
            code = colorCode[1..^1];    //Color
        }

        Text = slice2Colorize.TrimEnd('\0');
        var colorAsText = code.TrimEnd('\0');
        TextWithColorCode = textWithColorCode;
        ColorKind = Enum.Parse<TileColor>(colorAsText);
        ColorAsVec4 = FadeableColor.ToVec4(ColorKind);
        MemberName2Replace = memberName2Replace;
        Occurence = occurence;
    }

    public Vector2 TextSize => ImGui.CalcTextSize(Text);

    public override string ToString() => Text.ToString();
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
    /// <param name="span">the ROS which is interpreted as an array of words</param>
    /// <param name="separator">the character who will be used to split the ROS</param>
    private WordEnumerator(ReadOnlySpan<char> span, char separator)
    {
        _separator = separator;

        _remainder = span.Contains(separator) ? span[1..] : span;

        if (!span.Contains(separator))
            throw new ArgumentException(
                "The Enumerator expects a char which shall function as line splitter! If there is none" +
                "it cannot slice the ROS which shall be viewed as string[]");
    }

    public WordEnumerator(in TextInfo original, char separator) : this(original.Text, separator)
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
        //it's safe to assume that the entire thing is empty or was built up badly...
        if (idxOfChar == -1)
        {
            if (_separator is (char)32 && _remainder.Length == 0)
                return false;

            //the 2. purpose is to determine, when the above if condition is false, that there is really
            //only 1 word left, and we have to treat this separately...
            word = _remainder;
            _remainder = [];
        }
        else
        {
            word = _remainder[..(idxOfChar + 1)];
            _remainder = _remainder[(word.Length)..];
        }

        _tmp = new(word, 
                             _original.ColorKind.ToString(),
                             [], 
                             [],
                             (_tmp.Occurence.spanIdx, word.Length));

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

    private int _position;
    private TextInfo _current;

    public FormatTextEnumerator(ReadOnlySpan<char> text, int nrOfSlices2Format=10, bool skipBlackColor=false)
    {
        //don't know a value yet for this, but we use 10 for now
        SpanOwner<(int idx, int len)> matchPool = new(nrOfSlices2Format, false);
        _position = 0;
        _text = text;
        _skipBlackColor = skipBlackColor;
        //var result = rgx.Split(_text);
        //{Black} This is a {Red} super nice {Green} shiny looking text
        _colorPositions = matchPool.Span;

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
        int text = match.idx + match.len + 1;
        int lengthTilNextColor = _position < _colorPositions.Length - 1
            ? _text[text..].IndexOf('(')
            : _text.Length - text;
        var slice2Colorize = _text.Slice(text, lengthTilNextColor);
        bool isAMemberName = slice2Colorize.Contains('.'); //like: Match.Count and so on...
        var variable2Replace = isAMemberName                  
                                                  ? slice2Colorize[..slice2Colorize.IndexOf(' ')] 
                                                  : [];
        var textWithColorCode = _text.Slice(match.idx, lengthTilNextColor);
        var occurence = (match.idx, match.len);
        _current = new(slice2Colorize, color2Use, variable2Replace, textWithColorCode, occurence);
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
        int lengthTilNextColor = _position < _colorPositions.Length - 1
            ? _text[beginOfBlack..].IndexOf('(') + beginOfBlack
            : _text.Length;
        
        //we need here to check if we have empty chars inside this slice and get rid of them
        //span looks like: {11 Matches }
        var txt = _text[properStart..lengthTilNextColor];
        
        //try to remove them if you can, if not, execute the below code
        if (txt.TrimEnd('\0').Contains(char.MinValue))
        {
            Range allZeroes = txt.IndexOf(char.MinValue)..txt.LastIndexOf(char.MinValue);
            Range desired = (allZeroes.End.Value + 1)..;
            txt.AsWriteable().Swap(allZeroes, desired);
        }
        var textWithColorCode = _text[match.idx..lengthTilNextColor];
        var occurence = (match.idx, match.len);
        _current = new(txt, color2Use, [], textWithColorCode, occurence);
        
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

    [GeneratedRegex(pattern: @"\([a-zA-Z0-9\0]+\)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FindAllColorCodes();
    
    [GeneratedRegex(pattern: @"\((?!black\b)[A-Za-z]+[\s\0]*\)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FindNonBlackColorCodes();
}