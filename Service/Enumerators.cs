using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.RegularExpressions;

using DotNext.Buffers;
using ImGuiNET;
using Match_3.DataObjects;

namespace Match_3.Service;

public readonly ref struct TextInfo
{
    public readonly ReadOnlySpan<char> MemberName2Replace, Slice2Colorize;
    public readonly Vector4 ColorAsVec4;

    /// <summary>
    /// represents the color we want to convert to a Vector4 type
    /// </summary>
    /// <param name="slice2Colorize">the slice of a span like: (abc def ghi)</param>
    /// <param name="colorCode">the string colorCode like {Black} or {Red}</param>
    /// <param name="memberName2Replace"></param>
    public TextInfo(ReadOnlySpan<char> slice2Colorize, ReadOnlySpan<char> colorCode,
                    ReadOnlySpan<char> memberName2Replace)
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

        Slice2Colorize = slice2Colorize.TrimEnd('\0');
        var colorAsText = code.TrimEnd('\0');
        var color = Enum.Parse<TileColor>(colorAsText);
        ColorAsVec4 = FadeableColor.ToVec4(color);
        MemberName2Replace = memberName2Replace;
    }

    public Vector2 TextSize => ImGui.CalcTextSize(Slice2Colorize);

    public override string ToString() => Slice2Colorize.ToString();
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
        SpanOwner<(int idx, int len)> matchPool =
            //don't know a value yet for this but we use 15 for now
            new(nrOfSlices2Format, false);
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
        
        //we need here to check if we have empty chars inside this slice and get rid of them
        //span looks like: {11           Matches }
        var tmp = _text[properStart..relativeEnd];
        
        if (tmp.Contains(char.MinValue))
        {
            Range allZeroes = tmp.IndexOf(char.MinValue)..tmp.LastIndexOf(char.MinValue);
            Range desired = (allZeroes.End.Value + 1)..;
            tmp.AsWriteable().Swap(allZeroes, desired);
        }

        _current = new(tmp, color2Use, []);
        
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