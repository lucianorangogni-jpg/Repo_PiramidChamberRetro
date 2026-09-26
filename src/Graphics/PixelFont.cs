using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RetroGamePiramid.Graphics;

/// <summary>
/// Fuente bitmap procedural estilo arcade retro (atlas 8x8 para títulos y atlas mini 4x6 para HUD).
/// Genera los atlas y texturas en memoria durante la inicialización y garantiza cero asignaciones (0 allocations)
/// en cada llamada de dibujado.
/// </summary>
public sealed class PixelFont : IDisposable
{
    // Constantes del atlas 8x8 estándar
    public const int GLYPH_WIDTH = 8;
    public const int GLYPH_HEIGHT = 8;
    public const int ATLAS_COLS = 16;
    public const int ATLAS_ROWS = 8;

    // Constantes del atlas mini 4x6
    public const int MINI_GLYPH_WIDTH = 4;
    public const int MINI_GLYPH_HEIGHT = 6;
    public const int MINI_ATLAS_COLS = 16;
    public const int MINI_ATLAS_ROWS = 4;

    private readonly Texture2D _atlasTexture;
    private readonly Texture2D _miniAtlasTexture;
    private readonly Texture2D _pixelTexture;

    // Rectángulos reutilizables para cero allocations
    private Rectangle _srcRect = new(0, 0, GLYPH_WIDTH, GLYPH_HEIGHT);
    private Rectangle _destRect = new(0, 0, GLYPH_WIDTH, GLYPH_HEIGHT);
    private Rectangle _miniSrcRect = new(0, 0, MINI_GLYPH_WIDTH, MINI_GLYPH_HEIGHT);
    private Rectangle _miniDestRect = new(0, 0, MINI_GLYPH_WIDTH, MINI_GLYPH_HEIGHT);
    private Rectangle _boxRect = new(0, 0, 1, 1);

    private bool _isDisposed;

    public PixelFont(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        // 1. Textura 1x1 para dibujar marcos, recuadros y fondos sin allocations
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // 2. Atlas 8x8 para menús y títulos
        int atlasWidth = ATLAS_COLS * GLYPH_WIDTH;
        int atlasHeight = ATLAS_ROWS * GLYPH_HEIGHT;
        _atlasTexture = new Texture2D(graphicsDevice, atlasWidth, atlasHeight);
        Color[] pixels8 = new Color[atlasWidth * atlasHeight];
        Array.Fill(pixels8, Color.Transparent);

        for (int ascii = 32; ascii <= 90; ascii++)
        {
            int glyphIndex = ascii - 32;
            int col = glyphIndex % ATLAS_COLS;
            int row = glyphIndex / ATLAS_COLS;
            int startX = col * GLYPH_WIDTH;
            int startY = row * GLYPH_HEIGHT;

            ReadOnlySpan<byte> glyphData = GetGlyphData(ascii);
            for (int y = 0; y < GLYPH_HEIGHT; y++)
            {
                byte rowBits = glyphData[y];
                for (int x = 0; x < GLYPH_WIDTH; x++)
                {
                    bool isSet = (rowBits & (1 << (7 - x))) != 0;
                    if (isSet)
                    {
                        pixels8[(startY + y) * atlasWidth + (startX + x)] = Color.White;
                    }
                }
            }
        }
        _atlasTexture.SetData(pixels8);

        // 3. Atlas mini 4x6 para textos pequeños y marcos de opciones en pantalla
        int miniAtlasWidth = MINI_ATLAS_COLS * MINI_GLYPH_WIDTH;
        int miniAtlasHeight = MINI_ATLAS_ROWS * MINI_GLYPH_HEIGHT;
        _miniAtlasTexture = new Texture2D(graphicsDevice, miniAtlasWidth, miniAtlasHeight);
        Color[] pixelsMini = new Color[miniAtlasWidth * miniAtlasHeight];
        Array.Fill(pixelsMini, Color.Transparent);

        for (int ascii = 32; ascii <= 93; ascii++)
        {
            int glyphIndex = ascii - 32;
            int col = glyphIndex % MINI_ATLAS_COLS;
            int row = glyphIndex / MINI_ATLAS_COLS;
            int startX = col * MINI_GLYPH_WIDTH;
            int startY = row * MINI_GLYPH_HEIGHT;

            ReadOnlySpan<byte> miniData = GetMiniGlyphData(ascii);
            for (int y = 0; y < MINI_GLYPH_HEIGHT; y++)
            {
                byte rowBits = miniData[y];
                for (int x = 0; x < MINI_GLYPH_WIDTH; x++)
                {
                    bool isSet = (rowBits & (1 << (3 - x))) != 0;
                    if (isSet)
                    {
                        pixelsMini[(startY + y) * miniAtlasWidth + (startX + x)] = Color.White;
                    }
                }
            }
        }
        _miniAtlasTexture.SetData(pixelsMini);
    }

