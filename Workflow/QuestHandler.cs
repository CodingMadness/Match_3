using System.Diagnostics;
using DotNext.Collections.Generic;
using DotNext.Runtime;

using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Setup;
using Match_3.Variables.Extensions;

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

    protected abstract void CompareQuest2State();

    public static void ActivateHandlers()
    {
        ClickHandler.Instance.Subscribe();
        SwapHandler.Instance.Subscribe();
        MatchHandler.Instance.Subscribe();
    }

    //its private for now until I will need my other Handlers for Clicks and Destruction events, which has to be 
    //subbed and un-subbed at certain times..
    public void Subscribe()
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

    public event Action OnSwapTiles, OnTilesAlreadySwapped;

    private ClickHandler() : base(typeof(ClickHandler)) //--> NOTIFIER!
    {
        //Event-System rule of thumb:
        // ----> The "Notifier" has to declare the event-type!  AND has to invoke() it in his own code-base somewhere!
        // ----> The "receiver" has to subscribe the event AND handle with appropriate code logic the specific event-case!

        Game.OnTileClicked += CompareQuest2State; //--> registration!
    }

    protected override void CompareQuest2State()
    {
        // _whenTileClicked = TimeOnly.FromDateTime(DateTime.UtcNow);
        var currData = GameState.CurrData!;
        var firstClicked = currData.TileX;
        ref var secondClicked = ref currData.TileY;
        // var enemyMatches = currentData.Matches;

        if (firstClicked!.IsDeleted)
            return;

        //was Enemy tile clicked on, ofc after a matchX happened?
        if (IsEnemyTileClickedOn)
        {
            // OnEnemyTileClicked();
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
                OnSwapTiles();

                if (currData.WasSwapped)
                {
                    Debug.WriteLine("SWAPPED 2 TILES!");
                    //the moment we have the 1. swap, we notify the SwapHandler for this
                    //and he begins to keep track of (HOW LONG did the swap took) and
                    //(HOW MANY MISS-SWAPS HAPPENED!)
                    firstClicked.TileState &= ~TileState.Selected;
                    
                    //find all states whose "TileColor" were part of the swap!
                    //it can be ONLY 1 OR 2 but NEVER 0!
                    var second = secondClicked;
                    GameState.StatesFromQuestRelatedTiles = GameState.StatePerQuest!
                                                    .Where(x => x.TileKind == firstClicked.Body.TileKind ||
                                                           x.TileKind == second.Body.TileKind);
                    
                    OnTilesAlreadySwapped();
                    secondClicked = null; //he is the first now
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
}

public class SwapHandler : QuestHandler
{
    private SwapHandler() : base(typeof(SwapHandler))
    {
        ClickHandler.Instance.OnTilesAlreadySwapped += CompareQuest2State;
    }

    public static readonly SwapHandler Instance = GetInstance<SwapHandler>();

    public event Action OnCheckForMatch, OnMatchFound;

    private static (TileColor x, TileColor y) GetTileColorAndQuestData(out Quest? eventDataOfX, out Quest? eventDataOfY)
    {
        var state = GameState.CurrData!;
        var colorX = state.TileX!.Body.TileKind;
        var colorY = state.TileY!.Body.TileKind;
        eventDataOfX = GameState.GetQuestBy(colorX);
        eventDataOfY = GameState.GetQuestBy(colorY);
        return (colorX, colorY);
    }
    
    protected override void CompareQuest2State()
    {
        //Check GameState with Quest and see if he has to be punished!
        var swapState = GameState.CurrData!;

        var (x, y) = GetTileColorAndQuestData(out var quest0, out var quest1);
        
        //1. check if the tiles which were swapped are even needed for the Quest!
        //define some condition by which we can assert that all possible combinations are being gathered!
        if ((x != quest0?.TileKind && x != quest0?.TileKind) &&
            (y != quest1?.TileKind && y != quest1?.TileKind))
        {
            Debug.WriteLine($"Neither {nameof(x)} nor {nameof(y)} have to do anything with the Quest!?" +
                            $"so you have to be (in some way or another) to be punished!");
            
            // PunishPlayer();
        }
        else
        {
            if (GameState.StatesFromQuestRelatedTiles!.Any(z => (z.TileKind == x || z.TileKind == y) && z.IsQuestLost))
            {
                Debug.WriteLine("You lost this Quest already, now feel the punishment and continue with the other Quests, your time is running short!");
                return;
            }
                    
            //2. Check if we didnt exceed the allowed Swap-Count for all the different quests
            OnCheckForMatch();

            if (swapState.WasMatch)
            {
                swapState.IgnoredByMatch = x == swapState.Matches!.Body!.TileKind ? y : x;
                
                OnMatchFound();
            }
            else
            {
                //Now we increase the swapCount for the participating-TileColor's only if there was not a match
                //because then we need to +1 up and see if the player reached the limits
                GameState.StatesFromQuestRelatedTiles!.ForEach(z =>
                {
                    int swapCount_State = ++z.Swap.Count;
                    int swapCount_Quest = (int)(z.TileKind == quest0?.TileKind ? quest0?.SwapsAllowed.Count : quest1?.SwapsAllowed.Count)!;
                    z.IsQuestLost = swapCount_State == swapCount_Quest;
                    
                    if (z.IsQuestLost)
                    {
                        int currQuestCount = GameState.QuestCount--;
                        int countFromWhenIsLose = (1 * GameState.QuestCount / 3);
                        
                        GameState.IsGameOver = currQuestCount == countFromWhenIsLose;
                        
                        if (GameState.IsGameOver)
                            return /*Call "GameOver-callback"*/;
                        
                        Debug.WriteLine($"You lost this Quest for tiletype: {z.TileKind.ToStringFast().ToUpper()} " +
                                        $"and you have only {countFromWhenIsLose} entire Quest left to win!");
                    }
                    else
                    {
                        Debug.WriteLine($"You have still: {swapCount_Quest - swapCount_State} swaps left of tiletype:" +
                                        $"  {z.TileKind.ToStringFast().ToUpper()} ! use them wisely!");
                    }
                });
            }
        }
    }
}

//RECEIVER 
public class MatchHandler : QuestHandler
{
    public static readonly MatchHandler Instance = GetInstance<MatchHandler>();

    public event Action OnDeleteMatch;
    
    private MatchHandler() : base(typeof(MatchHandler))
    {
        SwapHandler.Instance.OnMatchFound += CompareQuest2State;
    }

    protected override void CompareQuest2State()
    {
        //IF the incoming match was a "Miss-match (a match not allowed to do because its not in the Quest written!)", then
        //we do +1 the "State.MissMatch.Count", ELSE we do +1 the "State.Match.Count"
        var allStates = GameState.StatePerQuest!;
        var StatesFromQuestRelatedTiles = GameState.StatesFromQuestRelatedTiles!;
        var matchData = GameState.CurrData!;
        var currMatch = matchData.Matches;
        var kindWhoTriggeredMatch = currMatch!.Body!.TileKind;
        var kindWhichWasIgnoredByMatch = matchData.IgnoredByMatch;
        var stateOfMatch = StatesFromQuestRelatedTiles.FirstOrNone(x => x.TileKind == kindWhoTriggeredMatch);
        var questOfMatch = GameState.Quests!;
        
        //example: RED swapsWith BLUE; RED is in Quest, Blue not;
        //BLUE got a Match, RED not;
        if (stateOfMatch.IsUndefined)
        {
            //a match with the requested "TileKind" was not found, a "wrong-match", so it was a "miss-match"!
            var stateOfMatchIgnoredKind = allStates.First(x => x.TileKind == kindWhichWasIgnoredByMatch);
            var properQuest = questOfMatch.First(x => x.TileKind == kindWhichWasIgnoredByMatch);
            int missMatchCount = stateOfMatchIgnoredKind.MissMatch.Count++;
            int diffCount = properQuest.SuccessfulMatches.Count- missMatchCount;
            currMatch.Clear();
            stateOfMatchIgnoredKind.IsQuestLost = diffCount > 0;

            if (stateOfMatchIgnoredKind.IsQuestLost)
                Debug.WriteLine($"<This is a WRONG match and you gotta be careful bro because" +
                                $" you only have {diffCount} chances left or else this Quest is lost!");
            else
                Debug.WriteLine($"YOU LOST THIS QUEST OF TYPE: {stateOfMatchIgnoredKind.IsQuestLost}");
        }
        else
        {
            //a match with the requested "TileKind" was found and hence it was a successful Quest-Bound match!
            int successCount = ++stateOfMatch.Value.SuccessfulMatch.Count;
            var properQuest = questOfMatch.First(x => x.TileKind == kindWhoTriggeredMatch);
            int diffCount = properQuest.SuccessfulMatches.Count- successCount;
            
            if (diffCount > 0)
            {
                Debug.WriteLine($"Okay, good catch, you got that match, you have only to do now {diffCount} more of kind: {kindWhoTriggeredMatch}");
            }
            else
            {
                Debug.WriteLine("YUHU, you got the Quest without falling into traps!"); ;
            }
            
            OnDeleteMatch();
        }
    }
}