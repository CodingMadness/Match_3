using Raylib_CsLo;
using System.Numerics;

namespace Match_3.GameTypes
{
    public readonly record struct AdaptableFont(Font Src, string Text, Vector2 Begin, float Size, FadeableColor Color)
    {
        public readonly AdaptableFont ScaleText()
        {
            float width = Raylib.MeasureTextEx(Src, Text, ITile.Size, 1f).X;
            float scale = Begin.X / width;
            //return ITile.Size * scale;
            return new AdaptableFont(Src, Text, Begin, ITile.Size * scale, Color);
        }

        public readonly AdaptableFont CenterText()
        {
            var scaledFont = ScaleText();
            Vector2 scaledSize = Raylib.MeasureTextEx(Src, Text, scaledFont.Size, 1f);
            Vector2 center = Begin * 0.5f;
            Vector2 textStart = new(center.X - scaledSize.X * 0.5f, center.Y - scaledSize.Y * 0.5f);
            var x = new AdaptableFont(Src, Text, textStart , scaledFont.Size, Color);
            return x;
        }        
    }
}
