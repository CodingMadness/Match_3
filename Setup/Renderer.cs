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
    private static int _currQuest;

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
            default:
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
        static Vector2 SetUiStartingPoint(ReadOnlySpan<char> colorCodedTxt, CanvasStartingPoints offset, Vector2 sameLine)
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
                CanvasStartingPoints.BottomRight => result with
                {
                    X = paddingAdjustedScreen.X, Y = paddingAdjustedScreen.Y
                },
                CanvasStartingPoints.MidLeft => result with { X = 0f, Y = center.Y },
                CanvasStartingPoints.Center => result with { X = center.X, Y = center.Y },
                CanvasStartingPoints.MidRight => result with { X = paddingAdjustedScreen.X, Y = center.Y },
                _ => result
            };

            sameLine = sameLine == Vector2.Zero ? result : sameLine;
            //sameLine becomes the actual newLine since it is overwritten with the newLine-pos
            Vector2 newLine = sameLine;
            var offsetResult = (int)sameLine.Y == (int)result.Y ? new Vector2(sameLine.X, result.Y) : newLine;
            ImGui.SetCursorPos(offsetResult);
            return offsetResult;
        }

        static Vector2 GetWrappedPos(Vector2 result, Vector2? anchorPos)
        {
            var screen = Grid.GetWindowSize();
            return result.X > screen.X ? anchorPos!.Value with { Y = ImGui.GetCursorPosY() } : result;
        }

        var formatTextEnumerator = new FormatTextEnumerator(colorCodedTxt, 4);

        ImGui.PushFont(CustomFont);
        var result = Vector2.Zero;
        Vector2? anchorFixPos = null;
        
        foreach (ref readonly var txtInfo in formatTextEnumerator)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, txtInfo.ColorAsVec4);
            result = SetUiStartingPoint(txtInfo.Slice2Colorize, anchor, result);
            anchorFixPos ??= result;
            DrawShape(DebugImGuiShapes.Rectangle, result, TileColor.Red, txtInfo.Slice2Colorize);
            ImGui.TextWrapped(txtInfo.Slice2Colorize);
            result = result with { X = result.X + txtInfo.TextSize.X };
            result = GetWrappedPos(result, anchorFixPos);
            ImGui.PopStyleColor();
        }

        ImGui.PopFont();
    }

    public static void DrawQuestLog(Span<Quest> quests)
    {
        var questRunner = quests;
        int questCount = GameState.Instance.Lvl.QuestCount;

        bool shallGetNewQuest = _currQuest++ < questCount;

        if (shallGetNewQuest)
        {
            foreach (ref readonly Quest quest in questRunner)
            {
                var logger = QuestBuilder.BuildQuestMessageFrom(quest);
                DrawText(logger, CanvasStartingPoints.MidLeft);
            }
        }
        else
        {
            //Draw here the logs repeatedly from the Queue.
            DrawText(GameState.Instance.GetPooledQuestLog(), CanvasStartingPoints.MidLeft);
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