using DotNext;

namespace Match_3.DataObjects;

public record SubEventData(int Count, float Elapsed)
{
    public int Count { get; set; } = Count;
    public float Elapsed { get; set; } = Elapsed;
}

public readonly record struct Quest(
    TileColor TileColor,
    GameTime Timer,
    SubEventData SuccessfulMatches,
    SubEventData SwapsAllowed,
    SubEventData ReplacementsAllowed,
    SubEventData NumberOfWrongMatchesAllowed)
{
    public static readonly Quest Empty       = default!;
    public const string MatchCountName       = nameof(State.SuccessfulMatch) + "." + nameof(State.SuccessfulMatch.Count);
    public const string MatchIntervalName    = nameof(State.SuccessfulMatch) + "." + nameof(State.SuccessfulMatch.Elapsed);
    public const string SwapCountName        = nameof(State.WrongSwaps) + "." + nameof(State.WrongSwaps.Count);
    public const string ReplacementCountName = nameof(State.Replacement) + "." + nameof(State.Replacement.Count);
    public const string WrongMatchName       = nameof(State.WrongMatch) + "." + nameof(State.WrongMatch.Count);
    public const string TileColorName        = nameof(Quest.TileColor) + "\0\0\0";  //HARDCODED Value, DO NOT CHANGE!

    public int GetValueByMemberName(ReadOnlySpan<char> name)
    {
        if (name.BitwiseEquals(MatchCountName))
            return SuccessfulMatches.Count;
        if (name.BitwiseEquals(MatchIntervalName))
            return (int)SuccessfulMatches.Elapsed;
        if (name.BitwiseEquals(SwapCountName))
            return SwapsAllowed.Count;
        if (name.BitwiseEquals(ReplacementCountName))
            return ReplacementsAllowed.Count;
        if (name.BitwiseEquals(WrongMatchName))
            return NumberOfWrongMatchesAllowed.Count;

        throw new ArgumentException("this code should not be reached at all!");
    }
}

public record State(
    TileColor TileKind,
    bool IsQuestLost,
    TimeOnly Now,
    SubEventData SuccessfulMatch,
    SubEventData WrongSwaps,
    SubEventData Replacement,
    SubEventData WrongMatch)
{
    public bool IsQuestLost { get; set; } = IsQuestLost;
}