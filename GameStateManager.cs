using Match_3.GameTypes;

namespace Match_3;

public static class GameStateManager
{
    private static int MaxCapacity => (int)Balls.Length * 5;
    public const int Max3PerKind = 3;
    private static readonly Dictionary<Balls, int> ToCollect = new(MaxCapacity);
    private static readonly Random rnd = new(DateTime.UtcNow.Millisecond);

    public static GameState State { get; private set; }

    static GameStateManager()
    {
        State = new(30, 3, 15, 10, ToCollect, 64, 3);
    }

    private static void LogQuest()
    {
        foreach (var pair in ToCollect)
        {
            Console.WriteLine($"You have to collect {pair.Value} {pair.Key}-tiles!");
            Console.WriteLine();
        }
    }

    public static void SetCollectQuest()
    {
        for (int i = 0; i < (int)Balls.Length-1; i++)
        {
            int count = rnd.Next(Max3PerKind - 1, Max3PerKind + 2);
            State!.ToCollect.TryAdd((Balls)i, (count));
        }
    }

    public static bool TryGetSubQuest(CandyShape shape, out int number)
    {
        return ToCollect.TryGetValue(shape.Ball, out number);
    }

    public static void RemoveSubQuest(CandyShape shape) => ToCollect.Remove(shape.Ball);

    public static bool IsSubQuestDone(CandyShape shape, int alreadyMatched) =>
        TryGetSubQuest(shape, out int result) && alreadyMatched >= result;

    public static void ChangeSubQuest(CandyShape shape, int toChangeWith)
        => State.ToCollect[shape.Ball] = toChangeWith;
       
    public static bool IsQuestDone() => ToCollect.Count == 0;

    public static void LoadLevel() 
    {
        State?.ToCollect.Clear();
        SetCollectQuest();
        LogQuest();

        int startUpTime = Utils.Round(rnd, 40..60, 3);
        int tileWidth = Utils.Round(rnd, 18..7, 3);
        int tileHeight = Utils.Round(rnd, 10..10, 3);
        int gameOverTime = 5;

        State = new(startUpTime, gameOverTime, tileWidth, tileHeight, ToCollect, ITile.Size, 4);
    }
}