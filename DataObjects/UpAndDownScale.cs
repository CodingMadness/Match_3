using System.Drawing;

namespace Match_3.DataObjects;


///We initialize a "Scale" by:
///  * min
///  * max
///  * speed
///  * ElapsedTime=1f
/// the greater the time value is, the smaller the final-scale
public readonly struct UpAndDownScale(float speed=1f, float min=1f,float max=2f, float currTime=1f)
{
    private static float _factor = 1f;
    private static bool _shallDownScale;
    
    private void Change()
    {
        if (currTime <= 0f)
            return;

        float x = speed * (1f / currTime);

        //we reached the "max", now we scale down to "min" 
        if (_shallDownScale)
        {
            _factor -= x;
            _shallDownScale = _factor >= min;
        }
        //we begin with "min", now we scale up to "max"
        else  
        {
            _shallDownScale = _factor >= max;
            _factor += x;
        }
    }
    
    public static Rectangle operator *(UpAndDownScale scale, IGridRect rect)
    {
        scale.Change();
        (Rectangle newBox, var factor) = (default, _factor + 1f);
        newBox.Width = (int)(rect.GridBox.Width * factor);
        newBox.Height = (int)(rect.GridBox.Height * factor);
        return newBox;
    }

    public static Rectangle operator *(UpAndDownScale scale, SizeF size)
    {
        scale.Change();
        (Rectangle newBox, var factor) = (default, _factor + 1f);
        newBox.Width = (int)(size.Width * factor);
        newBox.Height = (int)(size.Height * factor);
        return (newBox);
    }

    public override string ToString() => $"scaling by: <{_factor}>";
}