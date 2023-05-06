using System.Globalization;
using System.Text;
using FastEnumUtility;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Match_3.AssetManager;

namespace Match_3;


public static class UIRenderer
{
    private static Color? _questLogColor;
    //private static readonly IReadOnlyList<string> ColorNames = FastEnum.GetNames<KnownColor>();
    private static StringBuilder? MessageBuilder, Replacer= new(5);

    private const string _message =  "(Black) You have to collect an  " 
                                    + "(Red) amount of -1  "
                                    + "(Blue) Empty tiles!  " 
                                    + "(Black) and u have  "
                                    + "(Green) -2 seconds  " 
                                    + "(Purple) to make a new match, and " 
                                    + "(Yellow) keep in mind to not swap " 
                                    + "more than -3 times" ;
    
    private static readonly (TileType, Quest)[] MatchGoals = new (TileType, Quest)[(int)TileType.Length-1];
    
    public static bool? ShowFeatureBtn(out string btnId)
    {
        static Vector2 NewPos(Vector2 btnSize)
        {
            var screenCoord = Utils.GetScreenCoord();
            float halfWidth = screenCoord.X * 0.5f;
            float indentPos = halfWidth - (btnSize.X * 0.5f);
            Vector2 newPos = new(indentPos, screenCoord.Y - btnSize.Y);
            return newPos;
        }
        
        var flags = 
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoDecoration;
        bool open = true;

        var btnSize = new Vector2(FeatureBtn.width, FeatureBtn.height) *0.67f;
        var newPos = NewPos(btnSize);
        bool? result = null;
        btnId = "FeatureBtn";
        
        //begin rendering sub-window
        ImGui.SetWindowFocus(btnId);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        
        if (ImGui.Begin(btnId, ref open, flags))
        {
            ImGui.SetWindowSize(btnSize * 1.06f);
            ImGui.SetWindowPos(newPos);
            
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            
            //Center(buttonID);
            ImGui.SetCursorPos(Vector2.One*3);
            
            if (ImGui.ImageButton(btnId, (nint)FeatureBtn.id, btnSize))
            {
                result = true;
            }
            ImGui.PopStyleColor(2);
        }
        ImGui.End();
        ImGui.PopStyleVar(2);
        
        return result;
    }

    public static void CenterText(string text, Vector2? begin = null)
    {
        float winWidth = GetScreenWidth();//ImGui.GetWindowSize().X;
        float textWidth = ImGui.CalcTextSize(text).X;
        // calculate the indentation that centers the text on one line, relative
        // to window left, regardless of the `ImGuiStyleVar_WindowPadding` value
        float textIndentation = (winWidth - textWidth) * 0.5f;
        // if text is too long to be drawn on one line, `text_indentation` can
        // become too small or even negative, so we check a minimum indentation
        const float minIndentation = 20.0f;
        
        if (textIndentation <= minIndentation)
        {
            textIndentation = minIndentation;
        }
        
        Vector2 currentPos = new(textIndentation, ImGui.GetCursorPos().Y + (begin?.Y ?? ImGui.GetContentRegionAvail().Y) * 0.5f);
        
        var x = new TextStyleEnumerator(text);

        ImGui.SetCursorPos(currentPos); 
        //ImGui.PushTextWrapPos(wrapPos);

        int counter = 0;
        Vector2 tmp = Vector2.Zero;
        int totalSize = 0;
        //int percentage = (int)(winWidth / 6f);
        int wrapPosX = (int)(winWidth - textIndentation);
        bool shallWrap = false;
        
        foreach (ref readonly var slice in x)
        {
            foreach (var word in slice)
            {
                tmp = tmp == Vector2.Zero ? currentPos : tmp;
                totalSize += (int)word.TextSize.X;

                if (totalSize >= wrapPosX-(int)word.TextSize.X)
                {
                    tmp = currentPos with { Y = tmp.Y };
                    tmp.Y += word.TextSize.Y;
                    totalSize = 0;
                }
                ImGui.SetCursorPos(tmp);
                ImGui.PushTextWrapPos(winWidth - textIndentation);
                ImGui.TextColored(slice.ImGuiColor, word.Piece.ToString());
                ImGui.PopTextWrapPos();
                tmp.X += word.TextSize.X + 4f;
            }
        }
    }

