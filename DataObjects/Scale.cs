using System.Drawing;

namespace Match_3.DataObjects;


///We initialize a "Scale" by:
///  * min
///  * max
///  * speed
///  * ElapsedTime=1f
/// the greater the time value is, the smaller the final-scale
public readonly struct Scale(float speed=1f, float min=1f,float max=2f, float currTime=1f)
{
    private static float Factor = 1f;
    private static bool ShallDownScale;
    
    private void Change()
    {
        if (currTime <= 0f)
            return;

        float x = speed * (1f / currTime);

        //we reached the "max", now we scale down to "min" 
        if (ShallDownScale)
        {
            Factor -= x;
            ShallDownScale = Factor >= min;
        }
        //we begin with "min", now we scale up to "max"
        else  
        {
            ShallDownScale = Factor >= max;
            Factor += x;
        }
    }
    
    public static CSharpRect operator *(Scale scale, CSharpRect cSharpRect)
    {
        scale.Change();
        (CSharpRect newBox, var factor) = (default, Factor + 1f);
        newBox.Width = cSharpRect.Width * factor;
        newBox.Height = cSharpRect.Height * factor;
        return (newBox);
    }

    public static RayRect operator *(Scale scale, RayRect rayRect)
    {
        scale.Change();
        (RayRect newBox, var factor) = (default, Factor + 1f);
        newBox.width = rayRect.width * factor;
        newBox.height = rayRect.height * factor;
        return (newBox);
    }

    public static CSharpRect operator *(Scale scale, SizeF size)
    {
        scale.Change();
        (CSharpRect newBox, var factor) = (default, Factor + 1f);
        newBox.Width = size.Width * factor;
        newBox.Height = size.Height * factor;
        return (newBox);
    }

    public override string ToString() => $"scaling by: <{Factor}>";
}