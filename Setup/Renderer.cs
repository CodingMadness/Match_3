global using Vector2 = System.Numerics.Vector2;
using static Raylib_cs.Raylib;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Workflow;
using Raylib_cs;
using rlImGui_cs;

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
    private static void DrawShape(DebugImGuiShapes shape, Vector2 position, TileColorTypes colorTypesKind,
        ReadOnlySpan<char> text, float thickness = 1f, float scaleMultiplier = 1f)
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

    private static void DrawText(ReadOnlySpan<char> colorCodedTxt, CanvasStartingPoints anchor)
    {
        static (Vector2 start, float toWrapAt) SetUiStartingPoint(ReadOnlySpan<char> colorCodedTxt,
            CanvasStartingPoints offset, out bool callThisMethodOnlyOnce)
        {
            (Vector2 start, float toWrapAt) = (Vector2.Zero, 0f);
            Vector2 canvas = Game.ConfigPerStartUp.WindowSize;
            Vector2 txtSize = ImGui.CalcTextSize(colorCodedTxt);
            Vector2 paddingAdjustedScreen = new(canvas.X - txtSize.X, canvas.Y - txtSize.Y);
            Vector2 halfTxtSize = new(txtSize.X * 0.5f, txtSize.Y * 0.5f);
            Vector2 center = new(canvas.X * 0.5f - halfTxtSize.X, canvas.Y * 0.5f - halfTxtSize.Y);

            (start, toWrapAt) = offset switch
            {
                CanvasStartingPoints.TopLeft => (ImGui.GetStyle().FramePadding, center.X),
                CanvasStartingPoints.TopCenter => (start with { X = center.X, Y = 0f }, canvas.X),
                CanvasStartingPoints.TopRight => (start with { X = paddingAdjustedScreen.X, Y = 0f }, canvas.X),
                CanvasStartingPoints.BottomLeft => (start with { X = 0f, Y = canvas.Y - txtSize.Y }, center.X),
                CanvasStartingPoints.BottomCenter => (start with { X = center.X, Y = paddingAdjustedScreen.Y },
                    canvas.X),
                CanvasStartingPoints.BottomRight => (
                    start with { X = paddingAdjustedScreen.X, Y = paddingAdjustedScreen.Y }, canvas.X),
                CanvasStartingPoints.MidLeft => (start with { X = 0f, Y = center.Y }, center.X),
                CanvasStartingPoints.Center => (start with { X = center.X, Y = center.Y }, canvas.X),
                CanvasStartingPoints.MidRight => (start with { X = paddingAdjustedScreen.X, Y = center.Y }, canvas.X),
                _ => (Vector2.Zero, 0f)
            };

            ImGui.SetCursorPos(start);
            callThisMethodOnlyOnce = true;
            return (start, toWrapAt);
        }

        static bool TextShouldWrap(scoped in Vector2 current, float toWrapAt, Vector2 textSize)
        {
            var wrappedAt = (int)(toWrapAt - (current.X + textSize.X));
            return wrappedAt < 0;
        }

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

        static void DrawSegment(scoped in TextInfo segment, ref Vector2 current)
        {
            ImGui.TextColored(segment.Colour.Vector, segment.Text);
            MoveCursorRight(ref current, in segment);
        }

        //I am passing null, but only for easier code usage, semantically, this is usually not good practise!
        static void MoveCursorRight(scoped ref Vector2 current, scoped in TextInfo txtInfo)
        {
            current = current with { X = current.X + txtInfo.TextSize.X };
            ImGui.SetCursorPos(current);
        }

        static void DrawUntilNeed2Wrap(scoped in WordEnumerator enumerator,
                                   scoped ref Vector2 current,
                                   scoped in Vector2 fixPoint,
                                   float toWrapAt)
        {
            ref var blackWordsEnumerator = ref Unsafe.AsRef(in enumerator);
            bool doesRootSegmentFit = true; //has to be true otherwise the entire expression below will ALWAYS result in false...

            while (blackWordsEnumerator.MoveNext())
            {
                ref readonly var wordSegment = ref blackWordsEnumerator.Current;
                ref TextInfo fittingSegment = ref Unsafe.AsRef(in wordSegment);

                doesRootSegmentFit &= !TextShouldWrap(in current, toWrapAt, blackWordsEnumerator.RootSegment.TextSize);

                if (doesRootSegmentFit)
                    fittingSegment = ref Unsafe.AsRef(in blackWordsEnumerator.RootSegment);

                else if (TextShouldWrap(in current, toWrapAt, wordSegment.TextSize))
                {
                    // blackWordsEnumerator.MoveBack();
                    SetNextLine(fixPoint, ref current);
                }

                DrawSegment(in fittingSegment, ref current);
            }
        }

        static void SplitText(scoped in WordEnumerator enumerator,
            scoped ref Vector2 current,
            scoped in Vector2 fixStart,
            scoped in TextInfo segment,
            float toWrapAt)
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
                DrawUntilNeed2Wrap(in enumerator, ref current, in fixStart, toWrapAt);
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
                (fixStartingPos, toWrapAt) = SetUiStartingPoint(phraseSegment.Text, anchor, out hasBeenExecuted);
                current = fixStartingPos;
            }

            if (TextShouldWrap(in current, toWrapAt, phraseSegment.TextSize))
            {
                SplitText(in runThroughWords,
                    ref current,
                    in fixStartingPos,
                    in phraseSegment,
                    toWrapAt);
            }
            else
            {
                //this part simply draws each segment directly 1 by 1 next to each other
                DrawSegment(in phraseSegment, ref current);
            }
        }
        runThroughWords.Dispose();
    }

    public static void DrawQuestsFrom(QuestLogger logger, CanvasStartingPoints offset)
    {
        for (int i = 0; i < logger.QuestIndex; i++)
        {
            // var txt = logger.CurrentLog.Slice(0, logger.CurrentLog.LastIndexOf("help"));
            DrawText(logger.CurrentLog, offset);
        }

        logger.BeginFromStart();
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