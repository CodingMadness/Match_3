namespace Match_3;

public abstract class RuleHandler
{
    protected static THandler GetInstance<THandler>() where THandler : RuleHandler, new()
        => SingletonManager.GetOrCreateRuleHandler<THandler>();
    protected abstract void DefineRule();
    public abstract bool Check();
}

public sealed class EnemyMatchRuleHandler : RuleHandler
{
    private static Dictionary<TileType, int> EnemySpawnFactorPerType;
    private static int[] EnemySpawnTracker;

    public static EnemyMatchRuleHandler Instance => GetInstance<EnemyMatchRuleHandler>();
    
    protected override void DefineRule()
    {
        EnemySpawnFactorPerType = new();
        EnemySpawnTracker = new int[(int)TileType.Length];
        
        var countToMatch = Game.Level.ID switch
        {
            0 => Random.Shared.Next(2, 4),
            1 => Random.Shared.Next(3, 5),
            2 => Random.Shared.Next(5, 7),
            3 => Random.Shared.Next(7, 9),
            _ => default
        };
        
        for (int i = 1; i < (int)TileType.Length; i++)
        {
            EnemySpawnFactorPerType.Add((TileType)i, countToMatch);
            EnemySpawnTracker[i] = 0;
        }
    }

    public EnemyMatchRuleHandler()
    {
        DefineRule();
    }
    
    public override bool Check()
    {
        var type = GameState.Current.Body.TileType;
        ref int spawnTracker = ref EnemySpawnTracker[(int)type];
        EnemySpawnFactorPerType.TryGetValue(type, out int factor);
        return ++spawnTracker == factor;
    }
}