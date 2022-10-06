namespace Match_3.GameTypes;

public sealed class Goal
{
    public const int MAX_MATCHES_TO_COLLECT = 3;
    public const int MAX_TILES_PER_MATCH = 3;
    public float GoalTime;
    public int ClicksPerTileNeeded;
    public int TypeCountToCollect;
    public int MatchCountPerTilesToCollect;
    public int MissedSwapsTolerance;
}