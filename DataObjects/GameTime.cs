using Match_3.Service;

namespace Match_3.DataObjects;

public struct GameTime
{
    public float CurrentSeconds { get; private set; }

    private int MaxTimerValue { get; init; }

    public static GameTime CreateTimer(int countDownInSec)
    {
        return new GameTime
        {
            MaxTimerValue = countDownInSec,
            CurrentSeconds = countDownInSec
        };
    }

    public bool CountDown()
    {
        if (CurrentSeconds <= MaxTimerValue / 2f)
        {
            CurrentSeconds -= (GetFrameTime() * 1.15f).Trunc(1);
        }
        // subtract this frame from the globalTimer if it's not already expired
        CurrentSeconds -= (1.15f * MathF.Round(GetFrameTime(), 2));

        return MathF.Max(CurrentSeconds, 0f) == 0f;
    }

    public readonly override string ToString() => $"(Time Left: {CurrentSeconds} seconds)";
}