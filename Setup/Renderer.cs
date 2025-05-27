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
    /// <param name="colorKind"></param>
    /// <param name="text"></param>
    /// <param name="thickness"></param>
    /// <param name="scaleMultiplier"></param>
    private static void DrawShape(DebugImGuiShapes shape, Vector2 position, TileColor colorKind,
        ReadOnlySpan<char> text, float thickness = 1f, float scaleMultiplier = 1f)
    {
        switch (shape)
        {
            case DebugImGuiShapes.Circle:
                ImGui.GetWindowDrawList().AddCircleFilled(position, thickness,
                    ImGui.ColorConvertFloat4ToU32(FadeableColor.ToVec4(colorKind)));
                break;
            case DebugImGuiShapes.Rectangle:
                Vector2 actualSize = ImGui.CalcTextSize(text) * scaleMultiplier;
                ImGui.GetWindowDrawList().AddRect(position, position + actualSize,
                    ImGui.ColorConvertFloat4ToU32(FadeableColor.ToVec4(colorKind)));
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
                ImGui.SetNextWindowPos(Vector2.Zero);
                ImGui.SetNextWindowSize(Grid.GetWindowSize());
            }
        }
    }

    public static void End()
    {
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
            Vector2 screen = Grid.GetWindowSize();
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
                CanvasStartingPoints.Bottomcenter => result with { X = center.X, Y = paddingAdjustedScreen.Y },
                CanvasStartingPoints.BottomRight => result with { X = paddingAdjustedScreen.X, Y = paddingAdjustedScreen.Y },
                CanvasStartingPoints.MidLeft => result with { X = 0f, Y = center.Y },
                CanvasStartingPoints.Center => result with { X = center.X, Y = center.Y },
                CanvasStartingPoints.MidRight => result with { X = paddingAdjustedScreen.X, Y = center.Y },
                _ => result
            };

            ImGui.SetCursorPos(result);
            return result;
        }

        static bool TextShouldWrap(Vector2 current, Vector2 textSize)
        {
            var screen = Grid.GetWindowSize();
            return current.X + textSize.X > screen.X; //? current with { Y = current.Y + textSize.Y } : result;
        }
        
        static Vector2 SetNextLine(Vector2? fixStart)
        {
            ImGui.NewLine();
            Vector2 newLine = fixStart!.Value with { Y = ImGui.GetCursorPosY() };
            ImGui.SetCursorPos(newLine);
            return newLine;
        }

        //I am passing a null but only for easier code usage, semantically this is usually not good practise!
        static void UpdateNextPos(ref Vector2? current, ref readonly TextInfo txtInfo)
        {
            current = current!.Value with { X = current.Value.X + txtInfo.TextSize.X };
            ImGui.SetCursorPos(current!.Value);
        }
        
        var formatTextEnumerator = new FormatTextEnumerator(colorCodedTxt);

        ImGui.PushFont(CustomFont);
        Vector2? fixStartingPos = null, current = null;

        foreach (ref readonly var txtInfo in formatTextEnumerator)
        {
            fixStartingPos ??= SetUiStartingPoint(txtInfo.Slice2Colorize, anchor);
            current ??= fixStartingPos;

            if (TextShouldWrap(current.Value, txtInfo.TextSize))
            {
                current = SetNextLine(fixStartingPos);
                ImGui.TextColored(txtInfo.ColorAsVec4, txtInfo.Slice2Colorize);
                UpdateNextPos(ref current, in  txtInfo);
            }
            else
            {
                ImGui.TextColored(txtInfo.ColorAsVec4, txtInfo.Slice2Colorize);
                UpdateNextPos(ref current, in  txtInfo);
            }
        }

        ImGui.PopFont();
    }

    public static void DrawQuestLog(Span<Quest> quests)
    {
        foreach (ref readonly Quest quest in quests)
        {
            var logger = QuestBuilder.BuildQuestMessageFrom(quest);
            DrawText(logger, CanvasStartingPoints.MidLeft);
        }
    }
}

public static class TileRenderer
{
    private static void DrawTile(Texture2D atlas, Tile tile, float currTime)
    {
        var body = tile.Body;
        body.ScaleBox(currTime);
        DrawTexturePro(atlas, body.AssetRect, body.WorldRect, Vector2.Zero, 0f, body.Colour);
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