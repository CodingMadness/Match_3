using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Match_3.AssetManager;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public static class Renderer
{
    private static Texture Atlas;

    private static void DrawTile(Tile tile, float elapsedTime)
    {
        static void DrawCoordOnTop(Tile tile)
        {
            Font copy = GetFontDefault() with { baseSize = 800 };
            var begin = tile.End;
            float halfSize = Tile.Size * 0.5f;
            begin = begin with { X = begin.X - halfSize - 0, Y = begin.Y - halfSize - 20f };
            GameText coordText = new(copy, (tile.GridCell).ToString(), 10.5f)
            {
                Begin = begin,
                Color = tile.TileState == TileState.Selected ? RED : BLACK,
            };
            coordText.Color.AlphaSpeed = 0f;
            coordText.ScaleText();
            coordText.Draw(1f);
        }

        if (tile is EnemyTile enemy)
        {
            //enemy.TileState &= TileState.Selected;
            DrawTexturePro(Atlas, enemy.Body.TextureRect, enemy.Pulsate(elapsedTime), 
                    Vector2.Zero, 0f, enemy.Body.Color);
            return;
        }
        var body = tile.Body as TileShape;
        body!.Color.ElapsedTime = elapsedTime;
        DrawTexturePro(Atlas, body.TextureRect, tile.WorldBounds, Vector2.Zero, 0f, body.Color);
       // DrawCoordOnTop(tile);
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
                    Atlas = (basicTile is EnemyTile)
                        ? EnemyAtlas
                        : DefaultTileAtlas;

                    DrawTile(basicTile, elapsedTime);
                }
            }
        }

        //isDrawn = true;
        //Console.WriteLine("ITERATION OVER FOR THIS DRAW-CALL!");
    }
    
    public static void DrawInnerBox(MatchX? matches, float elapsedTime)
    {
        if (matches?.IsMatchActive == true)
        {
            matches.Body!.Color = RED;
            matches.Body!.Color.ElapsedTime = elapsedTime;
            DrawRectangleRec(matches.WorldBox, matches.Body!.Color.Apply());
        }
    }
    
    public static void DrawOuterBox(EnemyMatches? matches, float elapsedTime)
    {
        if (matches?.IsMatchActive == true)
        {
            matches.Body!.Color.AlphaSpeed = 0.2f;
            matches.Body!.Color.ElapsedTime = elapsedTime;
            DrawRectangleRec(matches.Border, matches.Body!.Color.Apply());
        }
    }
    
    public static void DrawTimer(float elapsedSeconds)
    {
        TimerText.Text = ((int)elapsedSeconds).ToString();
        FadeableColor color = elapsedSeconds > 0f ? BLUE : WHITE;
        TimerText.Color = color with { CurrentAlpha = 1f, TargetAlpha = 1f };
        TimerText.Begin = (Utils.GetScreenCoord() * 0.5f) with { Y = 0f };
        TimerText.ScaleText();
        TimerText.Draw(null);
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
        Rectangle screen = new(0f, 0f, GetScreenWidth(), GetScreenHeight());
        DrawTexturePro(bg.Texture, bg.Body.TextureRect, screen.DoScale(bg.Body.Scale), Vector2.Zero, 0f, bg.Body.Color);
    }
    
    /*
    public static void LogQuest(bool useConsole, QuestData current)
    {
        foreach (var pair in current.BallCountPerLevel.State)
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