    public static void TextCentered(string text) 
    {
        float winWidth = GetScreenWidth();//ImGui.GetWindowSize().X;
        float textWidth = ImGui.CalcTextSize(text).X;
        // calculate the indentation that centers the text on one line, relative
        // to window left, regardless of the `ImGuiStyleVar_WindowPadding` value
        float textIndentation = (winWidth - textWidth) * 0.5f;
        // if text is too long to be drawn on one line, `text_indentation` can
        // become too small or even negative, so we check a minimum indentation
        const float minIndentation = 20.0f;
        
        if (textIndentation <= minIndentation)
        {
            textIndentation = minIndentation;
        }
        
        Vector2 currentPos = new(textIndentation, ImGui.GetCursorPos().Y +  ImGui.GetContentRegionAvail().Y * 0.5f);

        ImGui.SetCursorPos(currentPos);
        ImGui.PushTextWrapPos(winWidth - textIndentation);
        ImGui.TextColored(Utils.AsVec4(BLACK),text);
        ImGui.PopTextWrapPos();
    }
    
    public static void ShowQuestLog()
    {
        void BuildMessageFrom(in LogData matchGoal, bool shallRestoreMsg)
        {
            string? GetNextValue(in LogData data, int offset)
            {
                //1. matchGoal.g.Match.Value.Count
                //2. matchGoal.t
                //3  matchGoal.Match.Value.Interval
                
                return offset switch
                {
                    0 => data.Count.ToString(),
                    1 => data.Type.ToString(),
                    2 => data.Interval.ToString(),
                    3 => data.MaxSwapsAllowed.ToString(),
                    _ => null
                };
            }
            
            TextChunk GetChunkFromMsg(in TextChunk chunk)
            {
                foreach (ref readonly var word in chunk)
                {
                    if (FastEnum.TryParse<TileType, int>(word.Piece,true, out _) || int.TryParse(word.Piece, out _))
                    {
                        var (idx, len) = word.RelativeLocation;
                        idx = _message.AsSpan().IndexOf(word.Piece, StringComparison.OrdinalIgnoreCase);
                        TextChunk ret = new(word.Piece, word.SystemColor, (idx, len));
                        return ret;
                    }
                }

                throw new ArgumentException("this shall not be called actually");
            }
            
            var msgIterator = new TextStyleEnumerator(_message);
            int counter = 0;
            int deletedLen = 0;
            
            foreach (ref readonly var chunk in msgIterator)
            {
                if (chunk.SystemColor.ToKnownColor() is KnownColor.Black or KnownColor.Purple)
                    continue;

                var value = GetNextValue(matchGoal, counter++);

                var current = shallRestoreMsg ? chunk : GetChunkFromMsg(chunk);

                MessageBuilder?.Replace(current.Piece.ToString(),
                    value, 
                    current.RelativeLocation.idx - deletedLen,
                    current.RelativeLocation.len);
                    
                deletedLen += current.Piece.Length - value!.Length;
            }
        }

        ImGui.SetWindowFontScale(1.5f);
        Vector2 begin = (ImGui.GetContentRegionAvail() * 0.5f) with { Y = 0 };
        _questLogColor ??= Utils.GetRndColor();
        MessageBuilder ??= new (_message);

        var spanIterator = MatchQuestHandler.Instance.GetSpanEnumerator();
        //we begin at index = 1 cause at index = 0 we have Empty, so we skip that one

        foreach (ref readonly var matchGoal in spanIterator)
        {
            BuildMessageFrom(matchGoal, false);
            CenterText(MessageBuilder.ToString());
            begin *= ImGui.GetWindowHeight() / MatchQuestHandler.Instance.GoalCountToReach;
            MessageBuilder.Clear();
            MessageBuilder.Append(_message);
        }
    } 

