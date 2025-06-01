using DotNext;
using Match_3.Service;

namespace Match_3.DataObjects;

public readonly record struct Quest(
    FadeableColor Colour,
    GameTime Timer,
    (int Count, float CountDown) Matches2Have,
    (int Count, float CountDown) SwapsAllowed,
    (int Count, float CountDown) ReplacementsAllowed,
    (int Count, float CountDown) NumberOfWrongMatchesAllowed)
{
    public const string MatchCountName       = nameof(QuestState.FoundMatch) + "." + nameof(QuestState.FoundMatch.Count);
    public const string MatchIntervalName    = nameof(QuestState.FoundMatch) + "." + nameof(QuestState.FoundMatch.Elapsed);
    public const string SwapCountName        = nameof(QuestState.WrongSwaps) + "." + nameof(QuestState.WrongSwaps.Count);
    public const string ReplacementCountName = nameof(QuestState.ReplacementsUsed) + "." + nameof(QuestState.ReplacementsUsed.Count);
    public const string WrongMatchName       = nameof(QuestState.WrongMatch) + "." + nameof(QuestState.WrongMatch.Count);
    public const string TileColorName        = nameof(Quest.Colour) + "\0\0\0";  //HARDCODED Value, DO NOT CHANGE!

    public int GetValueByMemberName(ReadOnlySpan<char> name)
    {
        if (name.BitwiseEquals(MatchCountName))
            return Matches2Have.Count;
        if (name.BitwiseEquals(MatchIntervalName))
            return (int)Matches2Have.CountDown;
        if (name.BitwiseEquals(SwapCountName))
            return SwapsAllowed.Count;
        if (name.BitwiseEquals(ReplacementCountName))
            return ReplacementsAllowed.Count;
        if (name.BitwiseEquals(WrongMatchName))
            return NumberOfWrongMatchesAllowed.Count;

        throw new ArgumentException("this code should not be reached at all!");
    }
}

public class QuestLogger(int QuestCount)
{
    // --- Lazy-Loaded Resources ---
    private SpanPool<char> _pool = new(Config.QuestLog.Length * QuestCount, Config.SegmentsOfQuestLog);
    public int _next;

    public ReadOnlySpan<char> CurrentLog => _pool.Peek(_next++);

    public int QuestIndex => _pool.PushCount;

    public void BeginFromStart() => _next = 0;

    public void UpdateNextQuestLog(Quest quest)
    {
        var copyLog = _pool.Push(Config.QuestLog);
        copyLog.Replace(Quest.TileColorName, quest.Colour.Name);
    }
}