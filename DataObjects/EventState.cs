namespace Match_3.DataObjects;

public class EventState
{
    public TileColor IgnoredByMatch;
    public Tile? TileX;
    public Tile? TileY;
    public MatchX? Matches = new();
    public bool WasMatch, WasSwapped;
}