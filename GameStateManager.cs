using Match_3.GameTypes;

namespace Match_3;

public static class GameStateManager
{
    private static int MaxCapacity => (int)Sweets.Length * 5;
    public const int Max3PerKind = 3;
    private static readonly Dictionary<Sweets, int> ToCollect = new(MaxCapacity);
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
        for (int i = 0; i < (int)Sweets.Length; i++)
        {
            int count = rnd.Next(Max3PerKind - 1, Max3PerKind + 2);
            State!.ToCollect.TryAdd((Sweets)i, (count));
        }
    }

    public static bool TryGetSubQuest(Candy shape, out int number)
    {
        return ToCollect.TryGetValue(shape.Sweet, out number);
    }

    public static void RemoveSubQuest(Candy shape) => ToCollect.Remove(shape.Sweet);

    public static bool IsSubQuestDone(Candy shape, int alreadyMatched) =>
        TryGetSubQuest(shape, out int result) && alreadyMatched >= result;

    public static void ChangeSubQuest(Candy shape, int toChangeWith)
        => State.ToCollect[shape.Sweet] = toChangeWith;
       
    public static bool IsQuestDone() => ToCollect.Count == 0;

    public static void SetNewLevl() 
    {
        State?.ToCollect.Clear();
        SetCollectQuest();
        LogQuest();

        int startUpTime = Utils.Round(rnd, 15..60, 3);
        int tileWidth = Utils.Round(rnd, 5..15, 3);
        int tileHeight = Utils.Round(rnd, 5..15, 3);
        int gameOverTime = 5;

        State = new(startUpTime, gameOverTime, tileWidth, tileHeight, ToCollect, Grid.TileSize, 4);
    }
}