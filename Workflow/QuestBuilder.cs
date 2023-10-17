using System.Diagnostics;
using System.Text;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using DotNext;
using Match_3.Service;
using Match_3.Variables;
using Match_3.Variables.Extensions;
using NoAlloq;
using TextInfo = Match_3.Service.TextInfo;

namespace Match_3.Workflow;

public static class QuestBuilder
{
    /*
     * The Core algorithm is:
     *   - We (efficiently) replace the "TemplateQuestLog" with the quest-data
     *   - Then we store that replaced-version of the log into the "StringPool"
     *   - We return the newly logString
     *   - When we have successfully created "QuestCount" Questlogs we will
     *   - Just return from now on the respective string from the pool
     */


    static QuestBuilder()
    {
        Grid.NotifyOnGridCreationDone += DefineQuest;
    }

    private static readonly Quest Empty = default;

    private static readonly string QuestLog = $"(Black) You have to collect an amount of" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.MatchCountName} {TileColor.Transparent.ToStringFast()} Matches" +
                                              $" (Black) and u have in between those only" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.MatchIntervalName} seconds left" +
                                              $" (Black) and also just" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.SwapCountName} swaps available" +
                                              $" (Black) for each new match" +
                                              $" (Black) and furthermore, you only are allowed to replace any given tile" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.ReplacementCountName} times at max" +
                                              $" (Black) for your own help as well as there is the tolerance for" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.MissMatchName} miss matches";

    private static readonly StringBuilder LogBuilder = new(QuestLog.Length);
    public static readonly GameTime[]? QuestTimers = new GameTime[Utils.TileColorLen];
    private static readonly Quest[] QuestForAllColors = new Quest[Utils.TileColorLen];
    public static int _questCounter;
    
    public static ref readonly Quest GetQuestFrom(TileColor key)
    {
        var enumerator = GetQuests();

        foreach (ref readonly var pair in enumerator)
        {
            if (pair.TileColor == key)
                return ref pair;
        }

        return ref Empty;
    }

    public static int QuestCount { get; private set; }

    public static bool ShallRecycle => _questCounter == QuestCount;

    public static FastSpanEnumerator<Quest> GetQuests()
        => new(QuestForAllColors.AsSpan(0, QuestCount));

    private static void DefineQuest(Span<byte> maxCountPerType)
    {
        //TODO: Write an implementation in where I am able
        //TODO: to store multiple different values in one dataStructure and store also the operation
        //TODO: like-> consider BigEndian-System and this theoretical example:
        //TODO:   (bits[3] = 28)  + (bits[5] = 63) + (bits[2] = 24)  ===> 221 as an example
        // Span<byte> bytes = stackalloc byte[] {10, 15, 20};
        // BitArray bits = new BitArray(bytes.ToArray());

        void Fill(Span<TileColor> toFill)
        {
            for (int i = 0; i < Utils.TileColorLen; i++)
                toFill[i] = i.ToColor();
        }

        int GetRandomInterval()
        {
            //we do netSingle() * 10f to have a real representative value for interval, like:
            // 0.4f * 10f => 2f will be the time we have left to make a match! and so on....
            float rndValue = Utils.Randomizer.NextSingle().Trunc(1);
            rndValue = rndValue.Equals(0f, 0.0f) ? 0.25f : rndValue;
            float finalInterval = MathF.Round(rndValue * 10f);
            finalInterval = finalInterval <= 2.5f ? 2.5f : finalInterval;
            int toEven = (int)MathF.Round(finalInterval, MidpointRounding.ToEven);
            return toEven;
        }

        const int tileCount = Utils.TileColorLen;
        // const int questLogParts = 4;
        scoped Span<TileColor> subset = stackalloc TileColor[tileCount];
        Fill(subset);
        subset.Shuffle(Utils.Randomizer);
        subset = subset.TakeRndItemsAtRndPos();
        scoped FastSpanEnumerator<TileColor> subsetEnumerator = new(subset);
        QuestCount = subset.Length;
        int trueIdx = 0;

        foreach (var type in subsetEnumerator)
        {
            int toEven = GetRandomInterval();
            SubQuest match = new(maxCountPerType[trueIdx] / Level.MaxTilesPerMatch, toEven);
            //these subQuests below are just placeholders until this class is done then I change them to
            //smth meaningful
            SubQuest swap = new(4, -1f);
            SubQuest replacement = new(5, -1f);
            SubQuest tolerance = new(6, -1f);
            QuestForAllColors[trueIdx] = new Quest(type, match, swap, replacement, tolerance);
            QuestTimers![trueIdx] = GameTime.GetTimer(toEven);
            trueIdx++;
        }

        //sort and filter the null's out
        QuestForAllColors.AsSpan().Where(x => x.Match.HasValue).Select(x => x).TakeInto(QuestForAllColors);
        QuestTimers.AsSpan().Where(x => x.IsInitialized).Select(x => x).TakeInto(QuestTimers);
    }

    public static ReadOnlySpan<char> BuildQuestLoggerFrom(Quest quest)
    {
        var questIterator = new PhraseEnumerator(LogBuilder.Clear().Append(QuestLog), true);
        
        foreach (TextInfo questPiece in questIterator)
        {
            var placeHolderColor = questPiece.ColorAsText.AsWriteable();
            var newColor = quest.TileColor.ToStringFast().AsSpan().AsWriteable();
            newColor.CopyTo(placeHolderColor);
            placeHolderColor[newColor.Length..].Clear();
            var memberName = questPiece.Variable2Replace.AsWriteable();
            var value = quest.GetValueByMemberName(memberName).ToString();
            value.CopyTo(memberName);
            memberName[value.Length..].Clear();
        }

        _questCounter++;
        return StringPool.Shared.GetOrAdd(LogBuilder.AsSpan());
    }
}