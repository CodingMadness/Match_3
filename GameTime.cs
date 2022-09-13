using Raylib_cs;

namespace Match_3;

public struct GameTime
{
    public GameTime()
    {
        
    }

    private int ElapsedSeconds { get; set; }
    public int INIT_TIME { get; init; }
    
    public static GameTime GetTimer(int lifetime)
    { 
        return new GameTime
        {
            INIT_TIME = lifetime,
            ElapsedSeconds = lifetime
        };
    }

    public void UpdateTimer()
    {
        // subtract this frame from the timer if it's not allready expired
        if ((ElapsedSeconds) > 0)
            ElapsedSeconds -= (int)(Raylib.GetFrameTime());
        
//        Console.WriteLine((int)ElapsedSeconds + " time gone");
    }

    public bool TimerDone()
    {
        return ElapsedSeconds <= 0;
    }

    public void Reset() => ElapsedSeconds = INIT_TIME;
}