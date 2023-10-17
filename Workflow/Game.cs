using System.Diagnostics;
using System.Numerics;
using System.Text;
using DotNext.Runtime;
using ImGuiNET;
using Match_3.Service;
using Match_3.Setup;
using Match_3.Variables;
using Raylib_cs;
using static Match_3.Setup.AssetManager;
using static Match_3.Service.Utils;

namespace Match_3.Workflow;

using System;

internal static class Game
{
    public static Level Level { get; private set; } = null!;
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    private static Background? _bgGameOver;
    private static Background _bgWelcome = null!;
    private static Background? _bgInGame1 = null!;
    private static GameTime _gameTimer;
    private static GameTime[]? _questTimers;
    private static EnemyMatchRuleHandler _enemyMatchRuleHandler;
    private static readonly StringBuilder TimeBuilder = new(3);

    private static bool _enterGame;
    private static bool _shallCreateEnemies;
    private static bool _runQuestTimers;

    public static event Action OnMatchFound;
    public static event Action OnTileClicked;
    public static event Action OnTileSwapped;
    public static event Action OnGameOver;

    private static void Main()
    {
        InitGame();
        MainGameLoop();
        CleanUp();
    }

    private static void InitGame()
    {
        Level = new(0, 60, 6, 12, 10);
        _runQuestTimers = false;
        _gameTimer = GameTime.GetTimer(Level.GameBeginAt);
        _matchesOf3 = new();
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(Level.WindowWidth, Level.WindowHeight, "Match3 By Shpendicus");
        SetTextureFilter(BgIngameTexture, TextureFilter.TEXTURE_FILTER_BILINEAR);
        LoadAssets(new(Level.GridWidth, Level.GridHeight));
        ShaderData = InitWobble2(GetScreenCoord());
        _bgWelcome = new(WelcomeTexture);
        _bgGameOver = new(GameOverTexture);

        //this has to be initialized RIGHT HERE in order to work!
        _questTimers = QuestBuilder.QuestTimers;
        QuestHandler.ActivateHandlers();
        Grid.Instance.Init(Level);
    }

