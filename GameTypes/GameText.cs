using System.Numerics;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3.GameTypes;

public class GameText
{
    private float _scaledSize;
        
    public GameText(Font src, string text, float initSize)
    {
        Src = src;
        Text = text;
        InitSize = initSize;
    }

    public FadeableColor Color;
    
    public Font Src;
        
    public string Text { get; set; }
        
    public float InitSize { get; set; }
        
    public Vector2 Begin { get; set; }
        
    public void ScaleText()
    {
        var pos = MeasureTextEx(Src, Text, InitSize, 1f);
        float scaleX = MathF.Round(GetScreenWidth() / pos.X);
        _scaledSize = InitSize * scaleX;
    }
        
    public void Draw(float? spacing)
    {
        if (Text.Length == 1)
        {
            Src.baseSize = 10;
            Begin = Begin with { X = Begin.X * 1f };
            _scaledSize = 80f;
            DrawTextEx(Src, Text, Begin, _scaledSize, spacing ?? _scaledSize / InitSize, Color);
            return;
        }

        if (spacing is not null)
        {
            var width = MeasureTextEx(Src, Text, InitSize, InitSize / spacing.Value).X;
            Begin = Begin with { X = Begin.X - width };
        }

        _scaledSize = _scaledSize == 0 ? InitSize : _scaledSize;
            
        DrawTextEx(Src, Text, Begin, _scaledSize, spacing ?? _scaledSize / InitSize, Color.Apply());
    }
}