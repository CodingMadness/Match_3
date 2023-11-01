namespace Match_3.StateHolder;

public class EventState
{
    public Tile? TileX;
    public Tile? TileY;
    public MatchX? Matches = new();
    public bool WasMatch, WasSwapped;
}