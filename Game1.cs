using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RetroGamePiramid.Audio;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;
using RetroGamePiramid.Graphics;
using RetroGamePiramid.Grid;
using RetroGamePiramid.Input;
using RetroGamePiramid.Scenes;
using RetroGamePiramid.Systems;

namespace RetroGamePiramid;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private VirtualViewport _virtualViewport = null!;
    private RoomGrid _roomGrid = null!;
    private TileRenderer _tileRenderer = null!;
    private Player _player = null!;
    private PlayerRenderer _playerRenderer = null!;
    private PixelFont _pixelFont = null!;
    private TitleMenu _titleMenu = null!;
    private PuzzleManager _puzzleManager = null!;
    private PuzzleRenderer _puzzleRenderer = null!;
    private HieroglyphRenderer _hieroglyphRenderer = null!;
    private Mummy _mummy = null!;
    private Mummy _mummyPlatform1 = null!;
    private Mummy _mummyPlatform3 = null!;
    private MummyRenderer _mummyRenderer = null!;
    private EgyptianAmbientMusic _ambientMusic = null!;
    private RetroSoundEffects _soundEffects = null!;

    private GameScreen _currentScreen = GameScreen.TitleMenu;
    private int _frameCounter;
    private KeyboardState _prevKeyboardState;
    private int _currentChamber = 1;
    private bool _wasJumpingOverMummy;
    private int _respawnTimer;

    private static readonly string[] HudRoomTitles =
    {
        "RECAMARA: 1",
        "RECAMARA: 1",
        "RECAMARA: 2",
        "RECAMARA: 3",
        "RECAMARA: 4",
        "RECAMARA: 5",
        "RECAMARA: 6",
        "RECAMARA: 7",
        "RECAMARA: 8",
        "RECAMARA: 9",
        "RECAMARA: 10"
    };

    private static readonly string[] WinModalTitles =
    {
        "!RECAMARA 1 COMPLETADA!",
        "!RECAMARA 1 COMPLETADA!",
        "!RECAMARA 2 COMPLETADA!",
        "!RECAMARA 3 COMPLETADA!",
        "!RECAMARA 4 COMPLETADA!",
        "!RECAMARA 5 COMPLETADA!",
        "!RECAMARA 6 COMPLETADA!",
        "!RECAMARA 7 COMPLETADA!",
        "!RECAMARA 8 COMPLETADA!",
        "!RECAMARA 9 COMPLETADA!",
        "!RECAMARA 10 COMPLETADA!"
    };

    private const string HUD_TEXT_MENU = "1: SALIR AL MENU";
    private const string HUD_TEXT_EXIT = "1: SALIR";
    private const string HUD_TEXT_RESTART = "2: RE-INICIAR";
    private const string HUD_TEXT_CONTINUE = "2: CONTINUAR";
    private const string HUD_POINTS_LABEL = "PUNTOS: ";
    private const string HUD_LIVES_COLON = ":  ";
    private const string HUD_KEY_COLON = ": ";
    private const string HUD_KEY_YES = "SI";
    private const string HUD_KEY_NO = "NO";
    private const string TEXT_ELIMINATED = "ELIMINADO";
    private const string TEXT_GAME_OVER = "GAME OVER";

    private static readonly Rectangle PlayerIconSourceRect = new(2, 2, 12, 14);
    private static readonly Rectangle PlayerIconDestRect = new(18 + 5, 18 + 1, 12, 14);
    private static readonly Rectangle KeyIconSourceRect = new(4, 5, 8, 11);
    private static readonly Rectangle KeyIconDestRect = new(18 + 45, 18 + 2, 8, 11);

    private static readonly Color HudBgColor = new(16, 10, 22, 220);
    private static readonly Color HudBorderColor = new(150, 110, 50);
    private static readonly Color HudTitleColor = new(255, 230, 130);
    private static readonly Color HudShadowColor = new(10, 5, 2);
    private static readonly Color HudTextColor = new(240, 215, 140);

    private static readonly Color EliminatedBgColor = new(35, 10, 10, 240);
    private static readonly Color EliminatedBorderColor = new(210, 50, 40);
    private static readonly Color EliminatedTitleColor = new(255, 80, 70);
    private static readonly Color EliminatedShadowColor = new(15, 5, 5);

    private static readonly Color ChamberWinBgColor = new(14, 18, 28, 245);
    private static readonly Color ChamberWinBorderColor = new(240, 195, 60);
    private static readonly Color ChamberWinTitleColor = new(255, 235, 120);
    private static readonly Color ChamberWinSubColor = new(130, 230, 255);
    private static readonly Color ChamberWinOptionColor = new(245, 230, 180);
    private static readonly Color ChamberWinShadowColor = new(5, 8, 15);

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Configuración de framerate fijo a 60 FPS (Fidelidad Retro)
        IsFixedTimeStep = true;
        TargetElapsedTime = GameConstants.TargetElapsedTime;

        // Resolución de ventana por defecto (3x escalado entero: 960x720)
        _graphics.PreferredBackBufferWidth = GameConstants.VIRTUAL_WIDTH * 3;
        _graphics.PreferredBackBufferHeight = GameConstants.VIRTUAL_HEIGHT * 3;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Inicialización de subsistemas de renderizado virtual y cuadrícula
        _virtualViewport = new VirtualViewport(GraphicsDevice, GameConstants.VIRTUAL_WIDTH, GameConstants.VIRTUAL_HEIGHT);
        _roomGrid = new RoomGrid();
        _roomGrid.LoadDefaultRoom();
        _tileRenderer = new TileRenderer(GraphicsDevice);

        // Inicialización de la tipografía retro pixel-art y el menú principal
        _pixelFont = new PixelFont(GraphicsDevice);
        _titleMenu = new TitleMenu();

        // Inicialización del puzle de la plataforma 0 (muro conmutable y tesoro)
        _puzzleManager = new PuzzleManager();
        _puzzleManager.Initialize(_roomGrid);
        _puzzleRenderer = new PuzzleRenderer(GraphicsDevice);
        _hieroglyphRenderer = new HieroglyphRenderer(GraphicsDevice);

        // Inicialización del arqueólogo y su renderizador pixel-art
        _player = new Player(new GridCoord(2, 13));
        _playerRenderer = new PlayerRenderer(GraphicsDevice);

        // Inicialización de la momia (enemigo patrullero en plataforma 2) y su renderizador
        _mummy = new Mummy(
            spawnX: 15 * GameConstants.TILE_SIZE,
            spawnY: 4 * GameConstants.TILE_SIZE,
            minX: 2 * GameConstants.TILE_SIZE,
            maxX: 16 * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right);

        // Momia guardiana de la plataforma 1 (Recámara 1): inicialmente inmóvil a la izquierda
        _mummyPlatform1 = new Mummy(
            spawnX: 2 * GameConstants.TILE_SIZE,
            spawnY: 8 * GameConstants.TILE_SIZE,
            minX: 2 * GameConstants.TILE_SIZE,
            maxX: 11 * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right,
            initiallyAwake: false,
            initiallyActive: true);

        // Tercera momia guardiana de la plataforma 3 (activa en Recámaras 5, 6 y 7 del nivel 2 de la pirámide)
        _mummyPlatform3 = new Mummy(
            spawnX: 14 * GameConstants.TILE_SIZE,
            spawnY: 3 * GameConstants.TILE_SIZE,
            minX: 2 * GameConstants.TILE_SIZE,
            maxX: 16 * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Left,
            initiallyAwake: false,
            initiallyActive: false);

        _mummyRenderer = new MummyRenderer(GraphicsDevice);
        _ambientMusic = new EgyptianAmbientMusic();
        _soundEffects = new RetroSoundEffects();
    }

    protected override void UnloadContent()
    {
        _soundEffects?.Dispose();
        _ambientMusic?.Dispose();
        _mummyRenderer?.Dispose();
        _puzzleRenderer?.Dispose();
        _hieroglyphRenderer?.Dispose();
        _pixelFont?.Dispose();
        _playerRenderer?.Dispose();
        _tileRenderer?.Dispose();
        _virtualViewport?.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _frameCounter++;
        KeyboardState keyboard = Keyboard.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
        }

        // Tecla M para silenciar o reactivar la música ambiental egipcia en cualquier momento
        if (keyboard.IsKeyDown(Keys.M) && !_prevKeyboardState.IsKeyDown(Keys.M))
        {
            _ambientMusic.ToggleMute();
            _soundEffects.IsMuted = _ambientMusic.IsMuted;
        }

        if (_currentScreen == GameScreen.TitleMenu)
        {
            _ambientMusic.Pause();
            _titleMenu.IsChamber10Unlocked = _puzzleManager.IsChamber10Unlocked;
            bool up = keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W);
            bool down = keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S);
            bool left = keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A);
            bool right = keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D);
            bool confirm = keyboard.IsKeyDown(Keys.Enter) || keyboard.IsKeyDown(Keys.Space);

            // Teclas directas 1 a 0 para selección rápida de las 10 recámaras en el menú
            if ((keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1)) &&
                !(_prevKeyboardState.IsKeyDown(Keys.D1) || _prevKeyboardState.IsKeyDown(Keys.NumPad1)))
            {
                _titleMenu.SelectedChamber = 1;
            }
            else if ((keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2)) &&
                     !(_prevKeyboardState.IsKeyDown(Keys.D2) || _prevKeyboardState.IsKeyDown(Keys.NumPad2)))
            {
                _titleMenu.SelectedChamber = 2;
            }
            else if ((keyboard.IsKeyDown(Keys.D3) || keyboard.IsKeyDown(Keys.NumPad3)) &&
                     !(_prevKeyboardState.IsKeyDown(Keys.D3) || _prevKeyboardState.IsKeyDown(Keys.NumPad3)))
            {
                _titleMenu.SelectedChamber = 3;
            }
            else if ((keyboard.IsKeyDown(Keys.D4) || keyboard.IsKeyDown(Keys.NumPad4)) &&
                     !(_prevKeyboardState.IsKeyDown(Keys.D4) || _prevKeyboardState.IsKeyDown(Keys.NumPad4)))
            {
                _titleMenu.SelectedChamber = 4;
            }
            else if ((keyboard.IsKeyDown(Keys.D5) || keyboard.IsKeyDown(Keys.NumPad5)) &&
                     !(_prevKeyboardState.IsKeyDown(Keys.D5) || _prevKeyboardState.IsKeyDown(Keys.NumPad5)))
            {
                _titleMenu.SelectedChamber = 5;
            }
            else if ((keyboard.IsKeyDown(Keys.D6) || keyboard.IsKeyDown(Keys.NumPad6)) &&
                     !(_prevKeyboardState.IsKeyDown(Keys.D6) || _prevKeyboardState.IsKeyDown(Keys.NumPad6)))
            {
                _titleMenu.SelectedChamber = 6;
            }
            else if ((keyboard.IsKeyDown(Keys.D7) || keyboard.IsKeyDown(Keys.NumPad7)) &&
                     !(_prevKeyboardState.IsKeyDown(Keys.D7) || _prevKeyboardState.IsKeyDown(Keys.NumPad7)))
            {
                _titleMenu.SelectedChamber = 7;
            }
            else if ((keyboard.IsKeyDown(Keys.D8) || keyboard.IsKeyDown(Keys.NumPad8)) &&
                     !(_prevKeyboardState.IsKeyDown(Keys.D8) || _prevKeyboardState.IsKeyDown(Keys.NumPad8)))
            {
                _titleMenu.SelectedChamber = 8;
            }
            else if ((keyboard.IsKeyDown(Keys.D9) || keyboard.IsKeyDown(Keys.NumPad9)) &&
                     !(_prevKeyboardState.IsKeyDown(Keys.D9) || _prevKeyboardState.IsKeyDown(Keys.NumPad9)))
            {
                _titleMenu.SelectedChamber = 9;
            }
            else if ((keyboard.IsKeyDown(Keys.D0) || keyboard.IsKeyDown(Keys.NumPad0)) &&
                     !(_prevKeyboardState.IsKeyDown(Keys.D0) || _prevKeyboardState.IsKeyDown(Keys.NumPad0)))
            {
                _titleMenu.SelectedChamber = 10;
            }

            if (_titleMenu.Update(up, down, left, right, confirm, out MenuOption selectedAction))
            {
                switch (selectedAction)
                {
                    case MenuOption.NewGame:
                        // Iniciar partida en la recámara elegida por el jugador
                        _currentChamber = _titleMenu.SelectedChamber;
                        _player.ResetLives();
                        _puzzleManager.ResetScore();
                        RestartCurrentRoom();
                        _titleMenu.CanContinue = true;
                        _currentScreen = GameScreen.Playing;
                        break;

                    case MenuOption.Continue:
                        // Reanudar la expedición en curso (si quedó eliminado pero con vidas, re-inicia cámara)
                        if (_player.IsEliminated && _player.Lives > 0)
                        {
                            RestartCurrentRoom();
                        }
                        _titleMenu.CanContinue = true;
                        _currentScreen = GameScreen.Playing;
                        break;

                    case MenuOption.Exit:
                        Exit();
                        break;
                }
            }
        }
        else if (_currentScreen == GameScreen.Playing)
        {
            _ambientMusic.Play();

            // Gestión de la pantalla interactiva al finalizar la recámara
            if (_puzzleManager.IsChamberCompleted)
            {
                bool press1 = (keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1)) &&
                              !(_prevKeyboardState.IsKeyDown(Keys.D1) || _prevKeyboardState.IsKeyDown(Keys.NumPad1));
                bool press2 = (keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2)) &&
                              !(_prevKeyboardState.IsKeyDown(Keys.D2) || _prevKeyboardState.IsKeyDown(Keys.NumPad2));
                bool press3 = (keyboard.IsKeyDown(Keys.D3) || keyboard.IsKeyDown(Keys.NumPad3)) &&
                              !(_prevKeyboardState.IsKeyDown(Keys.D3) || _prevKeyboardState.IsKeyDown(Keys.NumPad3));
                bool pressEscape = keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboardState.IsKeyDown(Keys.Escape);

                if (press1)
                {
                    // 1: Pasar a la siguiente recámara (1 -> 2 -> ... -> 10 -> 1)
                    int nextChamber = _currentChamber < GameConstants.TOTAL_CHAMBERS ? _currentChamber + 1 : 1;
                    LoadChamber(nextChamber);
                }
                else if (press2)
                {
                    // 2: Volver a jugar la misma recámara
                    RestartCurrentRoom();
                }
                else if (press3 || pressEscape)
                {
                    // 3: Salir al menú principal
                    _titleMenu.CanContinue = true;
                    _titleMenu.SelectedOption = MenuOption.Continue;
                    _titleMenu.SelectedChamber = _currentChamber;
                    _currentScreen = GameScreen.TitleMenu;
                }

                _prevKeyboardState = keyboard;
                base.Update(gameTime);
                return;
            }

            bool pressMenu1 = (keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1)) &&
                              !(_prevKeyboardState.IsKeyDown(Keys.D1) || _prevKeyboardState.IsKeyDown(Keys.NumPad1));
            bool pressMenuEscape = keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboardState.IsKeyDown(Keys.Escape);

            bool pressMenu2 = (keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2)) &&
                              !(_prevKeyboardState.IsKeyDown(Keys.D2) || _prevKeyboardState.IsKeyDown(Keys.NumPad2));

            // Toque 1 o Escape: Salir al menú principal
            if (pressMenu1 || pressMenuEscape)
            {
                if (_player.Lives == 0)
                {
                    _titleMenu.CanContinue = false;
                    _titleMenu.SelectedOption = MenuOption.NewGame;
                }
                else
                {
                    _titleMenu.SelectedOption = MenuOption.Continue;
                }
                _titleMenu.SelectedChamber = _currentChamber;
                _currentScreen = GameScreen.TitleMenu;
            }
            // Toque 2: Re-iniciar la pantalla manualmente (solo si el jugador aún tiene vidas)
            else if (pressMenu2 && _player.Lives > 0)
            {
                _respawnTimer = 0;
                RestartCurrentRoom();
            }
            else if (_player.IsEliminated)
            {
                // Si el jugador fue eliminado pero aún conserva vidas, reaparece (respawn)
                // automáticamente sin quedar bloqueado en la pantalla de Eliminado.
                if (_player.Lives > 0)
                {
                    _respawnTimer++;
                    if (_respawnTimer >= 60)
                    {
                        _respawnTimer = 0;
                        RestartCurrentRoom();
                    }
                }
            }
            else
            {
                _respawnTimer = 0;
                // Muestreo de entrada y actualización del jugador, momias y puzle (cero allocations)
                PlayerInput input = PlayerInput.FromKeyboard(keyboard);
                PlayerState prevPlayerState = _player.State;
                _player.Update(_roomGrid, in input);
                if (prevPlayerState != PlayerState.Jumping && _player.State == PlayerState.Jumping)
                {
                    _soundEffects.PlayJump();
                }

                _puzzleManager.Update(_roomGrid, _player, _mummy, _mummyPlatform3);
                if (_puzzleManager.JustCollectedTreasure)
                {
                    _soundEffects.PlayTreasure();
                }

                // Al tomar la llave se despierta la momia guardiana de la plataforma 1
                if (_puzzleManager.HasKey && !_mummyPlatform1.IsAwake && _mummyPlatform1.IsActive)
                {
                    _mummyPlatform1.WakeUp();
                }

                // Puzle 2 / 5: Detección de salto del jugador sobre la momia de plataforma 1 (tanto en Nivel 1 como caída en Nivel 0)
                bool canJumpOverMummy1 = _mummyPlatform1.IsActive &&
                                         !_mummyPlatform1.IsChasing &&
                                         !_mummyPlatform1.IsFalling &&
                                         ((_mummyPlatform1.IsAwake && !_mummyPlatform1.HasReachedLevel0) ||
                                          (_mummyPlatform1.HasReachedLevel0 && !_mummyPlatform1.IsPuzzleCompleted));

                if (canJumpOverMummy1)
                {
                    if (_player.State == PlayerState.Jumping)
                    {
                        float playerLeft = _player.Position.X;
                        float playerRight = _player.Position.X + Player.WIDTH;
                        float mummyLeft = _mummyPlatform1.Position.X;
                        float mummyRight = _mummyPlatform1.Position.X + Mummy.WIDTH;

                        bool horizOverlap = playerRight > mummyLeft && playerLeft < mummyRight;
                        bool isAbove = (_player.Position.Y + Player.HEIGHT) <= (_mummyPlatform1.Position.Y + 4f);

                        if (horizOverlap && isAbove)
                        {
                            _wasJumpingOverMummy = true;
                        }
                    }
                    else
                    {
                        if (_wasJumpingOverMummy)
                        {
                            _wasJumpingOverMummy = false;
                            if (!_player.IsEliminated)
                            {
                                if (!_mummyPlatform1.HasReachedLevel0)
                                {
                                    _puzzleManager.RegisterLevel1MummyJump();
                                }
                                bool chased = _mummyPlatform1.RegisterJumpOver();
                                if (chased)
                                {
                                    _puzzleManager.NotifyMummyChase();
                                }
                            }
                        }
                    }
                }
                else
                {
                    _wasJumpingOverMummy = false;
                }

                _mummy.Update(_player, _roomGrid);
                _mummyPlatform1.Update(_player, _roomGrid);
                _mummyPlatform3.Update(_player, _roomGrid);

                // Cuando se completa el puzle de la momia de plataforma 1 (en Recámara 2 tras caer al Nivel 0 y saltarla 2 veces más), se genera el nuevo tesoro (Treasure 3)
                if (_mummyPlatform1.IsPuzzleCompleted && !_puzzleManager.IsTreasure3Spawned)
                {
                    _puzzleManager.SpawnTreasure3(_roomGrid);
                }

                if (_player.IsEliminated)
                {
                    _wasJumpingOverMummy = false;
                    _mummyPlatform1.ResetJumpOver();
                    _soundEffects.PlayLoseLife();
                }
            }
        }

        _prevKeyboardState = keyboard;
        base.Update(gameTime);
    }

    /// <summary>
    /// Configura y carga una recámara específica (1 a 10) por su identificador numérico.
    /// Todas las recámaras cuentan con momias en Nivel 1 y Nivel 2, puzles, trampas, plataformas, entrada y salida.
    /// </summary>
    public void LoadChamber(int chamberNumber)
    {
        _respawnTimer = 0;
        _wasJumpingOverMummy = false;
        int normalized = (chamberNumber >= 1 && chamberNumber <= GameConstants.TOTAL_CHAMBERS) ? chamberNumber : 1;
        bool redirectedFromLockedApex = false;
        if (normalized == GameConstants.TOTAL_CHAMBERS && !_puzzleManager.IsChamber10Unlocked)
        {
            normalized = _puzzleManager.GetFirstIncompleteChamber();
            redirectedFromLockedApex = true;
        }

        _currentChamber = normalized;
        _roomGrid.LoadChamber(_currentChamber);
        _puzzleManager.Initialize(_roomGrid, _currentChamber);
        if (redirectedFromLockedApex)
        {
            _puzzleManager.NotifyChamber10Locked();
        }

        if (_currentChamber is >= 5 and <= 7)
        {
            // Nivel 2 de la pirámide (Recámaras 5, 6 y 7): 4 plataformas (filas 14, 11, 8 y 4) y 3 momias activas
            _mummy.Configure(
                spawnX: 15 * GameConstants.TILE_SIZE,
                spawnY: 7 * GameConstants.TILE_SIZE,
                minX: 2 * GameConstants.TILE_SIZE,
                maxX: 18 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Right);

            int level1GapCol = _currentChamber == 6 ? 12 : 11;
            if (_currentChamber == 5)
            {
                // En Recámara 5, la primera momia patrulla en el Nivel 0 (fila 13, Y = 208)
                _mummyPlatform1.Configure(
                    spawnX: 16 * GameConstants.TILE_SIZE,
                    spawnY: 13 * GameConstants.TILE_SIZE,
                    minX: 4 * GameConstants.TILE_SIZE,
                    maxX: 18 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                    initialFacing: Direction.Left,
                    initiallyAwake: true,
                    initiallyActive: true);
            }
            else
            {
                _mummyPlatform1.Configure(
                    spawnX: 2 * GameConstants.TILE_SIZE,
                    spawnY: 10 * GameConstants.TILE_SIZE,
                    minX: 2 * GameConstants.TILE_SIZE,
                    maxX: level1GapCol * GameConstants.TILE_SIZE - Mummy.WIDTH,
                    initialFacing: Direction.Right,
                    initiallyAwake: false,
                    initiallyActive: true);
            }

            if (_currentChamber == 6)
            {
                _mummyPlatform3.Configure(
                    spawnX: 15 * GameConstants.TILE_SIZE,
                    spawnY: 3 * GameConstants.TILE_SIZE,
                    minX: 4 * GameConstants.TILE_SIZE,
                    maxX: 18 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                    initialFacing: Direction.Left,
                    initiallyAwake: true,
                    initiallyActive: true);
            }
            else
            {
                _mummyPlatform3.Configure(
                    spawnX: 14 * GameConstants.TILE_SIZE,
                    spawnY: 3 * GameConstants.TILE_SIZE,
                    minX: 2 * GameConstants.TILE_SIZE,
                    maxX: 16 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                    initialFacing: Direction.Left,
                    initiallyAwake: true,
                    initiallyActive: true);
            }
        }
        else if (_currentChamber is 8 or 9)
        {
            // Recámaras 8 y 9 (Nivel 3 de la pirámide: 5 plataformas y 3 puertas de tesoros)
            // Momia de Nivel 2 (piso en fila 8 -> spawnY = 7*16), a la izquierda de Puerta 3 (col 14)
            _mummy.Configure(
                spawnX: 11 * GameConstants.TILE_SIZE,
                spawnY: 7 * GameConstants.TILE_SIZE,
                minX: 2 * GameConstants.TILE_SIZE,
                maxX: 14 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Left);

            // Momia de Nivel 1 (piso en fila 11 -> spawnY = 10*16), entre Puerta 2 (col 4) y el foso (col 12)
            _mummyPlatform1.Configure(
                spawnX: 5 * GameConstants.TILE_SIZE,
                spawnY: 10 * GameConstants.TILE_SIZE,
                minX: 5 * GameConstants.TILE_SIZE,
                maxX: 12 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Right,
                initiallyAwake: false,
                initiallyActive: true);

            // Momia de Nivel 3 (piso en fila 5 -> spawnY = 4*16)
            if (_currentChamber == 8)
            {
                _mummyPlatform3.Configure(
                    spawnX: 16 * GameConstants.TILE_SIZE,
                    spawnY: 4 * GameConstants.TILE_SIZE,
                    minX: 9 * GameConstants.TILE_SIZE,
                    maxX: 18 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                    initialFacing: Direction.Left,
                    initiallyAwake: true,
                    initiallyActive: true);
            }
            else
            {
                _mummyPlatform3.Configure(
                    spawnX: 9 * GameConstants.TILE_SIZE,
                    spawnY: 4 * GameConstants.TILE_SIZE,
                    minX: 2 * GameConstants.TILE_SIZE,
                    maxX: 11 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                    initialFacing: Direction.Left,
                    initiallyAwake: true,
                    initiallyActive: true);
            }
        }
        else if (_currentChamber is 2 or 4)
        {
            // Recámaras con Nivel 2 en fila 6 (spawnY = 5*16) y Nivel 1 en fila 10 (spawnY = 9*16)
            _mummy.Configure(
                spawnX: 15 * GameConstants.TILE_SIZE,
                spawnY: 5 * GameConstants.TILE_SIZE,
                minX: 4 * GameConstants.TILE_SIZE,
                maxX: 18 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Right);

            int level1GapCol = _currentChamber == 2 ? 12 : 11;
            int reqJumpsLevel1 = _currentChamber switch
            {
                2 => 2,
                4 => 3,
                _ => Mummy.REQUIRED_JUMP_OVERS
            };
            int reqJumpsLevel0 = _currentChamber switch
            {
                2 => 2,
                4 => 1,
                _ => 0
            };

            _mummyPlatform1.Configure(
                spawnX: 2 * GameConstants.TILE_SIZE,
                spawnY: 9 * GameConstants.TILE_SIZE,
                minX: 2 * GameConstants.TILE_SIZE,
                maxX: level1GapCol * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Right,
                initiallyAwake: false,
                initiallyActive: true,
                requiredJumpOvers: reqJumpsLevel1,
                requiredLevel0JumpOvers: reqJumpsLevel0);

            _mummyPlatform3.Configure(
                spawnX: 14 * GameConstants.TILE_SIZE,
                spawnY: 3 * GameConstants.TILE_SIZE,
                minX: 2 * GameConstants.TILE_SIZE,
                maxX: 16 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Left,
                initiallyAwake: false,
                initiallyActive: false);
        }
        else
        {
            // Recámaras 1, 3, 9, 10 con Nivel 2 en fila 5 (spawnY = 4*16) y Nivel 1 en fila 9 (spawnY = 8*16)
            int level2GapCol = _currentChamber == 3 ? 15 : 16;
            _mummy.Configure(
                spawnX: (level2GapCol - 1) * GameConstants.TILE_SIZE,
                spawnY: 4 * GameConstants.TILE_SIZE,
                minX: 2 * GameConstants.TILE_SIZE,
                maxX: level2GapCol * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Right);

            int level1GapCol = 11;
            int reqJumpsLevel1 = _currentChamber == 3 ? 1 : Mummy.REQUIRED_JUMP_OVERS;
            int reqJumpsLevel0 = _currentChamber == 3 ? 3 : 0;

            _mummyPlatform1.Configure(
                spawnX: 2 * GameConstants.TILE_SIZE,
                spawnY: 8 * GameConstants.TILE_SIZE,
                minX: 2 * GameConstants.TILE_SIZE,
                maxX: level1GapCol * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Right,
                initiallyAwake: false,
                initiallyActive: true,
                requiredJumpOvers: reqJumpsLevel1,
                requiredLevel0JumpOvers: reqJumpsLevel0);

            _mummyPlatform3.Configure(
                spawnX: 14 * GameConstants.TILE_SIZE,
                spawnY: 3 * GameConstants.TILE_SIZE,
                minX: 2 * GameConstants.TILE_SIZE,
                maxX: 16 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Left,
                initiallyAwake: false,
                initiallyActive: false);
        }

        _player.SetPosition(
            _puzzleManager.PlayerSpawnCoord.X * GameConstants.TILE_SIZE + (GameConstants.TILE_SIZE - Player.WIDTH) / 2f,
            _puzzleManager.PlayerSpawnCoord.Y * GameConstants.TILE_SIZE);
    }

    /// <summary>
    /// Re-inicia la cámara actual restableciendo la cuadrícula, los puzles, la momia y la posición del jugador.
    /// </summary>
    public void RestartCurrentRoom()
    {
        LoadChamber(_currentChamber);
    }

    protected override void Draw(GameTime gameTime)
    {
        // 1. Inicia el pase de renderizado virtual sobre el RenderTarget2D (320x240)
        _virtualViewport.Begin(_spriteBatch);

        if (_currentScreen == GameScreen.TitleMenu)
        {
            // Dibuja el menú principal interactivo
            _titleMenu.Draw(_spriteBatch, _pixelFont, _frameCounter);
        }
        else if (_currentScreen == GameScreen.Playing)
        {
            // Dibuja el mural jeroglífico egipcio en el fondo (cero allocations)
            _hieroglyphRenderer.Draw(_spriteBatch, _currentChamber);

            // Dibuja las baldosas de la cámara (cero allocations)
            _tileRenderer.Draw(_spriteBatch, _roomGrid);

            // Dibuja los elementos del puzle (piedra conmutable, muro secreto y cofres de tesoro)
            _puzzleRenderer.Draw(_spriteBatch, _puzzleManager, _pixelFont, _frameCounter);

            // Dibuja a las momias enemigas
            _mummyRenderer.Draw(_spriteBatch, _mummy, _frameCounter);
            _mummyRenderer.Draw(_spriteBatch, _mummyPlatform1, _frameCounter);
            _mummyRenderer.Draw(_spriteBatch, _mummyPlatform3, _frameCounter);

            // Dibuja al arqueólogo con sus animaciones retro y orientación
            _playerRenderer.Draw(_spriteBatch, _player);

            // Placa HUD incrustada dentro del marco superior de la recámara (fila 0: Y = 0..15, fuera del área de juego)
            // Formato: RECAMARA: X   PUNTOS: XXXXXX   1: SALIR   2: RE-INICIAR
            int panelX = GameConstants.TILE_SIZE; // X = 16
            int panelY = 2;
            int panelW = GameConstants.VIRTUAL_WIDTH - (GameConstants.TILE_SIZE * 2); // 288 px
            int panelH = 12;

            _pixelFont.DrawFrameBox(_spriteBatch, panelX, panelY, panelW, panelH, HudBgColor, HudBorderColor);

            string roomTitle = HudRoomTitles[_currentChamber];
            _pixelFont.DrawMiniText(_spriteBatch, roomTitle, panelX + 7, panelY + 5, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, roomTitle, panelX + 6, panelY + 4, HudTitleColor);

            int ptsLabelX = panelX + 74;
            _pixelFont.DrawMiniText(_spriteBatch, HUD_POINTS_LABEL, ptsLabelX + 1, panelY + 5, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_POINTS_LABEL, ptsLabelX, panelY + 4, HudTextColor);
            int ptsValX = ptsLabelX + (HUD_POINTS_LABEL.Length * 5);
            _pixelFont.DrawMiniInt(_spriteBatch, _puzzleManager.Score, ptsValX + 1, panelY + 5, HudShadowColor);
            _pixelFont.DrawMiniInt(_spriteBatch, _puzzleManager.Score, ptsValX, panelY + 4, HudTitleColor);

            _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_EXIT, panelX + 159, panelY + 5, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_EXIT, panelX + 158, panelY + 4, HudTextColor);

            _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_RESTART, panelX + 217, panelY + 5, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_RESTART, panelX + 216, panelY + 4, HudTextColor);

            // Cuadro de estado en una única línea ubicado a la izquierda: [Icono Jugador]:  X, [Icono Llave]: XX
            int statusX = 18;
            int statusY = 18;
            int statusW = 80;
            int statusH = 16;
            int textY = statusY + 6;

            _pixelFont.DrawFrameBox(_spriteBatch, statusX, statusY, statusW, statusH, HudBgColor, HudBorderColor);

            // 1. [Icono Jugador]:  X
            _spriteBatch.Draw(_playerRenderer.IdleTexture, PlayerIconDestRect, PlayerIconSourceRect, Color.White);
            int livesColonX = statusX + 18;
            _pixelFont.DrawMiniText(_spriteBatch, HUD_LIVES_COLON, livesColonX + 1, textY + 1, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_LIVES_COLON, livesColonX, textY, HudTextColor);
            int livesValX = livesColonX + (HUD_LIVES_COLON.Length * 5);
            _pixelFont.DrawMiniInt(_spriteBatch, _player.Lives, livesValX + 1, textY + 1, HudShadowColor);
            _pixelFont.DrawMiniInt(_spriteBatch, _player.Lives, livesValX, textY, HudTextColor);

            // 2. [Icono Llave]: XX
            _spriteBatch.Draw(_puzzleRenderer.KeyTexture, KeyIconDestRect, KeyIconSourceRect, Color.White);
            int keyColonX = statusX + 54;
            _pixelFont.DrawMiniText(_spriteBatch, HUD_KEY_COLON, keyColonX + 1, textY + 1, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_KEY_COLON, keyColonX, textY, HudTextColor);
            string keyText = _puzzleManager.HasKey ? HUD_KEY_YES : HUD_KEY_NO;
            Color keyColor = _puzzleManager.HasKey ? new Color(255, 230, 80) : HudTextColor;
            int keyValX = keyColonX + (HUD_KEY_COLON.Length * 5);
            _pixelFont.DrawMiniText(_spriteBatch, keyText, keyValX + 1, textY + 1, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, keyText, keyValX, textY, keyColor);

            // Modal de Recámara Completada
            if (_puzzleManager.IsChamberCompleted)
            {
                int winW = 200;
                int winH = 88;
                int winX = (GameConstants.VIRTUAL_WIDTH - winW) / 2;
                int winY = (GameConstants.VIRTUAL_HEIGHT - winH) / 2;

                _pixelFont.DrawFrameBox(_spriteBatch, winX, winY, winW, winH, ChamberWinBgColor, ChamberWinBorderColor);

                string titleText = WinModalTitles[_currentChamber];
                _pixelFont.DrawMiniText(_spriteBatch, titleText, winX + 33, winY + 9, ChamberWinShadowColor);
                _pixelFont.DrawMiniText(_spriteBatch, titleText, winX + 32, winY + 8, ChamberWinTitleColor);

                // Puntos obtenidos
                _pixelFont.DrawMiniText(_spriteBatch, "PUNTOS OBTENIDOS: ", winX + 16, winY + 23, ChamberWinShadowColor);
                _pixelFont.DrawMiniText(_spriteBatch, "PUNTOS OBTENIDOS: ", winX + 15, winY + 22, ChamberWinSubColor);
                int winPtsX = winX + 15 + ("PUNTOS OBTENIDOS: ".Length * 5);
                _pixelFont.DrawMiniInt(_spriteBatch, _puzzleManager.Score, winPtsX + 1, winY + 23, ChamberWinShadowColor);
                _pixelFont.DrawMiniInt(_spriteBatch, _puzzleManager.Score, winPtsX, winY + 22, ChamberWinTitleColor);

                // Vidas restantes
                _pixelFont.DrawMiniText(_spriteBatch, "VIDAS RESTANTES:  ", winX + 16, winY + 33, ChamberWinShadowColor);
                _pixelFont.DrawMiniText(_spriteBatch, "VIDAS RESTANTES:  ", winX + 15, winY + 32, ChamberWinSubColor);
                int winLivesX = winX + 15 + ("VIDAS RESTANTES:  ".Length * 5);
                _pixelFont.DrawMiniInt(_spriteBatch, _player.Lives, winLivesX + 1, winY + 33, ChamberWinShadowColor);
                _pixelFont.DrawMiniInt(_spriteBatch, _player.Lives, winLivesX, winY + 32, ChamberWinTitleColor);

                // Opciones interactivas
                _pixelFont.DrawMiniText(_spriteBatch, "1: PASAR A OTRA RECAMARA", winX + 16, winY + 47, ChamberWinShadowColor);
                _pixelFont.DrawMiniText(_spriteBatch, "1: PASAR A OTRA RECAMARA", winX + 15, winY + 46, ChamberWinOptionColor);

                _pixelFont.DrawMiniText(_spriteBatch, "2: VOLVER A JUGAR", winX + 16, winY + 59, ChamberWinShadowColor);
                _pixelFont.DrawMiniText(_spriteBatch, "2: VOLVER A JUGAR", winX + 15, winY + 58, ChamberWinOptionColor);

                _pixelFont.DrawMiniText(_spriteBatch, "3: SALIR AL MENU", winX + 16, winY + 71, ChamberWinShadowColor);
                _pixelFont.DrawMiniText(_spriteBatch, "3: SALIR AL MENU", winX + 15, winY + 70, ChamberWinOptionColor);
            }
            // Modal de Game Over únicamente cuando el arqueólogo se queda sin vidas (0 vidas restantes)
            else if (_player.IsEliminated && _player.Lives == 0)
            {
                int elimW = 130;
                int elimH = 40;
                int elimX = (GameConstants.VIRTUAL_WIDTH - elimW) / 2;
                int elimY = (GameConstants.VIRTUAL_HEIGHT - elimH) / 2;

                _pixelFont.DrawFrameBox(_spriteBatch, elimX, elimY, elimW, elimH, EliminatedBgColor, EliminatedBorderColor);

                // Título centrado en 8x8 con sombra
                _pixelFont.DrawTextCentered(_spriteBatch, TEXT_GAME_OVER, GameConstants.VIRTUAL_WIDTH, elimY + 8, EliminatedShadowColor, scale: 1);
                _pixelFont.DrawTextCentered(_spriteBatch, TEXT_GAME_OVER, GameConstants.VIRTUAL_WIDTH - 2, elimY + 7, EliminatedTitleColor, scale: 1);

                // Solo la opción de salir al menú
                int opt1X = elimX + (elimW - (HUD_TEXT_MENU.Length * 5)) / 2;
                _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_MENU, opt1X + 1, elimY + 24, EliminatedShadowColor);
                _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_MENU, opt1X, elimY + 23, HudTextColor);
            }
        }

        // 2. Finaliza el dibujado virtual y proyecta a pantalla con SamplerState.PointClamp y letterbox 4:3
        _virtualViewport.End(_spriteBatch, GraphicsDevice);

        base.Draw(gameTime);
    }
}
