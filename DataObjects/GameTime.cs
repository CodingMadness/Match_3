using Match_3.Service;

namespace Match_3.DataObjects;

public struct GameTime
{
    public float CurrentSeconds { get; private set; }

    public bool IsReset {get; private set;}

    private int MaxTimerValue { get; init; }

    public static GameTime GetTimer(int countDownInSec)
    {
        return new GameTime
        {
            MaxTimerValue = countDownInSec,
            CurrentSeconds = countDownInSec,
            IsReset = false
        };
    }

    public void CountDown()
    {
        if (CurrentSeconds <= MaxTimerValue / 2f)
        {
            CurrentSeconds -= (GetFrameTime() * 1.15f).Trunc(1);
        }
        // subtract this frame from the globalTimer if it's not already expired
        CurrentSeconds -= (1.15f * MathF.Round(GetFrameTime(), 2));
    }

    public readonly bool Done()
    {
        //bool done = CurrentSeconds.Equals(0f, 0.0f);
        //CurrentSeconds = done ? 0f : CurrentSeconds;
        bool done = CurrentSeconds.Trunc(1) == 0.0f;
        return done;
    }

    public readonly bool IsInitialized => CurrentSeconds > 0f;
    
    public void Reset(float? newStart)
    {
        CurrentSeconds = newStart ?? MaxTimerValue;
        IsReset = true;
    }

    public override string ToString() => $"(Time Left: {CurrentSeconds} seconds)";
}