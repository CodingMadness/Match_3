using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Match_3.AssetManager;
using static Match_3.Utils;
using static Raylib_CsLo.Raylib;
using static rlImGui.rlImGui;

#pragma warning disable CS8618

namespace Match_3;

internal static class Game
{
    public static GameState State { get; private set; }
    public static Level Level { get; private set; }
    private static Grid _grid;
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    private static Background bg1, bgIngame1, bgIngame2;

    private static bool _enterGame;
    private static bool _shallCreateEnemies;

    public static event Action<GameState> OnMatchFound;
    public static event Action<GameState> OnTileClicked;
    public static event Action<GameState> OnTileSwapped;
    
    private static void Main()
    {
        InitGame();
        MainGameLoop();
        CleanUp();
    }

    private static void InitGame()
    {
        Level = new(0,45*5, 4, 10, 10);
        State = new()
        {
            EventData = new Dictionary<Type, Numbers>((int)Type.Length)
        };
        _matchesOf3 = new();
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(Level.WindowWidth, Level.WindowHeight, "Match3 By Shpendicus");
        LoadAssets();
        bg1 = new(WelcomeTexture);
        bgIngame1 = new(IngameTexture1);
        bgIngame2 = new(IngameTexture2);
        QuestHandler<Type>.InitAllQuestHandlers();
        SetTextureFilter(IngameTexture1, TextureFilter.TEXTURE_FILTER_BILINEAR);
        _grid = new(Level);
        State.Grid = _grid;
    }
    
    private static void DragMouseToEnemies()
    {
        //we only fix the mouse point,
        //WHEN the cursor exceeds at a certain bounding box
        if (_enemyMatches is { } && _enemyMatches.WorldPos != INVALID_CELL)
        {
            bool outsideRect = !CheckCollisionPointRec(GetMousePosition(), _enemyMatches.Border);

            if (outsideRect && State.EnemiesStillPresent)
            {
                /*the player has to get these enemies out of the way b4 he can pass!*/
                SetMouseToWorldPos(_enemyMatches.WorldPos, 1);
            }
            else if (!State.EnemiesStillPresent && _enemyMatches.IsMatchActive)
            {
                //we set this to null, because we cant make any swaps after this, cause 
                //_secondClicked has a value! so we then can repeat the entire cycle!
                _secondClicked = null;
                //enemies were created from the matchesOf3, so we have to
                //delete all of them, because else we will reference always the base-matches internally which is bad!
                _enemyMatches.Clear(); 
                _matchesOf3?.Clear();
                //move freely
            }
        }
    }

