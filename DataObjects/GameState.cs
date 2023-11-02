using Match_3.Service;

namespace Match_3.DataObjects;

public static class GameState
{
    public static bool EnemiesStillPresent, WasGameWonB4Timeout, IsGameOver;
    public static int QuestCount;
    public static Quest[]? Quests;
    public static State[]? StatePerQuest;
    public static IEnumerable<State>? StatesFromQuestRelatedTiles;
    public static Level? CurrentLvl;
    public static EventState? CurrData;
    public static SpanQueue<char>? Logger; //whatever the logger logged, take that to render!
    
    public static FastSpanEnumerator<Quest> GetQuests()
        => new(Quests.AsSpan(0, QuestCount));

    public static ref readonly Quest GetQuestBy(TileColor tileColor)
    {
        var iterator = new FastSpanEnumerator<Quest>(Quests.AsSpan(0, QuestCount));

        foreach (ref readonly Quest quest in iterator)
        {
            if (quest.TileKind == tileColor)
                return ref quest;
        }

        return ref Quest.Empty;
    }
    
    public static State GetStateBy(TileColor tileColor)
    {
        var iterator = new FastSpanEnumerator<State>(StatePerQuest.AsSpan(0, QuestCount));

        foreach (State state in iterator)
        {
            if (state.TileKind == tileColor)
                return state;
        }

        return null!;
    }
}