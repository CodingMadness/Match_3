using System.Drawing;
using System.Runtime.CompilerServices;
using Match_3.Service;
using Raylib_cs;
using Rectangle = System.Drawing.Rectangle;

namespace Match_3.DataObjects;


///We initialize a "Scale" by:
///  * min
///  * max
///  * speed
///  * ElapsedTime=1f
public readonly struct Scale(float start, float speed=10f, float min=-1f, float max=1f, float elapsedTime=1f)
{
    private readonly float Result = start;
    
    public Scale GetFactor()
    {
        float sizeDirection = 1f;
        float factor = start;

        if (elapsedTime <= 0f)
            return this;
        
        //this means we reached the respective end (either in - or + area) 
        if (factor.Equals(min, 0.1f) || factor.Equals(max, 0.1f))
        {
            //so we start at scale1: then it scaled slowly down to "_minScale" and then from there
            //we change the multiplier to now ADD the x to the scale, so we scale back UP
            //this created this scaling flow
            sizeDirection *= -1;  
        }
        
        float x = speed * (1f / elapsedTime);
        factor += (sizeDirection * x);

        return new(elapsedTime: elapsedTime, start: factor);
    }

    void Test(float seconds)
    {
        Scale up = new(start: 0f, elapsedTime: seconds);
    }
    
    public static (CSharpRect newBox, Scale next) operator *(Scale scale, RectangleF cSharpRect)
    {
        (CSharpRect newBox, Scale next) = (default, scale.GetFactor());
        newBox.Width = cSharpRect.Width * next.Result;
        newBox.Height = cSharpRect.Height * next.Result;
        return (newBox, next);
    }

    public static (RayRect newBox, Scale next) operator *(Scale scale, RayRect rayRect)
    {
        (RayRect newBox, Scale next) = (default, scale.GetFactor());
        newBox.width = rayRect.width * next.Result;
        newBox.height = rayRect.height * next.Result;
        return (newBox, next);
    }

    public static (CSharpRect newBox, Scale next) operator *(Scale scale, SizeF size)
    {
        (CSharpRect newBox, var next) = (default, scale.GetFactor());
        newBox.Width = size.Width * next.Result;
        newBox.Height = size.Height * next.Result;
        return (newBox, next);
    }

    public override string ToString() => $"scaling by: <{Result}>";
}