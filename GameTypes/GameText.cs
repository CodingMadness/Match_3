using System.Numerics;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3.GameTypes
{
    public record struct GameText(Font Src, string Text, float InitSize)
    {
        private static bool wasResized;
        private static float ScaledSize;
        public Vector2 ToDrawAT { get; private set; }
        
        public void ScaleText()
        {
            if (!IsWindowResized())
                return;
            
            var pos = MeasureTextEx(Src, Text, InitSize, 1f);
            float scaleX = MathF.Round(Utils.GetScreenCoord().X / pos.X);
            ScaledSize = (InitSize * scaleX);
            wasResized = true;
        }

        public void AlignText()
        {
            if (!wasResized)
                return;
    
            Vector2 size = MeasureTextEx(Src, Text, ScaledSize, ScaledSize / InitSize);
            Vector2 begin = Utils.GetScreenCoord();
            begin.X = 0f;
            ToDrawAT = new (begin.X - size.X,  begin.Y - size.Y);
            wasResized = false;
        }
        
        public void Draw(FadeableColor c)
        {
            ScaledSize = ScaledSize == 0 ? InitSize : ScaledSize;
            DrawTextEx(Src, Text, ToDrawAT, ScaledSize, ScaledSize / InitSize, c);
            ToDrawAT = new(MathF.Round(0f), MathF.Round(GetScreenHeight() / 2));
        }
    }
}
