namespace Match_3.DataObjects;

public class EventState
{
    public State[]? StatePerQuest;
    public IEnumerable<State>? StatesFromQuestRelatedTiles;
    public TileColor IgnoredByMatch;
    public Tile? TileX, TileY;
    public MatchX? Matches = new();
    public bool WasMatch, 
                WasSwapped,
                EnemiesStillPresent,
                WasGameWonB4Timeout,
                IsGameOver;
}