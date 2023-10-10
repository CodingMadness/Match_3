using System.Diagnostics;
using System.Drawing;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using Match_3.Service;
using Match_3.Variables;

namespace Match_3.Workflow;

public static class QuestBuilder
{
    private const string TemplateQuestLog = $"(Black) You have to collect an amount of" +
                                            $" (EMPTY) <A> EMPTY Matches" +
                                            $" (Black) and u have in between those," +
                                            $" (EMPTY) only <B> seconds left" +
                                            $" (Black) and also just" +
                                            $" (EMPTY) <C> swaps available" +
                                            $" (Black) for each new match" +
                                            $" (Black) and furthermore, you only are allowed to replace any given tile" +
                                            $" (EMPTY) <D> times at max" +
                                            $" (Black) for your own help ";

    // //the length of any string which is inside the GameState class

    private static readonly int ExtraSpace = TemplateQuestLog.Length + TemplateQuestLog.Length / 5;
    private static readonly StringBuilder LogBuilder = new(ExtraSpace);
    
    private static int _questCounter, _repeater;
    private static readonly string[] QuestLogPool = new string[MatchQuestHandler.Instance.QuestCount];
    private static bool _shallRecycleLogs;

    static QuestBuilder()
    {
    }

    private static string BuildQuestMsgFrom(in Quest quest)
    {
        double GetNextValue(in Quest data, int offset)
        {
            double numericValue = 0d;

            switch (offset)
            {
                case 0:
                {
                    numericValue = data.Match!.Value.Count;
                    break;
                }
                case 1:
                {
                    numericValue = data.Match!.Value.Interval;
                    break;
                }
                case 2:
                {
                    numericValue = data.Swap!.Value.Count;
                    break;
                }
                case 3:
                {
                    numericValue = data.Replacement!.Value.Count;
                    break;
                }
            }

            return numericValue;
        }
         
        
        string colorAsTxt = $"{quest.ItemType}";
        LogBuilder.Clear().Append(TemplateQuestLog).Replace("EMPTY", colorAsTxt);

        var chunkIterator = new TextStyleEnumerator(LogBuilder);
        int counter = 0;
        char begin = 'A';

        foreach (ref readonly var phrase in chunkIterator)
        {
            if (phrase.SystemColor.ToKnownColor() is KnownColor.Black)
            {
                continue;
            }

            var value = GetNextValue(quest, counter++);

            LogBuilder.Replace($"<{begin++}>", $"<{value}>");
        }

        QuestLogPool[_questCounter++] = LogBuilder.ToString();
        _repeater = _questCounter - 1;
        _shallRecycleLogs = _questCounter == QuestLogPool.Length;
        _repeater = _shallRecycleLogs ? 0 : _repeater;

        return QuestLogPool[_repeater];
    }

    public static string GetQuestLogFromPool(in Quest quest)
    {
        string result;

        if (_shallRecycleLogs && _repeater < QuestLogPool.Length)
            result = QuestLogPool[_repeater++];
        else if (_repeater == QuestLogPool.Length)
        {
            _repeater = 0;
            result = QuestLogPool[_repeater++];
        }
        else
            result = BuildQuestMsgFrom(quest);

        return result;
    }

    public static void Reset() => LogBuilder.Clear();
}