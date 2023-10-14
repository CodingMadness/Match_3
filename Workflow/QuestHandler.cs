using System.Diagnostics.CodeAnalysis;
using Match_3.Variables;

namespace Match_3.Workflow;

public static class SingletonManager
{
    private const byte MaxQuestHandlerInstances = 5;
    private const byte MaxRuleHandlerInstances = 1;

    public static readonly Dictionary<Type, QuestHandler> QuestHandlerStorage = new(MaxQuestHandlerInstances);
    public static readonly Dictionary<Type, RuleHandler> RuleHandlerStorage = new(MaxRuleHandlerInstances);

    public static T GetOrCreateQuestHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]T>() where T : QuestHandler
    {
        lock (QuestHandlerStorage)
        {
            if (QuestHandlerStorage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = Activator.CreateInstance(typeof(T), true) as QuestHandler;

            QuestHandlerStorage.Add(typeof(T), toReturn!);

            return (T)toReturn!;
        }
    }

    public static T GetOrCreateRuleHandler<T>() where T : RuleHandler
    {
        lock (QuestHandlerStorage)
        {
            if (RuleHandlerStorage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = (RuleHandler?)Activator.CreateInstance(typeof(T), true);

            RuleHandlerStorage.Add(typeof(T), toReturn!);

            return (T)toReturn;
        }
    }
}


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
public abstract class QuestHandler
{
    private readonly Type _self;
    
    protected int QuestCounter { get; set; }

    protected int QuestLeft => QuestBuilder.QuestCount - QuestCounter;
    
    protected bool IsMainGoalReached => QuestLeft == 0;
    
    private bool IsActive { get; set; }

    protected static THandler GetInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]THandler>() where THandler : QuestHandler
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

    protected static bool IsSubQuestReached(EventType eventType, in Quest quest, in AllStats allStats,
        out int direction)
    {
        direction = default;

        return eventType switch
        {
            EventType.Swapped => (direction = quest.CompareSwaps(allStats)) == 0,
            EventType.Matched => (direction = quest.CompareMatches(allStats)) == 0,
            _ => false
        };
    }

    protected abstract void HandleEvent();
    
    public static void ActivateHandlers()
    {
        MatchQuestHandler.Instance.Subscribe();
        SwapQuestHandler.Instance.Subscribe();
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

public sealed class SwapQuestHandler : QuestHandler
{
    public static SwapQuestHandler Instance { get; } = GetInstance<SwapQuestHandler>();

    private SwapQuestHandler() : base(typeof(SwapQuestHandler)) 
        => Game.OnTileSwapped += HandleEvent;

    protected override void HandleEvent()
    {
        //... needs logic!...//
    }
}

public sealed class MatchQuestHandler : QuestHandler
{
    private void CompareFinalResults()
    {
        //clear whatever was written inside the logger, because we are done now anyways and we wanna store inside 
        //to see what is the final GameState
        
        GameState.Logger.Clear();

        switch (IsMainGoalReached)
        {
            case true when !GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = true;
                GameState.Logger.Append("NICE, YOU FINISHED THE ENTIRE MATCH-QUEST BEFORE THE GAME ENDED! WELL DONE Man!");
                break;

            case false when GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = false;
                GameState.Logger.Append(
                    "Unlucky, very close, you will do it next time, i am sure :-), but here your results" +
                    $"So far you have gotten at least {QuestCounter} done and you needed actually still {QuestLeft} more");
                break;

            case true when GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = false;
                GameState.Logger.Append("Good job, you did in time at least, you piece of shit!!");
                break;
        }
    }

    private MatchQuestHandler() : base(typeof(MatchQuestHandler))
    {
        Game.OnMatchFound += HandleEvent;
    }

    public static MatchQuestHandler Instance { get; } = GetInstance<MatchQuestHandler>();
    
    private static bool IsMatchQuestReached(out Quest? quest, in AllStats allStats, out int direction)
    {
        if (allStats.Matched is null)
        {
            quest = null;
            direction = int.MaxValue;
            return false;
        }

        var type = GameState.Tile.Body.TileColor;
        quest = QuestBuilder.GetQuestFrom(type);
        return IsSubQuestReached(EventType.Matched, quest.Value, allStats, out direction);
    }

    protected override void HandleEvent()
    {
        var type = GameState.Matches!.Body!.TileColor;
        ref var stats = ref Grid.GetStatsByType(type);

        if (stats.Matched is null)
        {
            GameState.Matches.Clear();
            return;
        }

        stats[EventType.Matched].Count++;

        if (IsMatchQuestReached(out var goal, stats, out int compareResult))
        {
            QuestCounter++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Sub goals done:  {QuestCounter}");
            Console.ResetColor();
            //Console.WriteLine($"yea, we got {stats.Matched!.Value.Count} match of type: {type} within");
            //Console.WriteLine($"current match goal:  {goal?.Match}");
            stats.Matched = null;
            //Console.WriteLine("YEA YOU GOT current MATCH AND ARE REWARDED FOR IT !: ");
            Console.Clear();
            // Grid.Instance.Delete(GameState.Matches);
        }
        else
        {
            CompareFinalResults();

            //error-code
            if (compareResult > 1)
            {
                GameState.Matches.Clear();
            }

            //Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"yea, we got {stats.Matched!.Value.Count} match of type: {type} within");
            Console.WriteLine($"current match goal:  {goal?.Match}");
            // Grid.Instance.Delete(GameState.Matches);
        }
    }
}
 
