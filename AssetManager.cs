using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public static unsafe class AssetManager
{
    public static Texture WelcomeTexture;
    public static Texture DefaultTileSprite;
    public static Texture EnemySprite;
    public static Texture IngameTexture1, IngameTexture2;
    public static Shader WobbleShader;
    
    public static Sound Splash;

    private static Font GameFont;

    public static GameText WelcomeText = new(GameFont, "Welcome young man!!", 7f);
    public static GameText GameOverText = new( GameFont, "!!", 7f);
    public static GameText TimerText = new( GameFont with { baseSize = 512 * 2 }, "Welcome young man!!", 11f);
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
        var stream = assembly?.GetManifestResourceStream(fileName);
        MemoryStream ms = new MemoryStream();

        if (stream == null)
        {
            throw new FileNotFoundException("Cannot find mappings file.", nameof(fileName) + ": " + fileName);
        }
        stream.CopyTo(ms);
        var buffer = ms.GetBuffer();
        return buffer;
    }

    public static void LoadAssets()
    {
        InitAudioDevice();
        var buffer = GetEmbeddedResource("Sounds.splash.mp3");
        var first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        var wave = LoadWaveFromMemory(".mp3", first, buffer.Length);
        Splash = LoadSoundFromWave(wave);

        buffer = GetEmbeddedResource("Fonts.candy font.ttf");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        GameFont = LoadFontFromMemory(".ttf", first, buffer.Length, 200, null, 0);

        buffer = GetEmbeddedResource(@"Sprites.Background.bgWelcome1.png");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        Image bg = LoadImageFromMemory(".png", first, buffer.Length);
        WelcomeTexture = LoadTextureFromImage(bg);

        buffer = GetEmbeddedResource(@"Sprites.Background.bgIngame1.png");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]); 
        bg = LoadImageFromMemory(".png", first, buffer.Length);
        IngameTexture1 = LoadTextureFromImage(bg);

        buffer = GetEmbeddedResource(@"Sprites.Tiles.set1.png");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        Image ballImg = LoadImageFromMemory(".png", first, buffer.Length);
        DefaultTileSprite = LoadTextureFromImage(ballImg);
        
        buffer = GetEmbeddedResource(@"Sprites.Tiles.set2.png");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        ballImg = LoadImageFromMemory(".png", first, buffer.Length);
        EnemySprite = LoadTextureFromImage(ballImg);
        
        buffer = GetEmbeddedResource("Shaders.wobble.frag");
        using Stream rsStream = new MemoryStream(buffer, 0, buffer.Length);
        using var reader = new StreamReader(rsStream);
        WobbleShader = LoadShaderFromMemory(null, reader.ReadToEnd());

        GameText.Src = GameFont;
    }
    
    public static (int sizeLoc, int timeLoc) InitShader()
    {
        int sizeLoc = GetShaderLocation(WobbleShader, "size");
        int secondsLoc = GetShaderLocation(WobbleShader, "seconds");
        int freqXLoc = GetShaderLocation(WobbleShader, "freqX");
        int freqYLoc = GetShaderLocation(WobbleShader, "freqY");
        int ampXLoc = GetShaderLocation(WobbleShader, "ampX");
        int ampYLoc = GetShaderLocation(WobbleShader, "ampY");
        int speedXLoc = GetShaderLocation(WobbleShader, "speedX");
        int speedYLoc = GetShaderLocation(WobbleShader, "speedY");

        // Shader uniform values that can be updated at any time
        float freqX = 34.0f;
        float freqY = 50.0f;
        float ampX = 5.0f;
        float ampY = 5.0f;
        float speedX = 8.0f;
        float speedY = 8.0f;
        
        SetShaderValue(WobbleShader, freqXLoc, ref freqX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleShader, freqYLoc, ref freqY,ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleShader, ampXLoc, ref ampX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleShader, ampYLoc, ref ampY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleShader, speedXLoc, ref speedX, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        SetShaderValue(WobbleShader, speedYLoc, ref speedY, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);

        return (sizeLoc, secondsLoc);
    }
}