using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;
using RetroGamePiramid.Grid;
using RetroGamePiramid.Systems;

namespace RetroGamePiramid.Graphics;

/// <summary>
/// Renderiza visualmente la piedra rúnica, el muro conmutable, el cofre del tesoro y el HUD del puzle.
/// Cero asignaciones en memoria heap en cada llamada a Draw.
/// </summary>
public sealed class PuzzleRenderer : IDisposable
{
    public const int SCARAB_SIZE = 10;

    private static readonly Color ScarabBoxBgColor = new(16, 10, 22, 220);
    private static readonly Color ScarabBoxBorderColor = new(150, 110, 50);

    private readonly Texture2D _chestTexture;
    private readonly Texture2D _chestOpenTexture;
    private readonly Texture2D _wallClosedTexture;
    private readonly Texture2D _wallOpenTexture;
    private readonly Texture2D _stoneActiveTexture;
    private readonly Texture2D _keyTexture;
    private readonly Texture2D _trapClosedTexture;
    private readonly Texture2D _trapOpenTexture;
    private readonly Texture2D[] _scarabTextures = new Texture2D[4];
    private readonly Texture2D _scarabSlotTexture;

    private Rectangle _destRect = new(0, 0, GameConstants.TILE_SIZE, GameConstants.TILE_SIZE);
    private Rectangle _scarabDestRect = new(0, 0, SCARAB_SIZE, SCARAB_SIZE);
    private bool _isDisposed;

    public Texture2D KeyTexture => _keyTexture;

    public PuzzleRenderer(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _chestTexture = GenerateChestTexture(graphicsDevice);
        _chestOpenTexture = GenerateChestOpenTexture(graphicsDevice);
        _wallClosedTexture = GenerateWallClosedTexture(graphicsDevice);
        _wallOpenTexture = GenerateWallOpenTexture(graphicsDevice);
        _stoneActiveTexture = GenerateStoneActiveTexture(graphicsDevice);
        _keyTexture = GenerateKeyTexture(graphicsDevice);
        _trapClosedTexture = GenerateTrapClosedTexture(graphicsDevice);
        _trapOpenTexture = GenerateTrapOpenTexture(graphicsDevice);
        for (int rot = 0; rot < 4; rot++)
        {
            _scarabTextures[rot] = GenerateScarabTexture(graphicsDevice, rot);
        }
        _scarabSlotTexture = GenerateScarabSlotTexture(graphicsDevice);
    }

    /// <summary>
    /// Dibuja los elementos del puzle de la plataforma 0 y el estado de la misión.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, PuzzleManager puzzle, PixelFont font, int frameCounter)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(puzzle);
        ArgumentNullException.ThrowIfNull(font);

        // 1. Dibuja la piedra de activación con resplandor si está pisada o activa
        if (puzzle.IsWallOpen)
        {
            _destRect.X = puzzle.StoneCoord.X * GameConstants.TILE_SIZE;
            _destRect.Y = puzzle.StoneCoord.Y * GameConstants.TILE_SIZE;
            spriteBatch.Draw(_stoneActiveTexture, _destRect, Color.White);
        }

        // 2. Dibuja el muro conmutable
        _destRect.X = puzzle.WallCoord.X * GameConstants.TILE_SIZE;
        _destRect.Y = puzzle.WallCoord.Y * GameConstants.TILE_SIZE;

        if (puzzle.IsWallOpen)
        {
            spriteBatch.Draw(_wallOpenTexture, _destRect, Color.White);
        }
        else
        {
            spriteBatch.Draw(_wallClosedTexture, _destRect, Color.White);
        }

        // 2b. Dibuja las puertas y piedras adicionales para pasar a los tesoros (Recámaras 8 y 9)
        if (puzzle.HasMultipleTreasureDoors)
        {
            if (puzzle.IsWall2Open)
            {
                _destRect.X = puzzle.Stone2Coord.X * GameConstants.TILE_SIZE;
                _destRect.Y = puzzle.Stone2Coord.Y * GameConstants.TILE_SIZE;
                spriteBatch.Draw(_stoneActiveTexture, _destRect, Color.White);
            }

            _destRect.X = puzzle.Wall2Coord.X * GameConstants.TILE_SIZE;
            _destRect.Y = puzzle.Wall2Coord.Y * GameConstants.TILE_SIZE;
            spriteBatch.Draw(puzzle.IsWall2Open ? _wallOpenTexture : _wallClosedTexture, _destRect, Color.White);

            if (puzzle.IsWall3Open)
            {
                _destRect.X = puzzle.Stone3Coord.X * GameConstants.TILE_SIZE;
                _destRect.Y = puzzle.Stone3Coord.Y * GameConstants.TILE_SIZE;
                spriteBatch.Draw(_stoneActiveTexture, _destRect, Color.White);
            }

            _destRect.X = puzzle.Wall3Coord.X * GameConstants.TILE_SIZE;
            _destRect.Y = puzzle.Wall3Coord.Y * GameConstants.TILE_SIZE;
            spriteBatch.Draw(puzzle.IsWall3Open ? _wallOpenTexture : _wallClosedTexture, _destRect, Color.White);
        }

