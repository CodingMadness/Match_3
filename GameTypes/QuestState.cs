namespace Match_3.GameTypes;

public sealed class QuestState
{
    public bool WasSwapped;
    public float ElapsedTime;
    public (Type ballType, int Count) TilesClicked;
    public (Type ballType, int collected) CollectPair;
    public (Type ballType, int count) Swapped;
    public bool EnemiesStillPresent;
    public int[] TotalCountPerType;
    public bool WasGameWonB4Timeout;
    public EnemyTile Enemy;
    public Grid Map;
}