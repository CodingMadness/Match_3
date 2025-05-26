using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Setup;
using Raylib_cs;
using rlImGui_cs;
using static Match_3.Setup.AssetManager;

namespace Match_3.Workflow;

//TODO: 1. Make all the "TileRelatedTypes" structs because they represent nothing but value holder with minimal state change
//TODO: 2. Fix the entire "QuestHandler" related Event logic, like what shall happen when certain tiles or matches are done, etc...
//TODO: 3. Write the algorithm for "TileGraph" which shall exchange 1 Graph with another so that there are not any distant tiles anymore
internal static class Game
{
    private static GameTime _gameTimer;
    private static bool _inGame;
    public static event Action OnTileClicked = null!;

    private static void Main()
    {
        Initialize();
        MainGameLoop();
        CleanUp();
    }

    private static void Initialize()
    {
        Config level = new(0, 300, 1, 20, 20);

        static void InitRaylib(ref readonly Config lvl)
        { 
            SetTargetFPS(60);
            InitWindow(lvl.WindowWidth, lvl.WindowHeight, "Match3 By Shpendicus");
            SetTextureFilter(BgIngameTexture, TextureFilter.Bilinear);
        }

        static void InitImGui(ref readonly Config lvl)
        {
            //<this has to be initialized RIGHT HERE in order to work!>
            rlImGui.Setup(false);
            UiRenderer.SetCurrentContext();
            LoadAssets(new(lvl.GridWidth, lvl.GridHeight), 32f);
            // For raylib only, because raylib needs to update the imgui-font at gpu-level
            rlImGui.ReloadFonts();
        }

        static void InitGameLevel(ref readonly Config lvl)
        {
            _gameTimer = GameTime.CreateTimer(lvl.GameBeginAt);
            GameState.Instance.Lvl = lvl;
            QuestHandler.ActivateHandlers();
            Grid.Init();
            QuestBuilder.DefineQuests();
        }

        //the calling order of these 3 methods is very important! DO NOT change it!
        InitRaylib(in level);
        InitImGui(in level);
        InitGameLevel(in level);
    }

    private static void MainGameLoop()
    {
        /// <summary>
        /// this checks for a lot of scenarios in which the game could end, either by failure OR
        /// by actually winning in time!
        /// </summary>
        /// <returns></returns>
        //------------------------------------------------------------
        static void HandleGameInput()
        {
            static void NotifyClickHandler()
            {
                static bool TileClicked(out Tile? tile)
                {
                    tile = default!;

                    if (!IsMouseButtonPressed(MouseButton.Left))
                        return false;

                    SingleCell tileCell = GetMousePosition();
                    tile = Grid.GetTile(tileCell.Start);
                    // Console.WriteLine(tile);
                    return tile is not null;
                }

                if (TileClicked(out var firstClickedTile))
                {
                    var currState = GameState.Instance.CurrData;
                    currState.TileX = firstClickedTile;
                    OnTileClicked();
                    Console.WriteLine(firstClickedTile);
                }
            }

            float currTime = _gameTimer.CurrentSeconds;
            _inGame |= IsKeyDown(KeyboardKey.Enter);
            // Console.WriteLine(currTime);

            if (!_inGame)
            {
                //UiRenderer.DrawQuestLog(GameState.Instance.GetQuests());
            }
            else if (_inGame)
            {
                var eventData = GameState.Instance.CurrData;
                eventData.WasGameLost = _gameTimer.CountDown();

                if (eventData.WasGameLost)
                {
                    //print to the main-window that the user has lost
                }
                else if (eventData.WasGameWon)
                {
                    //print to the main-window that the user has won
                }
                //game still running..!
                else
                {
                    var colorCodedText = $"(Blue) you have {(int)currTime} left to win! " +
                                         $"(Violet) and this is " +
                                         $"(Green) the rest of a long long sentence " +
                                         $"(Brown) but of course the joy continues, doesnt it?";

                    //UiRenderer.DrawText(colorCodedText, CanvasStartingPoints.TopLeft);
                    UiRenderer.DrawText(colorCodedText, CanvasStartingPoints.Center);
                    NotifyClickHandler();
                    TileRenderer.DrawGrid(currTime, GameState.Instance.Lvl.GridWidth, GameState.Instance.Lvl.GridHeight);
                }
            }
        }

        while (!WindowShouldClose())
        {
            UiRenderer.Begin();

            UiRenderer.CreateWindowSizedCanvas();
            HandleGameInput();

            UiRenderer.End();
        }
    }

    private static void CleanUp()
    {
        UnloadShader(WobbleEffect);
        UnloadTexture(DefaultTileAtlas);
        CloseWindow();
    }
}