using JetBrains.Annotations;

namespace Match_3.DataObjects;

public class QuestState(
    TileColorTypes ColourType,
    bool IsQuestLost,
    (int Count, float Elapsed) _foundMatch,
    (int Count, float Elapsed) _wrongSwaps,
    (int Count, float Elapsed) _replacementsUsed,
    (int Count, float Elapsed) _wrongMatch)
{
    public (int Count, float Elapsed) FoundMatch { get; set;} = _foundMatch;
    public (int Count, float Elapsed) WrongSwaps { get; set;} = _wrongSwaps;
    public (int Count, float Elapsed) ReplacementsUsed { get; set;} = _replacementsUsed;
    public (int Count, float Elapsed) WrongMatch { get; set;} = _wrongMatch;
    public bool IsQuestLost { get; set; } = IsQuestLost;
    public Tile? Current { get; set; }
    public TileColorTypes ColourType { get; } = ColourType;
}

public sealed class GameState
{
    // Singleton instance (thread-safe & lazy)
    private static readonly Lazy<GameState> _instance = new(() => new GameState());

    public readonly MatchX Matches = [];
    private int _questCount;

    public int QuestCount { get; set; }
    public QuestState[] States { get; private set; } = null!;
    public bool WasGameLost { get; set; }
    public bool WasGameWon { get; set; }
    public bool HaveAMatch { get; set; }
    public bool WasSwapped { get; set; }
    public int LevelId { get; set; }
    public Tile? TileY; //they must be fields, because I need later them to be used via "ref" directly!
    public Tile? TileX; //they must be fields, because I need later them to be used via "ref" directly!

    public IEnumerable<QuestState>? StatesFromQuestRelatedTiles;

    public TileColorTypes IgnoredByMatch { get; set; }

    public Direction LookUpUsedInMatchFinder { get; set; }

    public void InitStates(int questCount)
    {
        _questCount = questCount;
        States = new QuestState[questCount];
    }

    public void DefineStateType(int index, TileColorTypes colorType)
    {
        States[index] = new(colorType,
            false,
            (0, 0),
            (0, 0),
            (0, 0),
            (0, 0));
    }

    public static GameState Instance => _instance.Value;

    // Private constructor to prevent external instantiation
    private GameState()
    {
        // Initialize QuestLogger only when first needed (lazy via property)
    }

    [Pure]
    public QuestState GetStateBy(TileColorTypes tileColorTypes)
    {
        var states = States.AsSpan(0, _questCount);

        foreach (var state in states)
        {
            if (state.ColourType == tileColorTypes)
                return state;
        }

        return null!;
    }
}