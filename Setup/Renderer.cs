using System.Numerics;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Workflow;
using Raylib_cs;
using Vector2 = System.Numerics.Vector2;
using static Match_3.Setup.AssetManager;
using rlImGui_cs;

namespace Match_3.Setup;

public static class UiRenderer
{
    static UiRenderer()
    {
        rlImGui.Setup(false);
    }

    public static void BeginRendering(Action mainGameLoop)
    {
        BeginDrawing();
        {
            ClearBackground(White);
            //ImGui Context Start
            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoBackground |
                                           ImGuiWindowFlags.NoMove |
                                           ImGuiWindowFlags.NoResize;

            rlImGui.Begin();
            {
                if (ImGui.Begin("EntireGrid Overlay", ImGuiWindowFlags.NoResize))
                {                   
                    ImGui.SetWindowPos(default);
                    ImGui.SetWindowSize(Utils.GetScreen());

                    mainGameLoop();
                }

                ImGui.End();
            }
            rlImGui.End();
        }
        EndDrawing();
    }

    public static bool? DrawFeatureBtn(out string btnId)
    {
        static Vector2 NewPos(Vector2 btnSize)
        {
            var screenCoord = Utils.GetScreen();
            float halfWidth = screenCoord.X * 0.5f;
            float indentPos = halfWidth - (btnSize.X * 0.5f);
            Vector2 newPos = new(indentPos, screenCoord.Y - btnSize.Y);
            return newPos;
        }

        var flags =
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoDecoration;
        bool open = true;

        var btnSize = new Vector2(FeatureBtn.Width, FeatureBtn.Height) * 0.67f;
        var newPos = NewPos(btnSize);
        bool? result = null;
        btnId = "FeatureBtn";

        //begin rendering sub-window
        ImGui.SetWindowFocus(btnId);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

        if (ImGui.Begin(btnId, ref open, flags))
        {
            ImGui.SetWindowSize(btnSize * 1.06f);
            ImGui.SetWindowPos(newPos);

            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);

            //Center(buttonID);
            ImGui.SetCursorPos(Vector2.One * 3);

            if (ImGui.ImageButton(btnId, (nint)FeatureBtn.Id, btnSize))
            {
                result = true;
            }

            ImGui.PopStyleColor(2);
        }

        ImGui.End();
        ImGui.PopStyleVar(2);

        return result;
    }

    public static void DrawText(ReadOnlySpan<char> formatString)
    {
        //....       
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
                DrawText(logger);
            }
        }
        else
        {
            //Draw here the logs repeatedly from the pool!....
            DrawText(QuestBuilder.GetPooledQuestLog());
        }
    }

    public static void DrawTimer(float elapsedSeconds)
    {
        
    }

    /// <summary>
    /// NICE WE CAN DRAW NOW TEXT IN CURVY FASHION
    /// </summary>
    /// <param name="text"></param>
    /// <param name="curvature"></param>
    public static void DrawCurvedText(ReadOnlySpan<char> text, float curvature = 0.05f)
    {
        float radius = 200; // Adjust the radius of the curve

        // Calculate the total angle spanned by the curved text

        for (int i = 0; i < text.Length; i++)
        {
            // Calculate the angle for each character along the curve
            float angle = (i - (text.Length - 1) / 2.0f) * curvature;

            // Calculate the position of the character along the curve
            Vector2 position = new Vector2(
                GetScreenWidth() / 2f + radius * (float)Math.Sin(angle),
                GetScreenHeight() / 2f + radius * (1 - (float)Math.Cos(angle))
            );

            // Draw the character at the calculated position
            DrawTextEx(GetFontDefault(), text[i].ToString(), position, 11f, 0f, Black);
        }
    }

    public static void DrawWelcomeScreen()
    {
        InitWelcomeTxt();
        WelcomeText.Draw(null);
    }

    public static bool DrawGameOverScreen(bool isDone, bool? gameWon, ReadOnlySpan<char> input)
    {
        if (gameWon is null)
        {
            return false;
        }

        DrawText(input);
        return isDone;
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
        BeginShaderMode(WobbleEffect);
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
        EndShaderMode();
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