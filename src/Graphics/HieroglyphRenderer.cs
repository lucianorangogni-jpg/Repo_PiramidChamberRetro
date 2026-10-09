using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RetroGamePiramid.Core;

namespace RetroGamePiramid.Graphics;

/// <summary>
/// Renderiza los murales jeroglíficos egipcios en el fondo de la Recámara 1.
/// - Nivel 0 (Puzle 1): [Cofre 1] + [Cofre 2] + [Llave] -> [3 Puertas] -> [Cofre Radiante Extra].
/// - Nivel 1 (Puzle 2/5): [Momia + Salto 4x (IIII)] -> [Momia Cayendo al Foso (⬇)] -> [Cofre Renovado ☥].
/// - Nivel 2 (Puzle 6): [Momia + Trampa 3x (III)] -> [Momia Cayendo por Trampa (3x ⬇)] -> [Nuevo Tesoro en Cámara ☥].
/// Diseñado con una paleta de contraste mejorado para distinguir claramente los bajorrelieves sobre el fondo de la tumba.
/// Cero asignaciones en memoria heap (0 allocations) en cada llamada a Draw.
/// </summary>
public sealed class HieroglyphRenderer : IDisposable
{
    // Mural Nivel 0 (Puzle 1: Recarga secreta del tesoro)
    public const int MURAL_WIDTH = GameConstants.VIRTUAL_WIDTH; // 320 px
    public const int MURAL_HEIGHT = 64; // 4 filas de cuadrícula (filas 10 a 13)
    public const int MURAL_Y = 160; // Posición Y en pantalla virtual (fila 10)

    // Mural Nivel 1 (Puzle 2: Momia 4 saltos, caída al foso y nuevo tesoro)
    public const int MURAL_LEVEL1_WIDTH = GameConstants.VIRTUAL_WIDTH; // 320 px
    public const int MURAL_LEVEL1_HEIGHT = 48; // 3 filas de cuadrícula (filas 6 a 8)
    public const int MURAL_LEVEL1_Y = 96; // Posición Y en pantalla virtual (fila 6)

    // Mural Nivel 2 (Puzle 6: Trampa, 3 caídas de la momia y nuevo tesoro en cámara)
    public const int MURAL_LEVEL2_WIDTH = GameConstants.VIRTUAL_WIDTH; // 320 px
    public const int MURAL_LEVEL2_HEIGHT = 64; // 4 filas de cuadrícula (filas 1 a 4)
    public const int MURAL_LEVEL2_Y = 16; // Posición Y en pantalla virtual (fila 1)

    private readonly Texture2D _muralTexture;
    private readonly Texture2D _muralLevel1Texture;
    private readonly Texture2D _muralLevel2Texture;
    private Rectangle _destRect = new(0, MURAL_Y, MURAL_WIDTH, MURAL_HEIGHT);
    private Rectangle _destRectLevel1 = new(0, MURAL_LEVEL1_Y, MURAL_LEVEL1_WIDTH, MURAL_LEVEL1_HEIGHT);
    private Rectangle _destRectLevel2 = new(0, MURAL_LEVEL2_Y, MURAL_LEVEL2_WIDTH, MURAL_LEVEL2_HEIGHT);
    private bool _isDisposed;

    public Texture2D MuralTexture => _muralTexture;
    public Texture2D MuralLevel1Texture => _muralLevel1Texture;
    public Texture2D MuralLevel2Texture => _muralLevel2Texture;

    public HieroglyphRenderer(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _muralTexture = GenerateMuralTexture(graphicsDevice);
        _muralLevel1Texture = GenerateMuralLevel1Texture(graphicsDevice);
        _muralLevel2Texture = GenerateMuralLevel2Texture(graphicsDevice);
    }

    /// <summary>
    /// Dibuja los murales jeroglíficos en el fondo si la recámara activa es la Recámara 1.
    /// Totalmente libre de allocations en el bucle continuo.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, int chamberNumber)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        // Los murales visuales pertenecen exclusivamente a la Recámara 1
        if (chamberNumber != 1)
            return;

        spriteBatch.Draw(_muralLevel2Texture, _destRectLevel2, Color.White);
        spriteBatch.Draw(_muralLevel1Texture, _destRectLevel1, Color.White);
        spriteBatch.Draw(_muralTexture, _destRect, Color.White);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _muralTexture?.Dispose();
            _muralLevel1Texture?.Dispose();
            _muralLevel2Texture?.Dispose();
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

