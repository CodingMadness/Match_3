using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Match_3.AssetManager;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public static class Renderer
{
    private static Texture atlas = DefaultTileAtlas;
    public static ref Texture GetAtlas() => ref atlas;

    private static void Draw(Tile tile, float elapsedTime)
    {
        static void DrawCoordOnTop(Tile tile)
        {
            Font copy = GetFontDefault() with { baseSize = 1024 };
            var begin = tile.End;
            float halfSize = Tile.Size * 0.5f;
            begin = begin with { X = begin.X - halfSize - 0, Y = begin.Y - halfSize - (halfSize * 0.3f) };
            GameText coordText = new(copy, (tile.Cell).ToString(), 11.5f)
            {
                Begin = begin,
                Color = tile.State == State.Selected ? RED : BLACK,
            };
            coordText.Color.AlphaSpeed = 0f;
            coordText.ScaleText();
            coordText.Draw(2f);
        }

        if (tile is EnemyTile enemy)
            enemy.State &= State.Selected;
        
        var body = tile.Body;
        body.Color.ElapsedTime = elapsedTime;
        DrawTextureRec(GetAtlas(), tile.DestRect, tile.Begin, body.Color.Apply());
        DrawCoordOnTop(tile);
    }
    
    public static void DrawGrid(Grid map, float elapsedTime)
    {
        //Do this DrawGrid second per second ONLY ONCE
        for (int x = 0; x < map.TileWidth; x++)
        {
            for (int y = 0; y < map.TileHeight; y++)
            {
                Vector2 current = new(x, y);
                //the indexer can return a NULL when a tile is marked as
                //Disabled and the "IsDeleted" returns true
                Tile? basicTile = map[current];
                    
                if (basicTile is not null && !basicTile.IsDeleted)
                {
                    GetAtlas() = (basicTile is EnemyTile)
                        ? EnemyAtlas
                        : DefaultTileAtlas;

                    Draw(basicTile, elapsedTime);
                }
            }
        }

        //isDrawn = true;
        //Console.WriteLine("ITERATION OVER FOR THIS DRAW-CALL!");
    }
    
    public static void DrawBorder(MatchX? matches)
    {
        if (matches?.Count == 0)
        {
            //if (matches is EnemyMatches em)
              //  DrawRectangleRec(em.Border, ColorAlpha(RED, 1f));
            //else 
                DrawRectangleRec(matches.MapRect, ColorAlpha(RED, 1f));
        }
    }
    
    public static void UpdateTimer(ref GameTime globalTimer)
    {
        globalTimer.Run();

        TimerText.Text = ((int)globalTimer.ElapsedSeconds).ToString();
        FadeableColor color = globalTimer.ElapsedSeconds > 0f ? BLUE : WHITE;
        TimerText.Color = color with { CurrentAlpha = 1f, TargetAlpha = 1f };
        TimerText.Begin = (Utils.GetScreenCoord() * 0.5f) with { Y = 0f };
        TimerText.ScaleText();
        //timerText.DrawGrid(0.5f);
    }
    
    public static void ShowWelcomeScreen(bool hideWelcome)
    {
        FadeableColor tmp = RED;
        tmp.AlphaSpeed = hideWelcome ? 1f : 0f;
        WelcomeText.Color = tmp;
        WelcomeText.ScaleText();
        WelcomeText.Begin = (Utils.GetScreenCoord() * 0.5f) with { X = 0f };
        WelcomeText.Draw(null);
    }

    public static bool OnGameOver(ref GameTime globalTimer, bool? gameWon)
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
        return globalTimer.Done();
    }

    public static void DrawBackground()
    {
        DrawTexture(BGAtlas, 0, 0, WHITE);
    }
    
    public static void LogQuest(bool useConsole, Level current)
    {
        foreach (var pair in current.QuestPerLevel.Quest)
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
                Vector2 pos = center with {X = center.X * 1.5f, Y = 4 * Tile.Size };
                LogText.Begin = pos;
                LogText.Text = txt;
                LogText.Draw(null);                
                break;
            }
        }
    } 
}