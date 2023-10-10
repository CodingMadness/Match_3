using System.Reflection;
using Match_3.Service;
using Match_3.Variables;
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
    public static (int size, int time) ShaderData;
    public static Texture2D FeatureBtn;
    public static Sound SplashSound;
    private static Font GameFont;
    public static GameText WelcomeText = new(GameFont, "Welcome young man!!", 7f);
    public static GameText GameOverText = new(GameFont, "", 7f);
    public static GameText TimerText = new( GameFont with { baseSize = 512 * 2 }, "", 11f);
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

    public static (int sizeLoc, int timeLoc) InitShader()
    {
        int sizeLoc = GetShaderLocation(WobbleEffect, "size");
        int secondsLoc = GetShaderLocation(WobbleEffect, "seconds");
        int freqXLoc = GetShaderLocation(WobbleEffect, "freqX");
        int freqYLoc = GetShaderLocation(WobbleEffect, "freqY");
        int ampXLoc = GetShaderLocation(WobbleEffect, "ampX");
        int ampYLoc = GetShaderLocation(WobbleEffect, "ampY");
        int speedXLoc = GetShaderLocation(WobbleEffect, "speedX");
        int speedYLoc = GetShaderLocation(WobbleEffect, "speedY");

        // // Shader uniform values that can be updated at any time
        // float freqX = 34.0f;
        // float freqY = 50.0f;
        // float ampX = 8.5f;
        // float ampY = 7.33f;
        // float speedX = 8.0f;
        // float speedY = 8.0f;
        //
        // SetShaderValue(WobbleEffect, freqXLoc, freqX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        // SetShaderValue(WobbleEffect, freqYLoc, freqY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        // SetShaderValue(WobbleEffect, ampXLoc, ampX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        // SetShaderValue(WobbleEffect, ampYLoc, ampY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        // SetShaderValue(WobbleEffect, speedXLoc, speedX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        // SetShaderValue(WobbleEffect, speedYLoc, speedY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);

        return (sizeLoc, secondsLoc);
    }
    
    private static Shader LoadShader(string relativePath)
    {
        var buffer = GetEmbeddedResource($"Shaders.{relativePath}");
        using Stream rsStream = new MemoryStream(buffer, 0, buffer.Length);
        using var reader = new StreamReader(rsStream); 
        return LoadShaderFromMemory(null,  reader.ReadToEnd());
    }
    
    public static void LoadAssets()
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
        // WobbleEffect = LoadShader("wobble.frag");
        // ShaderData = InitShader();
        
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