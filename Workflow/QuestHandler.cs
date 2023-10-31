using System.Diagnostics;
using DotNext.Runtime;
using Match_3.Service;
using Match_3.Setup;
using Match_3.StateHolder;

namespace Match_3.Workflow;

file static class SingletonManager
{
    private const byte MaxQuestHandlerInstances = 5;

    public static readonly Dictionary<Type, QuestHandler> QuestHandlerStorage = new(MaxQuestHandlerInstances);

    public static T GetOrCreateQuestHandler<[DAM(DAMTypes.NonPublicConstructors)] T>() where T : QuestHandler
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

    protected static THandler GetInstance<[DAM(DAMTypes.NonPublicConstructors)] THandler>()
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

    protected QuestHandler()
    {
        _self = null!;
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
        ClickHandler.Instance.Subscribe();
        SwapHandler.Instance.Subscribe();
        MatchHandler.Instance.Subscribe();
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

public class ClickHandler : QuestHandler
{
    public static readonly ClickHandler Instance = GetInstance<ClickHandler>();
    
    public event Action? OnSwapTile, OnAfterTilesWereSwapped;
    
    private ClickHandler( ) : base(typeof(ClickHandler))     //--> NOTIFIER!
    {
        //Event-System rule of thumb:
        // ----> The "Notifier" has to declare the event-type!  AND has to invoke() it in his own code-base somewhere!
        // ----> The "receiver" has to subscribe the event AND handle with appropriate code logic the specific event-case!
        
        Game.OnTileClicked += CompareQuest2State;  //--> registration!
    }

    private void ProcessSelectedTiles()
    {
        // _whenTileClicked = TimeOnly.FromDateTime(DateTime.UtcNow);
        var currData = GameState.CurrData;
        var firstClicked = currData!.TileX;
        ref var secondClicked = ref currData.TileY;
        // var enemyMatches = currentData.Matches;

        if (firstClicked!.IsDeleted)
            return;

        //was Enemy tile clicked on, ofc after a matchX happened?
        if (IsEnemyTileClickedOn)
        {
            // Do this when the I need to handle enemy tiles!.....
        }
        else
        {
            if (IsUsualTileClickedOn)
            {
                // Do this when the I need to handle extra stuff for normal tiles!.....
            }

            firstClicked.TileState |= TileState.Selected;

            /*No tile selected yet*/
            if (secondClicked is null)
            {
                //prepare for next round, so we store first in second!
                secondClicked = firstClicked;
                return;
            }

            /*Same tile selected => deselect*/
            if (Comparer.StateAndBodyComparer.Singleton.Equals(firstClicked, secondClicked))
            {
                Console.Clear();
                //Console.WriteLine($"{tmpFirst.GridCell} was clicked AGAIN!");
                secondClicked.TileState &= ~TileState.Selected;
                secondClicked = null;
            }
            /*Different tile selected ==> swap*/
            else
            {
                firstClicked.TileState &= ~TileState.Selected;
                
                currData.TileY = secondClicked;
                currData.TileX = firstClicked;
                OnSwapTile?.Invoke();
                
                if (currData.WasSwapped)
                {
                    Debug.WriteLine("SWAPPED 2 TILES!");
                    //the moment we have the 1. swap, we notify the SwapHandler for this
                    //and he begins to keep track of (HOW LONG did the swap took) and
                    //(HOW MANY MISS-SWAPS HAPPENED!)
                    firstClicked.TileState &= ~TileState.Selected;
                    currData.Count++;
                    OnAfterTilesWereSwapped?.Invoke();
                    secondClicked = null;  //he is the first now
                }
                else
                {
                    firstClicked.TileState &= ~TileState.Selected;
                }
            }
        }
    }

    public static bool IsUsualTileClickedOn => Intrinsics.IsExactTypeOf<Tile>(GameState.CurrData!.TileX);

    public static bool IsEnemyTileClickedOn => GameState.CurrData?.Matches?.IsMatchActive == true
                                               && Intrinsics.IsExactTypeOf<EnemyTile>
                                                   (GameState.CurrData.TileX);
    
    protected override void CompareQuest2State()
    {
        ProcessSelectedTiles();
    }
}

public class SwapHandler : QuestHandler
{
    private SwapHandler() : base(typeof(SwapHandler))
    {
        ClickHandler.Instance.OnAfterTilesWereSwapped += CompareQuest2State;
    }

    public static readonly SwapHandler Instance = GetInstance<SwapHandler>();

    public event Action? OnCheckForMatch;
    
    protected override void CompareQuest2State()
    {
        //Check GameState with Quest and see if he has to be punished!
        var state = GameState.CurrData!;
        
        var colorX = state.TileX!.Body.TileColor;
        ref readonly var questForX = ref GameState.GetQuestBy(colorX);
        
        var colorY = state.TileY!.Body.TileColor;
        ref readonly var questForY = ref GameState.GetQuestBy(colorY);
        
        Debug.WriteLine($"Quest for: {questForX.TileColor} and {questForY.TileColor}");
        
        //1. check if the tiles which were swapped are even needed for the Quest!
        //define some condition by which we can assert that all possible combinations are being gathered!
        if ((colorX != questForX.TileColor && colorX != questForY.TileColor) &&
            (colorY != questForX.TileColor && colorY != questForY.TileColor))
        {
            Debug.WriteLine($"Neither {nameof(colorX)} nor {nameof(colorY)} have to do anything with the Quest!?");
        }
        else
        {
            Debug.WriteLine("YES this is okay!");
        }
    }
}

public class MatchHandler : QuestHandler
{
    public static readonly MatchHandler Instance = GetInstance<MatchHandler>();
   
    private MatchHandler() : base(typeof(MatchHandler))
    {
        Grid.OnMatchFound += CompareQuest2State;
    }

    protected override void CompareQuest2State()
    {
        var questRunner = GameState.GetQuests();

        foreach (var quest in questRunner)
        {
            int swaps2Do = quest.Swap!.Value.Count;
            EventState? currData = GameState.CurrData;
            float swapTimeAllowed = quest.Swap!.Value.Interval;
            int matchQuest = quest.Match!.Value.Count;

            TileColor
                x = currData!.TileX!.Body.TileColor,
                y = currData.TileY!.Body.TileColor;

            bool sameTileColor = (x == quest.TileColor || y == quest.TileColor);
            //the more this if- is executed the worse, because it says that we dont get matches for each swap we do!
            if (!currData.WasMatch || x != quest.TileColor || y != quest.TileColor)
            {
                Debug.WriteLine(
                    $"A swap between <{currData.TileX!.Body.TileColor}> " +
                    $"AND <{currData.TileY!.Body.TileColor}> was made " +
                    $"and it did not lead to a match..");
            }
            else
                switch (currData.WasMatch)
                {
                    //we have a match and also its the same color BUT we needed to long for the next swap
                    //and hence failed... 
                    case true when sameTileColor
                                   && swapTimeAllowed >= currData.Interval:
                        Debug.WriteLine("Upsi, you needed to long amigo");
                        break;
                    case true when sameTileColor &&
                                   currData.Count == matchQuest:
                        Debug.WriteLine("ADD BONUS POINTS TO SCORE!");
                        break;
                }

            if (currData.Count == swaps2Do && !currData.WasMatch)
            {
                Debug.WriteLine("Now you gotta be punished, NOT 1 MATCH!! WTF");
            }

            break;
        }
    }
}