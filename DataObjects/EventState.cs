namespace Match_3.DataObjects;

public class EventState
{
    public State[]? StatePerQuest;
    public IEnumerable<State>? StatesFromQuestRelatedTiles;
    public TileColor IgnoredByMatch;
    public Direction MatchFindingLookUp;
    public Tile? TileX, TileY;
 
    public readonly MatchX? Matches = new();
    public bool HaveAMatch, 
                WasSwapped,
                WasGameWonB4Timeout,
                IsGameOver;
}