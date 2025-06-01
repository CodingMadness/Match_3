using System.Diagnostics;
using System.Reflection;
using Match_3.DataObjects;
using Match_3.Service;
using NoAlloq;

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
            }

            // Ensure the type has a parameterless constructor (public or non-public)
            var constructor = typeof(T).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null
            );

            if (constructor == null)
            {
                throw new InvalidOperationException($"Type {typeof(T).Name} must have a parameterless constructor (private or protected).");
            }

            // Now create the instance (suppress warning since we've validated at runtime)
            toReturn = (T)Activator.CreateInstance(typeof(T), true)!;
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

    protected abstract void CompareQuestWithState();

    public static void ActivateQuestHandlers()
    {
        ClickHandler.Instance.Subscribe();
        SwapHandler.Instance.Subscribe();
        MatchHandler.Instance.Subscribe();
    }

    //it's private for now until I will need my other Handlers for Clicks and Destruction events, which has to be 
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
    public event Action? OnSwapTiles;
    public event Action? OnTilesAlreadySwapped;

    private ClickHandler() : base(typeof(ClickHandler)) //--> NOTIFIER!
    {
        //Event-System rule of thumb:
        // ----> The "Notifier" has to declare the event-type!  AND has to invoke() it in his own code-base somewhere!
        // ----> The "receiver" has to subscribe the event AND handle with appropriate code logic the specific event-case!

        Game.OnTileClicked += CompareQuestWithState; //--> registration!
    }

    protected override void CompareQuestWithState()
    {
        var _gameState = GameState.Instance;
        var firstClicked = _gameState.TileX;
        ref var secondClicked = ref _gameState.TileY;

        if (firstClicked!.IsDeleted) 
            return;
        
        firstClicked.State |= TileState.Selected;

        /*No tile selected yet*/
        if (secondClicked is null)
        {
            //prepare for next round, so we store first in second!
            secondClicked = firstClicked;
            return;
        }

        /*Same tile selected => deselect*/
        if (Comparer.BodyComparer.Singleton.Equals(firstClicked, secondClicked))
        {
            Console.Clear();
            secondClicked.State &= ~TileState.Selected;
            secondClicked = null;
        }
        /*Different tile selected ==> swap*/
        else
        {
            firstClicked.State &= ~TileState.Selected;
            _gameState.TileY = secondClicked;
            _gameState.TileX = firstClicked;
            OnSwapTiles?.Invoke();

            if (_gameState.WasSwapped)
            {
                Console.WriteLine("SWAPPED 2 TILES!");
                //the moment we have the 1. swap, we notify the SwapHandler for this
                //and he begins to keep track of (HOW LONG did the swap took) and
                //(HOW MANY MISS-SWAPS HAPPENED!)
                firstClicked.State &= ~TileState.Selected;

                //find all states whose "TileColor" were part of the swap!
                //it can be ONLY 1 OR 2 but NEVER 0!
                var second = secondClicked;
                _gameState.StatesFromQuestRelatedTiles
                    = _gameState.QuestStates.AsEnumerable()
                                       .Where(x => x.ColourType == firstClicked.Body.Colour.Type || x.ColourType == second.Body.Colour.Type);

                OnTilesAlreadySwapped?.Invoke();
                secondClicked = null; //he is the first now
            }
            else
            {
                firstClicked.State &= ~TileState.Selected;
            }
        }
    }
}

public class SwapHandler : QuestHandler
{
    private SwapHandler() : base(typeof(SwapHandler))
    {
        ClickHandler.Instance.OnTilesAlreadySwapped += CompareQuestWithState;
    }

    public static readonly SwapHandler Instance = GetInstance<SwapHandler>();

    public event Action? OnCheckForMatch;
    public event Action? OnMatchFound;

