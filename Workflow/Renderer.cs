using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Match_3.Service;
using Match_3.Setup;
using Match_3.Variables;
using Raylib_cs;
using static Match_3.Setup.AssetManager;
using Vector2 = System.Numerics.Vector2;

namespace Match_3.Workflow;

public static class UiRenderer
{
    static UiRenderer()
    {
        RlImGui.Setup(false);
    }
    
    public static void BeginRenderCycle(Action mainGameLoop)
    {
        BeginDrawing();
        {
            ClearBackground(WHITE);
            //ImGui Context Begin
            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoBackground |
                                           ImGuiWindowFlags.NoMove;

            RlImGui.Begin();
            {
                if (ImGui.Begin("Screen Overlay", flags))
                {
                    // ImGui.ShowDemoWindow();

                    ImGui.SetWindowPos(default);
                    ImGui.SetWindowSize(Utils.GetScreenCoord());

                    mainGameLoop();
                }

                ImGui.End();
            }
            RlImGui.End();
        }
        EndDrawing();
    }

    public static bool? DrawFeatureBtn(out string btnId)
    {
        static Vector2 NewPos(Vector2 btnSize)
        {
            var screenCoord = Utils.GetScreenCoord();
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

        var btnSize = new Vector2(FeatureBtn.width, FeatureBtn.height) * 0.67f;
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

            if (ImGui.ImageButton(btnId, (nint)FeatureBtn.id, btnSize))
            {
                result = true;
            }

            ImGui.PopStyleColor(2);
        }

        ImGui.End();
        ImGui.PopStyleVar(2);

        return result;
    }

    public static void DrawText(StringBuilder text)
    {
        // calculate the indentation that centers the text on one line, relative
        // to window left, regardless of the `ImGuiStyleVar_WindowPadding` value
        float winWidth = ImGui.GetWindowWidth();
        Vector2 currentPos = new(25f, ImGui.GetCursorPos().Y + ImGui.GetContentRegionAvail().Y * 0.5f);
        scoped var iterator = new TextStyleEnumerator(text);
        float totalSize = 0;
        const float spaceBetween = 5f;
        Vector2 tmp = currentPos;
        float wrapPosX = winWidth - 20f;

        void NewLine(Vector2 phraseSize)
        {
            tmp.X = currentPos.X; //reset to the X position from the beginning
            tmp.Y += phraseSize.Y * 1.15f;
            totalSize = phraseSize.X;
        }

        ImGui.SetCursorPos(currentPos);

        foreach (ref readonly var phrase in iterator)
        {
            totalSize += phrase.TextSize.X;

            if (totalSize > wrapPosX)
            {
                NewLine(phrase.TextSize);
            }

            //Draw words as long as they fit in the WINDOW_WIDTH
            foreach (var word in phrase)
            {
                ImGui.SetCursorPos(tmp);
                ImGui.PushTextWrapPos(wrapPosX);
                ImGui.TextColored(word.ColorV4, word.Text2Color);
                ImGui.PopTextWrapPos();
                tmp.X += word.TextSize.X + spaceBetween;
            }
        }
    }

    public static void DrawQuestLog()
    {
        ImGui.SetWindowFontScale(1.5f);

        var questIterator = MatchQuestHandler.Instance.GetQuests();
        //we begin at index = 1 cause at index = 0 we have Empty, so we skip that one

        foreach (ref readonly var quest in questIterator)
        {
            QuestBuilder.BuildQuestMsgFrom(quest);
            DrawText(GameState.Logger);
        }
    }

