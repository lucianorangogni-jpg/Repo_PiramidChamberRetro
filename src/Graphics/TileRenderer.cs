using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RetroGamePiramid.Core;
using RetroGamePiramid.Grid;

namespace RetroGamePiramid.Graphics;

/// <summary>
/// Renderiza las baldosas de la cámara mediante texturas procedurales 16x16 de pixel-art retro.
/// Cero asignaciones en memoria (0 allocations) en cada llamada a Draw.
/// </summary>
public sealed class TileRenderer : IDisposable
{
    private readonly Texture2D _wallTexture;
    private readonly Texture2D _ladderTexture;
    private readonly Texture2D _plateTexture;
    private readonly Texture2D _doorTexture;

    // Rectángulo reutilizable para evitar instanciaciones en Draw
    private Rectangle _destRect = new(0, 0, GameConstants.TILE_SIZE, GameConstants.TILE_SIZE);
    private bool _isDisposed;

    public Texture2D WallTexture => _wallTexture;
    public Texture2D LadderTexture => _ladderTexture;
    public Texture2D PlateTexture => _plateTexture;
    public Texture2D DoorTexture => _doorTexture;

    public TileRenderer(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _wallTexture = GenerateWallTexture(graphicsDevice);
        _ladderTexture = GenerateLadderTexture(graphicsDevice);
        _plateTexture = GeneratePressurePlateTexture(graphicsDevice);
        _doorTexture = GenerateDoorTexture(graphicsDevice);
    }

