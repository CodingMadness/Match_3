using Match_3.Service;

namespace Match_3.Variables;

public struct ScaleableFloat(float minScale, float maxScale)
{
    private float _direction = -1f;
    private float _finalScaleFactor = minScale.Equals(maxScale, 0.1f) ? minScale : 1f;

    public float Speed = 0f;
    public float ElapsedTime;

    public float GetFactor()
    {
        if (ElapsedTime <= 0f)
            return _finalScaleFactor;
        
        if (_finalScaleFactor.Equals(minScale, 0.1f) || 
            _finalScaleFactor.Equals(maxScale, 0.1f))
        {
            //so we start at scale1: then it scaled slowly down to "_minScale" and then from there
            //we change the multiplier to now ADD the x to the scale, so we scale back UP
            //this created this scaling flow
            _direction *= -1;  
        }
        float x = Speed * (1 / ElapsedTime); 
        return _finalScaleFactor += _direction * x;
    }
    
    public static implicit operator ScaleableFloat(float size) => new(size, size)
    {
        ElapsedTime = 0f,
        //_direction = 0,
    };
}