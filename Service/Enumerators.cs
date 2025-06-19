using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using DotNext.Buffers;

using Match_3.DataObjects;

namespace Match_3.Service;

public unsafe ref struct WordEnumerator(scoped in Segment rootSegment, char separator = ' ') : IDisposable
{
    public readonly ref readonly Segment RootSegment = ref rootSegment;
    private Segment _currentWordInfo;
    private ReadOnlySpan<char> _remainder;

    [UnscopedRef] public ref readonly Segment Current => ref _currentWordInfo;

    public bool EndReached { get; private set; }

    public bool MoveNext()
    {
        //RootSegment.Slice2Colorize = " Hey <separator> my <separator> friend <separator> how <separator> are <separator> you"
        ReadOnlySpan<char> rootText = RootSegment.Slice2Colorize;
        rootText = rootText[1..];
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
        //1. when "idxOfChar" is -1 and "separator" as well as "_remainder" are empty than,
        //indeed it's safe to assume that the entire thing is empty or was built up badly...
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

        var colorName = RootSegment.Colour.Name.AsSpan();
        _currentWordInfo = new(colorName,
            word,
            [],
            RootSegment.PosInCanvas,
            RootSegment.AlignmentRule);

        EndReached = _remainder is [];

        return true;
    }

    public void Reset()
    {
        _remainder = [];
        _currentWordInfo = default;
        EndReached = false;
    }

    public readonly void Dispose()
    {
        ref var blackWordsEnumerator = ref Unsafe.AsRef(in this);
        ref var nullRefReference = ref Unsafe.AsRef(in blackWordsEnumerator.RootSegment);
        nullRefReference = ref Unsafe.NullRef<Segment>();
        blackWordsEnumerator.Reset();
    }
}

public ref partial struct FormatTextEnumerator
{
    private int _position;

    private readonly SpanOwner<Segment?> _allSegments;
    private readonly ReadOnlySpan<char> _text;
    private WordEnumerator _wordEnumerator;
    private readonly TextAlignmentRule? AlignmentRule;

    public Vector2 TotalTextSize
    {
        get;
        init
        {
            field = value;

            switch (AlignmentRule)
            {
                case TextAlignmentRule.ColoredSegmentsInOneLine:
                {
                    foreach (var segment in _allSegments.Span)
                    {
                        if (segment is null)
                            continue;

                        if (segment.Value.ShouldWrap is not null &&
                            segment.Value.ShouldWrap.Value)
                        {
                            field += segment.Value.TextSize;
                        }
                    }
                }
                    break;
            }
        }
    }

    private Segment GetNextSegment(ValueMatch match, CanvasOffset? offset,
        TextAlignmentRule? rule)
    {
        var color2Use = _text.Slice(match.Index, match.Length);
        int endOfColorCode = match.Index + match.Length;
        int beginOfSlice2Colorize = endOfColorCode + 1;
        var textStartAtSlice2Colorize = _text[beginOfSlice2Colorize..];
        var slice2Colorize = textStartAtSlice2Colorize;
        int beginOfNextColorCode = textStartAtSlice2Colorize.IndexOf('(');

        slice2Colorize = beginOfNextColorCode > -1 ?
                            textStartAtSlice2Colorize.Slice(0, beginOfNextColorCode) :
                            textStartAtSlice2Colorize;

        bool isAMemberName = slice2Colorize.Contains('.');
        Range onlyTheMember = ..slice2Colorize.IndexOf(' ');
        var fieldValue2Replace = isAMemberName
            ? slice2Colorize[onlyTheMember]
            : [];
        var occurence = (match.Index, match.Length);
        Segment phrase = new(color2Use, slice2Colorize, fieldValue2Replace, offset, rule);
        return phrase;
    }

    public FormatTextEnumerator(ReadOnlySpan<char> text,
        CanvasOffset? offset = null,
        TextAlignmentRule? alignmentRule = null,
        int nrOfSlices2Format = 10,
        bool skipBlackColor = false)
    {
        //don't know the value yet for this, but we use 10 for now
        _allSegments = new(nrOfSlices2Format, false);
        _position = 0;
        _text = text;
        AlignmentRule = alignmentRule;

        //{Black} This is a {Red} super nice {Green} shiny looking text
        var colorFinder = FindAllColorCodes().EnumerateMatches(text);

        foreach (var result in colorFinder)
        {
            if (skipBlackColor)
            {
                if (_position % 2 == 0)
                {
                    _position++;
                    continue;
                }
            }

            _allSegments[_position++] = GetNextSegment(result, offset, alignmentRule);
            //Console.WriteLine($"Start:{result.Index}, Length: {result.Length}");
        }

        _allSegments = _allSegments.Span[.._position];

        if (!skipBlackColor)
        {
            //only here we need the 1st index to be valid since there are also black-color coded ones in the pool
            _position = 0;
            _wordEnumerator = new(in Unsafe.AsRef(in Current));
        }
        //needed to skip segments from the pool based on if we actually need black colorcodes
        _position = -1;
    }

    public static FormatTextEnumerator CreateQuestLogEnumerator(ReadOnlySpan<char> text)
    {
        var qlEnumerator = new FormatTextEnumerator(text, nrOfSlices2Format: 5, skipBlackColor: true);
        return qlEnumerator;
    }

    public ref readonly Segment Current => ref SpanUtility.RefValue(_allSegments[_position]);

    public bool MoveNext()
    {
        bool skipBlackColor = _allSegments[0] is null;

        if (skipBlackColor)
        {
            _position += 2;
        }
        else
        {
            _position++;
        }
        return _position < _allSegments.Length;
    }

    [UnscopedRef]
    public ref readonly WordEnumerator GetCleanWordEnumerator()
    {
        ref readonly var tmp = ref _wordEnumerator;
        ref var mutable = ref Unsafe.AsRef(in tmp);
        mutable.Reset();
        return ref tmp;
    }

    [GeneratedRegex(pattern: @"\([a-zA-Z0-9\0]+\)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FindAllColorCodes();
}