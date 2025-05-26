using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Text.RegularExpressions;

using DotNext.Buffers;
using ImGuiNET;

namespace Match_3.Service;

public readonly ref struct TextInfo
{
    public readonly ReadOnlySpan<char> MemberName2Replace, Slice2Colorize, ColorAsText;
    public readonly Vector4 ColorAsVec4;
    public readonly TileColor TileColor;
    private readonly char _separator;
    
    /// <summary>
    /// represents the color we want to convert to a Vector4 type
    /// </summary>
    /// <param name="slice2Colorize">the slice of a span like: (abc def ghi)</param>
    /// <param name="colorCode">the string colorCode like {Black} or {Red}</param>
    /// <param name="memberName2Replace"></param>
    /// <param name="separator">the char which is being used to iterate over each word within the piece</param>
    public TextInfo(ReadOnlySpan<char> slice2Colorize, ReadOnlySpan<char> colorCode,
                    ReadOnlySpan<char> memberName2Replace, char separator = ' ')
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
        ColorAsText = code.TrimEnd('\0');
        var color = Enum.Parse<KnownColor>(ColorAsText);
        TileColor = color; 
        ColorAsVec4 = Color.FromKnownColor(color).ToVec4();
        Vector2 offset = Vector2.One * 1.5f;
        //TextSize = ImGui.CalcTextSize(slice2Colorize) + offset;
        MemberName2Replace = memberName2Replace;
    }
    
    public TextInfo(ReadOnlySpan<char> current, Color sysCol, ReadOnlySpan<char> valuePlaceHolder) :
        this(current, sysCol.Name, valuePlaceHolder)
    {
    }

    public readonly Vector2 TextSize => ImGui.CalcTextSize(Slice2Colorize);

    public readonly override string ToString() => Slice2Colorize.ToString();
}

public ref partial struct FormatTextEnumerator
{
    private readonly bool _skipBlackColor;
    private readonly Span<(int idx, int len)> _colorPositions;
    private readonly ReadOnlySpan<char> _text;
    private readonly SpanOwner<(int idx, int len)> _matchPool;
    
    private int _position;
    private TextInfo _current;

    public FormatTextEnumerator(ReadOnlySpan<char> text, int nrOfSlices2Format=10, bool skipBlackColor=false)
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
    
    [GeneratedRegex(pattern: @"\((?!black\b)[A-Za-z]+[\s\0]*\)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FindNonBlackColorCodes();
}