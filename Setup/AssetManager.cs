using System.Numerics;
using System.Reflection;
using Match_3.Service;
using Match_3.StateHolder;
using Raylib_cs;

namespace Match_3.Setup;

public static class AssetManager
{
    public static Texture2D WelcomeTexture;
    public static Texture2D GameOverTexture;
    public static Texture2D DefaultTileAtlas;
    public static Texture2D EnemySprite;
    public static Texture2D BgIngameTexture;
    public static Shader WobbleEffect;
    public static (int secondsLoc, int gridSizeLoc, int shouldWobbleLoc) ShaderData;
    public static Texture2D FeatureBtn;
    public static Sound SplashSound;
    private static Font GameFont;
    public static GameText WelcomeText = new(GameFont, "Welcome young man!!", 7f);
    public static GameText GameOverText = new(GameFont, "", 7f);
    public static GameText TimerText = new(GameFont with { baseSize = 512 * 2 }, "", 11f);
    public static GameText QuestLogText = new(GameFont, "", 20f);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="relativePath">the path looks like: Root.Sub or Fonts.font3.oft</param>
    /// <exception cref="FileNotFoundException"></exception>
    private static byte[] GetEmbeddedResource(string relativePath)
    {
        var fileName = $"Match_3.Assets.{relativePath}";
        var assembly = Assembly.GetEntryAssembly();
        using var stream = assembly?.GetManifestResourceStream(fileName);
        using var ms = new MemoryStream();

        if (stream == null)
        {
            throw new FileNotFoundException("Cannot find mappings file.", nameof(fileName) + ": " + fileName);
        }

        stream.CopyTo(ms);
        var buffer = ms.GetBuffer();
        return buffer;
    }

    private static Sound LoadSound(string relativePath)
    {
        var buffer = GetEmbeddedResource($"Sounds.{relativePath}");
        var wave = LoadWaveFromMemory(".mp3", buffer);
        return LoadSoundFromWave(wave);
    }

    private static Font LoadFont(string relativePath)
    {
        var buffer = GetEmbeddedResource($"Fonts.{relativePath}");
        return LoadFontFromMemory(".otf", buffer, 200, null, 0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fullPath">For instance: Background.bg1.png OR Button.FeatureBtn.png</param>
    /// <returns></returns>
    private static Texture2D LoadTexture(string fullPath)
    {
        var buffer = GetEmbeddedResource(fullPath);
        Image bg = LoadImageFromMemory(".png", buffer);
        return LoadTextureFromImage(bg);
    }

    private static Texture2D LoadGuiTexture(string relativePath) => LoadTexture($"Sprites.GUI.{relativePath}");

    private static Texture2D LoadInGameTexture(string relativePath) => LoadTexture($"Sprites.Tiles.{relativePath}");

    public static (int timeLoc, int gridSizeLoc, int shouldWobbleLoc) InitWobble1(Vector2 gridSize)
    {
        int timeLoc = GetShaderLocation(WobbleEffect, "time");
        int gridSizeLoc = GetShaderLocation(WobbleEffect, "gridSize");
        int shouldWobbleLoc = GetShaderLocation(WobbleEffect, "shouldWobble");

        const float time = 0f;
        const bool shouldWobble = false;
        SetShaderValue(WobbleEffect, timeLoc, time, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, gridSizeLoc, gridSize, ShaderUniformDataType.SHADER_UNIFORM_VEC2);
        SetShaderValue(WobbleEffect, shouldWobbleLoc, shouldWobble, ShaderUniformDataType.SHADER_UNIFORM_INT);

        return (timeLoc, gridSizeLoc, shouldWobbleLoc);
    }
  
    public static (int secondsLoc, int gridSizeLoc, int shouldWobbleLoc) InitWobble2(Vector2 gridSize)
    {
        int sizeLoc = GetShaderLocation(WobbleEffect, "size");
        int secondsLoc = GetShaderLocation(WobbleEffect, "seconds");
        int shouldWobbleLoc = GetShaderLocation(WobbleEffect, "shouldWobble");
        
        int freqXLoc = GetShaderLocation(WobbleEffect, "freqX");
        int freqYLoc = GetShaderLocation(WobbleEffect, "freqY");
        int ampXLoc = GetShaderLocation(WobbleEffect, "ampX");
        int ampYLoc = GetShaderLocation(WobbleEffect, "ampY");
        int speedXLoc = GetShaderLocation(WobbleEffect, "speedX");
        int speedYLoc = GetShaderLocation(WobbleEffect, "speedY");

        // these are the fixed ones
        float freqX = 34.0f;
        float freqY = 50.0f;
        float ampX = 8.5f;
        float ampY = 7.33f;
        float speedX = 20.0f;
        float speedY = 15.0f;

        SetShaderValue(WobbleEffect, freqXLoc, freqX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, freqYLoc, freqY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, ampXLoc, ampX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, ampYLoc, ampY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, speedXLoc, speedX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, speedYLoc, speedY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, sizeLoc, gridSize, ShaderUniformDataType.SHADER_UNIFORM_VEC2);

        return (sizeLoc, secondsLoc, shouldWobbleLoc);  //these are the one i set up dynamically
    }

    private static Shader LoadShader(string relativePath)
    {
        var buffer = GetEmbeddedResource($"Shaders.{relativePath}");
        using Stream rsStream = new MemoryStream(buffer, 0, buffer.Length);
        using var reader = new StreamReader(rsStream);
        return LoadShaderFromMemory(null, reader.ReadToEnd());
    }

    public static void LoadAssets(Vector2 gridSize)
    {
        InitAudioDevice();

        SplashSound = LoadSound("splash.mp3");
        GameFont = LoadFont("font4.otf");
        WelcomeTexture = LoadGuiTexture("Background.bgWelcome1.png");
        FeatureBtn = LoadGuiTexture("Button.btn1.png");
        BgIngameTexture = LoadGuiTexture("Background.bgIngame1.png");
        GameOverTexture = LoadGuiTexture("Background.bgGameOver.png");
        DefaultTileAtlas = LoadInGameTexture("set1.png");
        EnemySprite = LoadInGameTexture("set2.png");
        WobbleEffect = LoadShader("wobble2.frag");
        ShaderData = InitWobble2(gridSize);
        WelcomeText.Src = GameFont;
        GameOverText.Src = GameFont;
        TimerText.Src = GameFont;
        QuestLogText.Src = GameFont;
    }

    public static void InitGameOverTxt()
    {
        GameOverText.InitSize *= 1f;
        GameOverText.ScaleText(Utils.GetScreenCoord().X);
        GameOverText.Color = RED;
        GameOverText.Begin = (Utils.GetScreenCoord() * 0.5f) with { X = 0f };
    }

    public static void InitWelcomeTxt()
    {
        WelcomeText.Color = RED;
        WelcomeText.ScaleText(Utils.GetScreenCoord().X);
        WelcomeText.Begin = (Utils.GetScreenCoord() * 0.5f) with { X = 0f };
    }
}