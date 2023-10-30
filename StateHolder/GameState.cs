using Match_3.Service;

namespace Match_3.StateHolder;

public static class GameState
{
    public static bool EnemiesStillPresent, WasGameWonB4Timeout, IsGameOver;

    public static Quest[]? Quests;
    public static Level? CurrentLvl;
    public static EventState? CurrData;
    public static SpanQueue<char>? Logger; //whatever the logger logged, take that to render!
}