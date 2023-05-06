using System.Reflection;
using Match_3.GameTypes;
using Raylib_CsLo;

namespace Match_3;

public static unsafe class AssetManager
{
    public static Texture WelcomeTexture;
    public static Texture GameOverTexture;
    public static Texture DefaultTileAtlas;
    public static Texture EnemySprite;
    public static Texture BgIngameTexture;
    public static Shader WobbleEffect;
    public static (int size, int time) ShaderData;
    public static Texture FeatureBtn;
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
    public static byte[] GetEmbeddedResource(string relativePath)
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

    private static Sound LoadSound(string fileNameOnly)
    {
        var buffer = GetEmbeddedResource($"Sounds.{fileNameOnly}");
        var first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        var wave = LoadWaveFromMemory(".mp3", first, buffer.Length);
        return LoadSoundFromWave(wave);
    }

    private static Font LoadFont(string fileNameOnly)
    {
        var buffer = GetEmbeddedResource($"Fonts.{fileNameOnly}");
        var first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        return LoadFontFromMemory(".ttf", first, buffer.Length, 200, null, 0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fullPath">For instance: Background.bg1.png OR Button.FeatureBtn.png</param>
    /// <returns></returns>
    private static Texture LoadTexture(string fullPath)
    {
        var buffer = GetEmbeddedResource(fullPath);
        var first = (byte*)Unsafe.AsPointer(ref buffer[0]); 
        Image bg = LoadImageFromMemory(".png", first, buffer.Length);
        return LoadTextureFromImage(bg);
    }

    private static Texture LoadGuiTexture(string relativePath)
    {
        return LoadTexture($"Sprites.GUI.{relativePath}");
    }
    
    private static Texture LoadInGameTexture(string fileNameOnly)
    {
          return LoadTexture($"Sprites.Tiles.{fileNameOnly}");
    }

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

        // Shader uniform values that can be updated at any time
        float freqX = 34.0f;
        float freqY = 50.0f;
        float ampX = 8.5f;
        float ampY = 7.33f;
        float speedX = 8.0f;
        float speedY = 8.0f;

        SetShaderValue(WobbleEffect, freqXLoc, ref freqX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, freqYLoc, ref freqY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, ampXLoc, ref ampX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, ampYLoc, ref ampY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, speedXLoc, ref speedX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleEffect, speedYLoc, ref speedY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);

        return (sizeLoc, secondsLoc);
    }
    
    private static Shader LoadShader(string fileNameOnly)
    {
        var buffer = GetEmbeddedResource($"Shaders.{fileNameOnly}");
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
        WobbleEffect = LoadShader("wobble.frag");
        ShaderData = InitShader();
        
        WelcomeText.Src = GameFont;
        GameOverText.Src = GameFont;
        TimerText.Src = GameFont;
        QuestLogText.Src = GameFont;
    }

    public static void InitGameOverTxt()
    {
        GameOverText.InitSize *= 1f;
        GameOverText.ScaleText(null);
        GameOverText.Color = RED;
        GameOverText.Begin = (Utils.GetScreenCoord() * 0.5f) with { X = 0f };
    }
    
    public static void InitWelcomeTxt()
    {
        WelcomeText.Color = RED;
        WelcomeText.ScaleText(null);
        WelcomeText.Begin = (Utils.GetScreenCoord() * 0.5f) with { X = 0f };
    }
}