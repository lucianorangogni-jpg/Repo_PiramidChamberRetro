using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RetroGamePiramid.Core;

namespace RetroGamePiramid.Graphics;

/// <summary>
/// Renderiza el mural jeroglífico egipcio en el fondo del Nivel 0 de la Recámara 1.
/// Representa mediante bajorrelieves tenues la fórmula visual para el Puzle 1:
/// [Cofre 1] + [Cofre 2] + [Llave] -> [Puerta 1][Puerta 2][Puerta 3] -> [Cofre Radiante Extra].
/// Diseñado con una paleta de bajo contraste ("tonada muy suave, que apenas se puedan distinguir").
/// Cero asignaciones en memoria heap (0 allocations) en cada llamada a Draw.
/// </summary>
public sealed class HieroglyphRenderer : IDisposable
{
    public const int MURAL_WIDTH = GameConstants.VIRTUAL_WIDTH; // 320 px
    public const int MURAL_HEIGHT = 64; // 4 filas de cuadrícula (filas 10 a 13)
    public const int MURAL_Y = 160; // Posición Y en pantalla virtual (fila 10)

    private readonly Texture2D _muralTexture;
    private Rectangle _destRect = new(0, MURAL_Y, MURAL_WIDTH, MURAL_HEIGHT);
    private bool _isDisposed;

    public Texture2D MuralTexture => _muralTexture;

    public HieroglyphRenderer(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _muralTexture = GenerateMuralTexture(graphicsDevice);
    }

    /// <summary>
    /// Dibuja el mural en el fondo si la recámara activa es la Recámara 1.
    /// Totalmente libre de allocations en el bucle continuo.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, int chamberNumber)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        // El mural visual del Puzle 1 pertenece exclusivamente a la Recámara 1
        if (chamberNumber != 1)
            return;

