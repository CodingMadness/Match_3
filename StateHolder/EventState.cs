namespace Match_3.StateHolder;

public class EventState
{
    public float Interval;
    public int Count;
    public Tile? TileX;
    public Tile? TileY;
    public MatchX? Matches;
    public bool wasMatch, StillTimeLeft, WasSwapped;
}