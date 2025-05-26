using Match_3.Service;

namespace Match_3.DataObjects;

public sealed class GameState
{
    // Singleton instance (thread-safe & lazy)
    private static readonly Lazy<GameState> _instance = new(() => new GameState());
    
    public static GameState Instance => _instance.Value;

    // Private constructor to prevent external instantiation
    private GameState()
    {
        // Initialize Logger only when first needed (lazy via property)
    }

    // --- Core Quest Data (immutable after initialization) ---
    public Config Lvl { get; set; }  // Set this during game initialization

    // QuestLog remains a computed string (now with instance access)
    public const string QuestLog = $"(Black) You have to collect ({Quest.TileColorName}) {Quest.MatchCountName} Matches " +
                             $"(Black) and u have in between those only ({Quest.TileColorName}) {Quest.MatchIntervalName} seconds left " +
                             $"(Black) and also just ({Quest.TileColorName}) {Quest.SwapCountName} swaps available per match " +
                             $"(Black) and furthermore, you only are allowed to replace any given tile ({Quest.TileColorName}) {Quest.ReplacementCountName} times maximum " +
                             $"(Black) for your own help as well as there is only tolerance for ({Quest.TileColorName}) {Quest.WrongMatchName} wrong matches";

    // --- Lazy-Loaded Resources ---
    private SpanQueue<char>? _logger;

    public SpanQueue<char> Logger => _logger ??= new SpanQueue<char>(QuestLog.Length * Lvl.QuestCount);

    public ReadOnlySpan<char> GetPooledQuestLog() => Logger.Dequeue(true);

    // --- Current State ---
    public readonly EventState CurrData = new();  

    public FastSpanEnumerator<Quest> GetQuests()
        => new(Lvl.Quests.AsSpan(0, Lvl.QuestCount));

    public ref readonly Quest GetQuestBy(TileColor tileColor)
    {
        var iterator = new FastSpanEnumerator<Quest>(Lvl.Quests.AsSpan(0, Lvl.QuestCount));

        foreach (ref readonly Quest quest in iterator)
        {
            if (quest.TileColor == tileColor)
                return ref quest;
        }

        return ref Quest.Empty;
    }
    
    public State GetStateBy(TileColor tileColor)
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