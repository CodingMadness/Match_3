using Raylib_cs;

namespace Match_3;

public struct GameTime
{
    public GameTime()
    {
        
    }
    
    public float ElapsedTime { get; set; }

    public  static GameTime GetTimer(float lifetime)
    { 
        return new GameTime
        {
            ElapsedTime = lifetime
        };
    }

    public void UpdateTimer()
    {
        // subtract this frame from the timer if it's not allready expired
        if (ElapsedTime > 0)
            ElapsedTime -= Raylib.GetFrameTime();
    }

    public bool TimerDone()
    {
        return ElapsedTime <= 0;
    }
}