        spriteBatch.Draw(_muralTexture, _destRect, Color.White);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _muralTexture.Dispose();
            _isDisposed = true;
        }
    }

    private static Texture2D GenerateMuralTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, MURAL_WIDTH, MURAL_HEIGHT);
        Color[] pixels = GenerateMuralPixels(MURAL_WIDTH, MURAL_HEIGHT);
        texture.SetData(pixels);
        return texture;
    }

    /// <summary>
    /// Genera la matriz de píxeles del mural jeroglífico con tonos suaves de bajorrelieve egipcio.
    /// Desacoplado de GraphicsDevice para facilitar pruebas unitarias.
    /// </summary>
    public static Color[] GenerateMuralPixels(int width, int height)
    {
        Color[] pixels = new Color[width * height];
        Array.Fill(pixels, Color.Transparent);

        // Paleta egipcia en bajo relieve: muy suave y cercana al fondo de tumba (18, 12, 22)
        Color groove = new(26, 18, 24);         // Hendiduras / sombras de cincelado
        Color stone = new(38, 28, 26);          // Relieve de arenisca tallada
        Color ochre = new(50, 36, 24);          // Pigmento ocre desvanecido
        Color fadedGold = new(68, 50, 26);      // Oro ceremonial apagado (cofres, llave, puertas)
        Color radiantGold = new(84, 62, 30);    // Oro sutilmente más cálido para el tesoro recargado
        Color turquoise = new(24, 40, 38);      // Pátina de turquesa / malaquita egipcia
        Color redOchre = new(46, 24, 20);       // Pigmento rojo / terracota apagado para flechas

        // 1. Cenefa decorativa egipcia superior e inferior (molduras y dentículos)
        DrawFrieze(pixels, width, 18, 78, groove, stone, ochre);
        DrawFrieze(pixels, width, 98, 222, groove, stone, ochre);

        // 2. Sección Izquierda (columnas 1 a 4, X = 18..78): Ojo de Horus y Cartucho Sagrado
        DrawEyeOfHorus(pixels, width, 24, 18, stone, ochre, turquoise, fadedGold, groove);
        DrawPharaohCartouche(pixels, width, 52, 14, stone, ochre, turquoise, groove);

        // 3. Sección Central: Fórmula visual del Puzle 1 (columnas 6 a 13, X = 98..224)
        // [Cofre 1] + [Cofre 2] + [Llave] -> [Puerta 1][Puerta 2][Puerta 3] -> [Cofre Radiante Extra]

        // Cofre 1 en x = 100
        DrawChestGlyph(pixels, width, 100, 18, stone, ochre, fadedGold, groove);

        // Símbolo de suma / Unión (Cruz Ankh ☥) en x = 115
        DrawAnkhGlyph(pixels, width, 115, 19, stone, ochre, fadedGold);

        // Cofre 2 en x = 125
        DrawChestGlyph(pixels, width, 125, 18, stone, ochre, fadedGold, groove);

        // Símbolo de suma / Unión (Cruz Ankh ☥) en x = 140
        DrawAnkhGlyph(pixels, width, 140, 19, stone, ochre, fadedGold);

        // Llave Sagrada en x = 150
        DrawKeyGlyph(pixels, width, 150, 17, stone, fadedGold, groove);

        // Flecha Ritual 1 (->) en x = 162
        DrawArrowGlyph(pixels, width, 162, 22, redOchre, ochre);

        // Tres Puertas Sagradas (III) en x = 174..198
        DrawThreeDoorsGlyph(pixels, width, 174, 13, stone, ochre, fadedGold, groove);

        // Flecha Ritual 2 (->) en x = 202
        DrawArrowGlyph(pixels, width, 202, 22, redOchre, ochre);

        // Cofre Radiante Extra (Tesoro Faraónico Renacido) en x = 213
        DrawRadiantChestGlyph(pixels, width, 213, 13, stone, ochre, fadedGold, radiantGold, turquoise, groove);

        return pixels;
    }

    private static void SetPixel(Color[] pixels, int width, int x, int y, Color color)
    {
        if (x >= 0 && x < width && y >= 0 && y < MURAL_HEIGHT)
        {
            pixels[y * width + x] = color;
        }
    }

    private static void DrawFrieze(Color[] pixels, int width, int startX, int endX, Color groove, Color stone, Color ochre)
    {
        for (int x = startX; x <= endX; x++)
        {
            // Cenefa superior
            SetPixel(pixels, width, x, 4, groove);
            SetPixel(pixels, width, x, 5, stone);
            if (x % 4 == 0)
            {
                SetPixel(pixels, width, x, 6, ochre);
            }

            // Cenefa inferior
            if (x % 4 == 0)
            {
                SetPixel(pixels, width, x, 41, ochre);
            }
            SetPixel(pixels, width, x, 42, stone);
            SetPixel(pixels, width, x, 43, groove);
        }
    }

    private static void DrawChestGlyph(Color[] pixels, int width, int startX, int startY, Color stone, Color ochre, Color gold, Color groove)
    {
        // Tapa curva egipcia
        for (int x = startX + 2; x <= startX + 9; x++)
            SetPixel(pixels, width, x, startY, groove);

        for (int x = startX + 1; x <= startX + 10; x++)
            SetPixel(pixels, width, x, startY + 1, gold);

        for (int x = startX; x <= startX + 11; x++)
            SetPixel(pixels, width, x, startY + 2, groove);

        // Cuerpo del cofre
        for (int y = startY + 3; y <= startY + 8; y++)
        {
            SetPixel(pixels, width, startX, y, stone);
            SetPixel(pixels, width, startX + 11, y, stone);
            for (int x = startX + 1; x <= startX + 10; x++)
            {
                SetPixel(pixels, width, x, y, ochre);
            }
        }

        // Cerradura dorada central
        SetPixel(pixels, width, startX + 5, startY + 4, gold);
        SetPixel(pixels, width, startX + 6, startY + 4, gold);
        SetPixel(pixels, width, startX + 5, startY + 5, gold);
        SetPixel(pixels, width, startX + 6, startY + 5, gold);

        // Base y patas
        for (int x = startX; x <= startX + 11; x++)
            SetPixel(pixels, width, x, startY + 9, stone);

        SetPixel(pixels, width, startX + 1, startY + 10, groove);
        SetPixel(pixels, width, startX + 2, startY + 10, groove);
        SetPixel(pixels, width, startX + 9, startY + 10, groove);
        SetPixel(pixels, width, startX + 10, startY + 10, groove);
    }

    private static void DrawAnkhGlyph(Color[] pixels, int width, int startX, int startY, Color stone, Color ochre, Color gold)
    {
        // Argolla ovalada superior
        SetPixel(pixels, width, startX + 2, startY, gold);
        SetPixel(pixels, width, startX + 3, startY, gold);
        SetPixel(pixels, width, startX + 1, startY + 1, gold);
        SetPixel(pixels, width, startX + 4, startY + 1, gold);
        SetPixel(pixels, width, startX + 1, startY + 2, gold);
        SetPixel(pixels, width, startX + 4, startY + 2, gold);
        SetPixel(pixels, width, startX + 2, startY + 3, gold);
        SetPixel(pixels, width, startX + 3, startY + 3, gold);

        // Travesaño horizontal
        for (int x = startX; x <= startX + 5; x++)
            SetPixel(pixels, width, x, startY + 4, gold);

        // Poste vertical
        for (int y = startY + 5; y <= startY + 8; y++)
        {
            SetPixel(pixels, width, startX + 2, y, ochre);
            SetPixel(pixels, width, startX + 3, y, stone);
        }
    }

    private static void DrawKeyGlyph(Color[] pixels, int width, int startX, int startY, Color stone, Color gold, Color groove)
    {
        // Argolla superior egipcia
        for (int x = startX + 2; x <= startX + 5; x++)
            SetPixel(pixels, width, x, startY, gold);

        SetPixel(pixels, width, startX + 1, startY + 1, gold);
        SetPixel(pixels, width, startX + 6, startY + 1, gold);
        SetPixel(pixels, width, startX + 1, startY + 2, gold);
        SetPixel(pixels, width, startX + 6, startY + 2, gold);

        for (int x = startX + 2; x <= startX + 5; x++)
            SetPixel(pixels, width, x, startY + 3, gold);

        // Vástago vertical
        for (int y = startY + 4; y <= startY + 11; y++)
        {
            SetPixel(pixels, width, startX + 3, y, gold);
            SetPixel(pixels, width, startX + 4, y, stone);
        }

        // Dientes de la llave hacia la derecha
        SetPixel(pixels, width, startX + 5, startY + 8, gold);
        SetPixel(pixels, width, startX + 6, startY + 8, gold);
        SetPixel(pixels, width, startX + 5, startY + 10, gold);
        SetPixel(pixels, width, startX + 6, startY + 10, gold);
        SetPixel(pixels, width, startX + 7, startY + 10, groove);
    }

    private static void DrawArrowGlyph(Color[] pixels, int width, int startX, int startY, Color redOchre, Color ochre)
    {
        // Línea horizontal
        for (int x = startX; x <= startX + 6; x++)
            SetPixel(pixels, width, x, startY + 1, redOchre);

        // Punta triangular hacia la derecha
        SetPixel(pixels, width, startX + 5, startY - 1, redOchre);
        SetPixel(pixels, width, startX + 6, startY, ochre);
        SetPixel(pixels, width, startX + 7, startY + 1, ochre);
        SetPixel(pixels, width, startX + 8, startY + 1, redOchre);
        SetPixel(pixels, width, startX + 6, startY + 2, ochre);
        SetPixel(pixels, width, startX + 5, startY + 3, redOchre);
    }

    private static void DrawThreeDoorsGlyph(Color[] pixels, int width, int startX, int startY, Color stone, Color ochre, Color gold, Color groove)
    {
        // 1. Marcas jeroglíficas superiores indicando tres ciclos (III)
        for (int y = startY; y <= startY + 2; y++)
        {
            SetPixel(pixels, width, startX + 3, y, gold);   // I
            SetPixel(pixels, width, startX + 12, y, gold);  // II
            SetPixel(pixels, width, startX + 21, y, gold);  // III
        }

        // 2. Tres Pilonos / Puertas ceremoniales
        int doorY = startY + 4;
        for (int d = 0; d < 3; d++)
        {
            int dx = startX + (d * 9);

            // Dintel superior ceremonial
            for (int x = dx; x <= dx + 6; x++)
                SetPixel(pixels, width, x, doorY, ochre);

            for (int x = dx + 1; x <= dx + 5; x++)
                SetPixel(pixels, width, x, doorY + 1, stone);

            // Jambas / columnas laterales y vano interior
            for (int y = doorY + 2; y <= doorY + 10; y++)
            {
                SetPixel(pixels, width, dx + 1, y, stone);
                SetPixel(pixels, width, dx + 5, y, stone);
                for (int x = dx + 2; x <= dx + 4; x++)
                {
                    SetPixel(pixels, width, x, y, groove); // Hueco de puerta
                }
            }

            // Umbral
            for (int x = dx; x <= dx + 6; x++)
                SetPixel(pixels, width, x, doorY + 11, stone);
        }
    }

    private static void DrawRadiantChestGlyph(
        Color[] pixels, int width, int startX, int startY,
        Color stone, Color ochre, Color gold, Color radiantGold, Color turquoise, Color groove)
    {
        // Resplandor celestial / rayos místicos superiores
        SetPixel(pixels, width, startX + 7, startY, radiantGold);
        SetPixel(pixels, width, startX + 7, startY + 1, radiantGold);

        SetPixel(pixels, width, startX + 3, startY + 1, gold);
        SetPixel(pixels, width, startX + 4, startY + 2, radiantGold);
        SetPixel(pixels, width, startX + 11, startY + 1, gold);
        SetPixel(pixels, width, startX + 10, startY + 2, radiantGold);

        // Sol alado / escarabajo sagrado coronando el cofre
        for (int x = startX + 5; x <= startX + 9; x++)
            SetPixel(pixels, width, x, startY + 3, radiantGold);
        SetPixel(pixels, width, startX + 7, startY + 4, turquoise);

        // Tapa del cofre resplandeciente
        int chestY = startY + 5;
        for (int x = startX + 2; x <= startX + 12; x++)
            SetPixel(pixels, width, x, chestY, radiantGold);

        for (int x = startX + 1; x <= startX + 13; x++)
            SetPixel(pixels, width, x, chestY + 1, gold);

        for (int x = startX; x <= startX + 14; x++)
            SetPixel(pixels, width, x, chestY + 2, groove);

        // Cuerpo del cofre ornamentado
        for (int y = chestY + 3; y <= chestY + 8; y++)
        {
            SetPixel(pixels, width, startX, y, stone);
            SetPixel(pixels, width, startX + 14, y, stone);
            for (int x = startX + 1; x <= startX + 13; x++)
            {
                SetPixel(pixels, width, x, y, ochre);
            }
        }

        // Gran gema turquesa central y resalte dorado
        SetPixel(pixels, width, startX + 6, chestY + 4, radiantGold);
        SetPixel(pixels, width, startX + 7, chestY + 4, turquoise);
        SetPixel(pixels, width, startX + 8, chestY + 4, radiantGold);
        SetPixel(pixels, width, startX + 6, chestY + 5, radiantGold);
        SetPixel(pixels, width, startX + 7, chestY + 5, turquoise);
        SetPixel(pixels, width, startX + 8, chestY + 5, radiantGold);

        // Base y patas
        for (int x = startX; x <= startX + 14; x++)
            SetPixel(pixels, width, x, chestY + 9, gold);

        SetPixel(pixels, width, startX + 1, chestY + 10, groove);
        SetPixel(pixels, width, startX + 2, chestY + 10, groove);
        SetPixel(pixels, width, startX + 12, chestY + 10, groove);
        SetPixel(pixels, width, startX + 13, chestY + 10, groove);
    }

    private static void DrawEyeOfHorus(
        Color[] pixels, int width, int startX, int startY,
        Color stone, Color ochre, Color turquoise, Color gold, Color groove)
    {
        // Ceja curva
        for (int x = startX + 2; x <= startX + 14; x++)
            SetPixel(pixels, width, x, startY, ochre);

        // Párpado superior
        for (int x = startX + 3; x <= startX + 13; x++)
            SetPixel(pixels, width, x, startY + 2, stone);

        // Iris / Pupila
        SetPixel(pixels, width, startX + 7, startY + 3, gold);
        SetPixel(pixels, width, startX + 8, startY + 3, turquoise);
        SetPixel(pixels, width, startX + 9, startY + 3, gold);
        SetPixel(pixels, width, startX + 7, startY + 4, turquoise);
        SetPixel(pixels, width, startX + 8, startY + 4, groove);
        SetPixel(pixels, width, startX + 9, startY + 4, turquoise);

        // Párpado inferior
        for (int x = startX + 3; x <= startX + 13; x++)
            SetPixel(pixels, width, x, startY + 5, stone);

        // Lágrima recta y espiral inferior
        for (int y = startY + 6; y <= startY + 10; y++)
            SetPixel(pixels, width, startX + 7, y, ochre);

        // Espiral de Horus hacia la derecha
        SetPixel(pixels, width, startX + 8, startY + 8, stone);
        SetPixel(pixels, width, startX + 9, startY + 9, stone);
        SetPixel(pixels, width, startX + 10, startY + 9, stone);
        SetPixel(pixels, width, startX + 11, startY + 8, stone);
    }

    private static void DrawPharaohCartouche(
        Color[] pixels, int width, int startX, int startY,
        Color stone, Color ochre, Color turquoise, Color groove)
    {
        // Marco ovalado del cartucho
        for (int x = startX + 2; x <= startX + 18; x++)
        {
            SetPixel(pixels, width, x, startY, stone);
            SetPixel(pixels, width, x, startY + 17, stone);
        }

        for (int y = startY + 1; y <= startY + 16; y++)
        {
            SetPixel(pixels, width, startX + 1, y, stone);
            SetPixel(pixels, width, startX + 19, y, stone);
        }

        // Nudo ceremonial en la base
        for (int x = startX; x <= startX + 20; x++)
            SetPixel(pixels, width, x, startY + 18, groove);

        // Pequeño escarabajo sagrado tallado en el interior
        for (int x = startX + 8; x <= startX + 12; x++)
            SetPixel(pixels, width, x, startY + 4, turquoise);
        for (int x = startX + 7; x <= startX + 13; x++)
            SetPixel(pixels, width, x, startY + 5, ochre);
        for (int x = startX + 8; x <= startX + 12; x++)
            SetPixel(pixels, width, x, startY + 6, ochre);

        // Símbolo de aguas ancestrales (~ ~ ~)
        for (int x = startX + 4; x <= startX + 16; x++)
        {
            if (x % 3 == 0)
                SetPixel(pixels, width, x, startY + 10, stone);
            else
                SetPixel(pixels, width, x, startY + 11, stone);
        }

        // Plumas sagradas de Ma'at
        for (int y = startY + 13; y <= startY + 15; y++)
        {
            SetPixel(pixels, width, startX + 8, y, ochre);
            SetPixel(pixels, width, startX + 12, y, ochre);
        }
    }
}
