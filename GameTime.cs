using Raylib_cs;

namespace Match_3;

public class GameTime
{
    public float ElapsedTime { get; set; }

    public void StartTimer(float lifetime)
    { 
        ElapsedTime = lifetime;
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