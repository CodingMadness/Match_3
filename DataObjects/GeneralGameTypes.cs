using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.Service;
using Raylib_cs;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace Match_3.DataObjects;

public enum CanvasOffset
{
    TopLeft, TopCenter, TopRight,
    BottomLeft, BottomCenter ,BottomRight,
    MidLeft, Center, MidRight
}

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    private float _currentAlpha;
    private readonly float _targetAlpha, _currSeconds;

    /// <summary>
    /// The greater this Value, the faster it fades!
    /// </summary>
    private readonly float _alphaSpeed;

    public readonly TileColorTypes Type;
    public readonly string Name;
    public readonly Vector4 Vector;

    private FadeableColor(Color toWrap)
    {
        _toWrap = toWrap;
        _alphaSpeed = 0.5f;
        _currentAlpha = 1.0f;
        _targetAlpha = 0.0f;
        _currSeconds = 1f;
        Type = toWrap.ToKnownColor();
        Name = toWrap.Name;
        Vector = ToVec4(Type);
    }

    private void Lerp()
    {
        //if you want to maybe stop fading at 0.5f so we explicitly check if currAlpha > Target-Alpha
        if (_currentAlpha > _targetAlpha)
            _currentAlpha -= _alphaSpeed * (1f / _currSeconds);
    }

    public FadeableColor Apply()
    {
        Lerp();
        return Raylib.Fade(AsRayColor(), _currentAlpha);
    }

    private static readonly TileColorTypes[] AllTileColors =
    [
        TileColorTypes.LightBlue, //--> Hellblau
        TileColorTypes.Turquoise, //--> Türkis
        TileColorTypes.Blue, //--> Blau
        TileColorTypes.LightGreen, //--> Hellgrün
        TileColorTypes.Green, //--> Grün
        TileColorTypes.Brown, //--> Braun
        TileColorTypes.Orange, //--> Orange
        TileColorTypes.Yellow, //--> Gelb
        TileColorTypes.Purple, //--> Rosa
        TileColorTypes.Magenta, //--> Pink
        TileColorTypes.Red, //--> Rot
    ];

    public static Vector4 ToVec4(TileColorTypes colorTypesKind)
    {
        Color systemColor = Color.FromKnownColor(colorTypesKind);

        return new(
            systemColor.R / 255.0f,
            systemColor.G / 255.0f,
            systemColor.B / 255.0f,
            systemColor.A / 255.0f);
    }

    public static void Fill(Span<TileColorTypes> toFill)
    {
        for (int i = 0; i < Config.TileColorCount; i++)
            toFill[i] = AllTileColors[i];
    }

    private readonly Raylib_cs.Color AsRayColor() => new(_toWrap.R, _toWrap.G, _toWrap.B, _toWrap.A);

    private readonly Color AsSysColor() => Color.FromArgb(_toWrap.A, _toWrap.R, _toWrap.G, _toWrap.B);

    public static int ToIndex(TileColorTypes toWrap)
    {
        return toWrap switch
        {
            TileColorTypes.LightBlue => 0, //--> Hellblau
            TileColorTypes.Turquoise => 1, //--> Dunkelblau
            TileColorTypes.Blue => 2, //--> Blau
            TileColorTypes.LightGreen => 3, //--> Hellgrün
            TileColorTypes.Green => 4, //--> Grün
            TileColorTypes.Brown => 5, //--> Braun
            TileColorTypes.Orange => 6, //--> Orange
            TileColorTypes.Yellow => 7, //--> Gelb
            TileColorTypes.MediumVioletRed => 8, //--> RotPink
            TileColorTypes.Purple => 9, //--> Rosa
            TileColorTypes.Magenta => 10, //--> Pink
            TileColorTypes.Red => 11,
            _ => throw new ArgumentOutOfRangeException(nameof(toWrap), toWrap,
                "No other _toWrap is senseful since we do not need other or more colors!")
        };
    }

    public static implicit operator Raylib_cs.Color(FadeableColor toWrap) => toWrap.AsRayColor();

    public static implicit operator Color(FadeableColor toWrap) => toWrap.AsSysColor();

    public static implicit operator FadeableColor(Color toWrap) => new(toWrap);

    public static implicit operator FadeableColor(Raylib_cs.Color toWrap) =>
        new(Color.FromArgb(toWrap.R, toWrap.G, toWrap.B));

    public static bool operator ==(FadeableColor c1, FadeableColor c2)
    {
        int bytes4C1 = Unsafe.As<Color, int>(ref c1._toWrap);
        int bytes4C2 = Unsafe.As<Color, int>(ref c2._toWrap);
        return bytes4C1 == bytes4C2;
    }

    public readonly bool Equals(FadeableColor other)
    {
        return this == other;
    }

    public readonly override bool Equals(object? obj) => obj is FadeableColor other && this == other;

    public readonly override int GetHashCode() => HashCode.Combine(_toWrap, _currentAlpha);

    public static bool operator !=(FadeableColor c1, FadeableColor c2) => !(c1 == c2);

    public readonly override string ToString() => _toWrap.Name;
}

