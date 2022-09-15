namespace Match_3
{
    public record GameState(int GameStartAt, 
                            int GameOverScreenTime, 
                            int TilemapWidth,  
                            int TilemapHeight,
                            Dictionary<ShapeKind, int> ToCollect,
                            int TileSize)
    {
        public int WINDOW_HEIGHT => TilemapHeight * TileSize;
        public int WINDOW_WIDTH => TilemapWidth * TileSize;

        public Int2 MIDDLE_TOP => new Int2(WINDOW_WIDTH/2, 0) - Int2.UnitX*25;
    }
}
