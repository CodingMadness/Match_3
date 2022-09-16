namespace Match_3;

public static class GameStateManager
{
    private static int MaxCapacity => (int)ShapeKind.Length * 5;
    public const int Max3PerKind = 3;
    private static readonly Dictionary<ShapeKind, int> ToCollect = new(MaxCapacity);
    private static readonly Random rnd = new (DateTime.UtcNow.Millisecond);
   
    public static GameState State { get; private set; }

    static GameStateManager()
    {
        State = new(30, 3, 15, 10, ToCollect, 64);  
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
        for (int i = 0; i < (int)ShapeKind.Length; i++)
        {
            int count = rnd.Next(Max3PerKind-1, Max3PerKind + 2);
            State!.ToCollect.TryAdd((ShapeKind)i, (count ));
        }
    }

    public static bool TryGetSubQuest(in Shape shape, out int number)
    {
        return  ToCollect.TryGetValue(shape.Kind, out number);
    }
     
    public static void RemoveSubQuest(in Shape shape) => ToCollect.Remove(shape.Kind);

    public static bool IsSubQuestDone(in Shape shape, int alreadyMatched) => 
        TryGetSubQuest(shape, out int result) && alreadyMatched >= result;

    public static bool IsQuestDone() => ToCollect.Count == 0;

    public static void SetNewLevl(int? startTime) 
    {
        State?.ToCollect.Clear();
        SetCollectQuest();
        LogQuest();

        int startUpTime = startTime is null ? Utils.RoundValueToNearestOf3(rnd, 15..60) : startTime.Value;
        int gameOverTime = 6;

        int tileWidth = Utils.RoundValueToNearestOf3(rnd, 6..15);
        int tileHeight = Utils.RoundValueToNearestOf3(rnd, 6..15);
        State = new(startUpTime, gameOverTime, tileWidth, tileHeight, ToCollect, Grid<Tile>.TileSize);
    }
}