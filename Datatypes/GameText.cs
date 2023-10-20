using System.Numerics;
using Raylib_cs;

namespace Match_3.Datatypes;

public class GameText(Font src, string text, float initSize) 
{
    private float ScaledSize { get; set; }

    public FadeableColor Color;
    
    public Font Src = src;
        
    public string Text { get; set; } = text;

    public float InitSize { get; set; } = initSize;

    public Vector2 Begin { get; set; }
        
    public void ScaleText(float scaleRelativeTo)
    {
        //default scale is always to screen
        var pos = MeasureTextEx(Src, Text, InitSize, 1f);
        float scaleX = MathF.Round(scaleRelativeTo / pos.X);
        ScaledSize = InitSize * scaleX;
    }
        
    public void Draw(float? spacing)
    {
        if (Text.Length == 1)
        {
            Src.baseSize = 10;
            Begin = Begin with { X = Begin.X * 1f };
            ScaledSize = 80f;
            DrawTextEx(Src, Text, Begin, ScaledSize, spacing ?? ScaledSize / InitSize, Color);
            return;
        }

        if (spacing is not null)
        {
            var width = MeasureTextEx(Src, Text, InitSize, InitSize / spacing.Value).X;
            Begin = Begin with { X = Begin.X - width };
        }

        ScaledSize = ScaledSize == 0 ? InitSize : ScaledSize;
            
        DrawTextEx(Src, Text, Begin, ScaledSize, spacing ?? ScaledSize / InitSize, Color.Apply());
    }
}