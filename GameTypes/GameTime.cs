using Raylib_CsLo;

namespace Match_3.GameTypes;

public struct GameTime
{
    public GameTime()
    {

    }

    public float ElapsedSeconds { get; set; }

    public bool IsReset {get; private set;}

    public int MaxTimerValue { get; init; }

    public static GameTime GetTimer(int seconds)
    {
        return new GameTime
        {
            MaxTimerValue = seconds,
            ElapsedSeconds = /*Raylib.GetFPS() **/ seconds
        };
    }

    public void Run()
    {
        if (ElapsedSeconds <= MaxTimerValue / 2f)
        {
            ElapsedSeconds -= Raylib.GetFrameTime() * 1.3f;
        }
        // subtract this frame from the globalTimer if it's not allready expired
        if (ElapsedSeconds > 0.000f)
            ElapsedSeconds -= MathF.Round(Raylib.GetFrameTime(), 2);

        //Console.WriteLine((int)ElapsedSeconds + "  time gone");
    }

    public readonly bool Done()
    {
        bool done = ElapsedSeconds <= 0f;
        //ElapsedSeconds = done ? 0f : ElapsedSeconds;
        return done;
    }

    public void Reset(float? newStart)
    {
        ElapsedSeconds = newStart ?? MaxTimerValue;
        IsReset = true;
    }
}