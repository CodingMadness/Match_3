using DotNext;

namespace Match_3.StateHolder;

public readonly record struct Quest(
    TileColor TileColor,
    GameTime Timer,
    SubQuest? Match,
    SubQuest? Swap,
    SubQuest? Replacement,
    SubQuest? MissMatch)
{
    public static readonly Quest Empty = default;
    
    public const string MatchCountName = nameof(Quest.Match) + "." + nameof(Quest.Match.Value.Count);
    public const string MatchIntervalName = nameof(Quest.Match) + "." + nameof(Quest.Match.Value.Interval);
    public const string SwapCountName = nameof(Quest.Swap) + "." + nameof(Quest.Swap.Value.Count);
    public const string ReplacementCountName = nameof(Quest.Replacement) + "." + nameof(Quest.Replacement.Value.Count);
    public const string MissMatchName = nameof(Quest.MissMatch) + "." + nameof(Quest.MissMatch.Value.Count);

    public int GetValueByMemberName(ReadOnlySpan<char> name)
    {
        //FOR NOW we compare the contents but only until I have written a replace method to work merely on 
        // Spans and does not create a new copy!!!
        if (name.BitwiseEquals(MatchCountName))
            return Match!.Value.Count;
        else if (name.BitwiseEquals(MatchIntervalName))
            return (int)Match!.Value.Interval;
        else if (name.BitwiseEquals(SwapCountName))
            return Swap!.Value.Count;
        else if (name.BitwiseEquals(ReplacementCountName))
            return Replacement!.Value.Count;
        else if (name.BitwiseEquals(MissMatchName))
            return MissMatch!.Value.Count;

        throw new ArgumentException("this code should not be reached at all!");
    }
}