using Match_3.GameTypes;

namespace Match_3;

public static class GameRuleManager
{
    private static readonly Random rnd = new(DateTime.UtcNow.Millisecond);

    public static Level State { get; }

    static GameRuleManager()
    {
        State = new(10, 3*3, 6, 5, (-1, new Dictionary<Balls, int>((int)Balls.Length)), 64);
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
                Program.Draw(logText.AlignText());
                */
                break;
            }
        }
    } 
    public static bool TryGetMatch3Quest(CandyShape shape, out int number)
    {
        return State.QuestPerLevel.Quest.TryGetValue(shape.Ball, out number);
    }
    public static void RemoveSubQuest(CandyShape shape) => State.QuestPerLevel.Quest.Remove(shape.Ball);
    public static bool IsQuestDone() => State.QuestPerLevel.Quest.Count == 0;
    public static void SetCountPerBall(int[] totalCountPerBall)
    {
        static void SetCollectQuest(int[] countsPerBall)
        {
            var level = State.QuestPerLevel.level;
        
            int BallCount = 0;
        
            if (ShallMakeRndQuests)
                BallCount = rnd.Next(1, level + 1);
            else
            {
                //THis states that under 4 level we only do increase the Ballcount the player
                //has to match, so in lvl0: Ball1 --> 2xmatch3,  Ball2 ---> 3xmatch3
                if (level <= 4)
                {
                    for (int i = 0; i <= level; i++)
                    {
                        BallCount += 2;
                    }
                }
            }
            
            for (int currentBall = 0; currentBall < BallCount; currentBall++)
            {
                int match3Count = rnd.Next(1, 2);

                if (match3Count < countsPerBall[currentBall])
                    State.QuestPerLevel.Quest.TryAdd((Balls)currentBall, match3Count);
                else
                    State.QuestPerLevel.Quest.TryAdd((Balls)currentBall, countsPerBall[currentBall] - match3Count);
            }
        }
        SetCollectQuest(totalCountPerBall);
        LogQuest(false);
    }

    public static bool TryGetEnemyQuest(CandyShape shape, out int clickCountPerEnemy)
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
        State.TilemapWidth += 2;
        State.TilemapHeight += 2;
        State.GameStartAt *= 50;
        State.GameOverScreenTime = 6;
    }
}