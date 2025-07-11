using System.Runtime.InteropServices;

using DotNext.Runtime;

using ImGuiNET;

using Match_3.Service;
using Match_3.Workflow;
using Raylib_cs;
using Color = System.Drawing.Color;

namespace Match_3.DataObjects;

public enum WrappingRule
{
    PatternBased,
    WindowBased
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

    public ref readonly T First => ref _first.Value;
    
    public static implicit operator ReadOnlySpan<T>(View<T> wrapper)
        => MemoryMarshal.CreateReadOnlySpan(in wrapper.First, wrapper.Length);
    
    public ReadOnlySpan<T> AsSpan() => (ReadOnlySpan<T>)this;
    
    public static implicit operator View<T>(scoped in ReadOnlySpan<T> wrapper) => new(wrapper);

    public override string ToString() => AsSpan().ToString();

    public ref T GetPinnableReference() => ref _first.GetPinnableReference();
}

public readonly struct Segment
{
    public readonly View<char>? MemberName2Replace;
    public readonly View<char> Slice2Colorize;
    
    //Render Logic:
    public readonly WrappingRule? AlignmentRule;
    public readonly CanvasOffset? PosInCanvas;
    public readonly FadeableColor Colour;
    public readonly Vector2? RenderPosition;
    public readonly bool? ShouldWrap;

    public (Vector2 start, float toWrapAt) GetRawOffset(CanvasOffset offset)
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
        ReadOnlySpan<char> memberName2Replace, CanvasOffset? start,
        WrappingRule? alignmentRule)
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
            // var result = GetRawOffset(start.Value);
            // bool isInCheck = result.toWrapAt - (result.start.X + TextSize.X) > 0;
            // bool isRightAlignmentRule = alignmentRule is WrappingRule.ColoredSegmentsInOneLine;
            // ShouldWrap = isRightAlignmentRule && isInCheck;
            // RenderPosition = result.start;
        }

        AlignmentRule = alignmentRule;
        PosInCanvas = start;
    }

    public Vector2 TextSize
    {
        get
        {
            var value = ImGui.CalcTextSize(Slice2Colorize);
            return value;
        }
    }

    public override string ToString() => ((ReadOnlySpan<char>)Slice2Colorize).ToString();
}

public record TextInfo(bool ShallWrap, Vector2? WrapAt, 
    float Size, ValueReference<byte> FontDataPtr, 
    WrappingRule Rule, FadeableColor Color);

/* 'Publishers' are:
 * Render-API (ImGui, raylib, UI-frameworks)
 * File-Operations (locally)
 * NFS-operations
 * Database-API
 */
/* 'VirtualObjects'
 *   - AssetFile is a virtual-map for an existing local file-and-folder Structure
 *   - Segment is a virtual-map for a piece of text which can be drawn
 *   - GameObject is a virtual-map for a bunch of pixel-objects which can be drawn
 */
public interface IPublishable<out T>
{
    public T? VirtualObject { get; }
}

public interface IDrawable<out T> : IPublishable<T>
{
    public Vector2 GetRawOffset(CanvasOffset offset)
    {
        Vector2 start = Vector2.Zero;
        Vector2 canvas = Game.ConfigPerStartUp.WindowSize;
        Vector2 center = new(canvas.X * 0.5f, canvas.Y * 0.5f);

        start = offset switch
        {
            CanvasOffset.TopLeft => Vector2.Zero,
            CanvasOffset.TopCenter => start with { X = center.X, Y = 0f },
            CanvasOffset.TopRight => start with { X = canvas.X, Y = 0f },
            CanvasOffset.BottomLeft => start with { X = 0f, Y = canvas.Y },
            CanvasOffset.BottomCenter => start with { X = center.X, Y = canvas.Y },
            CanvasOffset.BottomRight => start with { X = canvas.X, Y = canvas.Y },
            CanvasOffset.MidLeft => start with { X = 0f, Y = center.Y },
            CanvasOffset.Center => start with { X = center.X, Y = center.Y },
            CanvasOffset.MidRight => start with { X = canvas.X, Y = center.Y },
            _ => Vector2.Zero
        };

        return start;
    }
}

public record TextRenderSegment(Segment VirtualObject, TextInfo Info) : IDrawable<Segment>;

public record DrawableGameObject(IGameObject VirtualObject) : IDrawable<IGameObject>;

public record DrawableTile(IGameObject Tile, Texture2D SpriteSheet, TileColorTypes TileKind) : DrawableGameObject(Tile)
{
    public Vector2 OffsetInSpriteSheet
    {
        get
        {
            return TileKind switch
            {
                TileColorTypes.LightBlue => new(1f, 3f),
                TileColorTypes.Turquoise => new Vector2(2f, 1f),
                TileColorTypes.Blue => new Vector2(3f, 2f),
                TileColorTypes.LightGreen => new Vector2(0f, 3f),
                TileColorTypes.Green => new Vector2(3f, 1f),
                TileColorTypes.Brown => new Vector2(0f, 2f),
                TileColorTypes.Orange => new Vector2(1f, 2f),
                TileColorTypes.Yellow => new Vector2(1f, 1f),
                TileColorTypes.Purple => new Vector2(3f, 0f),
                TileColorTypes.Magenta => new Vector2(3f, 3f),
                TileColorTypes.Red => new Vector2(2f, 0f),
                _ => throw new ArgumentOutOfRangeException(nameof(TileKind), TileKind, "undefined color was passed!")
            };
        }
    }
}

