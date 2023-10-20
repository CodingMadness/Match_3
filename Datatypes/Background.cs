using Raylib_cs;

namespace Match_3.Datatypes;

public class Background(Texture2D bgTexture)
{
    public Shape Body { get; } = new()
    {
        AtlasLocation = new(0f, 0f),
        Size = new(bgTexture.width, bgTexture.height),
        ScaleableFloat = 1f
    };

    //public Rectangle ScreenRect = new(0f, 0f, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
    public Texture2D Texture = bgTexture;
}