    /// <summary>
    /// Dibuja texto con la fuente estándar 8x8.
    /// </summary>
    public void DrawText(SpriteBatch spriteBatch, string text, int x, int y, Color color, int scale = 1)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        if (string.IsNullOrEmpty(text))
            return;

        int currentX = x;
        int scaledWidth = GLYPH_WIDTH * scale;
        int scaledHeight = GLYPH_HEIGHT * scale;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '\n')
            {
                x = currentX;
                y += scaledHeight + (2 * scale);
                continue;
            }

            char upper = char.ToUpperInvariant(c);
            int ascii = upper;
            if (ascii < 32 || ascii > 90)
                ascii = 32;

            int glyphIndex = ascii - 32;
            int glyphCol = glyphIndex % ATLAS_COLS;
            int glyphRow = glyphIndex / ATLAS_COLS;

            _srcRect.X = glyphCol * GLYPH_WIDTH;
            _srcRect.Y = glyphRow * GLYPH_HEIGHT;

            _destRect.X = currentX;
            _destRect.Y = y;
            _destRect.Width = scaledWidth;
            _destRect.Height = scaledHeight;

            spriteBatch.Draw(_atlasTexture, _destRect, _srcRect, color);

            currentX += scaledWidth;
        }
    }

    /// <summary>
    /// Dibuja un solo carácter en tamaño mini (4x6). Retorna el avance horizontal en píxeles (5 px).
    /// Cero allocations.
    /// </summary>
    public int DrawMiniChar(SpriteBatch spriteBatch, char c, int x, int y, Color color)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        char upper = char.ToUpperInvariant(c);
        int ascii = upper;
        if (ascii < 32 || ascii > 93)
            ascii = 32;

        int glyphIndex = ascii - 32;
        int glyphCol = glyphIndex % MINI_ATLAS_COLS;
        int glyphRow = glyphIndex / MINI_ATLAS_COLS;

        _miniSrcRect.X = glyphCol * MINI_GLYPH_WIDTH;
        _miniSrcRect.Y = glyphRow * MINI_GLYPH_HEIGHT;

        _miniDestRect.X = x;
        _miniDestRect.Y = y;
        _miniDestRect.Width = MINI_GLYPH_WIDTH;
        _miniDestRect.Height = MINI_GLYPH_HEIGHT;

        spriteBatch.Draw(_miniAtlasTexture, _miniDestRect, _miniSrcRect, color);

        return MINI_GLYPH_WIDTH + 1; // 5 píxeles de avance por carácter
    }

    /// <summary>
    /// Dibuja texto en tamaño mini (4x6 píxeles por carácter) con espaciado de 1 píxel.
    /// Ideal para carteles compactos, HUD y marcos de opciones.
    /// Cero allocations.
    /// </summary>
    public void DrawMiniText(SpriteBatch spriteBatch, string text, int x, int y, Color color)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        if (string.IsNullOrEmpty(text))
            return;

        int currentX = x;
        for (int i = 0; i < text.Length; i++)
        {
            currentX += DrawMiniChar(spriteBatch, text[i], currentX, y, color);
        }
    }

    /// <summary>
    /// Dibuja un número entero usando la fuente mini (4x6) sin ninguna asignación en el heap (Zero Allocations).
    /// </summary>
    public int DrawMiniInt(SpriteBatch spriteBatch, int value, int x, int y, Color color, int minDigits = 1)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        Span<char> buffer = stackalloc char[16];
        int idx = buffer.Length;

        bool isNegative = value < 0;
        long v = isNegative ? -(long)value : value;

        do
        {
            buffer[--idx] = (char)('0' + (v % 10));
            v /= 10;
        } while (v > 0);

        while (buffer.Length - idx < minDigits)
        {
            buffer[--idx] = '0';
        }

        if (isNegative)
        {
            buffer[--idx] = '-';
        }

        int currentX = x;
        for (int i = idx; i < buffer.Length; i++)
        {
            currentX += DrawMiniChar(spriteBatch, buffer[i], currentX, y, color);
        }

        return currentX - x;
    }

    /// <summary>
    /// Dibuja un marco/recuadro decorativo retro con fondo y borde de 1 píxel.
    /// Cero allocations.
    /// </summary>
    public void DrawFrameBox(SpriteBatch spriteBatch, int x, int y, int width, int height, Color bgColor, Color borderColor)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        // 1. Fondo
        _boxRect.X = x;
        _boxRect.Y = y;
        _boxRect.Width = width;
        _boxRect.Height = height;
        spriteBatch.Draw(_pixelTexture, _boxRect, bgColor);

        // 2. Borde superior
        _boxRect.Height = 1;
        spriteBatch.Draw(_pixelTexture, _boxRect, borderColor);

        // 3. Borde inferior
        _boxRect.Y = y + height - 1;
        spriteBatch.Draw(_pixelTexture, _boxRect, borderColor);

        // 4. Borde izquierdo
        _boxRect.Y = y;
        _boxRect.Width = 1;
        _boxRect.Height = height;
        spriteBatch.Draw(_pixelTexture, _boxRect, borderColor);

        // 5. Borde derecho
        _boxRect.X = x + width - 1;
        spriteBatch.Draw(_pixelTexture, _boxRect, borderColor);
    }

    /// <summary>
    /// Dibuja texto 8x8 centrado horizontalmente.
    /// </summary>
    public void DrawTextCentered(SpriteBatch spriteBatch, string text, int totalWidth, int y, Color color, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
            return;

        int textPixelWidth = text.Length * GLYPH_WIDTH * scale;
        int startX = (totalWidth - textPixelWidth) / 2;
        DrawText(spriteBatch, text, startX, y, color, scale);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _pixelTexture?.Dispose();
            _miniAtlasTexture?.Dispose();
            _atlasTexture?.Dispose();
            _isDisposed = true;
        }
    }

    private static ReadOnlySpan<byte> GetGlyphData(int ascii) => ascii switch
    {
        32 => [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], // Space
        33 => [0x18, 0x18, 0x18, 0x18, 0x18, 0x00, 0x18, 0x00], // !
        34 => [0x66, 0x66, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00], // "
        35 => [0x6C, 0x6C, 0xFE, 0x6C, 0xFE, 0x6C, 0x6C, 0x00], // #
        36 => [0x18, 0x7E, 0xD8, 0x7C, 0x1B, 0x7E, 0x18, 0x00], // $
        37 => [0xC6, 0xCC, 0x18, 0x30, 0x60, 0xC6, 0x06, 0x00], // %
        38 => [0x38, 0x4C, 0x4C, 0x38, 0x59, 0x4E, 0x39, 0x00], // &
        39 => [0x18, 0x18, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00], // '
        40 => [0x0C, 0x18, 0x30, 0x30, 0x30, 0x18, 0x0C, 0x00], // (
        41 => [0x30, 0x18, 0x0C, 0x0C, 0x0C, 0x18, 0x30, 0x00], // )
        42 => [0x00, 0x66, 0x3C, 0xFF, 0x3C, 0x66, 0x00, 0x00], // *
        43 => [0x00, 0x18, 0x18, 0x7E, 0x18, 0x18, 0x00, 0x00], // +
        44 => [0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x18, 0x30], // ,
        45 => [0x00, 0x00, 0x00, 0x7E, 0x00, 0x00, 0x00, 0x00], // -
        46 => [0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x18, 0x00], // .
        47 => [0x06, 0x0C, 0x18, 0x30, 0x60, 0xC0, 0x80, 0x00], // /
        48 => [0x3C, 0x66, 0x6E, 0x76, 0x66, 0x66, 0x3C, 0x00], // 0
        49 => [0x18, 0x38, 0x18, 0x18, 0x18, 0x18, 0x7E, 0x00], // 1
        50 => [0x3C, 0x66, 0x06, 0x0C, 0x18, 0x30, 0x7E, 0x00], // 2
        51 => [0x3C, 0x66, 0x06, 0x1C, 0x06, 0x66, 0x3C, 0x00], // 3
        52 => [0x0C, 0x1C, 0x3C, 0x6C, 0xFE, 0x0C, 0x0C, 0x00], // 4
        53 => [0x7E, 0x60, 0x7C, 0x06, 0x06, 0x66, 0x3C, 0x00], // 5
        54 => [0x1C, 0x30, 0x60, 0x7C, 0x66, 0x66, 0x3C, 0x00], // 6
        55 => [0x7E, 0x06, 0x0C, 0x18, 0x30, 0x30, 0x30, 0x00], // 7
        56 => [0x3C, 0x66, 0x66, 0x3C, 0x66, 0x66, 0x3C, 0x00], // 8
        57 => [0x3C, 0x66, 0x66, 0x3E, 0x06, 0x0C, 0x38, 0x00], // 9
        58 => [0x00, 0x18, 0x18, 0x00, 0x18, 0x18, 0x00, 0x00], // :
        59 => [0x00, 0x18, 0x18, 0x00, 0x18, 0x18, 0x30, 0x00], // ;
        60 => [0x0E, 0x1C, 0x38, 0x70, 0x38, 0x1C, 0x0E, 0x00], // <
        61 => [0x00, 0x7E, 0x00, 0x7E, 0x00, 0x00, 0x00, 0x00], // =
        62 => [0x70, 0x38, 0x1C, 0x0E, 0x1C, 0x38, 0x70, 0x00], // >
        63 => [0x3C, 0x66, 0x06, 0x0C, 0x18, 0x00, 0x18, 0x00], // ?
        64 => [0x3C, 0x66, 0x6E, 0x6E, 0x60, 0x62, 0x3C, 0x00], // @
        65 => [0x18, 0x3C, 0x66, 0x66, 0x7E, 0x66, 0x66, 0x00], // A
        66 => [0x7C, 0x66, 0x66, 0x7C, 0x66, 0x66, 0x7C, 0x00], // B
        67 => [0x3C, 0x66, 0x60, 0x60, 0x60, 0x66, 0x3C, 0x00], // C
        68 => [0x78, 0x6C, 0x66, 0x66, 0x66, 0x6C, 0x78, 0x00], // D
        69 => [0x7E, 0x60, 0x60, 0x7C, 0x60, 0x60, 0x7E, 0x00], // E
        70 => [0x7E, 0x60, 0x60, 0x7C, 0x60, 0x60, 0x60, 0x00], // F
        71 => [0x3C, 0x66, 0x60, 0x6E, 0x66, 0x66, 0x3C, 0x00], // G
        72 => [0x66, 0x66, 0x66, 0x7E, 0x66, 0x66, 0x66, 0x00], // H
        73 => [0x3C, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3C, 0x00], // I
        74 => [0x1E, 0x0C, 0x0C, 0x0C, 0x0C, 0x6C, 0x38, 0x00], // J
        75 => [0x66, 0x6C, 0x78, 0x70, 0x78, 0x6C, 0x66, 0x00], // K
        76 => [0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x7E, 0x00], // L
        77 => [0x63, 0x77, 0x7F, 0x6B, 0x63, 0x63, 0x63, 0x00], // M
        78 => [0x66, 0x76, 0x7E, 0x7E, 0x6E, 0x66, 0x66, 0x00], // N
        79 => [0x3C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x00], // O
        80 => [0x7C, 0x66, 0x66, 0x7C, 0x60, 0x60, 0x60, 0x00], // P
        81 => [0x3C, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x0E, 0x00], // Q
        82 => [0x7C, 0x66, 0x66, 0x7C, 0x78, 0x6C, 0x66, 0x00], // R
        83 => [0x3C, 0x66, 0x60, 0x3C, 0x06, 0x66, 0x3C, 0x00], // S
        84 => [0x7E, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x00], // T
        85 => [0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x00], // U
        86 => [0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x18, 0x00], // V
        87 => [0x63, 0x63, 0x63, 0x6B, 0x7F, 0x77, 0x63, 0x00], // W
        88 => [0x66, 0x66, 0x3C, 0x18, 0x3C, 0x66, 0x66, 0x00], // X
        89 => [0x66, 0x66, 0x66, 0x3C, 0x18, 0x18, 0x18, 0x00], // Y
        90 => [0x7E, 0x06, 0x0C, 0x18, 0x30, 0x60, 0x7E, 0x00], // Z
        _ => [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
    };

    private static ReadOnlySpan<byte> GetMiniGlyphData(int ascii) => ascii switch
    {
        32 => [0x0, 0x0, 0x0, 0x0, 0x0, 0x0], // Space
        33 => [0x2, 0x2, 0x2, 0x0, 0x2, 0x0], // !
        34 => [0x5, 0x5, 0x0, 0x0, 0x0, 0x0], // "
        35 => [0x5, 0xF, 0x5, 0xF, 0x5, 0x0], // #
        36 => [0x7, 0xA, 0x6, 0x5, 0xE, 0x0], // $
        37 => [0x9, 0x2, 0x4, 0x8, 0x9, 0x0], // %
        38 => [0x4, 0xA, 0x4, 0xA, 0x5, 0x0], // &
        39 => [0x2, 0x2, 0x0, 0x0, 0x0, 0x0], // '
        40 => [0x2, 0x4, 0x4, 0x4, 0x2, 0x0], // (
        41 => [0x4, 0x2, 0x2, 0x2, 0x4, 0x0], // )
        42 => [0x0, 0xA, 0x4, 0xA, 0x0, 0x0], // *
        43 => [0x0, 0x2, 0x7, 0x2, 0x0, 0x0], // +
        44 => [0x0, 0x0, 0x0, 0x2, 0x2, 0x4], // ,
        45 => [0x0, 0x0, 0xF, 0x0, 0x0, 0x0], // -
        46 => [0x0, 0x0, 0x0, 0x0, 0x2, 0x0], // .
        47 => [0x1, 0x2, 0x4, 0x8, 0x0, 0x0], // /
        48 => [0x6, 0x9, 0x9, 0x9, 0x6, 0x0], // 0
        49 => [0x2, 0x6, 0x2, 0x2, 0x7, 0x0], // 1
        50 => [0x6, 0x9, 0x2, 0x4, 0xF, 0x0], // 2
        51 => [0xE, 0x1, 0x6, 0x1, 0xE, 0x0], // 3
        52 => [0x9, 0x9, 0xF, 0x1, 0x1, 0x0], // 4
        53 => [0xF, 0x8, 0xE, 0x1, 0xE, 0x0], // 5
        54 => [0x6, 0x8, 0xE, 0x9, 0x6, 0x0], // 6
        55 => [0xF, 0x1, 0x2, 0x4, 0x4, 0x0], // 7
        56 => [0x6, 0x9, 0x6, 0x9, 0x6, 0x0], // 8
        57 => [0x6, 0x9, 0x7, 0x1, 0x6, 0x0], // 9
        58 => [0x0, 0x6, 0x0, 0x6, 0x0, 0x0], // :
        59 => [0x0, 0x2, 0x0, 0x2, 0x4, 0x0], // ;
        60 => [0x2, 0x4, 0x8, 0x4, 0x2, 0x0], // <
        61 => [0x0, 0xF, 0x0, 0xF, 0x0, 0x0], // =
        62 => [0x4, 0x2, 0x1, 0x2, 0x4, 0x0], // >
        63 => [0xE, 0x1, 0x2, 0x0, 0x2, 0x0], // ?
        64 => [0x6, 0x9, 0xB, 0x8, 0x7, 0x0], // @
        65 => [0x6, 0x9, 0xF, 0x9, 0x9, 0x0], // A
        66 => [0xE, 0x9, 0xE, 0x9, 0xE, 0x0], // B
        67 => [0x7, 0x8, 0x8, 0x8, 0x7, 0x0], // C
        68 => [0xE, 0x9, 0x9, 0x9, 0xE, 0x0], // D
        69 => [0xF, 0x8, 0xE, 0x8, 0xF, 0x0], // E
        70 => [0xF, 0x8, 0xE, 0x8, 0x8, 0x0], // F
        71 => [0x7, 0x8, 0xB, 0x9, 0x7, 0x0], // G
        72 => [0x9, 0x9, 0xF, 0x9, 0x9, 0x0], // H
        73 => [0x7, 0x2, 0x2, 0x2, 0x7, 0x0], // I
        74 => [0x1, 0x1, 0x1, 0x9, 0x6, 0x0], // J
        75 => [0x9, 0xA, 0xC, 0xA, 0x9, 0x0], // K
        76 => [0x8, 0x8, 0x8, 0x8, 0xF, 0x0], // L
        77 => [0x9, 0xF, 0xF, 0x9, 0x9, 0x0], // M
        78 => [0x9, 0xD, 0xB, 0x9, 0x9, 0x0], // N
        79 => [0x6, 0x9, 0x9, 0x9, 0x6, 0x0], // O
        80 => [0xE, 0x9, 0xE, 0x8, 0x8, 0x0], // P
        81 => [0x6, 0x9, 0x9, 0xB, 0x7, 0x0], // Q
        82 => [0xE, 0x9, 0xE, 0xA, 0x9, 0x0], // R
        83 => [0x7, 0x8, 0x6, 0x1, 0xE, 0x0], // S
        84 => [0x7, 0x2, 0x2, 0x2, 0x2, 0x0], // T
        85 => [0x9, 0x9, 0x9, 0x9, 0x6, 0x0], // U
        86 => [0x9, 0x9, 0x9, 0x6, 0x2, 0x0], // V
        87 => [0x9, 0x9, 0xF, 0xF, 0x9, 0x0], // W
        88 => [0x9, 0x9, 0x6, 0x9, 0x9, 0x0], // X
        89 => [0x9, 0x9, 0x6, 0x2, 0x2, 0x0], // Y
        90 => [0xF, 0x1, 0x6, 0x8, 0xF, 0x0], // Z
        91 => [0x7, 0x6, 0x6, 0x6, 0x7, 0x0], // [
        92 => [0x8, 0x4, 0x2, 0x1, 0x0, 0x0], // \
        93 => [0xE, 0x6, 0x6, 0x6, 0xE, 0x0], // ]
        _ => [0x0, 0x0, 0x0, 0x0, 0x0, 0x0]
    };
}
