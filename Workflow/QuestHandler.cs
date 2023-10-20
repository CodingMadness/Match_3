using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Match_3.Datatypes;

namespace Match_3.Workflow;

public static class SingletonManager
{
    private const byte MaxQuestHandlerInstances = 5;

    public static readonly Dictionary<Type, QuestHandler> QuestHandlerStorage = new(MaxQuestHandlerInstances);

    public static T GetOrCreateQuestHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        T>() where T : QuestHandler
    {
        lock (QuestHandlerStorage)
        {
            if (QuestHandlerStorage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = Activator.CreateInstance(typeof(T), true) as QuestHandler;

            QuestHandlerStorage.Add(typeof(T), toReturn);

            return (T)toReturn;
        }
    }
}

/// <summary>
///The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
///or about other events
///Game -------->(notifies) QuestHandler-------->(compares) "GameState" with "Quest"
/// and based on the comparison, it decides what to do!
/// For instance
/// Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
///----->within a TimeSpan of X-sec
///----->without any miss-swap!
///the stats is to make per new Level the Quests harder!!
/// </summary>
public abstract class QuestHandler
{
    private readonly Type _self;

    protected int QuestCounter { get; set; }

    protected int QuestsLeft => QuestBuilder.QuestCount - QuestCounter;

    protected bool IsMainGoalReached => QuestsLeft == 0;

    private bool IsActive { get; set; }

    protected static THandler GetInstance<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        THandler>()
        where THandler : QuestHandler
        => SingletonManager.GetOrCreateQuestHandler<THandler>();

    /// <summary>
    ///The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
    ///or about other events
    ///Game -------->(notifies) QuestHandler-------->(compares) "GameState" with "Goal" and based on the comparison, it decides what to do!
    /// For instance
    /// Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    ///----->within a TimeSpan of X-sec
    ///----->without any miss-swap!
    ///the stats is to make per new Level the Quests harder!!
    /// </summary>
    protected QuestHandler(Type self)
    {
        _self = self;
    }

    // protected static bool IsSubQuestReached(EventType eventType, in Quest quest,
    //     in Stats stats,
    //     out int direction)
    // {
    //     direction = default;
    //
    //     return eventType switch
    //     {
    //         EventType.Swapped => (direction = quest.CompareSwaps(stats)) == 0,
    //         EventType.Matched => (direction = quest.CompareMatches(stats)) == 0,
    //         _ => false
    //     };
    // }

    protected abstract void CompareQuest2State();

    public static void ActivateHandlers()
    {
        MatchQuestHandler.Instance.Subscribe();
    }

    //its private for now until I will need my other Handlers for Clicks and Destruction events, which has to be 
    //subbed and un-subbed at certain times..
    private void Subscribe()
    {
        if (IsActive) return;

        SingletonManager.QuestHandlerStorage.TryAdd(_self, this);
        IsActive = true;
    }

    public void UnSubscribe()
    {
        if (!IsActive) return;

        SingletonManager.QuestHandlerStorage.Remove(_self);
        IsActive = false;
    }
}

public sealed class MatchQuestHandler : QuestHandler
{
    public static readonly MatchQuestHandler Instance = GetInstance<MatchQuestHandler>();

    private MatchQuestHandler() : base(typeof(MatchQuestHandler))
    {
        Game.OnTileSwapped += CompareQuest2State;
        Game.OnMatchFound += CompareQuest2State;
    }

    protected override void CompareQuest2State()
    {
        var questRunner = QuestBuilder.GetQuests();

        foreach (var quest in questRunner)
        {
            int swaps2Do = quest.Swap!.Value.Count;
            EventStats? currData = GameState.CurrentData;
            float swapTimeAllowed = quest.Swap!.Value.Interval;
            int matchQuest = quest.Match!.Value.Count;
            
            TileColor
                x = currData!.TileX!.Body.TileColor,
                y = currData.TileY!.Body.TileColor;

            bool sameTileColor = (x == quest.TileColor || y == quest.TileColor);
            //the more this if- is executed the worse, because it says that we dont get matches for each swap we do!
            if (!currData.wasMatch || x != quest.TileColor || y != quest.TileColor)
            {
                Debug.WriteLine(
                    $"A swap between <{currData.TileX!.Body.TileColor}> " +
                    $"AND <{currData.TileY!.Body.TileColor}> was made" +
                    $"and it did not lead to a match..");
            }
            //we have a match and also its the same color BUT we needed to long for the next swap
            //and hence failed... 
            else if (currData.wasMatch && sameTileColor
                     && swapTimeAllowed >= currData.Interval)
            {
                Debug.WriteLine("Upsi, you needed to long amigo");
            }
            else if (currData.wasMatch && sameTileColor &&
                     currData.Count == matchQuest)
            {
                Debug.WriteLine("ADD BONUS POINTS TO SCORE!");
            }


            if (currData.Count == swaps2Do && !currData.wasMatch)
            {
                Debug.WriteLine("Now you gotta be punished, NOT 1 MATCH!! WTF");
            }
        }
    }
}

