using System.Collections;
using Match_3.Service;

namespace Match_3.StateHolder;

public static class GameState
{
    public static bool EnemiesStillPresent, WasGameWonB4Timeout, IsGameOver;

    public static int QuestCount;
    public static Quest[]? Quests;
    public static Level? CurrentLvl;
    public static EventState? CurrData;
    public static SpanQueue<char>? Logger; //whatever the logger logged, take that to render!
    
    public static FastSpanEnumerator<Quest> GetQuests()
        => new(Quests.AsSpan().TrimEnd(default(Quest)));

    public static ref readonly Quest GetQuestBy(TileColor tileColor)
    {
        var iterator = new FastSpanEnumerator<Quest>(Quests.AsSpan(0, QuestCount));

        foreach (ref readonly var quest in iterator)
        {
            if (quest.TileColor == tileColor)
                return ref quest;
        }

        return ref Quest.Empty;
    }
}