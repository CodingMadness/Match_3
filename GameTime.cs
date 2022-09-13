using Raylib_cs;

namespace Match_3;

public struct GameTime
{
    public GameTime()
    {
        
    }

    public float ElapsedSeconds { get; private set; }
    public int MAX_TIMER_VALUE { get; init; }
    
    public static GameTime GetTimer(int lifetime)
    { 
        return new GameTime
        {
            MAX_TIMER_VALUE = lifetime,
            ElapsedSeconds = lifetime
        };
    }

    public void UpdateTimer()
    {
        // subtract this frame from the timer if it's not allready expired
        if ((ElapsedSeconds) > 0.000f)
            ElapsedSeconds -= (float)(Raylib.GetFrameTime());
        
        //Console.WriteLine((int)ElapsedSeconds + "  time gone");
    }

    public bool TimerDone()
    {
        return ElapsedSeconds <= 0;
    }

    public void Reset() => ElapsedSeconds = MAX_TIMER_VALUE;
}