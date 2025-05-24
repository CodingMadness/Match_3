using Match_3.Service;

namespace Match_3.DataObjects;

public static class GameState
{
    public static Config Lvl;
    public static readonly SpanQueue<char> Logger = new(1024);
    public static readonly EventState CurrData = new();

    public static FastSpanEnumerator<Quest> GetQuests()
        => new(Lvl.Quests.AsSpan(0, Lvl.QuestCount));

    public static ref readonly Quest GetQuestBy(TileColor tileColor)
    {
        var iterator = new FastSpanEnumerator<Quest>(Lvl.Quests.AsSpan(0, Lvl.QuestCount));

        foreach (ref readonly Quest quest in iterator)
        {
            if (quest.TileKind == tileColor)
                return ref quest;
        }

        return ref Quest.Empty;
    }
    
    public static State GetStateBy(TileColor tileColor)
    {
        var iterator = new FastSpanEnumerator<State>(CurrData.StatePerQuest.AsSpan(0, Lvl.QuestCount));

        foreach (State state in iterator)
        {
            if (state.TileKind == tileColor)
                return state;
        }

        return null!;
    }
}