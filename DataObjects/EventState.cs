namespace Match_3.DataObjects;

public class EventState
{
    public State[]? StatePerQuest;
    public Tile? TileX, TileY;
    public IEnumerable<State>? StatesFromQuestRelatedTiles;
    
    public TileColorTypes IgnoredByMatch;
    public Direction LookUpUsedInMatchFinder;
    public readonly MatchX Matches = new();
    public bool HaveAMatch, 
                WasSwapped,
                WasGameWon,
                WasGameLost;
}