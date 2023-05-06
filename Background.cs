using Raylib_CsLo;

namespace Match_3;

public class Background
{
    public Shape Body { get; }
    //public Rectangle ScreenRect = new(0f, 0f, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
    public Texture Texture;
    
    public Background(Texture bgTexture)
    {
        Body = new()
        {
            AtlasLocation = new(0f, 0f),
            Size = new(bgTexture.width, bgTexture.height),
            Form = ShapeKind.Rectangle,
            Scale = 1f
        };
        Texture = bgTexture;
    }
}