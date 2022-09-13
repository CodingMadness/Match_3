namespace Match_3;
public static class GameTasks
{
    private static int MaxCapacity => (int)ShapeKind.Length * 5;
    public const int Max3PerKind = 3;
    public static readonly Dictionary<ShapeKind, int> ToCollect = new(MaxCapacity);
    
    public static void SetQuest()
    {
        for (int i = 0; i < MaxCapacity; i++)
        {
            ToCollect.TryAdd((ShapeKind)i, Random.Shared.Next(Max3PerKind, Max3PerKind + 3));
        }
    }

    public static bool TryGetShapeKind(in Shape shape, out int number)
    {
        return  ToCollect.TryGetValue(shape.Kind, out number);
    }

    public static void RemoveQuest(in Shape shape) => ToCollect.Remove(shape.Kind);
}