using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Setup;
using Raylib_cs;
using rlImGui_cs;

namespace Match_3.Workflow;

//TODO: 1. Make all the "TileRelatedTypes" structs because they represent nothing but value holder with minimal state change
//TODO: 2. Fix the entire "QuestHandler" related Event logic, like what shall happen when certain tiles or matches are done, etc...
//TODO: 3. Write the algorithm for "TileGraph" which shall exchange 1 Graph with another so that there are not any distant tiles anymore
public static class Game
{
    public static Config ConfigPerStartUp { get; private set; }

    private static readonly GameState MainState = GameState.Instance;

    public static event Action OnTileClicked = null!;

    private static void Main()
    {
        Initialize();
        MainGameEntryPoint();
        CleanUp();
    }

    private static void Initialize()
    {
        //Singleton!
        //config only once when the application/game is started and never changed!
        ConfigPerStartUp = new(0, 300, 1, 20, 20);

        static void InitRaylib()
        {
            Raylib.SetConfigFlags(ConfigFlags.BorderlessWindowMode);
            Raylib.InitWindow(ConfigPerStartUp.WindowWidth, ConfigPerStartUp.WindowHeight, "Match3 By Shpendicus");
            Raylib.SetTextureFilter(AssetManager.DefaultTileAtlas, TextureFilter.Bilinear);
            Raylib.SetTargetFPS(144);
        }

        static void InitImGui()
        {
            //<this has to be initialized RIGHT HERE in order to work!>
            rlImGui.Setup(false);
            AssetManager.LoadAssets(32f);
            // For raylib only, because raylib needs to update the imgui-font at gpu-level;
        }

        static void InitGameLevel()
        {
            QuestBuilder.DefineGameRules(out var states, out var quests);
            MainState.QuestStates = states;
            MainState.ToAccomplish = quests;
            MainState.Logger = new(quests.Length);
            QuestBuilder.DefineQuestTextPerQuest(quests, MainState.Logger);
            MainState.Logger.BeginFromStart();
            QuestHandler.ActivateQuestHandlers();
            TileMap.Init();
        }

        //the calling order of these 3 methods is very important! DO NOT change it!
        InitRaylib();
        InitImGui();
        InitGameLevel();
    }

    private static void MainGameEntryPoint()
    {
        //this checks for a lot of scenarios in which the game could end, either by failure OR by actually winning in time
        static void HandleGameInput()
        {
            static void NotifyClickHandler()
            {
                static bool TileClicked(out Tile? tile)
                {
                    tile = null!;

                    if (!Raylib.IsMouseButtonPressed(MouseButton.Left))
                        return false;

                    SingleCell tileCell = Raylib.GetMousePosition();
                    tile = TileMap.GetTile(tileCell.Start);
                    // Console.WriteLine(tile);
                    return tile is not null;
                }

                if (TileClicked(out var firstClickedTile))
                {
                    if (firstClickedTile is null)
                        return;

                    var currState =
                        MainState.QuestStates.Single(x => x.ColourType == firstClickedTile.Body.Colour.Type);
                    currState.Current = firstClickedTile;
                    OnTileClicked();
                    Console.WriteLine(firstClickedTile);
                }
            }

            var gameTimer = MainState.GetCurrentTime(ConfigPerStartUp);
            float currTime = gameTimer.CurrentSeconds;
            MainState.IsInGame |= Raylib.IsKeyDown(KeyboardKey.Enter);

            if (!MainState.IsInGame)
            {
                UiRenderer.DrawQuestsFrom(MainState.Logger);
            }
            else
            {
                MainState.WasGameLost = gameTimer.CountDown();

                if (MainState.WasGameLost)
                {
                    //print to the main-window that the user has lost
                }
                else if (MainState.WasGameWon)
                {
                    //print to the main-window that the user has won
                }
                //game still running...!
                else
                {
                    NotifyClickHandler();
                    TileRenderer.DrawGrid(currTime, ConfigPerStartUp.GridWidth, ConfigPerStartUp.GridHeight);
                }
            }
        }

        //Game loop
        while (!Raylib.WindowShouldClose())
        {
            UiRenderer.BeginRaylib();
            UiRenderer.CreateCanvas(ConfigPerStartUp);
            //all draw-calls begin here
            var tile = Bakery.CreateTile(new Vector2(0, 0), TileColorTypes.Red);
            TileRenderer.DrawTile(AssetManager.DefaultTileAtlas, tile, 0f);
            //all draw-calls end here
            UiRenderer.EndCanvas();
            UiRenderer.EndRaylib();
        }
    }

    private static void CleanUp()
    {
        rlImGui.Shutdown();
        Raylib.CloseWindow();
    }
}