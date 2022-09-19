using System.Numerics;

namespace Match_3.GameTypes;

public record GameState(int GameStartAt,
                           int GameOverScreenTime,
                           int TilemapWidth,
                           int TilemapHeight,
                           Dictionary<Sweets, int> ToCollect,
                           int TileSize, int MaxAllowedSwpas)
{
    public int WINDOW_HEIGHT => TilemapHeight * TileSize;
    
    public int WINDOW_WIDTH => TilemapWidth * TileSize;

    public Vector2 TopCenter => new Vector2(WINDOW_WIDTH / 2, 0) - Vector2.UnitX * 25;
   
    public Vector2 Center => new (WINDOW_WIDTH / 2, WINDOW_HEIGHT / 2);
}