    private static void DragMouseToEnemies()
    {
        //we only fix the mouse point,
        //WHEN the cursor exceeds at a certain bounding box
        if (_enemyMatches is not null && _enemyMatches.WorldPos != InvalidCell)
        {
            bool outsideRect = !CheckCollisionPointRec(GetMousePosition(), _enemyMatches.Border.AsIntRayRect());

            if (outsideRect && GameState.EnemiesStillPresent)
            {
                /*the player has to get these enemies out of the way b4 he can pass!*/
                SetMouseToWorldPos(_enemyMatches.WorldPos, 1);
            }
            else if (!GameState.EnemiesStillPresent && _enemyMatches.IsMatchActive)
            {
                //we set this to null, because we cant make any swaps after this, cause 
                //_secondClicked has a value! so we then can repeat the entire cycle!
                _secondClicked = null;
                //enemies were created from the matchesOf3, so we have to
                //delete all of them, because else we will reference always the base-matches internally which is bad!
                _enemyMatches.Clear();
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
        gridPos /= Size;
        tile = Grid.Instance[gridPos];
        return tile is not null;
    }

    private static void ProcessSelectedTiles()
    {
        if (!TileClicked(out var firstClickedTile))
            return;

        if (firstClickedTile!.IsDeleted)
            return;

        //Enemy tile was clicked on , ofc after a matchX happened!
        if (_enemyMatches?.IsMatchActive == true && Intrinsics.IsExactTypeOf<EnemyTile>(firstClickedTile))
        {
            /*DestroyOnClickHandler.Instance.Subscribe();*/
            /*TileReplacementOnClickHandler.Instance.UnSubscribe();*/

            //we store our current values inside "GameState" which due to its static nature is then checked upon 
            //internally inside the QuestHandler's
            GameState.Tile = firstClickedTile;
            GameState.Matches = _enemyMatches;
            OnTileClicked();
        }
        else
        {
            if (Intrinsics.IsExactTypeOf<Tile>(firstClickedTile))
            {
                //Only when a default tile is clicked X-times, we wanna allow it to change
                //and since both event classes are active,
                //we will need still to unsub from the one who destroys on clicks
                //because he still active and listens to click-events, which we dont want

                /*DestroyOnClickHandler.Instance.UnSubscribe();*/
                /*TileReplacementOnClickHandler.Instance.Subscribe();*/

                GameState.Tile = firstClickedTile;

                if (GameState.WasFeatureBtnPressed == true)
                    OnTileClicked();
            }

            firstClickedTile.TileState |= TileState.Selected;

            /*No tile selected yet*/
            if (_secondClicked is null)
            {
                //prepare for next round, so we store first in second!
                _secondClicked = firstClickedTile;
                return;
            }

            /*Same tile selected => deselect*/
            if (StateAndBodyComparer.Singleton.Equals(firstClickedTile, _secondClicked))
            {
                Console.Clear();
                //Console.WriteLine($"{tmpFirst.GridCell} was clicked AGAIN!");
                _secondClicked.TileState &= TileState.Selected;
                _secondClicked = null;
            }
            /*Different tile selected ==> swap*/
            else
            {
                firstClickedTile.TileState &= TileState.Selected;

                if (Grid.Instance.Swap(firstClickedTile, _secondClicked))
                {
                    //the moment we have the 1. swap, we notify the MatchQuestHandler for this
                    //and he begins to count-down
                    GameState.WasSwapped = true;
                    OnTileSwapped();

                    _secondClicked.TileState &= TileState.Selected;
                }
                else
                {
                    _secondClicked.TileState &= TileState.Selected;
                }
            }
        }
    }

    private static void ComputeMatches()
    {
        if (!GameState.WasSwapped)
            return;

        void CreateEnemiesIfNeeded()
        {
            if (_shallCreateEnemies && (_enemyMatches is null || _enemyMatches.Count == 0))
            {
                _enemyMatches = Bakery.AsEnemies(Grid.Instance, _matchesOf3!);
                GameState.EnemiesStillPresent = true;
            }
        }

        bool shallCreateEnemies;

        if (shallCreateEnemies = Grid.Instance.WasAMatchInAnyDirection(_secondClicked!, _matchesOf3!))
        {
            int tileTypeIdx = (int)GameState.Tile.Body.TileColor;
            GameState.Tile = _secondClicked!;
            GameState.Matches = _matchesOf3;

            //TODO: Look into this timer problem, cause we dont wanna have TileType.Length timers only those who are in request!
            OnMatchFound();

            //if its the 1.time we set _runQuestTimers=true, but we also check for each new Matched
            _runQuestTimers = true;
        }

        shallCreateEnemies &= EnemyMatchRuleHandler.Instance.Check();

        switch (shallCreateEnemies)
        {
            case true:
                CreateEnemiesIfNeeded();
                break;
            case false:
                Grid.Instance.Delete(_matchesOf3!);
                break;
        }

        GameState.WasSwapped = false;
        _secondClicked = null;
    }

    private static void HardReset()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            //Grid.Instance = new Grid(Level);
            _shallCreateEnemies = false;
            _matchesOf3?.Clear();
            _enemyMatches?.Clear();
            GameState.EnemiesStillPresent = false;
            _shallCreateEnemies = false;
            _secondClicked = null;
            GameState.WasSwapped = false;
            Console.Clear();
        }
    }

