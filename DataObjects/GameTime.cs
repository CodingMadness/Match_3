using Match_3.Service;

namespace Match_3.DataObjects;

public struct GameTime
{
    public float ElapsedSeconds { get; private set; }

    public bool IsReset {get; private set;}

    private int MaxTimerValue { get; init; }

    public static GameTime GetTimer(int countDownInSec)
    {
        return new GameTime
        {
            MaxTimerValue = countDownInSec,
            ElapsedSeconds = countDownInSec,
            IsReset = false
        };
    }

    public void CountDown()
    {
        if (ElapsedSeconds <= MaxTimerValue / 2f)
        {
            ElapsedSeconds -= (GetFrameTime() * 1.15f).Trunc(1);
        }
        // subtract this frame from the globalTimer if it's not already expired
        ElapsedSeconds -= (1.15f * MathF.Round(GetFrameTime(), 2));
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

    public override string ToString() => $"(Time Left: {ElapsedSeconds} seconds)";
}