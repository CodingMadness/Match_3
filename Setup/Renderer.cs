using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Workflow;
using Microsoft.CodeAnalysis.Text;
using Raylib_cs;
using rlImGui_cs;
using static Match_3.Setup.AssetManager;
using Vector2 = System.Numerics.Vector2;

namespace Match_3.Setup;
public enum ImGuiShapes
{
    Circle,
    Rectangle,
    Triangle
}

public static class UiRenderer
{
    static UiRenderer()
    {
        
    }

    private static void DrawShape(ImGuiShapes shape, Vector2 position, Color color, ReadOnlySpan<char> text, float thickness, float scaleMultiplier=1f)
    {
        switch (shape)
        {
            case ImGuiShapes.Circle:
                ImGui.GetWindowDrawList().AddCircleFilled(position, thickness, ImGui.ColorConvertFloat4ToU32(Utils.ToVec4(color)));
                break;
            case ImGuiShapes.Rectangle:
                Vector2 actualSize = ImGui.CalcTextSize(text) * scaleMultiplier;
                ImGui.GetWindowDrawList().AddRect(position, position + actualSize, ImGui.ColorConvertFloat4ToU32(Utils.ToVec4(color)));
                break;
            case ImGuiShapes.Triangle:
                break;
            default:
                break;
        }
    }

    public const ImGuiWindowFlags EmptyCanvas =
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoDocking |
                                           ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoInputs |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoMove |
                                           ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoBringToFrontOnFocus |
                                           ImGuiWindowFlags.NoTitleBar;
    public static void SetCurrentContext()
    {
        // Validate existing context
        var ctx = ImGui.GetCurrentContext();
        nint newCtx;

        if (ctx == IntPtr.Zero)
        {
            newCtx = ImGui.CreateContext();
            ImGui.SetCurrentContext(newCtx);
            //throw new InvalidOperationException("Call ImGui.CreateContext() first!");
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
                ImGui.SetNextWindowSize(Utils.GetScreen());                
            }
        }
    }
   
    public static void End()
    {
        ImGui.End();
        rlImGui.End();
        EndDrawing();
    }
    
    public static void DrawText(ReadOnlySpan<char> formatableTxt, CanvasStartingPoints begin, float fontSize)
    {        
        static Vector2 SetUIStartingPoint(ReadOnlySpan<char> formatableTxt, CanvasStartingPoints begin)
        {
            Vector2 result = Vector2.Zero;
            Vector2 screen = Utils.GetScreen();
            Vector2 txtSize = ImGui.CalcTextSize(formatableTxt);
            Vector2 paddingAdjustedScreen = new(screen.X - txtSize.X, screen.Y - txtSize.Y);
            Vector2 halfTxtSize = new(txtSize.X * 0.5f, txtSize.Y * 0.5f);
            Vector2 Center = new((screen.X * 0.5f) - halfTxtSize.X, (screen.Y * 0.5f) - halfTxtSize.Y);
 
            switch (begin)
            {
                case CanvasStartingPoints.TopLeft:
                    result = new(0f, 0f);
                    break;
                case CanvasStartingPoints.TopCenter:
                    result = result with { X = Center.X, Y = 0f };
                    break;
                case CanvasStartingPoints.TopRight:
                    result = result with { X = paddingAdjustedScreen.X, Y = 0f };
                    break;
                case CanvasStartingPoints.BottomLeft:
                    result = result with { X = 0f, Y = screen.Y - txtSize.Y };
                    break;
                case CanvasStartingPoints.Bottomcenter:
                    result = result with { X = Center.X, Y = paddingAdjustedScreen.Y };
                    break;
                case CanvasStartingPoints.BottomRight:
                    result = result with { X = paddingAdjustedScreen.X, Y = paddingAdjustedScreen.Y };
                    break;
                case CanvasStartingPoints.MidLeft:
                    result = result with { X = 0f, Y = Center.Y };
                    break;
                case CanvasStartingPoints.Center:
                    result = result with { X = Center.X, Y = Center.Y };
                    break;
                case CanvasStartingPoints.MidRight:
                    result = result with { X = paddingAdjustedScreen.X, Y = Center.Y };
                    break;
                default:
                    break;
            }

            ImGui.SetCursorPos(result);
            return result;
        }

        var formatTextEnumerator = new FormatTextEnumerator(formatableTxt, 1);

        foreach (ref readonly var txtInfo in formatTextEnumerator)
        {
            ImGui.PushFont(CustomFont);
            
            var result = SetUIStartingPoint(txtInfo.Slice2Colorize, begin);
            DrawShape(ImGuiShapes.Rectangle, result, Red, txtInfo.Slice2Colorize, 1f);
            ImGui.TextColored(txtInfo.ColorAsVec4, txtInfo.Slice2Colorize);
   
            ImGui.PopFont();
        }
    }

    public static void DrawQuestLog(FastSpanEnumerator<Quest> quests)
    {
        ImGui.SetWindowFontScale(1.5f);

        var questRunner = quests;

        if (!QuestBuilder.ShallRecycle)
        {
            foreach (ref readonly Quest quest in questRunner)
            {
                var logger = QuestBuilder.BuildQuestMessageFrom(quest);
                DrawText(logger, CanvasStartingPoints.MidLeft, 50f);
            }
        }
        else
        {
            //Draw here the logs repeatedly from the pool!....
            DrawText(QuestBuilder.GetPooledQuestLog(), CanvasStartingPoints.MidLeft, 50f);
        }
    }
}

public static class TileRenderer
{
    private static void DrawTile(Texture2D atlas, Tile tile, float currTime)
    {
        var body = tile.Body;
        body.ScaleBox(currTime);
        DrawTexturePro(atlas, body.AssetRect, body.WorldRect, Vector2.Zero, 0f, body.Color);
    }

    public static void DrawGrid(float elapsedTime, int gridWidth, int gridHeight)
    {
        //BeginShaderMode(WobbleEffect);
        {
            for (int x = 0; x < 1; x++)
            {
                for (int y = 0; y < 1; y++)
                {
                    Tile? basicTile = Grid.GetTile(new(x, y));

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