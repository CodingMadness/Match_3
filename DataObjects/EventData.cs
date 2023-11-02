using DotNext;

namespace Match_3.DataObjects;

public record SubEventData(int Count, float Elapsed)
{
    public int Count { get; set; } = Count;
    public float Elapsed { get; set; } = Elapsed;
}

public readonly record struct Quest(
    TileColor TileKind,
    GameTime Timer,
    SubEventData SuccessfulMatches,
    SubEventData SwapsAllowed,
    SubEventData ReplacementsAllowed,
    SubEventData MissMatchesAllowed)
{
    public static readonly Quest Empty       = default!;
    public const string MatchCountName       = nameof(State.SuccessfulMatch) + "." + nameof(State.SuccessfulMatch.Count);
    public const string MatchIntervalName    = nameof(State.SuccessfulMatch) + "." + nameof(State.SuccessfulMatch.Elapsed);
    public const string SwapCountName        = nameof(State.Swap) + "." + nameof(State.Swap.Count);
    public const string ReplacementCountName = nameof(State.Replacement) + "." + nameof(State.Replacement.Count);
    public const string MissMatchName        = nameof(State.MissMatch) + "." + nameof(State.MissMatch.Count);
    
    public int GetValueByMemberName(ReadOnlySpan<char> name)
    {
        //FOR NOW we compare the contents but only until I have written a replace method to work merely on 
        // Spans and does not create a new copy!!!
        if (name.BitwiseEquals(MatchCountName))
            return SuccessfulMatches.Count;
        else if (name.BitwiseEquals(MatchIntervalName))
            return (int)SuccessfulMatches.Elapsed;
        else if (name.BitwiseEquals(SwapCountName))
            return SwapsAllowed.Count;
        else if (name.BitwiseEquals(ReplacementCountName))
            return ReplacementsAllowed.Count;
        else if (name.BitwiseEquals(MissMatchName))
            return MissMatchesAllowed.Count;

        throw new ArgumentException("this code should not be reached at all!");
    }
}

public record State(
    TileColor TileKind,
    bool IsQuestLost,
    TimeOnly Now,
    SubEventData SuccessfulMatch,
    SubEventData Swap,
    SubEventData Replacement,
    SubEventData MissMatch)
{
    public bool IsQuestLost { get; set; } = IsQuestLost;
}