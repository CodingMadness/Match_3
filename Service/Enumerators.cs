using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DotNext.Buffers;
using ImGuiNET;
using Match_3.DataObjects;

namespace Match_3.Service;

public readonly ref struct TextInfo
{
    public readonly ReadOnlySpan<char> MemberName2Replace, Text;
    public readonly FadeableColor Colour;
    public readonly (int spanIdx, int spanLen) Occurence;


    /// <summary>
    /// represents the color we want to convert to a Vector4 type
    /// </summary>
    /// <param name="slice2Colorize">the slice of a span like: (abc def ghi)</param>
    /// <param name="colorCode">the string colorCode like {Black} or {Red}</param>
    /// <param name="memberName2Replace">The member identifier inside a Class, like "nameof(Animal.Age)"</param>
    /// <param name="occurence"></param>
    public TextInfo(ReadOnlySpan<char> slice2Colorize, ReadOnlySpan<char> colorCode,
        ReadOnlySpan<char> memberName2Replace,
        (int spanIdx, int spanLen) occurence)
    {
        ReadOnlySpan<char> code;

        if (colorCode == ReadOnlySpan<char>.Empty)
        {
            code = "(Black)";
        }
        else if (!colorCode.Contains('('))
        {
            code = colorCode;
        }
        else
        {
            code = colorCode[1..^1]; //Color
        }

        Text = slice2Colorize.TrimEnd('\0');
        var colorAsText = code.TrimEnd('\0').ToString();
        Colour = Color.FromName(colorAsText);
        MemberName2Replace = memberName2Replace;
        Occurence = occurence;
    }

    public Vector2 TextSize => ImGui.CalcTextSize(Text);

    public override string ToString() => Text.ToString();
}

public unsafe ref struct WordEnumerator(scoped in TextInfo rootSegment, char separator = ' ')
{
    private TextInfo* _rootSegment = (TextInfo*)Unsafe.AsPointer(ref Unsafe.AsRef(in rootSegment));
    private TextInfo _currentWordInfo;
    private ReadOnlySpan<char> _remainder;

    [UnscopedRef] public ref readonly TextInfo Current => ref _currentWordInfo;

    [UnscopedRef] private ref readonly TextInfo RootSegment => ref *_rootSegment;

    public bool EndReached { get; private set; }

    public bool MoveNext()
    {
        //ReadOnlySpan<char> items = " abc <separator> def <separator> ghi <separator> jkl <separator> mno"
        const int separatorLen = 1;
        var rootText = RootSegment.Text[separatorLen..];
        bool veryFirstCallOfMoveNext = !EndReached && _remainder is [];
        _remainder = veryFirstCallOfMoveNext ? rootText : _remainder;

        //there has to be none word left in the remaining span,
        //because it can happen that an enumerator is actually finished, and the remainder is [],
        //but the enumerator used "MoveBack()" and has again exactly 1-item left, which makes the 'EndReached'
        //field being true which is semantically wrong and also when using "if (enumerator.EndReached) { DoStuff(); }"
        //it would not be called for the last word which was put back in via "MoveBack()"!
        if (EndReached)
        {
            return false;
        }

        int idxOfSeparator = _remainder.IndexOf(separator);
        ReadOnlySpan<char> word;

        //this separate check serves 2 purposes:
        //1. when "idxOfChar" is -1 and "separator" as well as "_remainder" are empty than indeed
        //it's safe to assume that the entire thing is empty or was built up badly...
        if (idxOfSeparator == -1)
        {
            if (_remainder.Length == 0)
                return false;

            //the 2. purpose is to determine, when the above if condition is false,
            //that there really was only 1-word in the span from the beginning,
            //and we have to treat this separately...
            word = _remainder;
            _remainder = [];
        }
        else
        {
            word = _remainder.Slice(0, idxOfSeparator + 1);
            _remainder = _remainder[word.Length..];
        }

        _currentWordInfo = new(word,
            RootSegment.Colour.Name,
            [],
            (_currentWordInfo.Occurence.spanIdx, word.Length));

        EndReached = _remainder is [];

        return true;
    }

    public bool MoveBack()
    {
        //current remainder: "those only really like "
        //desired remainder: "between those only really like "
        //root:              " and you have in between those only really like " 
        var root = RootSegment.Text;
        var currentPos = root.IndexOf(Current.Text);
        _remainder = root.Slice(currentPos);
        EndReached = false;
        return true;
    }

    public void Reset()
    {
        _remainder = [];
        _currentWordInfo = default;
        EndReached = false;
    }
}

public ref partial struct FormatTextEnumerator
{
    private readonly bool _skipBlackColor;
    private readonly Span<(int idx, int len)> _colorPositions;
    private readonly ReadOnlySpan<char> _text;
    private readonly WordEnumerator _wordEnumerator;

    private int _position;
    private TextInfo _phrase;

    public FormatTextEnumerator(ReadOnlySpan<char> text, int nrOfSlices2Format = 10, bool skipBlackColor = false)
    {
        //don't know a value yet for this, but we use 10 for now
        SpanOwner<(int idx, int len)> matchPool = new(nrOfSlices2Format, false);
        _position = 0;
        _text = text;
        _skipBlackColor = skipBlackColor;

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

        _wordEnumerator = new(in Unsafe.AsRef(in Current));
    }

    [UnscopedRef] public ref readonly TextInfo Current => ref _phrase;

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
        _text.Slice(match.idx, lengthTilNextColor + match.len);
        var occurence = (match.idx, match.len);
        _phrase = new(slice2Colorize, color2Use, variable2Replace, occurence);
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
            txt.Swap(allZeroes, desired);
        }

        var occurence = (match.idx, match.len);
        _phrase = new(txt, color2Use, [], occurence);

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
    public ref readonly WordEnumerator EnumerateSegment()
    {
        ref readonly var tmp = ref _wordEnumerator;
        ref var mutable = ref Unsafe.AsRef(in tmp);
        mutable.Reset();
        return ref tmp;
        //I need 'mutable.Current' to point to 
    }

    [GeneratedRegex(pattern: @"\([a-zA-Z0-9\0]+\)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FindAllColorCodes();

    [GeneratedRegex(pattern: @"\((?!black\b)[A-Za-z]+[\s\0]*\)",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FindNonBlackColorCodes();
}