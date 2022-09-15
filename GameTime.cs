using System.Numerics;
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
        // subtract this frame from the globalTimer if it's not allready expired
        if ((ElapsedSeconds) > 0.000f)
            ElapsedSeconds -= (float)(Raylib.GetFrameTime());
        
        //Console.WriteLine((int)ElapsedSeconds + "  time gone");
    }

    
    //public readonly TimeSpan Stop() => 
    //    TimeSpan.FromMilliseconds(ElapsedSeconds - Raylib.GetFrameTime());

    public readonly bool Done()
    {        
        bool done = ElapsedSeconds <= 0f;
        //ElapsedSeconds = done ? 0f : ElapsedSeconds;
        return done;
     }

    public void Reset(float? newStart) => 
        ElapsedSeconds = newStart is not null ? newStart.Value : MAX_TIMER_VALUE;
}