    private static Texture2D GenerateMuralLevel1Texture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, MURAL_LEVEL1_WIDTH, MURAL_LEVEL1_HEIGHT);
        Color[] pixels = GenerateMuralLevel1Pixels(MURAL_LEVEL1_WIDTH, MURAL_LEVEL1_HEIGHT);
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateMuralLevel2Texture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, MURAL_LEVEL2_WIDTH, MURAL_LEVEL2_HEIGHT);
        Color[] pixels = GenerateMuralLevel2Pixels(MURAL_LEVEL2_WIDTH, MURAL_LEVEL2_HEIGHT);
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

        // Paleta egipcia en bajo relieve: contraste mejorado sobre el fondo de tumba (18, 12, 22)
        Color groove = new(38, 28, 34);         // Hendiduras / sombras de cincelado acentuadas
        Color stone = new(78, 58, 48);          // Relieve de arenisca tallada (claramente discernible)
        Color ochre = new(105, 78, 50);         // Pigmento ocre egipcio cálido
        Color fadedGold = new(136, 102, 52);    // Oro ceremonial antiguo (cofres, llave, puertas)
        Color radiantGold = new(165, 126, 62);  // Oro radiante para el tesoro renovado
        Color turquoise = new(45, 96, 90);      // Pátina de turquesa / malaquita egipcia distintiva
        Color redOchre = new(115, 48, 38);      // Pigmento terracota / rojo ritual legible para flechas

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

    /// <summary>
    /// Genera la matriz de píxeles del mural jeroglífico del Nivel 1 (Puzle 2/5):
    /// [Momia + Salto 4x (IIII)] -> [Momia Cayendo al Foso (⬇)] -> [Cofre Sagrado Renovado ☥].
    /// Diseñado con una paleta de bajo contraste ("tonada muy suave, que apenas se puedan distinguir").
    /// Desacoplado de GraphicsDevice para facilitar pruebas unitarias.
    /// </summary>
    public static Color[] GenerateMuralLevel1Pixels(int width, int height)
    {
        Color[] pixels = new Color[width * height];
        Array.Fill(pixels, Color.Transparent);

        // Paleta egipcia en bajo relieve: contraste mejorado sobre el fondo de tumba (18, 12, 22)
        Color groove = new(38, 28, 34);         // Hendiduras / sombras de cincelado acentuadas
        Color stone = new(78, 58, 48);          // Relieve de arenisca tallada (claramente discernible)
        Color ochre = new(105, 78, 50);         // Pigmento ocre egipcio cálido
        Color fadedGold = new(136, 102, 52);    // Oro ceremonial antiguo (momia, saltos, cofre)
        Color radiantGold = new(165, 126, 62);  // Oro radiante místico para el nuevo cofre
        Color turquoise = new(45, 96, 90);      // Pátina de turquesa egipcia distintiva
        Color redOchre = new(115, 48, 38);      // Pigmento rojo ritual para flechas de caída y ojos

        // 1. Cenefa decorativa superior e inferior
        DrawFriezeLevel1(pixels, width, height, 26, 222, groove, stone, ochre);

        // 2. Sello Sagrado Egipcio a la izquierda (Escarabajo alado Khepri en x = 32..48)
        DrawScarabSealGlyph(pixels, width, height, 32, 14, stone, ochre, turquoise, groove);

        // 3. Glifo 1: Momia + Arco de Salto + 4 Marcas Sagradas (IIII) en x = 58..102
        DrawMummyJump4xGlyph(pixels, width, height, 58, 6, stone, ochre, fadedGold, radiantGold, redOchre, groove);

        // 4. Conector Ritual 1: Flecha hacia la derecha en x = 106
        DrawArrowLevel1Glyph(pixels, width, height, 106, 24, redOchre, ochre);

        // 5. Glifo 2: Momia precipitándose al foso / suelo abierto con flecha descendente (⬇) en x = 120..164
        DrawMummyFallingPitGlyph(pixels, width, height, 120, 8, stone, ochre, redOchre, groove);

        // 6. Conector Ritual 2: Flecha hacia la derecha en x = 168
        DrawArrowLevel1Glyph(pixels, width, height, 168, 24, redOchre, ochre);

        // 7. Glifo 3: Nuevo Cofre Sagrado Radiante + Cruz Ankh en x = 182..218
        DrawRadiantTreasureRenewedGlyph(pixels, width, height, 182, 10, stone, ochre, fadedGold, radiantGold, turquoise, groove);

        return pixels;
    }

    private static void SetPixelLevel1(Color[] pixels, int width, int height, int x, int y, Color color)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            pixels[y * width + x] = color;
        }
    }

    private static void DrawFriezeLevel1(Color[] pixels, int width, int height, int startX, int endX, Color groove, Color stone, Color ochre)
    {
        for (int x = startX; x <= endX; x++)
        {
            // Cenefa superior (y = 2..4)
            SetPixelLevel1(pixels, width, height, x, 2, groove);
            SetPixelLevel1(pixels, width, height, x, 3, stone);
            if (x % 4 == 0) SetPixelLevel1(pixels, width, height, x, 4, ochre);

            // Cenefa inferior (y = 42..44)
            if (x % 4 == 0) SetPixelLevel1(pixels, width, height, x, 42, ochre);
            SetPixelLevel1(pixels, width, height, x, 43, stone);
            SetPixelLevel1(pixels, width, height, x, 44, groove);
        }
    }

    private static void DrawScarabSealGlyph(Color[] pixels, int width, int height, int startX, int startY, Color stone, Color ochre, Color turquoise, Color groove)
    {
        // Disco solar superior
        for (int x = startX + 6; x <= startX + 9; x++)
            SetPixelLevel1(pixels, width, height, x, startY, ochre);
        for (int x = startX + 5; x <= startX + 10; x++)
            SetPixelLevel1(pixels, width, height, x, startY + 1, ochre);

        // Cuerpo del escarabajo
        for (int y = startY + 3; y <= startY + 9; y++)
        {
            for (int x = startX + 6; x <= startX + 9; x++)
            {
                SetPixelLevel1(pixels, width, height, x, y, (x == startX + 7 || x == startX + 8) && (y == startY + 5 || y == startY + 6) ? turquoise : stone);
            }
        }

        // Alas extendidas
        for (int y = startY + 4; y <= startY + 8; y++)
        {
            SetPixelLevel1(pixels, width, height, startX + 2, y, ochre);
            SetPixelLevel1(pixels, width, height, startX + 3, y, stone);
            SetPixelLevel1(pixels, width, height, startX + 4, y, stone);
            SetPixelLevel1(pixels, width, height, startX + 5, y, groove);

            SetPixelLevel1(pixels, width, height, startX + 10, y, groove);
            SetPixelLevel1(pixels, width, height, startX + 11, y, stone);
            SetPixelLevel1(pixels, width, height, startX + 12, y, stone);
            SetPixelLevel1(pixels, width, height, startX + 13, y, ochre);
        }

        // Patas inferiores
        SetPixelLevel1(pixels, width, height, startX + 5, startY + 11, groove);
        SetPixelLevel1(pixels, width, height, startX + 6, startY + 10, stone);
        SetPixelLevel1(pixels, width, height, startX + 9, startY + 10, stone);
        SetPixelLevel1(pixels, width, height, startX + 10, startY + 11, groove);
    }

    private static void DrawMummyJump4xGlyph(
        Color[] pixels, int width, int height, int startX, int startY,
        Color stone, Color ochre, Color fadedGold, Color radiantGold, Color redOchre, Color groove)
    {
        // 1. Las 4 Marcas Sagradas ("IIII") en lo alto del arco (y = startY a startY + 4)
        for (int i = 0; i < 4; i++)
        {
            int mx = startX + 17 + (i * 3);
            SetPixelLevel1(pixels, width, height, mx, startY, radiantGold);
            for (int y = startY + 1; y <= startY + 4; y++)
            {
                SetPixelLevel1(pixels, width, height, mx, y, fadedGold);
            }
        }

        // 2. Parábola de salto arqueada sobre la momia
        int apexX = startX + 21;
        int apexY = startY + 6;
        for (int x = startX + 4; x <= startX + 38; x++)
        {
            float dx = x - apexX;
            int arcY = apexY + (int)((dx * dx) / 16f);
            if (arcY <= startY + 32)
            {
                SetPixelLevel1(pixels, width, height, x, arcY, ochre);
                SetPixelLevel1(pixels, width, height, x, arcY + 1, stone);
            }
        }

        // 3. Glifo silueteado del arqueólogo saltando en la cúspide
        SetPixelLevel1(pixels, width, height, apexX, startY + 7, ochre);
        SetPixelLevel1(pixels, width, height, apexX - 1, startY + 8, stone);
        SetPixelLevel1(pixels, width, height, apexX, startY + 8, stone);
        SetPixelLevel1(pixels, width, height, apexX + 1, startY + 8, stone);
        SetPixelLevel1(pixels, width, height, apexX - 2, startY + 9, ochre);
        SetPixelLevel1(pixels, width, height, apexX + 2, startY + 9, ochre);

        // 4. Glifo de la Momia erguida bajo el salto
        int mummyX = startX + 16;
        int mummyY = startY + 16;

        for (int x = mummyX + 2; x <= mummyX + 7; x++)
            SetPixelLevel1(pixels, width, height, x, mummyY, stone);
        for (int x = mummyX + 1; x <= mummyX + 8; x++)
            SetPixelLevel1(pixels, width, height, x, mummyY + 1, ochre);

        SetPixelLevel1(pixels, width, height, mummyX + 1, mummyY + 2, groove);
        SetPixelLevel1(pixels, width, height, mummyX + 3, mummyY + 2, redOchre);
        SetPixelLevel1(pixels, width, height, mummyX + 6, mummyY + 2, redOchre);
        SetPixelLevel1(pixels, width, height, mummyX + 8, mummyY + 2, groove);

        for (int x = mummyX + 2; x <= mummyX + 7; x++)
            SetPixelLevel1(pixels, width, height, x, mummyY + 3, stone);

        for (int y = mummyY + 4; y <= mummyY + 10; y++)
        {
            SetPixelLevel1(pixels, width, height, mummyX, y, stone);
            SetPixelLevel1(pixels, width, height, mummyX + 9, y, stone);
            for (int x = mummyX + 1; x <= mummyX + 8; x++)
            {
                Color wrapCol = ((x + y) % 2 == 0) ? ochre : stone;
                SetPixelLevel1(pixels, width, height, x, y, wrapCol);
            }
        }

        SetPixelLevel1(pixels, width, height, mummyX + 4, mummyY + 5, fadedGold);
        SetPixelLevel1(pixels, width, height, mummyX + 5, mummyY + 5, fadedGold);

        for (int y = mummyY + 11; y <= mummyY + 16; y++)
        {
            SetPixelLevel1(pixels, width, height, mummyX + 2, y, stone);
            SetPixelLevel1(pixels, width, height, mummyX + 3, y, ochre);
            SetPixelLevel1(pixels, width, height, mummyX + 6, y, ochre);
            SetPixelLevel1(pixels, width, height, mummyX + 7, y, stone);
        }
        for (int x = mummyX + 1; x <= mummyX + 4; x++)
            SetPixelLevel1(pixels, width, height, x, mummyY + 17, groove);
        for (int x = mummyX + 5; x <= mummyX + 8; x++)
            SetPixelLevel1(pixels, width, height, x, mummyY + 17, groove);
    }

    private static void DrawArrowLevel1Glyph(Color[] pixels, int width, int height, int startX, int startY, Color redOchre, Color ochre)
    {
        for (int x = startX; x <= startX + 7; x++)
            SetPixelLevel1(pixels, width, height, x, startY + 1, redOchre);

        SetPixelLevel1(pixels, width, height, startX + 6, startY - 1, redOchre);
        SetPixelLevel1(pixels, width, height, startX + 7, startY, ochre);
        SetPixelLevel1(pixels, width, height, startX + 8, startY + 1, ochre);
        SetPixelLevel1(pixels, width, height, startX + 9, startY + 1, redOchre);
        SetPixelLevel1(pixels, width, height, startX + 7, startY + 2, ochre);
        SetPixelLevel1(pixels, width, height, startX + 6, startY + 3, redOchre);
    }

    private static void DrawMummyFallingPitGlyph(
        Color[] pixels, int width, int height, int startX, int startY,
        Color stone, Color ochre, Color redOchre, Color groove)
    {
        // 1. Plataforma con foso/abismo central
        for (int x = startX; x <= startX + 10; x++)
        {
            SetPixelLevel1(pixels, width, height, x, startY + 26, ochre);
            SetPixelLevel1(pixels, width, height, x, startY + 27, stone);
            SetPixelLevel1(pixels, width, height, x, startY + 28, groove);
        }

        for (int x = startX + 32; x <= startX + 42; x++)
        {
            SetPixelLevel1(pixels, width, height, x, startY + 26, ochre);
            SetPixelLevel1(pixels, width, height, x, startY + 27, stone);
            SetPixelLevel1(pixels, width, height, x, startY + 28, groove);
        }

        for (int y = startY + 29; y <= startY + 36; y++)
        {
            SetPixelLevel1(pixels, width, height, startX + 10, y, groove);
            SetPixelLevel1(pixels, width, height, startX + 32, y, groove);
        }

        // 2. Flecha Ritual Descendente (⬇) apuntando al abismo
        int arrowX = startX + 21;
        for (int y = startY; y <= startY + 8; y++)
        {
            SetPixelLevel1(pixels, width, height, arrowX, y, redOchre);
        }
        SetPixelLevel1(pixels, width, height, arrowX - 2, startY + 6, redOchre);
        SetPixelLevel1(pixels, width, height, arrowX - 1, startY + 7, ochre);
        SetPixelLevel1(pixels, width, height, arrowX, startY + 9, redOchre);
        SetPixelLevel1(pixels, width, height, arrowX + 1, startY + 7, ochre);
        SetPixelLevel1(pixels, width, height, arrowX + 2, startY + 6, redOchre);

        // 3. Momia precipitándose al foso en caída vertical
        int fallMummyX = startX + 16;
        int fallMummyY = startY + 12;

        for (int x = fallMummyX + 2; x <= fallMummyX + 7; x++)
            SetPixelLevel1(pixels, width, height, x, fallMummyY, stone);
        for (int x = fallMummyX + 1; x <= fallMummyX + 8; x++)
            SetPixelLevel1(pixels, width, height, x, fallMummyY + 1, ochre);
        SetPixelLevel1(pixels, width, height, fallMummyX + 3, fallMummyY + 2, redOchre);
        SetPixelLevel1(pixels, width, height, fallMummyX + 6, fallMummyY + 2, redOchre);

        for (int y = fallMummyY + 3; y <= fallMummyY + 9; y++)
        {
            for (int x = fallMummyX + 1; x <= fallMummyX + 8; x++)
            {
                SetPixelLevel1(pixels, width, height, x, y, (x % 2 == 0) ? ochre : stone);
            }
        }

        SetPixelLevel1(pixels, width, height, fallMummyX, fallMummyY - 2, stone);
        SetPixelLevel1(pixels, width, height, fallMummyX, fallMummyY - 1, ochre);
        SetPixelLevel1(pixels, width, height, fallMummyX + 9, fallMummyY - 2, stone);
        SetPixelLevel1(pixels, width, height, fallMummyX + 9, fallMummyY - 1, ochre);

        for (int y = fallMummyY + 10; y <= fallMummyY + 14; y++)
        {
            SetPixelLevel1(pixels, width, height, fallMummyX + 2, y, stone);
            SetPixelLevel1(pixels, width, height, fallMummyX + 3, y, ochre);
            SetPixelLevel1(pixels, width, height, fallMummyX + 6, y, ochre);
            SetPixelLevel1(pixels, width, height, fallMummyX + 7, y, stone);
        }
    }

    private static void DrawRadiantTreasureRenewedGlyph(
        Color[] pixels, int width, int height, int startX, int startY,
        Color stone, Color ochre, Color fadedGold, Color radiantGold, Color turquoise, Color groove)
    {
        // Rayos celestiales dorados
        SetPixelLevel1(pixels, width, height, startX + 7, startY, radiantGold);
        SetPixelLevel1(pixels, width, height, startX + 7, startY + 1, radiantGold);
        SetPixelLevel1(pixels, width, height, startX + 3, startY + 1, fadedGold);
        SetPixelLevel1(pixels, width, height, startX + 4, startY + 2, radiantGold);
        SetPixelLevel1(pixels, width, height, startX + 11, startY + 1, fadedGold);
        SetPixelLevel1(pixels, width, height, startX + 10, startY + 2, radiantGold);

        // Disco solar / gema de cabecera
        for (int x = startX + 5; x <= startX + 9; x++)
            SetPixelLevel1(pixels, width, height, x, startY + 3, radiantGold);
        SetPixelLevel1(pixels, width, height, startX + 7, startY + 4, turquoise);

        // Tapa arqueada del cofre sagrado renacido
        int chestY = startY + 5;
        for (int x = startX + 2; x <= startX + 12; x++)
            SetPixelLevel1(pixels, width, height, x, chestY, radiantGold);
        for (int x = startX + 1; x <= startX + 13; x++)
            SetPixelLevel1(pixels, width, height, x, chestY + 1, fadedGold);
        for (int x = startX; x <= startX + 14; x++)
            SetPixelLevel1(pixels, width, height, x, chestY + 2, groove);

        // Cuerpo del cofre ornamentado
        for (int y = chestY + 3; y <= chestY + 9; y++)
        {
            SetPixelLevel1(pixels, width, height, startX, y, stone);
            SetPixelLevel1(pixels, width, height, startX + 14, y, stone);
            for (int x = startX + 1; x <= startX + 13; x++)
            {
                SetPixelLevel1(pixels, width, height, x, y, ochre);
            }
        }

        // Cerradura dorada y gema turquesa central
        SetPixelLevel1(pixels, width, height, startX + 6, chestY + 5, radiantGold);
        SetPixelLevel1(pixels, width, height, startX + 7, chestY + 5, turquoise);
        SetPixelLevel1(pixels, width, height, startX + 8, chestY + 5, radiantGold);
        SetPixelLevel1(pixels, width, height, startX + 6, chestY + 6, radiantGold);
        SetPixelLevel1(pixels, width, height, startX + 7, chestY + 6, turquoise);
        SetPixelLevel1(pixels, width, height, startX + 8, chestY + 6, radiantGold);

        // Base y patas
        for (int x = startX; x <= startX + 14; x++)
            SetPixelLevel1(pixels, width, height, x, chestY + 10, stone);
        SetPixelLevel1(pixels, width, height, startX + 1, chestY + 11, groove);
        SetPixelLevel1(pixels, width, height, startX + 2, chestY + 11, groove);
        SetPixelLevel1(pixels, width, height, startX + 12, chestY + 11, groove);
        SetPixelLevel1(pixels, width, height, startX + 13, chestY + 11, groove);

        // Cruz Ankh ☥ a la derecha simbolizando renacimiento / vida
        int ankhX = startX + 19;
        int ankhY = startY + 8;
        SetPixelLevel1(pixels, width, height, ankhX + 2, ankhY, fadedGold);
        SetPixelLevel1(pixels, width, height, ankhX + 3, ankhY, fadedGold);
        SetPixelLevel1(pixels, width, height, ankhX + 1, ankhY + 1, fadedGold);
        SetPixelLevel1(pixels, width, height, ankhX + 4, ankhY + 1, fadedGold);
        SetPixelLevel1(pixels, width, height, ankhX + 2, ankhY + 2, fadedGold);
        SetPixelLevel1(pixels, width, height, ankhX + 3, ankhY + 2, fadedGold);

        for (int x = ankhX; x <= ankhX + 5; x++)
            SetPixelLevel1(pixels, width, height, x, ankhY + 3, fadedGold);

        for (int y = ankhY + 4; y <= ankhY + 8; y++)
        {
            SetPixelLevel1(pixels, width, height, ankhX + 2, y, ochre);
            SetPixelLevel1(pixels, width, height, ankhX + 3, y, stone);
        }
    }

    /// <summary>
    /// Genera la matriz de píxeles del mural jeroglífico del Nivel 2 (Puzle 6 / US-019):
    /// [Momia + Trampa 3x (III)] -> [Momia Cayendo por Trampa (3x ⬇)] -> [Nuevo Tesoro en Cámara ☥].
    /// Diseñado con una paleta de contraste mejorado para distinguir claramente los bajorrelieves sobre el fondo de la tumba.
    /// Desacoplado de GraphicsDevice para facilitar pruebas unitarias.
    /// </summary>
    public static Color[] GenerateMuralLevel2Pixels(int width, int height)
    {
        Color[] pixels = new Color[width * height];
        Array.Fill(pixels, Color.Transparent);

        // Paleta egipcia en bajo relieve: contraste mejorado sobre el fondo de tumba (18, 12, 22)
        Color groove = new(38, 28, 34);         // Hendiduras / sombras de cincelado acentuadas
        Color stone = new(78, 58, 48);          // Relieve de arenisca tallada (claramente discernible)
        Color ochre = new(105, 78, 50);         // Pigmento ocre egipcio cálido
        Color fadedGold = new(136, 102, 52);    // Oro ceremonial antiguo (momia, trampas, marcas III, cofre)
        Color radiantGold = new(165, 126, 62);  // Oro radiante místico para el nuevo cofre y marcas
        Color turquoise = new(45, 96, 90);      // Pátina de turquesa egipcia distintiva
        Color redOchre = new(115, 48, 38);      // Pigmento rojo ritual para flechas y ojos

        // 1. Cenefa decorativa superior e inferior (enmarca la pared visible)
        DrawFriezeLevel2(pixels, width, height, 116, 290, groove, stone, ochre);

        // 2. Glifo 1: Momia + Trampa de suelo + 3 Marcas Sagradas (III) en x = 124..168
        DrawMummyTrap3xGlyph(pixels, width, height, 124, 8, stone, ochre, fadedGold, radiantGold, redOchre, groove);

        // 3. Conector Ritual 1: Flecha hacia la derecha en x = 172
        DrawArrowLevel2Glyph(pixels, width, height, 172, 28, redOchre, ochre);

        // 4. Glifo 2: Momia precipitándose por la trampa con flecha descendente (⬇) y 3 marcas en x = 184..228
        DrawMummyFallingTrap3xGlyph(pixels, width, height, 184, 8, stone, ochre, fadedGold, radiantGold, redOchre, groove);

        // 5. Conector Ritual 2: Flecha hacia la derecha en x = 232
        DrawArrowLevel2Glyph(pixels, width, height, 232, 28, redOchre, ochre);

        // 6. Glifo 3: Momia derrotada + Nuevo Cofre Sagrado Renovado en Cámara en x = 244..286
        DrawRenewedTreasureInChamberGlyph(pixels, width, height, 244, 10, stone, ochre, fadedGold, radiantGold, turquoise, groove);

        return pixels;
    }

    private static void SetPixelLevel2(Color[] pixels, int width, int height, int x, int y, Color color)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            pixels[y * width + x] = color;
        }
    }

    private static void DrawFriezeLevel2(Color[] pixels, int width, int height, int startX, int endX, Color groove, Color stone, Color ochre)
    {
        for (int x = startX; x <= endX; x++)
        {
            // Cenefa superior (y = 4..6)
            SetPixelLevel2(pixels, width, height, x, 4, groove);
            SetPixelLevel2(pixels, width, height, x, 5, stone);
            if (x % 4 == 0) SetPixelLevel2(pixels, width, height, x, 6, ochre);

            // Cenefa inferior (y = 54..56)
            if (x % 4 == 0) SetPixelLevel2(pixels, width, height, x, 54, ochre);
            SetPixelLevel2(pixels, width, height, x, 55, stone);
            SetPixelLevel2(pixels, width, height, x, 56, groove);
        }
    }

    private static void DrawMummyTrap3xGlyph(
        Color[] pixels, int width, int height, int startX, int startY,
        Color stone, Color ochre, Color fadedGold, Color radiantGold, Color redOchre, Color groove)
    {
        // 1. Las 3 Marcas Sagradas ("III") de advertencia en lo alto (y = startY a startY + 5)
        for (int i = 0; i < 3; i++)
        {
            int mx = startX + 20 + (i * 4);
            SetPixelLevel2(pixels, width, height, mx, startY, radiantGold);
            for (int y = startY + 1; y <= startY + 5; y++)
            {
                SetPixelLevel2(pixels, width, height, mx, y, fadedGold);
            }
        }

        // 2. Glifo de la Momia erguida frente a la trampa
        int mummyX = startX + 4;
        int mummyY = startY + 14;

        for (int x = mummyX + 2; x <= mummyX + 7; x++)
            SetPixelLevel2(pixels, width, height, x, mummyY, stone);
        for (int x = mummyX + 1; x <= mummyX + 8; x++)
            SetPixelLevel2(pixels, width, height, x, mummyY + 1, ochre);

        SetPixelLevel2(pixels, width, height, mummyX + 1, mummyY + 2, groove);
        SetPixelLevel2(pixels, width, height, mummyX + 3, mummyY + 2, redOchre);
        SetPixelLevel2(pixels, width, height, mummyX + 6, mummyY + 2, redOchre);
        SetPixelLevel2(pixels, width, height, mummyX + 8, mummyY + 2, groove);

        for (int x = mummyX + 2; x <= mummyX + 7; x++)
            SetPixelLevel2(pixels, width, height, x, mummyY + 3, stone);

        for (int y = mummyY + 4; y <= mummyY + 12; y++)
        {
            SetPixelLevel2(pixels, width, height, mummyX, y, stone);
            SetPixelLevel2(pixels, width, height, mummyX + 9, y, stone);
            for (int x = mummyX + 1; x <= mummyX + 8; x++)
            {
                Color wrapCol = ((x + y) % 2 == 0) ? ochre : stone;
                SetPixelLevel2(pixels, width, height, x, y, wrapCol);
            }
        }

        SetPixelLevel2(pixels, width, height, mummyX + 4, mummyY + 6, fadedGold);
        SetPixelLevel2(pixels, width, height, mummyX + 5, mummyY + 6, fadedGold);

        for (int y = mummyY + 13; y <= mummyY + 18; y++)
        {
            SetPixelLevel2(pixels, width, height, mummyX + 2, y, stone);
            SetPixelLevel2(pixels, width, height, mummyX + 3, y, ochre);
            SetPixelLevel2(pixels, width, height, mummyX + 6, y, ochre);
            SetPixelLevel2(pixels, width, height, mummyX + 7, y, stone);
        }
        for (int x = mummyX + 1; x <= mummyX + 4; x++)
            SetPixelLevel2(pixels, width, height, x, mummyY + 19, groove);
        for (int x = mummyX + 5; x <= mummyX + 8; x++)
            SetPixelLevel2(pixels, width, height, x, mummyY + 19, groove);

        // 3. Trampa de suelo
        int trapX = startX + 16;
        int trapY = startY + 34;
        for (int x = trapX; x <= trapX + 4; x++)
        {
            SetPixelLevel2(pixels, width, height, x, trapY, stone);
            SetPixelLevel2(pixels, width, height, x, trapY + 1, ochre);
            SetPixelLevel2(pixels, width, height, x, trapY + 2, groove);
        }
        for (int x = trapX + 5; x <= trapX + 19; x++)
        {
            SetPixelLevel2(pixels, width, height, x, trapY, fadedGold);
            SetPixelLevel2(pixels, width, height, x, trapY + 1, (x % 3 == 0) ? groove : stone);
            SetPixelLevel2(pixels, width, height, x, trapY + 2, groove);
        }
        for (int x = trapX + 20; x <= trapX + 24; x++)
        {
            SetPixelLevel2(pixels, width, height, x, trapY, stone);
            SetPixelLevel2(pixels, width, height, x, trapY + 1, ochre);
            SetPixelLevel2(pixels, width, height, x, trapY + 2, groove);
        }
    }

    private static void DrawArrowLevel2Glyph(Color[] pixels, int width, int height, int startX, int startY, Color redOchre, Color ochre)
    {
        for (int x = startX; x <= startX + 7; x++)
            SetPixelLevel2(pixels, width, height, x, startY + 1, redOchre);

        SetPixelLevel2(pixels, width, height, startX + 6, startY - 1, redOchre);
        SetPixelLevel2(pixels, width, height, startX + 7, startY, ochre);
        SetPixelLevel2(pixels, width, height, startX + 8, startY + 1, ochre);
        SetPixelLevel2(pixels, width, height, startX + 9, startY + 1, redOchre);
        SetPixelLevel2(pixels, width, height, startX + 7, startY + 2, ochre);
        SetPixelLevel2(pixels, width, height, startX + 6, startY + 3, redOchre);
    }

    private static void DrawMummyFallingTrap3xGlyph(
        Color[] pixels, int width, int height, int startX, int startY,
        Color stone, Color ochre, Color fadedGold, Color radiantGold, Color redOchre, Color groove)
    {
        // 1. Tres marcas doradas ("III") de caídas
        for (int i = 0; i < 3; i++)
        {
            int mx = startX + 4 + (i * 4);
            SetPixelLevel2(pixels, width, height, mx, startY + 2, radiantGold);
            for (int y = startY + 3; y <= startY + 7; y++)
            {
                SetPixelLevel2(pixels, width, height, mx, y, fadedGold);
            }
        }

        // 2. Flecha ritual descendente (⬇) hacia la trampa
        int arrowX = startX + 22;
        for (int y = startY + 2; y <= startY + 11; y++)
            SetPixelLevel2(pixels, width, height, arrowX, y, redOchre);

        SetPixelLevel2(pixels, width, height, arrowX - 2, startY + 9, redOchre);
        SetPixelLevel2(pixels, width, height, arrowX - 1, startY + 10, ochre);
        SetPixelLevel2(pixels, width, height, arrowX, startY + 12, redOchre);
        SetPixelLevel2(pixels, width, height, arrowX + 1, startY + 10, ochre);
        SetPixelLevel2(pixels, width, height, arrowX + 2, startY + 9, redOchre);

        // 3. Trampa con compuerta batiente abierta
        int trapPitX = startX + 10;
        int trapPitY = startY + 34;

        for (int x = trapPitX; x <= trapPitX + 6; x++)
        {
            SetPixelLevel2(pixels, width, height, x, trapPitY, ochre);
            SetPixelLevel2(pixels, width, height, x, trapPitY + 1, stone);
            SetPixelLevel2(pixels, width, height, x, trapPitY + 2, groove);
        }
        for (int x = trapPitX + 22; x <= trapPitX + 28; x++)
        {
            SetPixelLevel2(pixels, width, height, x, trapPitY, ochre);
            SetPixelLevel2(pixels, width, height, x, trapPitY + 1, stone);
            SetPixelLevel2(pixels, width, height, x, trapPitY + 2, groove);
        }
        for (int y = trapPitY + 1; y <= trapPitY + 9; y++)
        {
            SetPixelLevel2(pixels, width, height, trapPitX + 7, y, fadedGold);
            SetPixelLevel2(pixels, width, height, trapPitX + 8, y, groove);
        }
        for (int y = trapPitY + 1; y <= trapPitY + 10; y++)
        {
            SetPixelLevel2(pixels, width, height, trapPitX + 21, y, groove);
        }

        // 4. Momia precipitándose por el foso
        int fallMummyX = trapPitX + 11;
        int fallMummyY = startY + 16;

        for (int x = fallMummyX + 2; x <= fallMummyX + 7; x++)
            SetPixelLevel2(pixels, width, height, x, fallMummyY, stone);
        for (int x = fallMummyX + 1; x <= fallMummyX + 8; x++)
            SetPixelLevel2(pixels, width, height, x, fallMummyY + 1, ochre);
        SetPixelLevel2(pixels, width, height, fallMummyX + 3, fallMummyY + 2, redOchre);
        SetPixelLevel2(pixels, width, height, fallMummyX + 6, fallMummyY + 2, redOchre);

        for (int y = fallMummyY + 3; y <= fallMummyY + 10; y++)
        {
            for (int x = fallMummyX + 1; x <= fallMummyX + 8; x++)
            {
                SetPixelLevel2(pixels, width, height, x, y, (x % 2 == 0) ? ochre : stone);
            }
        }

        SetPixelLevel2(pixels, width, height, fallMummyX, fallMummyY - 2, stone);
        SetPixelLevel2(pixels, width, height, fallMummyX, fallMummyY - 1, ochre);
        SetPixelLevel2(pixels, width, height, fallMummyX + 9, fallMummyY - 2, stone);
        SetPixelLevel2(pixels, width, height, fallMummyX + 9, fallMummyY - 1, ochre);

        for (int y = fallMummyY + 11; y <= fallMummyY + 15; y++)
        {
            SetPixelLevel2(pixels, width, height, fallMummyX + 2, y, stone);
            SetPixelLevel2(pixels, width, height, fallMummyX + 3, y, ochre);
            SetPixelLevel2(pixels, width, height, fallMummyX + 6, y, ochre);
            SetPixelLevel2(pixels, width, height, fallMummyX + 7, y, stone);
        }
    }

    private static void DrawRenewedTreasureInChamberGlyph(
        Color[] pixels, int width, int height, int startX, int startY,
        Color stone, Color ochre, Color fadedGold, Color radiantGold, Color turquoise, Color groove)
    {
        // 1. Rayos celestiales dorados sobre el cofre renovado
        SetPixelLevel2(pixels, width, height, startX + 7, startY, radiantGold);
        SetPixelLevel2(pixels, width, height, startX + 7, startY + 1, radiantGold);
        SetPixelLevel2(pixels, width, height, startX + 3, startY + 1, fadedGold);
        SetPixelLevel2(pixels, width, height, startX + 4, startY + 2, radiantGold);
        SetPixelLevel2(pixels, width, height, startX + 11, startY + 1, fadedGold);
        SetPixelLevel2(pixels, width, height, startX + 10, startY + 2, radiantGold);

        // Disco solar / gema de cabecera
        for (int x = startX + 5; x <= startX + 9; x++)
            SetPixelLevel2(pixels, width, height, x, startY + 3, radiantGold);
        SetPixelLevel2(pixels, width, height, startX + 7, startY + 4, turquoise);

        // Tapa del cofre
        int chestY = startY + 5;
        for (int x = startX + 2; x <= startX + 12; x++)
            SetPixelLevel2(pixels, width, height, x, chestY, radiantGold);
        for (int x = startX + 1; x <= startX + 13; x++)
            SetPixelLevel2(pixels, width, height, x, chestY + 1, fadedGold);
        for (int x = startX; x <= startX + 14; x++)
            SetPixelLevel2(pixels, width, height, x, chestY + 2, groove);

        // Cuerpo del cofre ornamentado
        for (int y = chestY + 3; y <= chestY + 9; y++)
        {
            SetPixelLevel2(pixels, width, height, startX, y, stone);
            SetPixelLevel2(pixels, width, height, startX + 14, y, stone);
            for (int x = startX + 1; x <= startX + 13; x++)
            {
                SetPixelLevel2(pixels, width, height, x, y, ochre);
            }
        }

        // Cerradura sagrada dorada y gema turquesa
        SetPixelLevel2(pixels, width, height, startX + 6, chestY + 5, radiantGold);
        SetPixelLevel2(pixels, width, height, startX + 7, chestY + 5, turquoise);
        SetPixelLevel2(pixels, width, height, startX + 8, chestY + 5, radiantGold);
        SetPixelLevel2(pixels, width, height, startX + 6, chestY + 6, radiantGold);
        SetPixelLevel2(pixels, width, height, startX + 7, chestY + 6, turquoise);
        SetPixelLevel2(pixels, width, height, startX + 8, chestY + 6, radiantGold);

        // Base y patas
        for (int x = startX; x <= startX + 14; x++)
            SetPixelLevel2(pixels, width, height, x, chestY + 10, stone);
        SetPixelLevel2(pixels, width, height, startX + 1, chestY + 11, groove);
        SetPixelLevel2(pixels, width, height, startX + 2, chestY + 11, groove);
        SetPixelLevel2(pixels, width, height, startX + 12, chestY + 11, groove);
        SetPixelLevel2(pixels, width, height, startX + 13, chestY + 11, groove);

        // 2. Cruz Ankh (☥) sagrada de vida / renacimiento
        int ankhX = startX + 19;
        int ankhY = startY + 8;
        SetPixelLevel2(pixels, width, height, ankhX + 2, ankhY, fadedGold);
        SetPixelLevel2(pixels, width, height, ankhX + 3, ankhY, fadedGold);
        SetPixelLevel2(pixels, width, height, ankhX + 1, ankhY + 1, fadedGold);
        SetPixelLevel2(pixels, width, height, ankhX + 4, ankhY + 1, fadedGold);
        SetPixelLevel2(pixels, width, height, ankhX + 2, ankhY + 2, fadedGold);
        SetPixelLevel2(pixels, width, height, ankhX + 3, ankhY + 2, fadedGold);

        for (int x = ankhX; x <= ankhX + 5; x++)
            SetPixelLevel2(pixels, width, height, x, ankhY + 3, fadedGold);

        for (int y = ankhY + 4; y <= ankhY + 8; y++)
        {
            SetPixelLevel2(pixels, width, height, ankhX + 2, y, ochre);
            SetPixelLevel2(pixels, width, height, ankhX + 3, y, stone);
        }

        // 3. Indicador de la cámara del tesoro de Nivel 0
        int chamX = startX + 28;
        int chamY = startY + 7;
        for (int x = chamX; x <= chamX + 8; x++)
        {
            SetPixelLevel2(pixels, width, height, x, chamY, ochre);
            SetPixelLevel2(pixels, width, height, x, chamY + 1, stone);
        }
        for (int y = chamY + 2; y <= chamY + 9; y++)
        {
            SetPixelLevel2(pixels, width, height, chamX + 1, y, stone);
            SetPixelLevel2(pixels, width, height, chamX + 7, y, stone);
            for (int x = chamX + 2; x <= chamX + 6; x++)
            {
                SetPixelLevel2(pixels, width, height, x, y, groove);
            }
        }
        SetPixelLevel2(pixels, width, height, chamX + 3, chamY + 6, radiantGold);
        SetPixelLevel2(pixels, width, height, chamX + 4, chamY + 6, radiantGold);
        SetPixelLevel2(pixels, width, height, chamX + 5, chamY + 6, radiantGold);
        SetPixelLevel2(pixels, width, height, chamX + 3, chamY + 7, fadedGold);
        SetPixelLevel2(pixels, width, height, chamX + 4, chamY + 7, turquoise);
        SetPixelLevel2(pixels, width, height, chamX + 5, chamY + 7, fadedGold);

        for (int x = chamX; x <= chamX + 8; x++)
            SetPixelLevel2(pixels, width, height, x, chamY + 10, stone);
    }
}
