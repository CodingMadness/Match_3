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
    public static Config ConfigPerStartUp { get; private set; }

    public static Logger QuestLogger { get; private set; } = null!;
    
    public static QuestHolder QuestHolder { get; private set; } = null!;
    
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
        //config only once when the application/game is started and never changed!
        ConfigPerStartUp = new(0, 300, 1, 20, 20);
         
        static void InitRaylib()
        { 
            SetTargetFPS(60);
            InitWindow(ConfigPerStartUp.WindowWidth, ConfigPerStartUp.WindowHeight, "Match3 By Shpendicus");
            SetTextureFilter(BgIngameTexture, TextureFilter.Bilinear);
        }

        static void InitImGui()
        {
            //<this has to be initialized RIGHT HERE in order to work!>
            rlImGui.Setup(false);
            UiRenderer.SetCurrentContext();
            LoadAssets(new(ConfigPerStartUp.GridWidth, ConfigPerStartUp.GridHeight), 32f);
            // For raylib only, because raylib needs to update the imgui-font at gpu-level
            rlImGui.ReloadFonts();
        }

        static void InitGameLevel()
        {
            _gameTimer = GameTime.CreateTimer(ConfigPerStartUp.GameBeginAt);
            QuestHolder = QuestBuilder.BuildQuests();
            QuestLogger = new(QuestHolder); 
            QuestHandler.ActivateHandlers();
            TileMap.Init();
        }

        //the calling order of these 3 methods is very important! DO NOT change it!
        InitRaylib();
        InitImGui();
        InitGameLevel();
    }

    private static void MainGameLoop()
    {
        //this checks for a lot of scenarios in which the game could end, either by failure OR by actually winning in time
        static void HandleGameInput()
        {
            static void NotifyClickHandler()
            {
                static bool TileClicked(out Tile? tile)
                {
                    tile = null!;

                    if (!IsMouseButtonPressed(MouseButton.Left))
                        return false;

                    SingleCell tileCell = GetMousePosition();
                    tile = TileMap.GetTile(tileCell.Start);
                    // Console.WriteLine(tile);
                    return tile is not null;
                }

                if (TileClicked(out var firstClickedTile))
                {
                    var currState = GameState.Instance.GetStateBy(firstClickedTile!.Body.Colour.Type);
                    currState.Current = firstClickedTile;
                    OnTileClicked();
                    Console.WriteLine(firstClickedTile);
                }
            }

            float currTime = _gameTimer.CurrentSeconds;
            _inGame |= IsKeyDown(KeyboardKey.Enter);
            // Console.WriteLine(currTime);

            if (!_inGame)
            {
                var quests = QuestHolder.Quests;
                UiRenderer.DrawQuestLog(quests);
            }
            else if (_inGame)
            {
                var gameState = GameState.Instance;
                gameState.WasGameLost = _gameTimer.CountDown();

                if (gameState.WasGameLost)
                {
                    //print to the main-window that the user has lost
                }
                else if (gameState.WasGameWon)
                {
                    //print to the main-window that the user has won
                }
                //game still running...!
                else
                {
                    var colorCodedText = $"(Blue) you have {(int)currTime} left to win! " +
                                         $"(Black) and this is " +
                                         $"(Green) the rest of a long phrase " +
                                         $"(Brown) but of course the joy continues, doesnt it? " +
                                         $"(Yellow) and i dont know what to type here anymore lol :D " +
                                         $"(Black) but maybe I will find some more placeholder " +
                                         $"(Purple) so we can debug this game";

                    UiRenderer.DrawText(colorCodedText, CanvasStartingPoints.Center);
                    NotifyClickHandler();
                    TileRenderer.DrawGrid(currTime, ConfigPerStartUp.GridWidth, ConfigPerStartUp.GridHeight);
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