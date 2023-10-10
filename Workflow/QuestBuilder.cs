using System.Drawing;
using System.Text;
using Match_3.Service;
using Match_3.Variables;

namespace Match_3.Workflow;

public static class QuestBuilder
{
    private const string QuestLog = $"(Black) You have to collect an amount of" +
                                    $" (EMPTY) <A> EMPTY Matches" +
                                    $" (Black) and u have in between those," +
                                    $" (EMPTY) only <B> seconds left" +
                                    $" (Black) and also just" +
                                    $" (EMPTY) <C> swaps available" +
                                    $" (Black) for each new match" +
                                    $" (Black) and furthermore, you only are allowed to replace any given tile" +
                                    $" (EMPTY) <D> times at max" +
                                    $" (Black) for your own help ";

    //the length of any string which is inside the GameState class
    private static readonly StringBuilder Logger = new(QuestLog.Length);

    static QuestBuilder()
    {
        GameState.Logger = Logger;
    }
    
    public static void BuildQuestMsgFrom(in Quest quest)
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
        Logger.Clear().Append(QuestLog).Replace("EMPTY", colorAsTxt);

        var chunkIterator = new TextStyleEnumerator(Logger);
        int counter = 0;
        char begin = 'A';

        foreach (ref readonly var phrase in chunkIterator)
        {
            if (phrase.SystemColor.ToKnownColor() is KnownColor.Black)
            {
                continue;
            }

            var value = GetNextValue(quest, counter++);

            Logger.Replace($"<{begin++}>", $"<{value}>");
        }
    }

    public static void Reset() => Logger.Clear();

}