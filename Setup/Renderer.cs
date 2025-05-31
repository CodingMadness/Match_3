using System.Runtime.CompilerServices;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Workflow;
using Raylib_cs;
using rlImGui_cs;
using static Match_3.Setup.AssetManager;
using Vector2 = System.Numerics.Vector2;

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

    public static void SetCurrentContext()
    {
        // Validate existing context
        var ctx = ImGui.GetCurrentContext();

        if (ctx == IntPtr.Zero)
        {
            var newCtx = ImGui.CreateContext();
            ImGui.SetCurrentContext(newCtx);
        }
    }

    public static void Begin()
    {
        BeginDrawing();
        {
            ClearBackground(White);

            rlImGui.Begin();
            {
                ImGui.PushFont(CustomFont);
                ImGui.SetNextWindowPos(Vector2.Zero);
                ImGui.SetNextWindowSize(Game.ConfigPerStartUp.WindowInWorldCoordinates);
            }
        }
    }

    public static void End()
    {
        ImGui.PopFont();
        ImGui.End();
        rlImGui.End();
        EndDrawing();
    }

    public static void CreateWindowSizedCanvas()
    {
        const ImGuiWindowFlags emptyCanvas =
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoInputs |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoTitleBar;

        ImGui.Begin("Tilemap-overlaying-Canvas", emptyCanvas);
    }

    public static void DrawText(ReadOnlySpan<char> colorCodedTxt, CanvasStartingPoints anchor)
    {
        static Vector2 SetUiStartingPoint(ReadOnlySpan<char> colorCodedTxt, CanvasStartingPoints offset)
        {
            Vector2 result = Vector2.Zero;
            Vector2 screen = Game.ConfigPerStartUp.WindowInWorldCoordinates;
            Vector2 txtSize = ImGui.CalcTextSize(colorCodedTxt);
            Vector2 paddingAdjustedScreen = new(screen.X - txtSize.X, screen.Y - txtSize.Y);
            Vector2 halfTxtSize = new(txtSize.X * 0.5f, txtSize.Y * 0.5f);
            Vector2 center = new(screen.X * 0.5f - halfTxtSize.X, screen.Y * 0.5f - halfTxtSize.Y);

            result = offset switch
            {
                CanvasStartingPoints.TopLeft => ImGui.GetStyle().FramePadding,
                CanvasStartingPoints.TopCenter => result with { X = center.X, Y = 0f },
                CanvasStartingPoints.TopRight => result with { X = paddingAdjustedScreen.X, Y = 0f },
                CanvasStartingPoints.BottomLeft => result with { X = 0f, Y = screen.Y - txtSize.Y },
                CanvasStartingPoints.BottomCenter => result with { X = center.X, Y = paddingAdjustedScreen.Y },
                CanvasStartingPoints.BottomRight => result with
                {
                    X = paddingAdjustedScreen.X, Y = paddingAdjustedScreen.Y
                },
                CanvasStartingPoints.MidLeft => result with { X = 0f, Y = center.Y },
                CanvasStartingPoints.Center => result with { X = center.X, Y = center.Y },
                CanvasStartingPoints.MidRight => result with { X = paddingAdjustedScreen.X, Y = center.Y },
                _ => result
            };

            ImGui.SetCursorPos(result);
            return result;
        }

        static bool TextShouldWrap(ref readonly Vector2? current, Vector2 textSize)
        {
            var canvas = Game.ConfigPerStartUp.WindowInWorldCoordinates;
            var wrappedAt = (int)(canvas.X - (current!.Value.X + textSize.X));
            return wrappedAt < 0;
        }

        static void SetNextLine(scoped ref readonly Vector2? fixStart, ref Vector2? current)
        {
            ImGui.NewLine();
            Vector2 framePadding = ImGui.GetStyle().FramePadding;
            Vector2 newLine = fixStart!.Value with
            {
                X = fixStart.Value.X + framePadding.X + ImGui.GetStyle().ItemSpacing.X,
                Y = ImGui.GetCursorPosY() + framePadding.Y
            };
            ImGui.SetCursorPos(newLine);
            current = newLine;
        }

        static void DrawSegment(scoped in TextInfo segment, ref Vector2? current)
        {
            ImGui.TextColored(segment.Colour.Vector, segment.Text);
            MoveCursorRight(ref current, in segment);
        }

        //I am passing a null but only for easier code usage, semantically this is usually not good practise!
        static void MoveCursorRight(ref Vector2? current, ref readonly TextInfo txtInfo)
        {
            current = current!.Value with { X = current.Value.X + txtInfo.TextSize.X };
            ImGui.SetCursorPos(current.Value);
        }

        static void DrawUntilEnd(scoped in WordEnumerator enumerator, scoped ref Vector2? current)
        {
            ref var blackWordsEnumerator = ref Unsafe.AsRef(in enumerator);

            while (blackWordsEnumerator.MoveNext())
            {
                ref readonly var wordSegment = ref blackWordsEnumerator.Current;

                if (TextShouldWrap(ref current, wordSegment.TextSize))
                {
                    blackWordsEnumerator.MoveBack();
                    return;
                }

                DrawSegment(in wordSegment, ref current);
            }
        }

        static void SplitText(scoped in WordEnumerator enumerator,
            scoped ref Vector2? current, scoped ref readonly Vector2? fixStart,
            scoped ref readonly TextInfo segment)
        {
            //if we are about to wrap the text,
            //we need to know if its only black-default text so we  
            //put the words 1 by 1 while they fit still in the same line 
            //and only then put the non-fitting ones into the next line
            if (segment.Colour.Type is TileColorTypes.Black)
                DrawUntilEnd(in enumerator, ref current);

            //if its colored-text which has to be actually wrapped
            //we need to place it directly to the next line since we   
            //don't want a 2-line split colored-text only a sequential line 
            SetNextLine(in fixStart, ref current);

            while (!enumerator.EndReached)
            {
                DrawUntilEnd(in enumerator, ref current);
            }
        }
        //------------------------------------------------------------------------------------------------------------//

        var formatTextEnumerator = new FormatTextEnumerator(colorCodedTxt);
        Vector2? fixStartingPos = null, current = null;

        while (formatTextEnumerator.MoveNext())
        {
            ref readonly var phraseSegment = ref formatTextEnumerator.Current;

            fixStartingPos ??= anchor is not CanvasStartingPoints.CursorPos
                ? SetUiStartingPoint(phraseSegment.Text, anchor)
                : ImGui.GetCursorPos();

            current ??= fixStartingPos;

            if (TextShouldWrap(in current, phraseSegment.TextSize))
            {
                SplitText(in formatTextEnumerator.EnumerateSegment(), ref current, in fixStartingPos, in phraseSegment);
            }
            else
            {
                //this part simply draws each segments directly 1 by 1 next to each other
                DrawSegment(in phraseSegment, ref current);
            }
        }
    }

    public static void DrawQuestLog(Span<Quest> quests)
    {
        foreach (ref readonly Quest quest in quests)
        {
            var logger = !Game.QuestLogger.IsLoggerFull
                ? QuestBuilder.BuildQuestMessageFrom(in quest, Game.QuestLogger)
                : Game.QuestLogger.CurrentLog;

            int x = 1;
            DrawText(logger, CanvasStartingPoints.Center);
            break;
        }
    }
}

public static class TileRenderer
{
    private static void DrawTile(Texture2D atlas, Tile tile, float currTime)
    {
        var body = tile.Body;
        body.ScaleBox(currTime);
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
                        DrawTile(DefaultTileAtlas, basicTile, elapsedTime);
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
            DrawTile(DefaultTileAtlas, tile, currTime);
        }
    }
}