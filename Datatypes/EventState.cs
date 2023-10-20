namespace Match_3.Datatypes;

public class EventState
{
    public float Interval;
    public int Count;
    public Tile? TileX;
    public Tile? TileY;
    public MatchX? Matches;
    public bool wasMatch, StillTimeLeft, WasSwapped;
}