public class QuestState(TileColorTypes ColourType)
{
    public (int Count, float Elapsed) FoundMatch { get; set; }
    public (int Count, float Elapsed) WrongSwaps { get; set; }
    public (int Count, float Elapsed) ReplacementsUsed { get; set; }
    public (int Count, float Elapsed) WrongMatch { get; set; }
    public bool IsQuestLost { get; set; }
    public Tile? Current { get; set; }
    public TileColorTypes ColourType { get; } = ColourType;
}

public sealed class GameState
{
    // Singleton instance (thread-safe & lazy)
    private static readonly Lazy<GameState> _instance = new(() => new GameState());

    public MatchX Matches { get; } = [];

    //make this be loaded only once on a custom method()!
    public QuestState[] QuestStates { get; set; } = null!;
    public Quest[] ToAccomplish { get; set; } = null!;
    public QuestLogger Logger { get; set; } = null!;

    public int CurrentQuestCount
    {
        get => field is 0 ? ToAccomplish.Length : field;
        set;
    }

    public bool IsInGame { get; set; }
    public GameTime GetCurrentTime(in Config config) => GameTime.CreateTimer(config.GameBeginsAt);
    public bool WasGameLost { get; set; }
    public bool WasGameWon { get; set; }
    public bool IsGameStillRunning => !WasGameLost && !WasGameWon;
    public bool HaveAMatch { get; set; }
    public bool WasSwapped { get; set; }

    public int LevelId { get; set; }
    public Tile? TileY; //they must be fields, because I need later them to be used via "ref" directly!
    public Tile? TileX; //they must be fields, because I need later them to be used via "ref" directly!

    public IEnumerable<QuestState>? StatesFromQuestRelatedTiles;

    public TileColorTypes IgnoredByMatch { get; set; }

    public Direction LookUpUsedInMatchFinder { get; set; }

    public static GameState Instance => _instance.Value;

    // Private constructor to prevent external instantiation
    private GameState()
    {
        // Initialize QuestLogger only when first needed (lazy via property)
    }
}

public struct GameTime
{
    public float CurrentSeconds { get; private set; }

    private int MaxTimerValue { get; init; }

    public static GameTime CreateTimer(int countDownInSec)
    {
        return new GameTime
        {
            MaxTimerValue = countDownInSec,
            CurrentSeconds = countDownInSec
        };
    }

    public bool CountDown()
    {
        if (CurrentSeconds <= MaxTimerValue / 2f)
        {
            CurrentSeconds -= (Raylib.GetFrameTime() * 1.15f).Trunc(1);
        }

        // subtract this frame from the globalTimer if it's not already expired
        CurrentSeconds -= (1.15f * MathF.Round(Raylib.GetFrameTime(), 2));

        return MathF.Max(CurrentSeconds, 0f) == 0f;
    }

    public readonly override string ToString() => $"(Time Left: {CurrentSeconds} seconds)";
}

///We initialize a "Scale" by:
///  * min
///  * max
///  * speed
///  * ElapsedTime=1f
/// the greater the time value is, the smaller the final-scale
public readonly struct UpAndDownScale(float speed = 1f, float min = 1f, float max = 2f, float currTime = 1f)
{
    private static float _factor = 1f;
    private static bool _shallDownScale;

    private void Change()
    {
        if (currTime <= 0f)
            return;

        float x = speed * (1f / currTime);

        //we reached the "max", now we scale down to "min"
        if (_shallDownScale)
        {
            _factor -= x;
            _shallDownScale = _factor >= min;
        }
        //we begin with "min", now we scale up to "max"
        else
        {
            _shallDownScale = _factor >= max;
            _factor += x;
        }
    }

    public static Rectangle operator *(UpAndDownScale scale, IGridRect rect)
    {
        scale.Change();
        (Rectangle newBox, var factor) = (default, _factor + 1f);
        newBox.Width = (int)(rect.GridBox.Width * factor);
        newBox.Height = (int)(rect.GridBox.Height * factor);
        return newBox;
    }

    public static Rectangle operator *(UpAndDownScale scale, SizeF size)
    {
        scale.Change();
        (Rectangle newBox, var factor) = (default, _factor + 1f);
        newBox.Width = (int)(size.Width * factor);
        newBox.Height = (int)(size.Height * factor);
        return (newBox);
    }

    public override string ToString() => $"scaling by: <{_factor}>";
}