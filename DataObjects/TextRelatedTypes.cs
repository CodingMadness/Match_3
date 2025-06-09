using System.Runtime.InteropServices;
using DotNext.Runtime;
using Match_3.Service;
using Match_3.Workflow;

namespace Match_3.DataObjects;

public enum TextAlignmentRule
{
    ColoredSegmentsInOneLine,
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



// public readonly struct Segment
// {
//     public readonly View<char>? MemberName2Replace;
//     public readonly View<char> Slice2Colorize;
//
//     //Render Logic:
//     public readonly TextAlignmentRule? AlignmentRule;
//     public readonly CanvasOffset? PosInCanvas;
//     public readonly FadeableColor Colour;
//     public readonly Vector2? RenderPosition;
//     public readonly bool? ShouldWrap;
//
//     private (Vector2 start, float toWrapAt) GetRawOffset(CanvasOffset offset)
//     {
//         (Vector2 start, float toWrapAt) = (Vector2.Zero, 0f);
//         Vector2 canvas = Game.ConfigPerStartUp.WindowSize;
//         Vector2 center = new(canvas.X * 0.5f, canvas.Y * 0.5f);
//
//         (start, toWrapAt) = offset switch
//         {
//             CanvasOffset.TopLeft => (Vector2.Zero, center.X),
//             CanvasOffset.TopCenter => (start with { X = center.X, Y = 0f }, canvas.X),
//             CanvasOffset.TopRight => (start with { X = canvas.X, Y = 0f }, canvas.X),
//             CanvasOffset.BottomLeft => (start with { X = 0f, Y = canvas.Y }, center.X),
//             CanvasOffset.BottomCenter => (start with { X = center.X, Y = canvas.Y }, canvas.X),
//             CanvasOffset.BottomRight => (start with { X = canvas.X, Y = canvas.Y }, canvas.X),
//             CanvasOffset.MidLeft => (start with { X = 0f, Y = center.Y }, center.X),
//             CanvasOffset.Center => (start with { X = center.X, Y = center.Y }, canvas.X),
//             CanvasOffset.MidRight => (start with { X = canvas.X, Y = center.Y }, canvas.X),
//             _ => (Vector2.Zero, 0f)
//         };
//
//         return (start, toWrapAt);
//     }
//
//     public Segment(ReadOnlySpan<char> colorCode, ReadOnlySpan<char> slice2Colorize,
//         ReadOnlySpan<char> memberName2Replace, CanvasOffset? start,
//         TextAlignmentRule? alignmentRule)
//     {
//         ReadOnlySpan<char> code;
//
//         if (colorCode == ReadOnlySpan<char>.Empty)
//             code = "(Black)";
//         else if (!colorCode.Contains('('))
//             code = colorCode;
//         else
//             code = colorCode[1..^1];
//
//         Slice2Colorize = new(slice2Colorize.TrimEnd('\0').ToString());
//         var colorAsText = code.TrimEnd('\0').ToString();
//         Colour = Color.FromName(colorAsText);
//         MemberName2Replace = memberName2Replace is [] ? null : new(memberName2Replace.ToString());
//
//         if (start is not null)
//         {
//             //we have yet to 'clean' the "RenderPosition" after the call below
//             var result = GetRawOffset(start.Value);
//             bool isInCheck = result.toWrapAt - (result.start.X + TextSize.X) > 0;
//             bool isRightAlignmentRule = alignmentRule is TextAlignmentRule.ColoredSegmentsInOneLine;
//             ShouldWrap = isRightAlignmentRule && isInCheck;
//             RenderPosition = result.start;
//         }
//
//         AlignmentRule = alignmentRule;
//         PosInCanvas = start;
//     }
//
//     public Vector2 TextSize
//     {
//         get
//         {
//             // ReadOnlySpan<char> x = Slice2Colorize;
//             // var value = ImGui.CalcTextSize("test test");
//             // return value;
//             return Vector2.Zero;
//         }
//     }
//
//     public override string ToString() => ((ReadOnlySpan<char>)Slice2Colorize).ToString();
// }