using Raylib_CsLo;
using System.Numerics;

namespace Match_3.GameTypes
{
    public readonly record struct GameFont(Font Src, string Text, Vector2 Begin, float Size, FadeableColor Color)
    {
        public readonly GameFont ScaleText()
        {
            float width = Raylib.MeasureTextEx(Src, Text, Size, 1f).X;
            Vector2 screen = new(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            float scale = screen.X / width;
            return new GameFont(Src, Text, Begin, Size * scale, Color);
        }

        public readonly GameFont CenterText()
        {
            GameFont scaledFont = ScaleText();
            Vector2 scaledSize = Raylib.MeasureTextEx(Src, Text, scaledFont.Size, 1f);
            Vector2 roundedSize = new(MathF.Round(scaledSize.X), MathF.Round(scaledSize.Y));
            Vector2 textStart = new(Begin.X - roundedSize.X * 0.5f, Begin.Y - roundedSize.Y * 0.5f);
            textStart = textStart.X < 0 && textStart.Y < 0 ? Vector2.Negate(textStart) : textStart;
            var x = new GameFont(Src, Text, textStart , scaledFont.Size, Color);
            return x;
        }        
    }
}