    private static bool TileClicked(out Tile? tile)
    {
        tile = default!;

        if (!IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            return false;

        var mouseVec2 = GetMousePosition();
        Vector2 gridPos = new Vector2((int)mouseVec2.X, (int)mouseVec2.Y);
        gridPos /= Tile.Size;
        tile = _grid[gridPos];
        return tile is { };
    }
    
    private static void ProcessSelectedTiles()
    {
        if (!TileClicked(out var firstClickedTile))
            return;

        //Console.WriteLine(firstClickedTile.GridCell);
        
        //Enemy tile was clicked on , ofc after a matchX happened!
        if (_enemyMatches?.IsMatchActive == true && firstClickedTile is EnemyTile e)
        {
            var elapsedSeconds = (TimeOnly.FromDateTime(DateTime.Now) - _enemyMatches.CreatedAt).Seconds;
            
            State.Enemy = e;
            State.Grid = _grid;
            State.DefaultTile = e;
            var existent = State.LoadData();
            existent.Click.Count++;
            existent.Click.Seconds = elapsedSeconds;
            State.Update();
            OnTileClicked(State);
        }
        
        firstClickedTile!.Select();
        
        /*No tile selected yet*/
        if (_secondClicked is null)
        {
            //prepare for next round, so we store first in second!
            _secondClicked = firstClickedTile;
            //_secondClicked.Select();
            return;
        }

        /*Same tile selected => deselect*/
        if (StateAndBodyComparer.Singleton.Equals(firstClickedTile, _secondClicked))
        {
            Console.Clear();
            //Console.WriteLine($"{tmpFirst.GridCell} was clicked AGAIN!");
            _secondClicked.DeSelect();
            _secondClicked = null;
        }
        /*Different tile selected ==> swap*/
        else
        {
            firstClickedTile.DeSelect();
            
            if (_grid.Swap(firstClickedTile, _secondClicked))
            {
                State.WasSwapped = true;
                OnTileSwapped(State);
                _secondClicked.DeSelect();
            }
            else
            {
                _secondClicked.DeSelect();
            }
        }
    }
    
    static void ComputeMatches()
    {
        if (!State.WasSwapped)
            return;

        bool ShallTransformMatchesToEnemyMatches() => Randomizer.NextSingle() <= 0.5f;

        void CreateEnemiesIfNeeded()
        {
            if (_shallCreateEnemies && 
                (_enemyMatches is null || _enemyMatches.Count == 0) &&
                _matchesOf3?.Count > 0)
            {
                _enemyMatches = _matchesOf3.AsEnemies(_grid);
            }
        }
        
        if (_grid.WasAMatchInAnyDirection(_secondClicked!, _matchesOf3!) /*&& !_shallCreateEnemies*/)
        {
            State.DefaultTile = _secondClicked;
            var matchData = State.LoadData();
            matchData.Match.Count++;
            State.Matches = _matchesOf3;
            State.EnemiesStillPresent = _shallCreateEnemies = true;
            State.Update();
            OnMatchFound(State);
        }
        else
            _matchesOf3!.Clear();
        
        State.WasSwapped = false;
        _secondClicked = null;
    }
    
    private static void HardReset()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            _grid = new Grid(Level);
            _shallCreateEnemies = true;
            _matchesOf3?.Clear();
            _enemyMatches?.Clear();
            State.EnemiesStillPresent = false;
            _secondClicked = null;
            State.WasSwapped = false;
            Console.Clear();
        }
    }

    private static void MainGameLoop()
    {
        Setup(false);	// sets up ImGui with ether a dark or light default theme
        float slider = 1f;
         
        while (!WindowShouldClose())
        {
            BeginDrawing();
            Begin();
            ClearBackground(WHITE);
            Renderer.DrawBackground(ref bg1);
            
            if (!_enterGame)
            {
                Renderer.ShowWelcomeScreen();
                //Renderer.LogQuest(false, Level);
            }
            if (IsKeyDown(KeyboardKey.KEY_ENTER) || _enterGame)
            {
                Level.GameTimer.Run();
                
                bool isGameOver = Level.GameTimer.Done();

               // Console.WriteLine("TIME  : "+Level.GameTimer.ElapsedSeconds);
                
                if (isGameOver)
                {
                    if (Renderer.OnGameOver(Level.GameTimer.Done(),false))
                    {
                        //leave Gameloop and hence end the game
                        return;
                    }
                }
                else if (State.WasGameWonB4Timeout)
                {
                    if (Renderer.OnGameOver(Level.GameTimer.Done(), true))
                    {
                        InitGame();
                        State.WasGameWonB4Timeout = false;
                        continue;
                    }
                }
                else
                {
                    bgIngame1.Body.Scale = 1f;
                    Renderer.DrawBackground(ref bgIngame2);
                    float elapsedTime = Level.GameTimer.ElapsedSeconds;
                    Renderer.DrawTimer(elapsedTime);
                    DragMouseToEnemies();
                    ProcessSelectedTiles();
                    ComputeMatches();
                    Renderer.DrawOuterBox(_enemyMatches,elapsedTime);  //works!
                    Renderer.DrawInnerBox(_matchesOf3, elapsedTime) ;  //works!
                    Renderer.DrawGrid(_grid, elapsedTime);
                    HardReset();
                }
                _enterGame = true;
            }
        
            End();
            EndDrawing();
            
        }
    }

    private static void CleanUp()
    {
        Shutdown();
        UnloadTexture(DefaultTileAtlas);
        CloseWindow();
    }
}