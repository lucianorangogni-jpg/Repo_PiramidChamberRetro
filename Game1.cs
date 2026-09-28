using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
    private Mummy _mummy = null!;
    private MummyRenderer _mummyRenderer = null!;

    private GameScreen _currentScreen = GameScreen.TitleMenu;
    private int _frameCounter;
    private KeyboardState _prevKeyboardState;
    private int _currentChamber = 1;

    private const string HUD_ROOM_NAME = "RECAMARA 1";
    private const string HUD_TEXT_MENU = "1: SALIR AL MENU";
    private const string HUD_TEXT_RESTART = "2: RE-INICIAR";
    private const string HUD_TEXT_CONTINUE = "2: CONTINUAR";
    private const string HUD_STATUS_TITLE = "ESTADO";
    private const string HUD_POINTS_LABEL = "PUNTOS: ";
    private const string HUD_LIVES_LABEL = "VIDAS:  ";
    private const string HUD_KEY_LABEL = "LLAVE:  ";
    private const string HUD_KEY_YES = "SI";
    private const string HUD_KEY_NO = "NO";
    private const string TEXT_ELIMINATED = "ELIMINADO";
    private const string TEXT_GAME_OVER = "GAME OVER";

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

        // Inicialización del arqueólogo y su renderizador pixel-art
        _player = new Player(new GridCoord(2, 13));
        _playerRenderer = new PlayerRenderer(GraphicsDevice);

        // Inicialización de la momia (enemigo patrullero en plataforma 2) y su renderizador
        _mummy = new Mummy(
            spawnX: 13 * GameConstants.TILE_SIZE,
            spawnY: 4 * GameConstants.TILE_SIZE,
            minX: 4 * GameConstants.TILE_SIZE,
            maxX: 18 * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right);
        _mummyRenderer = new MummyRenderer(GraphicsDevice);
    }

    protected override void UnloadContent()
    {
        _mummyRenderer?.Dispose();
        _puzzleRenderer?.Dispose();
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

        if (_currentScreen == GameScreen.TitleMenu)
        {
            bool up = keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W);
            bool down = keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S);
            bool confirm = keyboard.IsKeyDown(Keys.Enter) || keyboard.IsKeyDown(Keys.Space);

            if (_titleMenu.Update(up, down, confirm, out MenuOption selectedAction))
            {
                switch (selectedAction)
                {
                    case MenuOption.NewGame:
                        // Reiniciar vidas, puntaje, cámara, puzles y posición del arqueólogo
                        _currentChamber = 1;
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
                    // 1: Pasar a otra recámara (alternar entre Recámara 1 y Recámara 2)
                    int nextChamber = _currentChamber == 1 ? 2 : 1;
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
                _currentScreen = GameScreen.TitleMenu;
            }
            // Toque 2: Re-iniciar la pantalla (solo si el jugador aún tiene vidas)
            else if (pressMenu2 && _player.Lives > 0)
            {
                RestartCurrentRoom();
            }
            else if (!_player.IsEliminated)
            {
                // Muestreo de entrada y actualización del jugador, momia y puzle (cero allocations)
                PlayerInput input = PlayerInput.FromKeyboard(keyboard);
                _player.Update(_roomGrid, in input);
                _mummy.Update(_player);
                _puzzleManager.Update(_roomGrid, _player);
            }
        }

        _prevKeyboardState = keyboard;
        base.Update(gameTime);
    }

    /// <summary>
    /// Configura y carga una recámara específica por su identificador numérico.
    /// </summary>
    public void LoadChamber(int chamberNumber)
    {
        _currentChamber = chamberNumber;
        _roomGrid.LoadChamber(chamberNumber);
        _puzzleManager.Initialize(_roomGrid);
        if (chamberNumber == 2)
        {
            _mummy.Configure(
                spawnX: 10 * GameConstants.TILE_SIZE,
                spawnY: 5 * GameConstants.TILE_SIZE,
                minX: 4 * GameConstants.TILE_SIZE,
                maxX: 16 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Right);
        }
        else
        {
            _mummy.Configure(
                spawnX: 13 * GameConstants.TILE_SIZE,
                spawnY: 4 * GameConstants.TILE_SIZE,
                minX: 4 * GameConstants.TILE_SIZE,
                maxX: 18 * GameConstants.TILE_SIZE - Mummy.WIDTH,
                initialFacing: Direction.Right);
        }
        _player.SetPosition(2 * GameConstants.TILE_SIZE + (GameConstants.TILE_SIZE - Player.WIDTH) / 2f, 13 * GameConstants.TILE_SIZE);
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
            // Dibuja las baldosas de la cámara (cero allocations)
            _tileRenderer.Draw(_spriteBatch, _roomGrid);

            // Dibuja los elementos del puzle (piedra conmutable, muro secreto y cofres de tesoro)
            _puzzleRenderer.Draw(_spriteBatch, _puzzleManager, _pixelFont, _frameCounter);

            // Dibuja a la momia enemiga patrullando la plataforma nivel 2
            _mummyRenderer.Draw(_spriteBatch, _mummy, _frameCounter);

            // Dibuja al arqueólogo con sus animaciones retro y orientación
            _playerRenderer.Draw(_spriteBatch, _player);

            // Marco retro para las opciones en pantalla (lateral izquierdo)
            int panelX = 18;
            int panelY = 18;
            int panelW = 90;
            int panelH = 28;

            _pixelFont.DrawFrameBox(_spriteBatch, panelX, panelY, panelW, panelH, HudBgColor, HudBorderColor);

            string roomTitle = _currentChamber == 1 ? "RECAMARA 1" : "RECAMARA 2";
            _pixelFont.DrawMiniText(_spriteBatch, roomTitle, panelX + 5, panelY + 4, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, roomTitle, panelX + 4, panelY + 3, HudTitleColor);

            _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_MENU, panelX + 5, panelY + 12, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_MENU, panelX + 4, panelY + 11, HudTextColor);

            _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_RESTART, panelX + 5, panelY + 20, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_RESTART, panelX + 4, panelY + 19, HudTextColor);

            // Panel HUD derecho: Estado de la partida (Puntaje, Vidas y Llave)
            int rightPanelW = 90;
            int rightPanelH = 36;
            int rightPanelX = GameConstants.VIRTUAL_WIDTH - 18 - rightPanelW; // 320 - 18 - 90 = 212
            int rightPanelY = 18;

            _pixelFont.DrawFrameBox(_spriteBatch, rightPanelX, rightPanelY, rightPanelW, rightPanelH, HudBgColor, HudBorderColor);

            _pixelFont.DrawMiniText(_spriteBatch, HUD_STATUS_TITLE, rightPanelX + 5, rightPanelY + 4, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_STATUS_TITLE, rightPanelX + 4, rightPanelY + 3, HudTitleColor);

            _pixelFont.DrawMiniText(_spriteBatch, HUD_POINTS_LABEL, rightPanelX + 5, rightPanelY + 12, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_POINTS_LABEL, rightPanelX + 4, rightPanelY + 11, HudTextColor);
            int ptsValX = rightPanelX + 4 + (HUD_POINTS_LABEL.Length * 5);
            _pixelFont.DrawMiniInt(_spriteBatch, _puzzleManager.Score, ptsValX + 1, rightPanelY + 12, HudShadowColor);
            _pixelFont.DrawMiniInt(_spriteBatch, _puzzleManager.Score, ptsValX, rightPanelY + 11, HudTextColor);

            _pixelFont.DrawMiniText(_spriteBatch, HUD_LIVES_LABEL, rightPanelX + 5, rightPanelY + 20, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_LIVES_LABEL, rightPanelX + 4, rightPanelY + 19, HudTextColor);
            int livesValX = rightPanelX + 4 + (HUD_LIVES_LABEL.Length * 5);
            _pixelFont.DrawMiniInt(_spriteBatch, _player.Lives, livesValX + 1, rightPanelY + 20, HudShadowColor);
            _pixelFont.DrawMiniInt(_spriteBatch, _player.Lives, livesValX, rightPanelY + 19, HudTextColor);

            _pixelFont.DrawMiniText(_spriteBatch, HUD_KEY_LABEL, rightPanelX + 5, rightPanelY + 28, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, HUD_KEY_LABEL, rightPanelX + 4, rightPanelY + 27, HudTextColor);
            string keyText = _puzzleManager.HasKey ? HUD_KEY_YES : HUD_KEY_NO;
            Color keyColor = _puzzleManager.HasKey ? new Color(255, 230, 80) : HudTextColor;
            int keyValX = rightPanelX + 4 + (HUD_KEY_LABEL.Length * 5);
            _pixelFont.DrawMiniText(_spriteBatch, keyText, keyValX + 1, rightPanelY + 28, HudShadowColor);
            _pixelFont.DrawMiniText(_spriteBatch, keyText, keyValX, rightPanelY + 27, keyColor);

            // Modal de Recámara Completada
            if (_puzzleManager.IsChamberCompleted)
            {
                int winW = 200;
                int winH = 88;
                int winX = (GameConstants.VIRTUAL_WIDTH - winW) / 2;
                int winY = (GameConstants.VIRTUAL_HEIGHT - winH) / 2;

                _pixelFont.DrawFrameBox(_spriteBatch, winX, winY, winW, winH, ChamberWinBgColor, ChamberWinBorderColor);

                string titleText = _currentChamber == 1 ? "!RECAMARA 1 COMPLETADA!" : "!RECAMARA 2 COMPLETADA!";
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
            // Modal de Eliminación / Game Over si el arqueólogo sufrió una caída fatal
            else if (_player.IsEliminated)
            {
                bool isGameOver = _player.Lives == 0;
                int elimW = 130;
                int elimH = isGameOver ? 40 : 48;
                int elimX = (GameConstants.VIRTUAL_WIDTH - elimW) / 2;
                int elimY = (GameConstants.VIRTUAL_HEIGHT - elimH) / 2;

                _pixelFont.DrawFrameBox(_spriteBatch, elimX, elimY, elimW, elimH, EliminatedBgColor, EliminatedBorderColor);

                string modalTitle = isGameOver ? TEXT_GAME_OVER : TEXT_ELIMINATED;

                // Título centrado en 8x8 con sombra
                _pixelFont.DrawTextCentered(_spriteBatch, modalTitle, GameConstants.VIRTUAL_WIDTH, elimY + 8, EliminatedShadowColor, scale: 1);
                _pixelFont.DrawTextCentered(_spriteBatch, modalTitle, GameConstants.VIRTUAL_WIDTH - 2, elimY + 7, EliminatedTitleColor, scale: 1);

                if (isGameOver)
                {
                    // Solo la opción de salir al menú
                    int opt1X = elimX + (elimW - (HUD_TEXT_MENU.Length * 5)) / 2;
                    _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_MENU, opt1X + 1, elimY + 24, EliminatedShadowColor);
                    _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_MENU, opt1X, elimY + 23, HudTextColor);
                }
                else
                {
                    // Opciones centradas de salida o reinicio con vidas restantes
                    int opt1X = elimX + (elimW - (HUD_TEXT_MENU.Length * 5)) / 2;
                    _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_MENU, opt1X + 1, elimY + 23, EliminatedShadowColor);
                    _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_MENU, opt1X, elimY + 22, HudTextColor);

                    int opt2X = elimX + (elimW - (HUD_TEXT_CONTINUE.Length * 5)) / 2;
                    _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_CONTINUE, opt2X + 1, elimY + 33, EliminatedShadowColor);
                    _pixelFont.DrawMiniText(_spriteBatch, HUD_TEXT_CONTINUE, opt2X, elimY + 32, HudTextColor);
                }
            }
        }

        // 2. Finaliza el dibujado virtual y proyecta a pantalla con SamplerState.PointClamp y letterbox 4:3
        _virtualViewport.End(_spriteBatch, GraphicsDevice);

        base.Draw(gameTime);
    }
}