    private static (TileColorTypes x, TileColorTypes y) GetTileColorAndQuestData(out Quest? eventDataOfX, out Quest? eventDataOfY)
    {
        var gameState = GameState.Instance;
        var questHolder = gameState.ToAccomplish;

        var colorX = gameState.TileX!.Body.Colour.Type;
        var colorY = gameState.TileY!.Body.Colour.Type;

        eventDataOfX = questHolder.AsSpan().SingleOrDefault(x => x.Colour.Type == colorX);
        eventDataOfY = questHolder.AsSpan().SingleOrDefault(x => x.Colour.Type == colorY);

        return (colorX, colorY);
    }
    
    protected override void CompareQuestWithState()
    {
        //Check GameState with Quest and see if he has to be punished!
        var gameState = GameState.Instance;
        var (x, y) = GetTileColorAndQuestData(out var quest0, out var quest1);
        var swapState = gameState;
        //1. check if the tiles which were swapped are even needed for the Quest!
        //define some condition by which we can assert that all possible combinations are being gathered!
        if ((x != quest0?.Colour.Type && x != quest0?.Colour.Type) &&
            (y != quest1?.Colour.Type && y != quest1?.Colour.Type))
        {
            Debug.WriteLine($"Neither { nameof(x) } nor { nameof(y) } have to do anything with the Quest!?" +
                            $"so you have to be (in some way or another) to be punished!");
            
            // PunishPlayer();
        }
        else
        {
            //var debugLog = QuestBuilder.GetPooledQuestLog();
            
            if (swapState.StatesFromQuestRelatedTiles!.Any(z => (z.ColourType == x || z.ColourType == y) && z.IsQuestLost))
            {
                var debugValue = swapState.StatesFromQuestRelatedTiles!.First(z => (z.ColourType == x || z.ColourType == y) && z.IsQuestLost);

                Debug.WriteLine($"You lost Quest of type: {debugValue.ColourType}");// already, now feel the punishment and continue with the other Quests, your time is running short!");
                return;
            }
                    
            //2. Check if we didnt exceed the allowed Swap-Count for all the different quests
            OnCheckForMatch?.Invoke();

            //since we have a 3x match the Body cannot be null so we use '.Body!' expression
            if (swapState.HaveAMatch)
            {
                swapState.IgnoredByMatch = x == swapState.Matches.Body!.Colour.Type ? y : x;
                OnMatchFound?.Invoke();
            }
            else
            {
                void HandleSwaps(in QuestState z)
                {
                    z.WrongSwaps.IncCount(1);
                    int missSwapState = z.WrongSwaps.Count;
                    int maxAllowedSwapsQuest = (int)(z.ColourType == quest0?.Colour.Type ? quest0?.SwapsAllowed.Count : quest1?.SwapsAllowed.Count)!;
                    
                    z.IsQuestLost = missSwapState == maxAllowedSwapsQuest;

                    if (z.IsQuestLost)
                    {
                        int currQuestCount = swapState.CurrentQuestCount--;
                        int countFromWhenIsLose = 1 * swapState.CurrentQuestCount / 3;

                        swapState.WasGameLost = currQuestCount == countFromWhenIsLose;

                        if (swapState.WasGameLost)
                        {
                            Debug.WriteLine("GAME IS OVER NOW BECAUSE WE LOST ATLEAST 2/3 OF QUESTS! HOW BAD ARE WE ?!");
                            return;
                        }

                        Debug.WriteLine($"You lost this Quest for tiletype: {z.ColourType.ToString().ToUpper()} " +
                                        $"and you are only {countFromWhenIsLose} entire Quest away to lose!");
                    }
                    else
                    {
                        Debug.WriteLine($"You have still: {maxAllowedSwapsQuest - missSwapState} swaps left of tiletype:" +
                                        $"  {z.ColourType.ToString().ToUpper()} ! use them wisely!");
                    }
                }
                //Now we increase the swapCount for the participating-TileColor's only if there was not a match
                //because then we need to +1 up and see if the player reached the limits
                var states = swapState.StatesFromQuestRelatedTiles!;
                
                foreach (var state in states)
                {
                    HandleSwaps(state);
                }
            }
        }
    }
}

