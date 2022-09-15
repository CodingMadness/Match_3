using Raylib_cs;

namespace Match_3;
public static class GameStateManager
{
    private static int MaxCapacity => (int)ShapeKind.Length * 5;
    public const int Max3PerKind = 3;
    private static readonly Dictionary<ShapeKind, int> ToCollect = new(MaxCapacity);
    private static Random rnd = new Random(DateTime.UtcNow.Millisecond);
   
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
            int count = rnd.Next(Max3PerKind-1, Max3PerKind + 3);
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

    public static void SetNewLevl() 
    {
        static int roundRndValueToNearestOf3(Range r)
        {
            const float toRoundTo = 3.0f;
            int value = rnd.Next(r.Start.Value, r.End.Value);
            value = (int)(value % toRoundTo == 0 ? value : ((int)MathF.Round(value / toRoundTo)) * toRoundTo);
            return value;
        }

        State?.ToCollect.Clear();
        SetCollectQuest();
        LogQuest();

        int startUpTime = roundRndValueToNearestOf3(15..60);
        int gameOverTime = 3;

        int tileWidth = roundRndValueToNearestOf3(6..15);
        int tileHeight = roundRndValueToNearestOf3(6..15);
        State = new(startUpTime, gameOverTime, tileWidth, tileHeight, ToCollect, Grid<Tile>.TileSize);
    }
}