        // 3. Dibuja los cofres del tesoro (Plataforma 0 y Plataforma Nivel 2, y nuevos tesoros si fueron generados)
        if (puzzle.IsTreasure4Spawned)
        {
            DrawChest(spriteBatch, puzzle.Treasure4, puzzle.Treasure4Coord, frameCounter);
        }
        else if (puzzle.IsTreasure3Spawned)
        {
            if (puzzle.Treasure.IsCollected)
            {
                DrawChest(spriteBatch, puzzle.Treasure3, puzzle.Treasure3Coord, frameCounter);
            }
            else
            {
                DrawChest(spriteBatch, puzzle.Treasure, puzzle.TreasureCoord, frameCounter);
            }
        }
        else
        {
            DrawChest(spriteBatch, puzzle.Treasure, puzzle.TreasureCoord, frameCounter);
        }
        DrawChest(spriteBatch, puzzle.Treasure2, puzzle.Treasure2Coord, frameCounter);
        if (puzzle.HasExtraTreasure)
        {
            if (puzzle.IsTreasure6Spawned)
            {
                DrawChest(spriteBatch, puzzle.Treasure6, puzzle.Treasure6Coord, frameCounter);
            }
            else
            {
                DrawChest(spriteBatch, puzzle.Treasure5, puzzle.Treasure5Coord, frameCounter);
            }
        }

        // 4. Dibuja la llave dorada colgada si no ha sido recogida
        DrawKey(spriteBatch, puzzle.Key, puzzle.KeyCoord, frameCounter);

        // 5. Dibuja la trampa de suelo bajo la llave (abierta o cerrada)
        DrawTrap(spriteBatch, puzzle.Trap, puzzle.TrapCoord);

        // 5b. Dibuja la trampa de suelo en Nivel 2 (abierta o cerrada) si está habilitada
        if (puzzle.HasTrap2)
        {
            DrawTrap(spriteBatch, puzzle.Trap2, puzzle.Trap2Coord);
        }

        // 5c. Dibuja la trampa de suelo en Nivel 3 (abierta o cerrada) si está habilitada
        if (puzzle.HasTrap3)
        {
            DrawTrap(spriteBatch, puzzle.Trap3, puzzle.Trap3Coord);
        }

        // 5d. Dibuja el recuadro compacto de escarabajos que indica los puzles restantes de la recámara
        DrawScarabPuzzleBox(spriteBatch, puzzle, font, frameCounter);

