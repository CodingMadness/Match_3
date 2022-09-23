using System.Numerics;
using Raylib_CsLo;

namespace Match_3.GameTypes
{
    public readonly record struct GameText(Font Src, string Text, Vector2 Begin, float Size, FadeableColor Color)
    {
        public GameText ScaleText()
        {
            float width = Raylib.MeasureTextEx(Src, Text, Size, 1f).X;
            Vector2 screen = new(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            float scale = MathF.Round(screen.X / width);
            return new GameText(Src, Text, Begin, Size * scale, Color);
        }

        public GameText AlignText()
        {
            GameText scaledFont = ScaleText();
            Vector2 scaledSize = Raylib.MeasureTextEx(Src, Text, scaledFont.Size, 1f);
            Vector2 roundedSize = new(MathF.Round(scaledSize.X), MathF.Round(scaledSize.Y));
            Vector2 textStart = new(Begin.X - roundedSize.X * 0.5f, Begin.Y - roundedSize.Y * 0.5f);
            
            if (textStart.X < 0)
                textStart.X = -textStart.X;

            if (textStart.Y < 0)
                textStart.Y = -textStart.Y;

            var x = new GameText(Src, Text, textStart , MathF.Round(scaledFont.Size), Color);
            return x;
        }        
    }
}