public class MatchHandler : QuestHandler
{
    public static readonly MatchHandler Instance = GetInstance<MatchHandler>();

    // public event Action? OnDeleteMatch;
    
    private MatchHandler() : base(typeof(MatchHandler))
    {
        SwapHandler.Instance.OnMatchFound += CompareQuestWithState;
    }

    protected override void CompareQuestWithState()
    {
        //IF the incoming match was a "Miss-match (a match not allowed to do because its not in the Quest written!)", then
        //we do +1 the "QuestState.MissMatch.Count", ELSE we do +1 the "QuestState.Match.Count"
        var currMatchState = GameState.Instance;
        var allStates = currMatchState.QuestStates;
        var statesFromQuestRelatedTiles = currMatchState.StatesFromQuestRelatedTiles!;
        var currMatch = currMatchState.Matches;
        //since we seemingly triggered a 3x-match the Body cannot (should not?) be null so we use '.Body!' expression
        var kindWhoTriggeredMatch = currMatch.Body!.Colour.Type;
        var kindWhichWasIgnoredByMatch = currMatchState.IgnoredByMatch;
        var stateOfMatch = statesFromQuestRelatedTiles.SingleOrDefault(x => x.ColourType == kindWhoTriggeredMatch);
        var questOfMatch = currMatchState.ToAccomplish;
        
        //example: RED swapsWith BLUE; RED is in Quest, Blue not;
        //BLUE got a Match, RED not;
        if (stateOfMatch is null)
        {
            //a match with the requested "ColourType" was not found, a "wrong-match", so it was a "miss-match"!
            var stateOfMatchIgnoredKind = allStates.First(x => x.ColourType == kindWhichWasIgnoredByMatch);
            var properQuest = questOfMatch.First(x => x.Colour.Type == kindWhichWasIgnoredByMatch);
            (int Count, float CountDown) match = stateOfMatchIgnoredKind.WrongMatch;
            match.Count++;
            stateOfMatchIgnoredKind.WrongMatch = match;
            int missMatchCount = stateOfMatchIgnoredKind.WrongMatch.Count;
            stateOfMatchIgnoredKind.WrongMatch.IncCount(1);
            int diffCount = properQuest.Matches2Have.Count - missMatchCount;
            stateOfMatchIgnoredKind.IsQuestLost = diffCount > 0;
            currMatch.Clear(currMatchState.LookUpUsedInMatchFinder);

            if (stateOfMatchIgnoredKind.IsQuestLost)
                Debug.WriteLine($"<This is a WRONG match and you gotta be careful bro because" +
                                $" you only have {diffCount} chances left or else this Quest, ({kindWhichWasIgnoredByMatch}) is lost!");
            else
                Debug.WriteLine($"YOU LOST THIS QUEST OF TYPE: {stateOfMatchIgnoredKind.IsQuestLost}");
        }
        else
        {
            currMatchState.Matches.BuildMatchBox(currMatchState.LookUpUsedInMatchFinder);
            //a match with the requested "ColourType" was found and hence it was a successful Quest-Bound match!
            stateOfMatch.FoundMatch.IncCount(1);
            int successCount = stateOfMatch.FoundMatch.Count;
            var properQuest = questOfMatch.First(x => x.Colour.Type == kindWhoTriggeredMatch);
            int diffCount = properQuest.Matches2Have.Count- successCount;
            
            if (diffCount > 0)
            {
                Debug.WriteLine($"Okay, good catch, you got that match, you have only to do now {diffCount} more of kind: {kindWhoTriggeredMatch}");
            }
            else
            {
                Debug.WriteLine("YUHU, you got the Quest without falling into traps!"); 
            }
            //BeginFromStart the missSwaps back to 0 because we won that particular quest
            
            stateOfMatch.WrongSwaps.SetCount(0);
            //OnDeleteMatch();
        }
    }
}