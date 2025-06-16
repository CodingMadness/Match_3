global using Vector2 = System.Numerics.Vector2;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Workflow;
using Raylib_cs;
using rlImGui_cs;
using static Raylib_cs.Raylib;

namespace Match_3.Setup;

public enum DebugImGuiShapes
{
    Circle,
    Rectangle,
    Triangle
}

public static class UiRenderer
{
    /// <summary>
    /// Only for debug purposes!
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="position"></param>
    /// <param name="colorTypesKind"></param>
    /// <param name="text"></param>
    /// <param name="thickness"></param>
    /// <param name="scaleMultiplier"></param>
    private static void DrawShape(Vector2 position, TileColorTypes colorTypesKind, ReadOnlySpan<char> text,
                                  DebugImGuiShapes shape = DebugImGuiShapes.Rectangle,
                                  float thickness = 1f, float scaleMultiplier = 1f)
    {
        switch (shape)
        {
            case DebugImGuiShapes.Circle:
                ImGui.GetWindowDrawList().AddCircleFilled(position, thickness,
                    ImGui.ColorConvertFloat4ToU32(FadeableColor.ToVec4(colorTypesKind)));
                break;
            case DebugImGuiShapes.Rectangle:
                Vector2 actualSize = ImGui.CalcTextSize(text) * scaleMultiplier;
                ImGui.GetWindowDrawList().AddRect(position, position + actualSize,
                    ImGui.ColorConvertFloat4ToU32(FadeableColor.ToVec4(colorTypesKind)));
                break;
            case DebugImGuiShapes.Triangle:
                break;
        }
    }

    public static void BeginRaylib()
    {
        BeginDrawing();
        ClearBackground(White);
    }

    public static void EndRaylib() => EndDrawing();

    public static void CreateCanvas(in Config config)
    {
        const ImGuiWindowFlags emptyCanvas =
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoInputs |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoBringToFrontOnFocus;

        rlImGui.Begin();
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(config.WindowSize);
        ImGui.Begin("Tilemap-overlaying-Canvas", emptyCanvas);
        // ImGui.PushFont(AssetManager.CustomFont);
    }

    public static void EndCanvas()
    {
        // ImGui.PopFont();
        ImGui.End();
        rlImGui.End();
    }

    private static void DrawText(ReadOnlySpan<char> colorCodedTxt, CanvasOffset anchor)
    {
        static void SetNextLine(scoped in Vector2 fixStart, ref Vector2 current)
        {
            ImGui.NewLine();
            Vector2 framePadding = ImGui.GetStyle().FramePadding;
            Vector2 newLine = fixStart with
            {
                X = fixStart.X + framePadding.X + ImGui.GetStyle().ItemSpacing.X,
                Y = ImGui.GetCursorPos().Y + framePadding.Y
            };
            ImGui.SetCursorPos(newLine);
            current = newLine;
        }

        static void DrawSegment(scoped in Segment segment, ref Vector2 current)
        {
            ImGui.TextColored(segment.Colour.Vector, segment.Slice2Colorize);
            current = current with { X = current.X + segment.TextSize.X };
            ImGui.SetCursorPos(current);
        }

        static void DrawUntilNeed2Wrap(scoped in WordEnumerator enumerator,
                                   scoped ref Vector2 current,
                                   scoped in Vector2 fixPoint)
        {
            ref var blackWordsEnumerator = ref Unsafe.AsRef(in enumerator);

            //has to be true otherwise the entire '&=' expression below will ALWAYS result in false...
            bool doesRootSegmentFit = true;

            while (blackWordsEnumerator.MoveNext())
            {
                ref readonly var wordSegment = ref blackWordsEnumerator.Current;
                ref Segment fittingSegment = ref Unsafe.AsRef(in wordSegment);
                bool shouldWrap = blackWordsEnumerator.RootSegment.ShouldWrap!.Value;
                doesRootSegmentFit &= shouldWrap;

                if (doesRootSegmentFit)
                    fittingSegment = ref Unsafe.AsRef(in blackWordsEnumerator.RootSegment);

                else if (wordSegment.ShouldWrap!.Value)
                    SetNextLine(fixPoint, ref current);

                DrawSegment(in fittingSegment, ref current);
            }
        }

        static void SplitText(scoped in WordEnumerator enumerator,
            scoped ref Vector2 current,
            scoped in Vector2 fixStart,
            scoped in Segment segment)
        {
            //if we are about to wrap the text,
            //we need to know if its only black-default text so we,
            //put words 1 by 1 while they fit still in the same line
            //and only then put the non-fitting ones into the next line
            bool isColor = segment.Colour.Type is not TileColorTypes.Black;

            //if its colored-text which has to be actually wrapped
            //we need to place it directly to the next line since we
            //don't want a 2-line split colored-text only a sequential line
            if (isColor)
                SetNextLine(in fixStart, ref current);

            while (!enumerator.EndReached)
            {
                DrawUntilNeed2Wrap(in enumerator, ref current, in fixStart);
            }
        }
        //------------------------------------------------------------------------------------------------------------//

        var formatTextEnumerator = new FormatTextEnumerator(colorCodedTxt);
        Vector2 current = Vector2.Zero;
        (Vector2 fixStartingPos, float toWrapAt) = (Vector2.Zero, 0f);
        bool hasBeenExecuted = false;
        scoped ref readonly var runThroughWords = ref formatTextEnumerator.GetCleanWordEnumerator();

        while (formatTextEnumerator.MoveNext())
        {
            ref readonly var phraseSegment = ref formatTextEnumerator.Current;
            runThroughWords = ref formatTextEnumerator.GetCleanWordEnumerator();

            if (!hasBeenExecuted)
            {
                fixStartingPos = phraseSegment.RenderPosition!.Value;
                current = fixStartingPos;
            }

            if (phraseSegment.ShouldWrap!.Value)
            {
                SplitText(in runThroughWords,
                    ref current,
                    in fixStartingPos,
                    in phraseSegment);
            }
            else
            {
                //this part simply draws each segment directly 1 by 1 next to each other
                DrawSegment(in phraseSegment, ref current);
            }
        }
        runThroughWords.Dispose();
    }

