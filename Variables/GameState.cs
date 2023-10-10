using System.Text;

namespace Match_3.Variables;

public static class GameState
{
    public static bool WasSwapped;
    public static bool EnemiesStillPresent;
    public static bool WasGameWonB4Timeout;
    public static Tile Tile;
    public static MatchX? Matches;
    public static bool? WasFeatureBtnPressed;
    public static bool IsGameOver;
    public static StringBuilder Logger; //whatever the logger logged, take that to render!
}