namespace Match_3.DataObjects;

public class QuestState(TileColorTypes ColourType)
{
    public (int Count, float Elapsed) FoundMatch { get; set; }
    public (int Count, float Elapsed) WrongSwaps { get; set; }
    public (int Count, float Elapsed) ReplacementsUsed { get; set; }
    public (int Count, float Elapsed) WrongMatch { get; set; }
    public bool IsQuestLost { get; set; }
    public Tile? Current { get; set; }
    public TileColorTypes ColourType { get; } = ColourType;
}

public sealed class GameState
{
    // Singleton instance (thread-safe & lazy)
    private static readonly Lazy<GameState> _instance = new(() => new GameState());

    public MatchX Matches { get; } = [];

    //make this be loaded only once on a custom method()!
    public QuestState[] States { get; set; } = null!;
    public Quest[] ToAccomplish { get; set; } = null!;
    public QuestLogger Logger { get; set; } = null!;

    public int CurrentQuestCount
    {
        get => field is 0 ? ToAccomplish.Length : field;
        set;
    }

    public bool IsInGame { get; set; }
    public GameTime GetCurrentTime(in Config config) => GameTime.CreateTimer(config.GameBeginAt);
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

    public static GameState Instance => _instance.Value;

    // Private constructor to prevent external instantiation
    private GameState()
    {
        // Initialize QuestLogger only when first needed (lazy via property)
    }
}


///Game -> GameState(Has ongoing state-change)
///        |-------->QuestHolder (only init at start up, never changed after that!)
///        |-------->QuestState  (init at start up, but changes possible after that! unlimited times)
///        |-------->QuestLogger (init at start up, but changes possible after that! unlimited times)