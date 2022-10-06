namespace Match_3.GameTypes;

public sealed class GameState
{
    public bool WasSwapped;
    public float ElapsedTime;
    public (Type ballType, int Count) TilesClicked;
    public (Type ballType, int collected) CollectPair;
    public (Type ballType, int count) Swapped;
    public bool AreEnemiesStillPresent;
    public int[] TotalCountPerType;
    public bool WasGameWonB4Timeout;
    public EnemyTile Enemy;
    public Grid Map;
}