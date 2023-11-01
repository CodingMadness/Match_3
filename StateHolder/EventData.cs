using DotNext;

namespace Match_3.StateHolder;

public record SubEventData(int Count, float Elapsed)
{
    public int Count { get; set; } = Count;
    public float Elapsed { get; set; } = Elapsed;
}

public readonly record struct Quest(
    TileColor TileColor,
    GameTime Timer,
    SubEventData Match,
    SubEventData Swap,
    SubEventData Replacement,
    SubEventData MissMatch)
{
    public static readonly Quest Empty = default!;
    public const string MatchCountName    = nameof(State.Match) + "." + nameof(State.Match.Count);
    public const string MatchIntervalName = nameof(State.Match) + "." + nameof(State.Match.Elapsed);
    public const string SwapCountName     = nameof(State.Swap) + "." + nameof(State.Swap.Count);
    public const string ReplacementCountName = nameof(State.Replacement) + "." + nameof(State.Replacement.Count);
    public const string MissMatchName     = nameof(State.MissMatch) + "." + nameof(State.MissMatch.Count);
    
    public int GetValueByMemberName(ReadOnlySpan<char> name)
    {
        //FOR NOW we compare the contents but only until I have written a replace method to work merely on 
        // Spans and does not create a new copy!!!
        if (name.BitwiseEquals(MatchCountName))
            return Match.Count;
        else if (name.BitwiseEquals(MatchIntervalName))
            return (int)Match.Elapsed;
        else if (name.BitwiseEquals(SwapCountName))
            return Swap.Count;
        else if (name.BitwiseEquals(ReplacementCountName))
            return Replacement.Count;
        else if (name.BitwiseEquals(MissMatchName))
            return MissMatch.Count;

        throw new ArgumentException("this code should not be reached at all!");
    }
}

public record State(
    TileColor TileColor,
    bool IsQuestLost,
    TimeSpan Elapsed,
    SubEventData Match,
    SubEventData Swap,
    SubEventData Replacement,
    SubEventData MissMatch)
{
    public bool IsQuestLost { get; set; } = IsQuestLost;
}