    public static void DrawTimer(float elapsedSeconds)
    {
        //horrible performance: use a stringBuilder to reuse values!
        TimerText.Text = ((int)elapsedSeconds).ToString();
        TimerText.Src.baseSize = 512 * 16;
        FadeableColor color = elapsedSeconds > 0f ? BLUE : WHITE;
        TimerText.Color = color with { CurrentAlpha = 1f, TargetAlpha = 1f };
        TimerText.Begin = (Utils.GetScreenCoord() * 0.5f) with { Y = 0f };
        TimerText.ScaleText(GetScreenWidth());
        TimerText.Draw(1f);
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
            DrawTextEx(GetFontDefault(), text[i].ToString(), position, 11f, 0f, BLACK);
        }
    }

    public static void DrawWelcomeScreen()
    {
        InitWelcomeTxt();
        WelcomeText.Draw(null);
    }

    public static bool DrawGameOverScreen(bool isDone, bool? gameWon, StringBuilder input)
    {
        if (gameWon is null)
        {
            return false;
        }

        DrawText(input);
        return isDone;
    }

    public static void DrawBackground(Background? bg)
    {
        if (bg is null)
            return;

        RectangleF screen = new(0f, 0f, GetScreenWidth(), GetScreenHeight());

        DrawTexturePro(bg.Texture, bg.Body.TextureRect.AsIntRayRect(),
            screen.DoScale(bg.Body.ScaleableFloat).AsIntRayRect(),
            Vector2.Zero, 0f, bg.Body.FixedWhite);
    }
}

public static class GameObjectRenderer
{
    private static void DrawTile(ref Texture2D atlas, Tile tile, float elapsedTime)
    {
        static void DrawCoordOnTop(Tile tile)
        {
            Font copy = GetFontDefault() with { baseSize = 800 };
            var begin = tile.End;
            float halfSize = Tile.Size * 0.5f;
            begin = begin with { X = begin.X - halfSize + halfSize / 1.5f, Y = begin.Y - halfSize - halfSize / 3 };

            GameText coordText = new(copy, (tile.GridCell).ToString(), 10.5f)
            {
                Begin = begin,
                Color = (tile.TileState & TileState.Selected) == TileState.Selected ? RED : BLACK,
            };

            coordText.Color.AlphaSpeed = 0f;
            coordText.ScaleText(GetScreenWidth());
            coordText.Draw(1f);
        }

        if (tile is EnemyTile enemy)
        {
            enemy.Body.ScaleableFloat = 1f;
            DrawTexturePro(atlas, enemy.Body.TextureRect.AsIntRayRect(), enemy.Pulsate(elapsedTime).AsIntRayRect(),
                Vector2.Zero, 0f, enemy.Body.Color);
            return;
        }

        var body = tile.Body;
        DrawTexturePro(atlas, body.TextureRect.AsIntRayRect(), tile.MapBox.AsIntRayRect(), Vector2.Zero, 0f,
            body.Color);
        DrawCoordOnTop(tile);
    }

    public static void DrawGrid(float elapsedTime)
    {
        for (int x = 0; x < Grid.Instance.TileWidth; x++)
        {
            for (int y = 0; y < Grid.Instance.TileHeight; y++)
            {
                Vector2 current = new(x, y);
                var basicTile = Grid.Instance[current];

                if (basicTile is not null && !basicTile.IsDeleted && basicTile is not EnemyTile)
                {
                    DrawTile(ref DefaultTileAtlas, basicTile, elapsedTime);
                }
            }
        }
    }

    public static void DrawMatches(MatchX? match, float elapsedTime, bool shallCreateEnemies)
    {
        if (!shallCreateEnemies)
            return;

        if (match is null)
            return;

        Texture2D matchTexture = match is not null and not EnemyMatches ? ref DefaultTileAtlas : ref EnemySprite;

        for (int i = 0; i < match?.Count; i++)
        {
            var gridCell = match[i].GridCell;
            var tile = Grid.Instance[gridCell];

            if (tile is null)
                continue;

            DrawTile(ref matchTexture, tile, elapsedTime);
        }
    }

    public static void DrawInnerBox(MatchX? matches, float elapsedTime)
    {
        if (matches?.IsMatchActive == true)
        {
            DrawRectangleRec(matches.WorldBox.AsIntRayRect(), matches.Body!.ToConstColor(RED.AsSysColor()));
        }
    }

    public static void DrawOuterBox(EnemyMatches? matches, float elapsedTime)
    {
        if (matches?.IsMatchActive == true)
        {
            /*
            matches.Body!.Color.AlphaSpeed = 0.2f;
            matches.Body!.Color.ElapsedTime = elapsedTime;
            */
            DrawRectangleRec(matches.Border.AsIntRayRect(), matches.Body!.ToConstColor(RED.AsSysColor()));
        }
    }
}