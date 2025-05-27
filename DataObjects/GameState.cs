using JetBrains.Annotations;
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
    
    public const string QuestLog = $"(Black) You have to collect ({Quest.TileColorName}\0\0\0) {Quest.MatchCountName} Matches " +
                             $"(Black) and u have in between those only ({Quest.TileColorName}\0\0\0) {Quest.MatchIntervalName} seconds left " +
                             $"(Black) and also just ({Quest.TileColorName}\0\0\0) {Quest.SwapCountName} swaps available per match " +
                             $"(Black) and furthermore, you only are allowed to replace any given tile ({Quest.TileColorName}\0\0\0) {Quest.ReplacementCountName} times maximum " +
                             $"(Black) for your own help as well as there is only tolerance for ({Quest.TileColorName}\0\0\0) {Quest.WrongMatchName} wrong matches";

    // --- Lazy-Loaded Resources ---
    private SpanQueue<char>? _logger;

    public SpanQueue<char> Logger => _logger ??= new SpanQueue<char>(QuestLog.Length * Lvl.QuestCount);

    [Pure]
    public ReadOnlySpan<char> GetPooledQuestLog() => Logger.Dequeue(true);

    // --- Current State ---
    public readonly EventState CurrData = new();  

    [Pure]
    public Span<Quest> GetQuests() => Lvl.Quests.AsSpan(0, Lvl.QuestCount);

    [Pure]
    public ref readonly Quest GetQuestBy(TileColor tileColor)
    {
        var onlyNeededQuests = Lvl.Quests.AsSpan(0, Lvl.QuestCount);

        foreach (ref readonly Quest quest in onlyNeededQuests)
        {
            if (quest.TileColor == tileColor)
                return ref quest;
        }

        return ref Quest.Empty;
    }
    
    [Pure]
    public State GetStateBy(TileColor tileColor)
    {
        var states = CurrData.StatePerQuest.AsSpan(0, Lvl.QuestCount);

        foreach (State state in states)
        {
            if (state.TileKind == tileColor)
                return state;
        }

        return null!;
    }
}