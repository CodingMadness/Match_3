using System.Numerics;
using Raylib_CsLo;

namespace Match_3.GameTypes
{
    public record struct GameText(Font Src, string Text, Vector2 Begin, float Size, FadeableColor Color)
    {
        internal float scaleDiffInX;

        private float ComputeDiffInX(Vector2 screen)
        {
            if (scaleDiffInX == 0)
                scaleDiffInX = screen.X;
            else
            {
                if (screen.X >= scaleDiffInX)
                    scaleDiffInX = screen.X - scaleDiffInX;
                else
                    scaleDiffInX -= screen.X ;
            }

            return scaleDiffInX;
        }
        
        public void ScaleText()
        {
            var pos = Raylib.MeasureTextEx(Src, Text, Size, 1f);
            Vector2 screen = new(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            scaleDiffInX = ComputeDiffInX(screen);
            //Console.WriteLine(scaleDiffInX);
            float scaleX = MathF.Round(screen.X / pos.X);
            float scaleY = MathF.Round(screen.Y / pos.Y);
            //Vector2 scaled = new(scaleX, scaleY);
            Size = (Size * scaleX) / (Size * scaleY);
            //return this with { Size = (Size * scaleX) / (Size * scaleY) };
        }

        public void AlignText()
        {
            ScaleText();
            //if WINDOW_WIDTH is getting smaller, we want to move the entire textPosition
            //to the LEFT (smaller in X)
            Vector2 newPos = new(Begin.X - scaleDiffInX, Begin.Y);
            Begin = newPos;
        }        
    }
}
