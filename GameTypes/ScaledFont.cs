using Raylib_CsLo;
using System.Numerics;

namespace Match_3.GameTypes
{
    public record struct ScaledFont(Font Src, string Text, Int2 Begin, float Size)
    {
        public readonly ScaledFont ScaleText()
        {
            float width = Raylib.MeasureTextEx(Src, Text, Size, 1f).X;
            float scale = Begin.X / width;
            //return Size * scale;
            
            return new ScaledFont(Src, Text, Begin, Size * scale);
        }

        public readonly ScaledFont CenterText()
        {
            var scaledFont = ScaleText();

            Vector2 scaledSize = Raylib.MeasureTextEx(Src, Text, scaledFont.Size, 1f);
            Int2 center = Begin * 0.5f;
            Vector2 textStart = new(center.X - scaledSize.X * 0.5f, center.Y - scaledSize.Y * 0.5f);
            var x = new ScaledFont(Src, Text, textStart , scaledFont.Size);
            return x;
        }

        private static void DrawScaledFont(in ScaledFont font)
        {
            var scaled = font.CenterText();
            Raylib.DrawTextEx(scaled.Src, scaled.Text, scaled.Begin, scaled.Size, 1f/*, scaled.Spacing*/, Raylib.RED);
        }
    }
}
