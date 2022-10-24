using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Match_3.GameTypes;
using Raylib_CsLo;
using RayWrapper.Base;
using static Match_3.AssetManager;
using static Raylib_CsLo.Raylib;
using Rectangle = Raylib_CsLo.Rectangle;
using Texture = Raylib_CsLo.Texture;

namespace Match_3;

public static class Renderer
{
    public static bool? ButtonClicked()
    {
        static Vector2 NewPos(Vector2 btnSize)
        {
            var screenCoord = Utils.GetScreenCoord();
            float halfWidth = screenCoord.X * 0.4f;
            Vector2 newPos = new(halfWidth, screenCoord.Y - btnSize.Y * 1.3f);
            return newPos;
        }

        void Center(string text) 
        {
            float win_width = ImGui.GetWindowSize().X;
            float text_width = ImGui.CalcTextSize(text).X;

            // calculate the indentation that centers the text on one line, relative
            // to window left, regardless of the `ImGuiStyleVar_WindowPadding` value
            float text_indentation = (win_width - text_width) * 0.5f;

            // if text is too long to be drawn on one line, `text_indentation` can
            // become too small or even negative, so we check a minimum indentation
            float min_indentation = 20.0f;
            if (text_indentation <= min_indentation) {
                text_indentation = min_indentation;
            }
            ImGui.SameLine(text_indentation);
            //ImGui.PushTextWrapPos(win_width - text_indentation);
            ImGui.PushTextWrapPos();
            ImGui.TextWrapped(text);
            ImGui.PopTextWrapPos();
        }
        
        var flags = ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoDecoration;
        bool open = true;

        var btnSize = new Vector2(FeatureBtn.width, FeatureBtn.height);
        var newPos = NewPos(btnSize);

        //begin rendering sub-window
        if (ImGui.Begin("THIS IS MY GAME SUB-WINDOW IN WINDOW", ref open))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, 0f);
            
            if (ImGui.ImageButton((nint)FeatureBtn.id, btnSize ))
            {
                 return true;
            }
        }
        ImGui.End();
        return null;
    }
    
    private static void DrawTile(ref Texture atlas, Tile tile, float elapsedTime)
    {
        static void DrawCoordOnTop(Tile tile)
        {
            Font copy = GetFontDefault() with { baseSize = 800 };
            var begin = tile.End;
            float halfSize = Tile.Size * 0.5f;
            begin = begin with { X = begin.X - halfSize + halfSize/1.5f, Y = begin.Y - halfSize - halfSize/3 };
            GameText coordText = new(copy, (tile.GridCell).ToString(), 10.5f)
            {
                Begin = begin,
                Color = (tile.TileState & TileState.Selected)==TileState.Selected ? RED : BLACK,
            };
            coordText.Color.AlphaSpeed = 0f;
            coordText.ScaleText(null);
            coordText.Draw(1f);
        }

        if (tile is EnemyTile enemy)
        {
            enemy.Body.Scale = 1f;
            //enemy.TileState &= TileState.Selected;
            DrawTexturePro(atlas, enemy.Body.TextureRect, enemy.Pulsate(elapsedTime), 
                    Vector2.Zero, 0f, enemy.Body.Color);
            return;
        }
        
        var body = tile.Body;
        DrawTexturePro(atlas, body.TextureRect, tile.WorldBounds, Vector2.Zero, 0f, body.Color);
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
        
        Texture matchTexture = (match is not null and not EnemyMatches) ? ref DefaultTileAtlas : ref EnemySprite;

        for (int i = 0; i < match?.Count; i++)
        {
            var gridCell = match[(i)].GridCell;
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
            DrawRectangleRec(matches.WorldBox, matches.Body!.ToConstColor(RED));
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
            DrawRectangleRec(matches.Border, matches.Body!.ToConstColor(RED));
        }
    }
    
    public static void DrawTimer(float elapsedSeconds)
    {
        //horrible performance: use a stringbuilder to reuse values!
        TimerText.Text = ((int)elapsedSeconds).ToString();
        TimerText.Src.baseSize = 512*16;
        FadeableColor color = elapsedSeconds > 0f ? BLUE : WHITE;
        TimerText.Color = color with { CurrentAlpha = 1f, TargetAlpha = 1f };
        TimerText.Begin = (Utils.GetScreenCoord() * 0.5f) with { Y = 0f };
        TimerText.ScaleText(null);
        TimerText.Draw(1f);
    }
    
    public static void ShowWelcomeScreen()
    {
        InitWelcomeTxt();
        WelcomeText.Draw(null);
    }
    
    public static bool DrawGameOverResult(bool isDone, bool? gameWon)
    {
        if (gameWon is null)
        {
            return false;
        }
        InitGameOverTxt();
        GameOverText.Text = gameWon.Value ? "YEA, YOU WON!" : "AHH, DONT WORRY, YOU WILL GET THERE";
        GameOverText.Draw(null);
        return isDone;
    }

    public static void DrawBackground(ref Background bg)
    {
        Rectangle screen = new(0f,0f, GetScreenWidth(), GetScreenHeight());

        DrawTexturePro(bg.Texture, bg.Body.TextureRect, screen.DoScale(bg.Body.Scale), 
            Vector2.Zero, 0f, bg.Body.FIXED_WHITE);
    }
    
    /*
    public static void LogQuest(bool useConsole, QuestData match)
    {
        foreach (var pair in match.BallCountPerLevel.State)
        {
            if (useConsole)
            {
                 Console.WriteLine($"You have to collect {pair.Value} {pair.Key}-tiles!");
                 Console.WriteLine();
            }
            else
            {
                var center = Utils.GetScreenCoord() * 0.5f;
                string txt = $"You have to collect {pair.Value} {pair.Key}-tiles!";
                Vector2 pos = center with {X = center.X * 1.5f, Y = 4 * Tile.ScaleFactor };
                LogText.Begin = pos;
                LogText.Text = txt;
                LogText.Draw(null);                
                break;
            }
        }
    } 
    */
}