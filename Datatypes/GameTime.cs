
using Match_3.Service;

namespace Match_3.Datatypes;

public struct GameTime
{
    public float ElapsedSeconds { get; private set; }

    public bool IsReset {get; private set;}

    private int MaxTimerValue { get; init; }

    public static GameTime GetTimer(int seconds)
    {
        return new GameTime
        {
            MaxTimerValue = seconds,
            ElapsedSeconds = seconds,
            IsReset = false
        };
    }

    public void Run()
    {
        if (ElapsedSeconds <= MaxTimerValue / 2f)
        {
            ElapsedSeconds -= (GetFrameTime() * 1.15f).Trunc(1);
        }
        // subtract this frame from the globalTimer if it's not already expired
        if (ElapsedSeconds > 0.000f)
            ElapsedSeconds -= MathF.Round(GetFrameTime(), 2);

        //Console.WriteLine((int)ElapsedSeconds + "  time gone");
    }

    public readonly bool Done()
    {
        //bool done = ElapsedSeconds.Equals(0f, 0.0f);
        //ElapsedSeconds = done ? 0f : ElapsedSeconds;
        bool done = ElapsedSeconds.Trunc(1) == 0.0f;
        return done;
    }

    public readonly bool IsInitialized => ElapsedSeconds > 0f;
    
    public void Reset(float? newStart)
    {
        ElapsedSeconds = newStart ?? MaxTimerValue;
        IsReset = true;
    }
}