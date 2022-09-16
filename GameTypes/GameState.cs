namespace Match_3.GameTypes;

public record GameState(int GameStartAt,
                           int GameOverScreenTime,
                           int TilemapWidth,
                           int TilemapHeight,
                           Dictionary<ShapeKind, int> ToCollect,
                           int TileSize, int MaxAllowedSwpas)
{
    public int WINDOW_HEIGHT => TilemapHeight * TileSize;
    
    public int WINDOW_WIDTH => TilemapWidth * TileSize;

    public Int2 TopCenter => new Int2(WINDOW_WIDTH / 2, 0) - Int2.UnitX * 25;
   
    public Int2 Center => new (WINDOW_WIDTH / 2, WINDOW_HEIGHT / 2);
}