        // 6. Cartel de notificación (tesoro encontrado, llave obtenida o puerta bloqueada)
        if (puzzle.NotificationTimer > 0 && !string.IsNullOrEmpty(puzzle.NotificationMessage))
        {
            // Sombra y texto
            Color noticeColor = new(255, 230, 80);
            Color shadowColor = new(40, 20, 10);
            font.DrawTextCentered(spriteBatch, puzzle.NotificationMessage, GameConstants.VIRTUAL_WIDTH + 1, 97, shadowColor, scale: 1);
            font.DrawTextCentered(spriteBatch, puzzle.NotificationMessage, GameConstants.VIRTUAL_WIDTH, 96, noticeColor, scale: 1);
        }
    }

    private void DrawTrap(SpriteBatch spriteBatch, FloorTrap trap, GridCoord coord)
    {
        _destRect.X = coord.X * GameConstants.TILE_SIZE;
        _destRect.Y = coord.Y * GameConstants.TILE_SIZE;

        if (trap.IsOpen)
        {
            spriteBatch.Draw(_trapOpenTexture, _destRect, Color.White);
        }
        else
        {
            spriteBatch.Draw(_trapClosedTexture, _destRect, Color.White);
        }
    }

    private void DrawChest(SpriteBatch spriteBatch, Treasure treasure, GridCoord coord, int frameCounter)
    {
        _destRect.X = coord.X * GameConstants.TILE_SIZE;
        _destRect.Y = coord.Y * GameConstants.TILE_SIZE;

        if (!treasure.IsCollected)
        {
            // Efecto sutil de flotación/brillo retro
            bool glow = (frameCounter / 10) % 2 == 0;
            Color chestTint = glow ? Color.White : new Color(255, 235, 170);
            spriteBatch.Draw(_chestTexture, _destRect, chestTint);
        }
        else
        {
            spriteBatch.Draw(_chestOpenTexture, _destRect, Color.White);
        }
    }

    private void DrawKey(SpriteBatch spriteBatch, Key key, GridCoord coord, int frameCounter)
    {
        if (key.IsCollected)
            return;

        _destRect.X = coord.X * GameConstants.TILE_SIZE;
        _destRect.Y = coord.Y * GameConstants.TILE_SIZE;

        // Efecto retro de resplandor para la llave sagrada
        bool glow = (frameCounter / 10) % 2 == 0;
        Color keyTint = glow ? Color.White : new Color(255, 235, 140);
        spriteBatch.Draw(_keyTexture, _destRect, keyTint);
    }

    private void DrawScarabPuzzleBox(SpriteBatch spriteBatch, PuzzleManager puzzle, PixelFont font, int frameCounter)
    {
        int totalPuzzles = puzzle.TotalChamberPuzzles;
        int boxX = 102;
        int boxY = 18;
        int boxW = 6 + (totalPuzzles * 12);
        int boxH = 16;

        font.DrawFrameBox(spriteBatch, boxX, boxY, boxW, boxH, ScarabBoxBgColor, ScarabBoxBorderColor);

        for (int i = 0; i < totalPuzzles; i++)
        {
            int slotX = boxX + 4 + (i * 12);
            int slotY = boxY + 3;

            // Dibujar la hendidura tallada del escarabajo en el recuadro
            _scarabDestRect.X = slotX;
            _scarabDestRect.Y = slotY;
            _scarabDestRect.Width = SCARAB_SIZE;
            _scarabDestRect.Height = SCARAB_SIZE;
            spriteBatch.Draw(_scarabSlotTexture, _scarabDestRect, Color.White);

            if (puzzle.IsScarabVisible(i))
            {
                int spinTimer = puzzle.GetScarabSpinTimer(i);
                if (spinTimer > 0)
                {
                    // El escarabajo gira rápidamente sobre su eje y se encoge al final antes de desaparecer
                    int elapsed = PuzzleManager.SCARAB_SPIN_DURATION_FRAMES - spinTimer;
                    int rotStep = (elapsed / 3) % 4;
                    int size = spinTimer <= 16 ? Math.Max(2, (spinTimer * SCARAB_SIZE) / 16) : SCARAB_SIZE;
                    int offset = (SCARAB_SIZE - size) / 2;

                    _scarabDestRect.X = slotX + offset;
                    _scarabDestRect.Y = slotY + offset;
                    _scarabDestRect.Width = size;
                    _scarabDestRect.Height = size;

                    Color spinTint = (elapsed % 4 < 2) ? Color.White : new Color(255, 240, 120);
                    spriteBatch.Draw(_scarabTextures[rotStep], _scarabDestRect, spinTint);
                }
                else
                {
                    bool shimmer = ((frameCounter + (i * 7)) / 18) % 2 == 0;
                    Color idleTint = shimmer ? Color.White : new Color(240, 225, 175);
                    spriteBatch.Draw(_scarabTextures[0], _scarabDestRect, idleTint);
                }
            }
        }
    }

    private static Texture2D GenerateChestTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color goldLight = new(255, 225, 90);
        Color goldBase = new(220, 180, 50);
        Color goldDark = new(160, 120, 30);
        Color ruby = new(240, 40, 50);
        Color jewelCyan = new(40, 220, 240);
        Color woodBase = new(95, 55, 25);
        Color lockDark = new(50, 30, 15);

        for (int y = 5; y <= 15; y++)
        {
            for (int x = 2; x <= 13; x++)
            {
                int index = y * 16 + x;

                // Tapa abombada del cofre (filas 5 a 8)
                if (y == 5 && x >= 4 && x <= 11)
                {
                    pixels[index] = goldLight;
                }
                else if (y == 6 && x >= 3 && x <= 12)
                {
                    pixels[index] = (x == 3 || x == 12) ? goldDark : goldLight;
                }
                else if (y == 7 || y == 8)
                {
                    if (x == 2 || x == 13)
                        pixels[index] = goldDark;
                    else if (x == 7 || x == 8)
                        pixels[index] = ruby; // Gema central
                    else
                        pixels[index] = goldBase;
                }
                // Cuerpo del cofre (filas 9 a 15)
                else if (y >= 9)
                {
                    if (x == 2 || x == 13 || y == 9 || y == 15)
                    {
                        pixels[index] = goldDark;
                    }
                    else if (y == 11 && (x == 7 || x == 8))
                    {
                        pixels[index] = jewelCyan; // Cerradura mágica de zafiro
                    }
                    else if (y == 12 && (x == 7 || x == 8))
                    {
                        pixels[index] = lockDark;
                    }
                    else
                    {
                        pixels[index] = woodBase;
                    }
                }
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateChestOpenTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color goldDark = new(160, 120, 30);
        Color goldLight = new(240, 200, 70);
        Color woodDark = new(65, 38, 18);
        Color emptyInside = new(35, 20, 10);

        for (int y = 7; y <= 15; y++)
        {
            for (int x = 2; x <= 13; x++)
            {
                int index = y * 16 + x;

                if (y == 7 && x >= 3 && x <= 12)
                {
                    pixels[index] = goldLight; // Tapa abierta alzada
                }
                else if (y >= 8 && y <= 10 && x >= 3 && x <= 12)
                {
                    pixels[index] = emptyInside; // Interior vacío
                }
                else if (x == 2 || x == 13 || y == 15)
                {
                    pixels[index] = goldDark;
                }
                else
                {
                    pixels[index] = woodDark;
                }
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateWallClosedTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];

        Color stoneLight = new(210, 160, 75);
        Color stoneBase = new(165, 115, 45);
        Color stoneDark = new(110, 70, 25);
        Color runeCyan = new(60, 220, 255);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int index = y * 16 + x;

                if (x == 0 || y == 0)
                    pixels[index] = stoneLight;
                else if (x == 15 || y == 15)
                    pixels[index] = stoneDark;
                // Ankh / Glifo místico en el centro de la puerta
                else if (y == 4 && (x >= 6 && x <= 9))
                    pixels[index] = runeCyan;
                else if ((y == 5 || y == 6) && (x == 6 || x == 9))
                    pixels[index] = runeCyan;
                else if (y == 7 && (x >= 6 && x <= 9))
                    pixels[index] = runeCyan;
                else if ((y == 8 || y == 9 || y == 12) && (x == 7 || x == 8))
                    pixels[index] = runeCyan;
                else if (y == 10 && (x >= 4 && x <= 11))
                    pixels[index] = runeCyan;
                else if (y == 11 && (x == 7 || x == 8))
                    pixels[index] = runeCyan;
                else
                    pixels[index] = stoneBase;
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateWallOpenTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color archGold = new(190, 145, 60);
        Color runeGlow = new(140, 245, 255);

        // Marco lateral del pasaje abierto
        for (int y = 0; y < 16; y++)
        {
            pixels[y * 16 + 0] = archGold;
            pixels[y * 16 + 1] = runeGlow;
            pixels[y * 16 + 14] = runeGlow;
            pixels[y * 16 + 15] = archGold;
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateStoneActiveTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color plateActive = new(255, 230, 140);
        Color jewelRadiant = new(200, 255, 255);
        Color jewelCyan = new(50, 240, 255);
        Color foundation = new(90, 60, 30);

        for (int y = 11; y < 16; y++)
        {
            for (int x = 2; x <= 13; x++)
            {
                int index = y * 16 + x;

                if (y == 11)
                {
                    pixels[index] = (x == 7 || x == 8) ? jewelRadiant : plateActive;
                }
                else if (y == 12)
                {
                    pixels[index] = (x >= 6 && x <= 9) ? jewelCyan : plateActive;
                }
                else if (y >= 13)
                {
                    pixels[index] = foundation;
                }
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateKeyTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color chainLink = new(170, 140, 70);
        Color chainDark = new(90, 70, 30);
        Color goldLight = new(255, 235, 100);
        Color goldBase = new(220, 180, 50);
        Color goldDark = new(150, 110, 30);
        Color rubyGlow = new(240, 50, 60);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int index = y * 16 + x;

                // Eslabones de cadena colgando del techo (filas 0 a 4)
                if (y <= 4 && (x == 7 || x == 8))
                {
                    pixels[index] = (y % 2 == 0) ? chainLink : chainDark;
                }
                // Anilla superior de la llave / ankh (filas 5 a 9)
                else if (y >= 5 && y <= 9)
                {
                    if (y == 5 && x >= 6 && x <= 9)
                        pixels[index] = goldLight;
                    else if (y == 9 && x >= 6 && x <= 9)
                        pixels[index] = goldBase;
                    else if ((x == 5 || x == 10) && y >= 6 && y <= 8)
                        pixels[index] = (x == 5) ? goldLight : goldDark;
                    else if ((x == 7 || x == 8) && y == 7)
                        pixels[index] = rubyGlow; // Gema incrustada en la anilla
                }
                // Vástago de la llave (filas 10 a 15)
                else if (y >= 10 && y <= 15)
                {
                    if (x == 7)
                        pixels[index] = goldLight;
                    else if (x == 8)
                        pixels[index] = goldBase;
                    // Dientes / guarda de la llave egipcia (filas 12 y 14)
                    else if ((y == 12 || y == 14) && (x == 9 || x == 10))
                        pixels[index] = goldLight;
                    else if ((y == 13 || y == 15) && x == 10)
                        pixels[index] = goldDark;
                }
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateTrapClosedTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];

        Color stoneLight = new(215, 175, 105);
        Color stoneBase = new(185, 145, 75);
        Color stoneDark = new(135, 100, 45);
        Color ironHinge = new(100, 105, 115);
        Color ironRivet = new(160, 165, 175);
        Color seamLine = new(70, 50, 20);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int index = y * 16 + x;

                if (y == 0 || x == 0)
                {
                    pixels[index] = stoneLight;
                }
                else if (y == 15 || x == 15)
                {
                    pixels[index] = stoneDark;
                }
                else if (x == 7 || x == 8)
                {
                    pixels[index] = seamLine;
                }
                else if ((x >= 2 && x <= 4 && y >= 2 && y <= 4) || (x >= 11 && x <= 13 && y >= 2 && y <= 4))
                {
                    pixels[index] = (x == 3 && y == 3) || (x == 12 && y == 3) ? ironRivet : ironHinge;
                }
                else if ((x == 3 && y == 10) || (x == 4 && y == 11) || (x == 12 && y == 9) || (x == 11 && y == 10))
                {
                    pixels[index] = seamLine;
                }
                else
                {
                    pixels[index] = (x + y) % 5 == 0 ? stoneLight : stoneBase;
                }
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateTrapOpenTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color stoneDark = new(90, 65, 30);
        Color stoneBase = new(140, 105, 50);
        Color ironHinge = new(70, 75, 85);
        Color pitShadow = new(10, 5, 10, 180);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int index = y * 16 + x;

                if (x <= 1 && y >= 3)
                {
                    pixels[index] = (x == 0) ? stoneDark : stoneBase;
                }
                else if (x >= 14 && y >= 3)
                {
                    pixels[index] = (x == 15) ? stoneDark : stoneBase;
                }
                else if ((x == 2 || x == 3 || x == 12 || x == 13) && (y == 0 || y == 1))
                {
                    pixels[index] = ironHinge;
                }
                else if (y <= 2)
                {
                    pixels[index] = pitShadow;
                }
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateScarabTexture(GraphicsDevice graphicsDevice, int rotationStep)
    {
        Texture2D texture = new(graphicsDevice, SCARAB_SIZE, SCARAB_SIZE);
        texture.SetData(GenerateScarabPixels(rotationStep));
        return texture;
    }

    private static Texture2D GenerateScarabSlotTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, SCARAB_SIZE, SCARAB_SIZE);
        texture.SetData(GenerateScarabSlotPixels());
        return texture;
    }

    /// <summary>
    /// Genera la matriz de píxeles (10x10) de un escarabajo egipcio sagrado (Jepri) en una de sus 4 rotaciones (0=0°, 1=90°, 2=180°, 3=270°).
    /// Desacoplado de GraphicsDevice para permitir pruebas unitarias.
    /// </summary>
    public static Color[] GenerateScarabPixels(int rotationStep = 0)
    {
        Color[] basePixels = new Color[SCARAB_SIZE * SCARAB_SIZE];
        Array.Fill(basePixels, Color.Transparent);

        Color goldBright = new(255, 225, 85);
        Color goldBase = new(215, 170, 45);
        Color goldDark = new(135, 95, 25);
        Color turquoiseLight = new(70, 215, 195);
        Color turquoiseDark = new(30, 140, 130);
        Color legsColor = new(165, 120, 35);

        for (int y = 0; y < SCARAB_SIZE; y++)
        {
            for (int x = 0; x < SCARAB_SIZE; x++)
            {
                int idx = y * SCARAB_SIZE + x;

                // Antenas / mandíbulas superiores (fila 0)
                if (y == 0 && (x == 3 || x == 6))
                {
                    basePixels[idx] = goldBright;
                }
                // Cabeza del escarabajo (fila 1)
                else if (y == 1 && x >= 3 && x <= 6)
                {
                    basePixels[idx] = (x == 4 || x == 5) ? goldBright : goldDark;
                }
                // Tórax / pronoto dorado (filas 2 y 3)
                else if ((y == 2 || y == 3) && x >= 2 && x <= 7)
                {
                    if (x == 2 || x == 7)
                        basePixels[idx] = goldDark;
                    else if (y == 2 && (x == 4 || x == 5))
                        basePixels[idx] = goldBright;
                    else
                        basePixels[idx] = goldBase;
                }
                // Élitros (caparazón de turquesa y oro, filas 4 a 7)
                else if (y >= 4 && y <= 7 && x >= 2 && x <= 7)
                {
                    if (x == 2 || x == 7)
                        basePixels[idx] = goldBase;
                    else if (x == 4 || x == 5)
                        basePixels[idx] = (y == 4) ? goldBright : turquoiseLight;
                    else
                        basePixels[idx] = turquoiseDark;
                }
                // Extremo inferior redondeado del caparazón (fila 8)
                else if (y == 8 && x >= 3 && x <= 6)
                {
                    basePixels[idx] = (x == 4 || x == 5) ? turquoiseDark : goldDark;
                }
                // Patas laterales (filas 2, 5 y 7 en columnas 0..1 y 8..9)
                else if ((y == 2 || y == 5 || y == 7) && (x == 1 || x == 8))
                {
                    basePixels[idx] = legsColor;
                }
                else if ((y == 1 || y == 5 || y == 8) && (x == 0 || x == 9))
                {
                    basePixels[idx] = goldDark;
                }
            }
        }

        int normRot = ((rotationStep % 4) + 4) % 4;
        if (normRot == 0)
        {
            return basePixels;
        }

        Color[] rotated = new Color[SCARAB_SIZE * SCARAB_SIZE];
        for (int y = 0; y < SCARAB_SIZE; y++)
        {
            for (int x = 0; x < SCARAB_SIZE; x++)
            {
                Color c = basePixels[y * SCARAB_SIZE + x];
                int rx = normRot switch
                {
                    1 => SCARAB_SIZE - 1 - y,
                    2 => SCARAB_SIZE - 1 - x,
                    _ => y
                };
                int ry = normRot switch
                {
                    1 => x,
                    2 => SCARAB_SIZE - 1 - y,
                    _ => SCARAB_SIZE - 1 - x
                };
                rotated[ry * SCARAB_SIZE + rx] = c;
            }
        }

        return rotated;
    }

    /// <summary>
    /// Genera la matriz de píxeles (10x10) del hueco tallado en piedra donde reposa cada escarabajo.
    /// </summary>
    public static Color[] GenerateScarabSlotPixels()
    {
        Color[] pixels = new Color[SCARAB_SIZE * SCARAB_SIZE];
        Array.Fill(pixels, Color.Transparent);

        Color recessShadow = new(10, 6, 14, 190);
        Color recessCenter = new(24, 16, 28, 170);

        for (int y = 1; y <= 8; y++)
        {
            for (int x = 2; x <= 7; x++)
            {
                if ((y == 1 || y == 8) && (x == 2 || x == 7))
                    continue;

                pixels[y * SCARAB_SIZE + x] = (y == 1 || x == 2) ? recessShadow : recessCenter;
            }
        }

        return pixels;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _scarabSlotTexture?.Dispose();
            for (int i = 0; i < _scarabTextures.Length; i++)
            {
                _scarabTextures[i]?.Dispose();
            }
            _trapOpenTexture?.Dispose();
            _trapClosedTexture?.Dispose();
            _keyTexture?.Dispose();
            _chestTexture?.Dispose();
            _chestOpenTexture?.Dispose();
            _wallClosedTexture?.Dispose();
            _wallOpenTexture?.Dispose();
            _stoneActiveTexture?.Dispose();
            _isDisposed = true;
        }
    }
}