    /// <summary>
    /// Dibuja todas las baldosas de la cuadrícula en el SpriteBatch activo.
    /// Completamente libre de allocations (sin LINQ, sin new, sin boxing).
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, RoomGrid grid)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(grid);

        for (int y = 0; y < GameConstants.GRID_ROWS; y++)
        {
            for (int x = 0; x < GameConstants.GRID_COLUMNS; x++)
            {
                TileType tile = grid.GetTile(x, y);
                if (tile == TileType.Empty)
                    continue;

                Texture2D? texture = tile switch
                {
                    TileType.SolidWall => _wallTexture,
                    TileType.Ladder => _ladderTexture,
                    TileType.PressurePlate => _plateTexture,
                    TileType.ExitDoor => _doorTexture,
                    _ => null
                };

                if (texture != null)
                {
                    _destRect.X = x * GameConstants.TILE_SIZE;
                    _destRect.Y = y * GameConstants.TILE_SIZE;
                    spriteBatch.Draw(texture, _destRect, Color.White);
                }
            }
        }
    }

    private static Texture2D GenerateWallTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, GameConstants.TILE_SIZE, GameConstants.TILE_SIZE);
        Color[] pixels = new Color[GameConstants.TILE_SIZE * GameConstants.TILE_SIZE];

        Color mortar = new(54, 34, 16);
        Color baseGold = new(188, 134, 52);
        Color highlight = new(228, 175, 88);
        Color shade = new(132, 86, 28);
        Color accent = new(246, 198, 114);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int index = y * 16 + x;

                // Líneas de juntura de mortero horizontales
                if (y == 0 || y == 8)
                {
                    pixels[index] = mortar;
                    continue;
                }

                // Curso 1 de ladrillos (filas 1 a 7): junta vertical en columna 8
                if (y < 8)
                {
                    if (x == 8)
                    {
                        pixels[index] = mortar;
                    }
                    else if (y == 1 || x == 0 || x == 9)
                    {
                        pixels[index] = highlight;
                    }
                    else if (y == 7 || x == 7 || x == 15)
                    {
                        pixels[index] = shade;
                    }
                    else if ((x == 3 && y == 3) || (x == 12 && y == 5))
                    {
                        pixels[index] = accent;
                    }
                    else
                    {
                        pixels[index] = baseGold;
                    }
                }
                // Curso 2 de ladrillos (filas 9 a 15, decalado): juntas verticales en columnas 4 y 12
                else
                {
                    if (x == 4 || x == 12)
                    {
                        pixels[index] = mortar;
                    }
                    else if (y == 9 || x == 0 || x == 5 || x == 13)
                    {
                        pixels[index] = highlight;
                    }
                    else if (y == 15 || x == 3 || x == 11 || x == 15)
                    {
                        pixels[index] = shade;
                    }
                    else if ((x == 8 && y == 11) || (x == 2 && y == 13))
                    {
                        pixels[index] = accent;
                    }
                    else
                    {
                        pixels[index] = baseGold;
                    }
                }
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateLadderTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, GameConstants.TILE_SIZE, GameConstants.TILE_SIZE);
        Color[] pixels = new Color[GameConstants.TILE_SIZE * GameConstants.TILE_SIZE];

        Color railHighlight = new(196, 138, 70);
        Color railBase = new(144, 94, 42);
        Color railShadow = new(84, 48, 18);
        Color rungHighlight = new(218, 168, 98);
        Color rungShadow = new(112, 70, 26);
        Color ropeKnot = new(238, 204, 132);

        // Inicializar a transparente
        Array.Fill(pixels, Color.Transparent);

        for (int y = 0; y < 16; y++)
        {
            // Larguero vertical izquierdo (columnas 3 y 4)
            pixels[y * 16 + 3] = railHighlight;
            pixels[y * 16 + 4] = railShadow;

            // Larguero vertical derecho (columnas 11 y 12)
            pixels[y * 16 + 11] = railBase;
            pixels[y * 16 + 12] = railShadow;
        }

        // Peldaños horizontales en filas (2, 3), (7, 8), (12, 13)
        int[] rungRows = [2, 7, 12];
        foreach (int r in rungRows)
        {
            for (int x = 3; x <= 12; x++)
            {
                pixels[r * 16 + x] = rungHighlight;
                pixels[(r + 1) * 16 + x] = rungShadow;
            }

            // Nudos de cuerda en las uniones
            pixels[r * 16 + 3] = ropeKnot;
            pixels[(r + 1) * 16 + 4] = ropeKnot;
            pixels[r * 16 + 11] = ropeKnot;
            pixels[(r + 1) * 16 + 12] = ropeKnot;
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GeneratePressurePlateTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, GameConstants.TILE_SIZE, GameConstants.TILE_SIZE);
        Color[] pixels = new Color[GameConstants.TILE_SIZE * GameConstants.TILE_SIZE];

        Array.Fill(pixels, Color.Transparent);

        Color plateBevel = new(220, 185, 110);
        Color plateSurface = new(175, 138, 85);
        Color plateMid = new(135, 102, 60);
        Color plateShadow = new(90, 65, 38);
        Color foundation = new(72, 50, 26);
        Color deepFoundation = new(44, 28, 14);

        Color jewelGlow = new(120, 240, 255);
        Color jewelCyan = new(40, 205, 225);
        Color jewelCore = new(15, 150, 175);

        for (int y = 10; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int index = y * 16 + x;

                if (y == 10 && x >= 2 && x <= 13)
                {
                    pixels[index] = plateBevel;
                }
                else if (y == 11 && x >= 2 && x <= 13)
                {
                    // Gema sagrada en el centro
                    if (x == 7 || x == 8)
                        pixels[index] = jewelGlow;
                    else if (x == 6 || x == 9)
                        pixels[index] = jewelCyan;
                    else
                        pixels[index] = plateSurface;
                }
                else if (y == 12 && x >= 2 && x <= 13)
                {
                    if (x >= 7 && x <= 8)
                        pixels[index] = jewelCore;
                    else if (x == 6 || x == 9)
                        pixels[index] = jewelCyan;
                    else
                        pixels[index] = plateMid;
                }
                else if (y == 13 && x >= 2 && x <= 13)
                {
                    pixels[index] = plateShadow;
                }
                else if (y == 14 && x >= 1 && x <= 14)
                {
                    pixels[index] = foundation;
                }
                else if (y == 15 && x >= 1 && x <= 14)
                {
                    pixels[index] = deepFoundation;
                }
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateDoorTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, GameConstants.TILE_SIZE, GameConstants.TILE_SIZE);
        Color[] pixels = new Color[GameConstants.TILE_SIZE * GameConstants.TILE_SIZE];

        Array.Fill(pixels, Color.Transparent);

        Color archHighlight = new(255, 228, 125);
        Color archGold = new(235, 190, 70);
        Color archShadow = new(148, 102, 28);
        Color doorPanel = new(184, 134, 34);
        Color doorTrim = new(118, 78, 18);
        Color doorSeam = new(55, 32, 10);
        Color emblemRadiant = new(255, 245, 160);
        Color emblemGold = new(240, 195, 60);
        Color stoneStep = new(115, 82, 46);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int index = y * 16 + x;

                // Escalón inferior de piedra
                if (y == 15)
                {
                    pixels[index] = stoneStep;
                    continue;
                }

                // Esquinas transparentes del arco exterior
                if ((y == 0 && (x <= 2 || x >= 13)) || (y == 1 && (x <= 1 || x >= 14)))
                {
                    continue;
                }

                // Dintel superior arqueado
                if (y <= 2)
                {
                    pixels[index] = (y == 0 || (y == 1 && x >= 4 && x <= 11)) ? archHighlight : archGold;
                    continue;
                }

                // Columnas de soporte laterales (x=1..2 y x=13..14)
                if (x == 1 || x == 14)
                {
                    pixels[index] = archShadow;
                    continue;
                }
                if (x == 2 || x == 13)
                {
                    pixels[index] = archGold;
                    continue;
                }

                // Interior del pórtico (x=3..12)
                if (x == 7 || x == 8)
                {
                    // Emblema solar en el centro del pórtico
                    if (y >= 5 && y <= 6)
                        pixels[index] = emblemRadiant;
                    else if (y == 7 || y == 8)
                        pixels[index] = emblemGold;
                    else
                        pixels[index] = doorSeam;
                }
                else if (x == 3 || x == 12 || y == 3 || y == 14)
                {
                    pixels[index] = doorTrim;
                }
                else
                {
                    pixels[index] = doorPanel;
                }
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _wallTexture?.Dispose();
            _ladderTexture?.Dispose();
            _plateTexture?.Dispose();
            _doorTexture?.Dispose();
            _isDisposed = true;
        }
    }
}
