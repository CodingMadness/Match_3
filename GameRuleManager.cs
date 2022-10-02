using Match_3.GameTypes;

namespace Match_3;

public static class GameRuleManager
{
    public static Level State { get; }

    static GameRuleManager()
    {
        State = new(10,
            3*3,
            6,
            5,
            (-1, new Dictionary<Type, int>((int)Type.Length)),
            64,
            3);
    }
  
    public static bool ShallMakeRndQuests { get; set; }
    public static void LogQuest(bool useConsole)
    {
        foreach (var pair in State.QuestPerLevel.Quest)
        {
            if (useConsole)
            {
               // Console.WriteLine($"You have to collect {pair.Value} {pair.Key}-tiles!");
                //Console.WriteLine();
            }
            else
            {
                /*
                string txt = $"You have to collect {pair.Value} {pair.Key}-tiles!";
                Vector2 pos = State.Center with {X = State.Center.X * 1.5f, Y = 4 * ITile.ScaledSize };
                GameText logText = new(AssetManager.WelcomeFont, txt, pos, 20f, Raylib.RED);
                //Raylib.DrawText(string.Empty, pos.X, pos.Y *= 1.2f, logText.ScaledSize, Raylib.RED);
                Program.DrawGrid(logText.AlignText());
                */
                break;
            }
        }
    } 
    public static bool TryGetMatch3Quest(TileShape shape, out int matchesNeeded)
    {
        return State.QuestPerLevel.Quest.TryGetValue(shape.Ball, out matchesNeeded);
    }
    public static void RemoveSubQuest(TileShape shape) => State.QuestPerLevel.Quest.Remove(shape.Ball);
    public static bool IsQuestDone() => State.QuestPerLevel.Quest.Count == 0;
    public static void SetCountPerType(int[] totalCountPerBall)
    {
        static int GetRndMatch3Quest()
        {
            var range = State.QuestPerLevel switch
            {
                { level: 0 } => 1..2,
                { level: 1 } => 2..3,
                { level: 2 } => 3..4,
                { level: 3 } => 4..6,
                _ => -1..-1
            };
            return Utils.Randomizer.Next(range.Start.Value, range.End.Value);
        }
            
        static void SetCollectQuest(int[] countsPerBall)
        {            
            for (int currentBall = 0; currentBall < (int)Type.Length; currentBall++)
            {
                var matchesNeeded = GetRndMatch3Quest();

                if (matchesNeeded < countsPerBall[currentBall])
                    State.QuestPerLevel.Quest.TryAdd((Type)currentBall, matchesNeeded);
                else
                    State.QuestPerLevel.Quest.TryAdd((Type)currentBall, countsPerBall[currentBall] - matchesNeeded);
            }
        }
        
        SetCollectQuest(totalCountPerBall);
        LogQuest(false);
    }
    public static bool TryGetEnemyQuest(TileShape shape, out int clickCountPerEnemy)
    {
        //We just say: Take the matxh3Quest and reinterpret it
        //also as a ClickQuest, so same data will be used to store
        //the amount of Clicks the player has to do, in order to remove 
        //a certain enemy tile!
        bool success = TryGetMatch3Quest(shape, out clickCountPerEnemy);
        clickCountPerEnemy *= Utils.Randomizer.Next(2, 3);
        return success;
    }
    public static void InitNewLevel()
    {
        State.SetNextLevel();
        State.TilemapWidth += 6;
        State.TilemapHeight += 5;
        State.GameStartAt *= 70;
        State.GameOverScreenTime = 6;
    }
}