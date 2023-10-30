using System.Numerics;

namespace Match_3.StateHolder;

public class EventState
{
    public Vector2 Coords;
    public float Interval;
    public int Count;
    public Tile? TileX;
    public Tile? TileY;
    public MatchX? Matches;
    public bool WasMatch, StillTimeLeft, WasSwapped;
}