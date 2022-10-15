using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;

using static Match_3.AssetManager;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public static class Renderer
{
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
            coordText.ScaleText();
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
        
        var body = tile.Body as TileShape;
        DrawTexturePro(atlas, body.TextureRect, tile.WorldBounds, Vector2.Zero, 0f, body.Color);
        DrawCoordOnTop(tile);
    }
    
    public static void DrawGrid(Grid map, float elapsedTime,(int size, int speed) shaderLoc)
    {
        Vector2 size = Utils.GetScreenCoord();
       
        
            for (int x = 0; x < map.TileWidth; x++)
            {
                for (int y = 0; y < map.TileHeight; y++)
                {
                    Vector2 current = new(x, y);
                    Tile? basicTile = map[current];
                        
                    if (basicTile is not null && !basicTile.IsDeleted && basicTile is not EnemyTile)
                    {
                        DrawTile(ref DefaultTileSprite, basicTile, elapsedTime);
                    }
                }
            }
    }

    public static void DrawMatches(MatchX? match, Grid map, float elapsedTime, bool shallCreateEnemies)
    {
        if (!shallCreateEnemies)
            return;
        
        if (match is null)
            return;
        
        Texture matchTexture = (match is MatchX and not EnemyMatches) ? ref DefaultTileSprite : ref EnemySprite;

        for (int i = 0; i < match.Count; i++)
        {
            var gridCell = match[(i)].GridCell;
            var tile = map[gridCell];
            if (tile is null)
                continue;
            DrawTile(ref matchTexture, tile, elapsedTime);
        }
    }
    
    public static void DrawInnerBox(MatchX? matches, float elapsedTime)
    {
        if (matches?.IsMatchActive == true)
        {
            DrawRectangleRec(matches.WorldBox, matches.Body.ToConstColor(RED));
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
            DrawRectangleRec(matches.Border, matches.Body.ToConstColor(RED));
        }
    }
    
    public static void DrawTimer(float elapsedSeconds)
    {
        //horrible performance: use a stringbuilder to reuse values!
        TimerText.Text = ((int)elapsedSeconds).ToString();
        FadeableColor color = elapsedSeconds > 0f ? BLUE : WHITE;
        TimerText.Color = color with { CurrentAlpha = 1f, TargetAlpha = 1f };
        TimerText.Begin = (Utils.GetScreenCoord() * 0.5f) with { Y = 0f };
        TimerText.ScaleText();
        TimerText.Draw(1f);
    }
    
    public static void ShowWelcomeScreen()
    {
        WelcomeText.Color = RED;
        WelcomeText.ScaleText();
        WelcomeText.Begin = (Utils.GetScreenCoord() * 0.5f) with { X = 0f };
        WelcomeText.Draw(null);
    }
    
    public static bool OnGameOver(bool isDone, bool? gameWon)
    {
        if (gameWon is null)
        {
            return false;
        }
        
        ClearBackground(WHITE);
        GameOverText.Src.baseSize = 2;
        GameOverText.Begin = (Utils.GetScreenCoord() * 0.5f) with { X = 0f };
        GameOverText.Text = gameWon.Value ? "YOU WON!" : "YOU LOST";
        GameOverText.ScaleText();
        GameOverText.Draw(null);
        return isDone;
    }

    public static void DrawBackground(ref Background bg)
    {
        DrawTexturePro(bg.Texture, bg.Body.TextureRect, Utils.ScreenRect.DoScale(bg.Body.Scale), 
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