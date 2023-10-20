namespace Match_3.Datatypes;

public static class GameState
{
    public static bool EnemiesStillPresent, WasGameWonB4Timeout, IsGameOver;

    public static Quest[]? Quests;
    public static EventState? CurrentData;
    public static GameStateMessagePool? Logger; //whatever the logger logged, take that to render!
}