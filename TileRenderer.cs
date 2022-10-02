using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public static class TileRenderer
{
    private static Texture atlas = AssetManager.DefaultTileAtlas;
    public static ref Texture GetAtlas() => ref atlas;

    private static void Draw(Tile tile, float elapsedTime)
    {
        static void DrawCoordOnTop(Tile tile)
        {
            Font copy = GetFontDefault() with { baseSize = 1024 };
            var begin = (tile as ITile).End;
            float halfSize = ITile.Size * 0.5f;
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
        
        var body = tile!.Body;
        body.Color.ElapsedTime = elapsedTime;
        DrawTextureRec(GetAtlas(), tile.DestRect, (tile as ITile).Begin, body.Color.Apply());
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
                ITile? basicTile = map[current];
                    
                if (basicTile is not null && !basicTile.IsDeleted)
                {
                    GetAtlas() = (basicTile is EnemyTile)
                        ? AssetManager.EnemyAtlas
                        : AssetManager.DefaultTileAtlas;

                    Draw((Tile)basicTile, elapsedTime);
                }
            }
        }

        //isDrawn = true;
        //Console.WriteLine("ITERATION OVER FOR THIS DRAW-CALL!");
    }
    
}