    public static void ShowTimer(float elapsedSeconds)
    {
        //horrible performance: use a stringBuilder to reuse values!
        TimerText.Text = ((int)elapsedSeconds).ToString();
        TimerText.Src.baseSize = 512*16;
        FadeableColor color = elapsedSeconds > 0f ? BLUE : WHITE;
        TimerText.Color = color with { CurrentAlpha = 1f, TargetAlpha = 1f };
        TimerText.Begin = (Utils.GetScreenCoord() * 0.5f) with { Y = 0f };
        TimerText.ScaleText(null);
        TimerText.Draw(1f);
    }
    
    public static void ShowWelcomeScreen()
    {
        InitWelcomeTxt();
        WelcomeText.Draw(null);
    }
    
    public static bool ShowGameOverScreen(bool isDone, bool? gameWon, string input)
    {
        if (gameWon is null)
        {
            return false;
        }
         
        CenterText(input);
        return isDone;
    }

    public static void ShowBackground(Background bg)
    {
        Rectangle screen = new(0f,0f, GetScreenWidth(), GetScreenHeight());

        DrawTexturePro(bg.Texture, bg.Body.TextureRect, screen.DoScale(bg.Body.Scale), 
            Vector2.Zero, 0f, bg.Body.FIXED_WHITE);
    }
}

public static class GameObjectRenderer
{
    private static void DrawTile(ref Texture atlas, Tile tile, float elapsedTime)
    {
        static void DrawCoordOnTop(Tile tile)
        {
            Font copy = GetFontDefault() with { baseSize = 800 };
            var begin = tile.End;
            float halfSize = Tile.Size * 0.5f;
            begin = begin with { X = begin.X - halfSize + halfSize/1.5f, Y = begin.Y - halfSize - halfSize/3 };
            GameText coordText = new(copy, (tile.GridCell).ToString(), 10.5f)
            {
                Begin = begin,
                Color = (tile.TileState & TileState.Selected)==TileState.Selected ? RED : BLACK,
            };
            coordText.Color.AlphaSpeed = 0f;
            coordText.ScaleText(null);
            coordText.Draw(1f);
        }

        if (tile is EnemyTile enemy)
        {
            enemy.Body.Scale = 1f;
            //enemy.TileState &= TileState.Selected;
            DrawTexturePro(atlas, enemy.Body.TextureRect, enemy.Pulsate(elapsedTime), 
                           Vector2.Zero, 0f, enemy.Body.Color);
            return;
        }
        
        var body = tile.Body;
        DrawTexturePro(atlas, body.TextureRect, tile.WorldBounds, Vector2.Zero, 0f, body.Color);
        DrawCoordOnTop(tile);
    }
    
    public static void DrawGrid(float elapsedTime)
    {
        for (int x = 0; x < Grid.Instance.TileWidth; x++)
        {
            for (int y = 0; y < Grid.Instance.TileHeight; y++)
            {
                Vector2 current = new(x, y);
                var basicTile = Grid.Instance[current];
                    
                if (basicTile is not null && !basicTile.IsDeleted && basicTile is not EnemyTile)
                {
                    DrawTile(ref DefaultTileAtlas, basicTile, elapsedTime);
                }
            }
        }
    }

    public static void DrawMatches(MatchX? match, float elapsedTime, bool shallCreateEnemies)
    {
        if (!shallCreateEnemies)
            return;
        
        if (match is null)
            return;
        
        Texture matchTexture = (match is not null and not EnemyMatches) ? ref DefaultTileAtlas : ref EnemySprite;

        for (int i = 0; i < match?.Count; i++)
        {
            var gridCell = match[(i)].GridCell;
            var tile = Grid.Instance[gridCell];
            if (tile is null)
                continue;
            DrawTile(ref matchTexture, tile, elapsedTime);
        }
    }
    
    public static void DrawInnerBox(MatchX? matches, float elapsedTime)
    {
        if (matches?.IsMatchActive == true)
        {
            DrawRectangleRec(matches.WorldBox, matches.Body!.ToConstColor(RED));
        }
    }
    
    public static void DrawOuterBox(EnemyMatches? matches, float elapsedTime)
    {
        if (matches?.IsMatchActive == true)
        {
            /*
            matches.Body!.Color.AlphaSpeed = 0.2f;
            matches.Body!.Color.ElapsedTime = elapsedTime;
            */
            DrawRectangleRec(matches.Border, matches.Body!.ToConstColor(RED));
        }
    }
}