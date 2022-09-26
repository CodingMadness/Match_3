using Match_3.GameTypes;

namespace Match_3;

public static class GameRuleManager
{
    private static int MaxCapacity => (int)Balls.Length * 5;
    public const int Max3PerKind = 3;
    private static readonly Random rnd = new(DateTime.UtcNow.Millisecond);
    public static int Level { get; private set; }
    public static Level State { get; private set; }

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
    public static bool TryGetSubQuest(CandyShape shape, out int number)
    {
        return State.QuestPerLevel.Quest.TryGetValue(shape.Ball, out number);
    }
    public static void RemoveSubQuest(CandyShape shape) => State.QuestPerLevel.Quest.Remove(shape.Ball);
    public static bool IsSubQuestDone(CandyShape shape, int alreadyMatched) =>
        TryGetSubQuest(shape, out int result) && alreadyMatched >= result;
    public static void ChangeSubQuest(CandyShape shape, int toChangeWith)
        => State.QuestPerLevel.Quest[shape.Ball] = toChangeWith;
    public static bool IsQuestDone() => State.QuestPerLevel.Quest.Count == 0;
    public static void DefineMatch3Quest(int[] countsPerBall)
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
                    for (int i = 0; i < BallCount; i++)
                    {
                        int match3Count = rnd.Next(1, 2);

                        if (match3Count < countsPerBall[i])
                            State.QuestPerLevel.Quest.TryAdd((Balls)i, match3Count);
                        else
                            State.QuestPerLevel.Quest.TryAdd((Balls)i, countsPerBall[i] - match3Count);
                    }
                }
            }
        }
        SetCollectQuest(countsPerBall);
        LogQuest(false);
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