using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CommunityToolkit.HighPerformance;
using DotNext.Buffers;
using DotNext.Runtime;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Workflow;

namespace Match_3.Service;

public enum TextAlignmentRule
{
    ColoredSegmentsInOneLine,
    BlackSegmentsInOneLine,
}

/// <summary>
/// Non-ref struct wrapper around ROS in order to store in non-ref structs
/// </summary>
/// <param name="data"></param>
/// <typeparam name="T"></typeparam>
public readonly struct View<T>(in ReadOnlySpan<T> data)
{
    //wraps "ref char _reference" from ReadOnlySpan<char> because I cannot use ref in non-ref struct
    private readonly ValueReference<T> _first = new(ref data.Mutable()[0]);

    public int Length { get; init; } = data.Length;

    private ref readonly T First => ref _first.Value;

    public ref T this[int index] => ref _first.Value;

    public static implicit operator ReadOnlySpan<T>(View<T> wrapper)
        => MemoryMarshal.CreateReadOnlySpan(in wrapper.First, wrapper.Length);

    public override string ToString() => ((ReadOnlySpan<T>)this).ToString();
}

public readonly struct Segment
{
    public readonly View<char>? MemberName2Replace;
    public readonly View<char> Slice2Colorize;
    public readonly (int spanIdx, int spanLen) Occurence;

    //Render Logic:
    public readonly TextAlignmentRule? AlignmentRule;
    public readonly CanvasOffset? PosInCanvas;
    public readonly FadeableColor Colour;
    public readonly Vector2? RenderPosition;
    public readonly bool? ShouldWrap;

    private (Vector2 start, float toWrapAt) GetRawOffset(CanvasOffset offset)
    {
        (Vector2 start, float toWrapAt) = (Vector2.Zero, 0f);
        Vector2 canvas = Game.ConfigPerStartUp.WindowSize;
        Vector2 center = new(canvas.X * 0.5f, canvas.Y * 0.5f);

        (start, toWrapAt) = offset switch
        {
            CanvasOffset.TopLeft => (Vector2.Zero, center.X),
            CanvasOffset.TopCenter => (start with { X = center.X, Y = 0f }, canvas.X),
            CanvasOffset.TopRight => (start with { X = canvas.X, Y = 0f }, canvas.X),
            CanvasOffset.BottomLeft => (start with { X = 0f, Y = canvas.Y }, center.X),
            CanvasOffset.BottomCenter => (start with { X = center.X, Y = canvas.Y }, canvas.X),
            CanvasOffset.BottomRight => (start with { X = canvas.X, Y = canvas.Y }, canvas.X),
            CanvasOffset.MidLeft => (start with { X = 0f, Y = center.Y }, center.X),
            CanvasOffset.Center => (start with { X = center.X, Y = center.Y }, canvas.X),
            CanvasOffset.MidRight => (start with { X = canvas.X, Y = center.Y }, canvas.X),
            _ => (Vector2.Zero, 0f)
        };

        return (start, toWrapAt);
    }

    public Segment(ReadOnlySpan<char> colorCode, ReadOnlySpan<char> slice2Colorize,
        ReadOnlySpan<char> memberName2Replace,
        (int spanIdx, int spanLen) occurence, CanvasOffset? start,
        TextAlignmentRule? alignmentRule)
    {
        ReadOnlySpan<char> code;

        if (colorCode == ReadOnlySpan<char>.Empty)
            code = "(Black)";
        else if (!colorCode.Contains('('))
            code = colorCode;
        else
            code = colorCode[1..^1];

        Slice2Colorize = new(slice2Colorize.TrimEnd('\0').ToString());
        var colorAsText = code.TrimEnd('\0').ToString();
        Colour = Color.FromName(colorAsText);
        MemberName2Replace = memberName2Replace is [] ? null : new(memberName2Replace.ToString());

        if (start is not null)
        {
            //we have yet to 'clean' the "RenderPosition" after the call below
            var result = GetRawOffset(start.Value);
            bool isInCheck = result.toWrapAt - (result.start.X + TextSize.X) > 0;
            bool isRightAlignmentRule = alignmentRule is TextAlignmentRule.ColoredSegmentsInOneLine;
            ShouldWrap = isRightAlignmentRule && isInCheck;
            RenderPosition = result.start;
        }

        Occurence = occurence;
        AlignmentRule = alignmentRule;
        PosInCanvas = start;
    }

    public Vector2 TextSize
    {
        get
        {
            // ReadOnlySpan<char> x = Slice2Colorize;
            // var value = ImGui.CalcTextSize("test test");
            // return value;
            return Vector2.Zero;
        }
    }

    public override string ToString() => ((ReadOnlySpan<char>)Slice2Colorize).ToString();
}

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

        var occurence = (_currentWordInfo.Occurence.spanIdx, word.Length);
        var colorName = RootSegment.Colour.Name.AsSpan();
        _currentWordInfo = new(colorName,
            word,
            [],
            occurence,
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
    private readonly WordEnumerator _wordEnumerator;
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
        Segment phrase = new(color2Use, slice2Colorize, fieldValue2Replace, occurence, offset, rule);
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
        //we set to -1 in order for 'MoveNext()' logic to work
        _position = -1;

        if (!skipBlackColor)
            _wordEnumerator = new(in Unsafe.AsRef(in Current));
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