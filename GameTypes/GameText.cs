using System.Numerics;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3.GameTypes
{
    public class GameText
    {
        private static bool wasResized;
        private static float scaledSize;
        
        public GameText(Font src, string text, float initSize)
        {
            Src = src;
            Text = text;
            InitSize = initSize;
        }

        public FadeableColor Color { get; set; }
        public Font Src { get; }
        public string Text { get; set; }
        public float InitSize { get; set; }
        public Vector2 Begin { get; set; }

        public void ScaleText()
        {
            var pos = MeasureTextEx(Src, Text, InitSize, 1f);
            float scaleX = MathF.Round(Utils.GetScreenCoord().X / pos.X);
            scaledSize = InitSize * scaleX;
        }
        
        public void Draw(float? spacing)
        {
            scaledSize = scaledSize == 0 ? InitSize : scaledSize;
            DrawTextEx(Src, Text, Begin, scaledSize, spacing ?? scaledSize / InitSize, Color);
        }
    }
}