    public static void DrawQuestsFrom(QuestLogger logger, CanvasOffset offset)
    {
        for (int i = 0; i < logger.QuestIndex; i++)
        {
            // var txt = logger.CurrentLog.Distance(0, logger.CurrentLog.LastIndexOf("help"));
            DrawText(logger.CurrentLog, offset);
        }

        logger.BeginFromStart();
    }

    public static void Test_NewDrawLogic(QuestLogger logger, CanvasOffset  offset)
    {
        static void DrawSegment(scoped in Segment segment, ref Vector2 current)
        {
            ImGui.TextColored(segment.Colour.Vector, segment.Slice2Colorize.Mutable());
            current = current with { X = current.X + segment.TextSize.X };
            ImGui.SetCursorPos(current);
        }

        scoped var formatTextEnumerator = new FormatTextEnumerator(logger.CurrentLog, offset, TextAlignmentRule.ColoredSegmentsInOneLine);
        Vector2 fixStartingPos = Vector2.Zero;
        bool hasBeenExecuted = false;
        Vector2 current = Vector2.Zero;
        bool segmentShouldWrap = false;
        Vector2 entireTextBlockSize = (Vector2)formatTextEnumerator.TotalTextSize!;

        while (formatTextEnumerator.MoveNext())
        {
            ref readonly var phraseSegment = ref formatTextEnumerator.Current;
            ref readonly var runThroughWords = ref formatTextEnumerator.GetCleanWordEnumerator();

            if (!hasBeenExecuted)
            {
                (fixStartingPos,segmentShouldWrap) = (phraseSegment.RenderPosition!.Value, phraseSegment.ShouldWrap!.Value);
                current = fixStartingPos;
            }

            if (segmentShouldWrap)
            {
                // SplitText(in runThroughWords,
                //     ref current,
                //     in fixStartingPos,
                //     in phraseSegment);
            }
            else
            {
                //this part simply draws each segment directly 1 by 1 next to each other
                DrawSegment(in phraseSegment, ref current);
            }
        }
    }
}

public static class TileRenderer
{
    public static void DrawTile(Texture2D atlas, Tile tile, float currTime)
    {
        var body = tile.Body;
        // body.ScaleBox(currTime);
        DrawTexturePro(atlas, body.AtlasInfo, body.WorldRect, Vector2.Zero, 0f, body.Colour);
    }

    public static void DrawGrid(float elapsedTime, int gridWidth, int gridHeight)
    {
        //BeginShaderMode(WobbleEffect);
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Tile? basicTile = TileMap.GetTile(new(x, y));

                    if (basicTile is not null && !basicTile.IsDeleted)
                    {
                        DrawTile(AssetManager.Instance.DefaultTileAtlas, basicTile, elapsedTime);
                    }
                }
            }
        }
        //EndShaderMode();
    }

    public static void DrawMatches(MatchX match, float currTime, bool shallCreateEnemies)
    {
        if (match.Count is 0)
            return;

        foreach (var tile in match)
        {
            tile.Body.ScaleBox(currTime);
            DrawTile(AssetManager.Instance.DefaultTileAtlas, tile, currTime);
        }
    }
}