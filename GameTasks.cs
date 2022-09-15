using Raylib_cs;

namespace Match_3;
public static class GameTasks
{
    private static int MaxCapacity => (int)ShapeKind.Length * 5;
    public const int Max3PerKind = 3;
    public static readonly Dictionary<ShapeKind, int> ToCollect = new(MaxCapacity);
    
    public static void SetQuest()
    {
        for (int i = 0; i < (int)ShapeKind.Length; i++)
        {
            int count = Random.Shared.Next(Max3PerKind-1, Max3PerKind + 3);
            ToCollect.TryAdd((ShapeKind)i, (count ));
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

    public static void LogQuest()
    {
        foreach (var pair in ToCollect)
        {
            Console.WriteLine($"You have to collect {pair.Value} {pair.Key}-tiles!");
            Console.WriteLine();
        }        
    }
}