    private static void HandleGameInput()
    {
        GameTime gameOverTimer = GameTime.GetTimer(Level.GameOverScreenCountdown + 10);
        // int shallWobble = GetShaderLocation(WobbleEffect, "shallWobble");
        float currTime = _gameTimer.ElapsedSeconds;

        double t = GetTime() * 10f;
        if (_enterGame)
        {
            // UiRenderer.DrawCurvedText("HELLO LOVELY WORLD!", 0.1f);

            Vector2 size = new(Size, Size);
            // Debug.WriteLine(currTime);
            SetShaderVal(ShaderData.gridSizeLoc, size);
            SetShaderVal(ShaderData.secondsLoc, currTime);
            SetShaderVal(ShaderData.shouldWobbleLoc, CoinFlip());
        }

        else if (!_enterGame)
        {
            // UiRenderer.DrawCurvedText("HELLO LOVELY WORLD!", 0.1f);
            UiRenderer.DrawQuestLog();
        }

        if (IsKeyDown(KeyboardKey.KEY_ENTER) || _enterGame)
        {
            _gameTimer.Run();

            // int tileTypeIdx = (int)GameState.Tile.Body.TileType;
            // ref var questTimer = ref _questTimers[tileTypeIdx];
            //
            // //if it got activated in "ComputeMatches()" then we can count down, else it sleeps!
            // if (_runQuestTimers)
            //     questTimer.Run();
            //
            //
            // if (_runQuestTimers)
            //     if (questTimer.Done())
            //     { 
            //     }
            // Console.WriteLine(questTimer.Done()
            //     ? $"YOU MISSED 1 MATCH OF TYPE {GameState.Tile.Body.TileType} AND NOW YOU LOSE BONUS OR GET PUNISHED!"
            //     : $"YOU STILL HAVE {questTimer.ElapsedSeconds} TIME LEFT!");

            GameState.IsGameOver = _gameTimer.Done();

            if (GameState.IsGameOver)
            {
                OnGameOver();
                gameOverTimer.Run();
                TimeBuilder.Append($"{gameOverTimer.ElapsedSeconds}");

                UiRenderer.DrawText(TimeBuilder);
                UiRenderer.DrawBackground(_bgGameOver);
                UiRenderer.DrawTimer(gameOverTimer.ElapsedSeconds);
                ImGui.SetWindowFontScale(2f);

                if (UiRenderer.DrawGameOverScreen(gameOverTimer.Done(), GameState.WasGameWonB4Timeout,
                        GameState.Logger))
                    return;
            }
            else if (GameState.WasGameWonB4Timeout)
            {
                if (UiRenderer.DrawGameOverScreen(_gameTimer.Done(), true, GameState.Logger))
                {
                    //Begin new Level!
                    InitGame();
                    GameState.WasGameWonB4Timeout = false;
                }
            }
            else
            {
                Debug.WriteLine(currTime);
                // var pressed = UIRenderer.DrawFeatureBtn(out _);
                // GameState.WasFeatureBtnPressed ??= pressed;
                // GameState.WasFeatureBtnPressed = pressed ?? GameState.WasFeatureBtnPressed;
                UiRenderer.DrawBackground(_bgInGame1);
                UiRenderer.DrawTimer(currTime);
                DragMouseToEnemies();
                ProcessSelectedTiles();
                ComputeMatches();
                GameObjectRenderer.DrawOuterBox(_enemyMatches, currTime);
                GameObjectRenderer.DrawInnerBox(_matchesOf3, currTime);

                GameObjectRenderer.DrawGrid(currTime);
                //GameObjectRenderer.DrawMatches(_enemyMatches, currTime, _shallCreateEnemies);

                HardReset();
            }

            _enterGame = true;
        }
    }

    private static void MainGameLoop()
    {
        // BitsetExample bitsetExample = new BitsetExample();
        //
        // // Set bits to represent values
        // bitsetExample.SetBit(5);
        // bitsetExample.SetBit(6);
        // bitsetExample.SetBit(13);
        // bitsetExample.SetBit(14);
        // bitsetExample.SetBit(15);
        // bitsetExample.SetBit(19);
        // bitsetExample.SetBit(20);
        // bitsetExample.SetBit(23);
        //
        // // Get the original values
        // int[] values = bitsetExample.GetSetValues();
        // foreach (int value in values)
        // {
        //     Console.WriteLine("Value: " + value);
        // }

        while (!WindowShouldClose())
        {
            UiRenderer.BeginRenderCycle(HandleGameInput);
        }
    }

    private static void CleanUp()
    {
        UnloadShader(WobbleEffect);
        UnloadTexture(DefaultTileAtlas);
        CloseWindow();
    }
}