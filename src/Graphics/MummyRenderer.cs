using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;

namespace RetroGamePiramid.Graphics;

/// <summary>
/// Renderiza visualmente al enemigo Momia (Mommy) mediante sprites pixel-art procedurales de 16x16.
/// Implementa animación de marcha retro de 2 frames con vendajes envejecidos y ojos carmesí brillantes.
/// Garantiza cero asignaciones en memoria heap (0 allocations) en cada llamada a Draw.
/// </summary>
public sealed class MummyRenderer : IDisposable
{
    private readonly Texture2D _walk1Texture;
    private readonly Texture2D _walk2Texture;

    private Rectangle _destRect = new(0, 0, GameConstants.TILE_SIZE, GameConstants.TILE_SIZE);
    private bool _isDisposed;

    public MummyRenderer(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _walk1Texture = GenerateWalk1Texture(graphicsDevice);
        _walk2Texture = GenerateWalk2Texture(graphicsDevice);
    }

    /// <summary>
    /// Dibuja a la momia con su frame de animación y orientación actual.
    /// Cero allocations.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Mummy mummy, int frameCounter)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(mummy);

        // Ciclo de animación de marcha de 2 frames (alterna cada 12 ticks a 60 FPS)
        bool isFrame1 = (frameCounter / 12) % 2 == 0;
        Texture2D texture = isFrame1 ? _walk1Texture : _walk2Texture;

        SpriteEffects effects = mummy.Facing == Direction.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        // Centrado respecto al hitbox de 14x16 en el tile de 16x16
        _destRect.X = (int)MathF.Round(mummy.Position.X - 1f);
        _destRect.Y = (int)MathF.Round(mummy.Position.Y);

        spriteBatch.Draw(texture, _destRect, null, Color.White, 0f, Vector2.Zero, effects, 0f);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _walk1Texture?.Dispose();
            _walk2Texture?.Dispose();
            _isDisposed = true;
        }
    }

    private static Texture2D GenerateWalk1Texture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color wrapLight = new(240, 230, 200);
        Color wrapBase = new(210, 195, 160);
        Color wrapDark = new(150, 135, 100);
        Color outline = new(65, 50, 30);
        Color eyeGlow = new(255, 30, 30);
        Color eyePupil = new(255, 230, 80);
        Color goldAmulet = new(225, 185, 45);
        Color jewelCyan = new(40, 210, 230);

        // --- Cabeza con vendajes (filas 1 a 5) ---
        // Corona/parte superior
        SetPixel(pixels, 5, 1, outline); SetPixel(pixels, 6, 1, wrapLight); SetPixel(pixels, 7, 1, wrapLight); SetPixel(pixels, 8, 1, outline);
        for (int x = 4; x <= 9; x++)
        {
            SetPixel(pixels, x, 2, (x == 4 || x == 9) ? outline : (x % 2 == 0 ? wrapLight : wrapBase));
        }

        // Hendidura de ojos y mirada carmesí
        SetPixel(pixels, 4, 3, outline);
        SetPixel(pixels, 5, 3, wrapDark);
        SetPixel(pixels, 6, 3, wrapBase);
        SetPixel(pixels, 7, 3, eyeGlow);
        SetPixel(pixels, 8, 3, eyePupil);
        SetPixel(pixels, 9, 3, outline);

        // Mandíbula vendada
        for (int x = 4; x <= 9; x++)
        {
            SetPixel(pixels, x, 4, (x == 4 || x == 9) ? outline : (x % 2 == 0 ? wrapDark : wrapBase));
            SetPixel(pixels, x, 5, (x == 4 || x == 9) ? outline : wrapBase);
        }

        // --- Torso y brazos extendidos (filas 6 a 10) ---
        for (int x = 4; x <= 8; x++)
        {
            SetPixel(pixels, x, 6, wrapLight);
        }
        SetPixel(pixels, 3, 6, outline);
        SetPixel(pixels, 9, 6, outline);

        // Brazos momificados alzados hacia delante (x=9 a 13)
        SetPixel(pixels, 3, 7, outline);
        SetPixel(pixels, 4, 7, wrapBase);
        SetPixel(pixels, 5, 7, goldAmulet);
        SetPixel(pixels, 6, 7, jewelCyan);
        SetPixel(pixels, 7, 7, wrapBase);
        SetPixel(pixels, 8, 7, wrapLight);
        SetPixel(pixels, 9, 7, wrapLight);
        SetPixel(pixels, 10, 7, wrapBase);
        SetPixel(pixels, 11, 7, wrapLight);
        SetPixel(pixels, 12, 7, outline);

        SetPixel(pixels, 4, 8, outline);
        SetPixel(pixels, 5, 8, wrapDark);
        SetPixel(pixels, 6, 8, goldAmulet);
        SetPixel(pixels, 7, 8, wrapBase);
        SetPixel(pixels, 8, 8, wrapLight);
        SetPixel(pixels, 9, 8, wrapBase);
        SetPixel(pixels, 10, 8, wrapLight);
        SetPixel(pixels, 11, 8, outline);

        // Cinturón y vendas deshilachadas
        for (int x = 4; x <= 8; x++)
        {
            SetPixel(pixels, x, 9, (x % 2 == 0) ? wrapDark : wrapBase);
            SetPixel(pixels, x, 10, (x % 2 == 0) ? wrapBase : wrapLight);
        }
        SetPixel(pixels, 3, 10, wrapDark); // Tira de lino colgante

        // --- Piernas paso A (filas 11 a 15) ---
        // Pierna trasera (izquierda)
        SetPixel(pixels, 4, 11, wrapBase); SetPixel(pixels, 5, 11, wrapLight);
        SetPixel(pixels, 4, 12, wrapDark); SetPixel(pixels, 5, 12, wrapBase);
        SetPixel(pixels, 3, 13, outline);  SetPixel(pixels, 4, 13, wrapBase);
        SetPixel(pixels, 3, 14, outline);  SetPixel(pixels, 4, 14, wrapDark);
        SetPixel(pixels, 3, 15, outline);  SetPixel(pixels, 4, 15, wrapBase);

        // Pierna delantera (derecha)
        SetPixel(pixels, 7, 11, wrapLight); SetPixel(pixels, 8, 11, wrapBase);
        SetPixel(pixels, 7, 12, wrapBase);  SetPixel(pixels, 8, 12, wrapLight);
        SetPixel(pixels, 7, 13, wrapBase);  SetPixel(pixels, 8, 13, outline);
        SetPixel(pixels, 8, 14, wrapLight); SetPixel(pixels, 9, 14, outline);
        SetPixel(pixels, 8, 15, wrapDark);  SetPixel(pixels, 9, 15, outline);

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateWalk2Texture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color wrapLight = new(240, 230, 200);
        Color wrapBase = new(210, 195, 160);
        Color wrapDark = new(150, 135, 100);
        Color outline = new(65, 50, 30);
        Color eyeGlow = new(255, 30, 30);
        Color eyePupil = new(255, 230, 80);
        Color goldAmulet = new(225, 185, 45);
        Color jewelCyan = new(40, 210, 230);

        // --- Cabeza con vendajes (filas 1 a 5) ---
        SetPixel(pixels, 5, 1, outline); SetPixel(pixels, 6, 1, wrapLight); SetPixel(pixels, 7, 1, wrapLight); SetPixel(pixels, 8, 1, outline);
        for (int x = 4; x <= 9; x++)
        {
            SetPixel(pixels, x, 2, (x == 4 || x == 9) ? outline : (x % 2 == 0 ? wrapBase : wrapLight));
        }

        // Hendidura de ojos y mirada carmesí
        SetPixel(pixels, 4, 3, outline);
        SetPixel(pixels, 5, 3, wrapDark);
        SetPixel(pixels, 6, 3, wrapBase);
        SetPixel(pixels, 7, 3, eyeGlow);
        SetPixel(pixels, 8, 3, eyePupil);
        SetPixel(pixels, 9, 3, outline);

        // Mandíbula vendada
        for (int x = 4; x <= 9; x++)
        {
            SetPixel(pixels, x, 4, (x == 4 || x == 9) ? outline : (x % 2 == 0 ? wrapBase : wrapDark));
            SetPixel(pixels, x, 5, (x == 4 || x == 9) ? outline : wrapBase);
        }

        // --- Torso y brazos extendidos (filas 6 a 10) ---
        for (int x = 4; x <= 8; x++)
        {
            SetPixel(pixels, x, 6, wrapBase);
        }
        SetPixel(pixels, 3, 6, outline);
        SetPixel(pixels, 9, 6, outline);

        // Brazos extendidos hacia delante con oscilación de marcha
        SetPixel(pixels, 3, 7, outline);
        SetPixel(pixels, 4, 7, wrapLight);
        SetPixel(pixels, 5, 7, goldAmulet);
        SetPixel(pixels, 6, 7, jewelCyan);
        SetPixel(pixels, 7, 7, wrapLight);
        SetPixel(pixels, 8, 7, wrapBase);
        SetPixel(pixels, 9, 7, wrapLight);
        SetPixel(pixels, 10, 7, wrapBase);
        SetPixel(pixels, 11, 7, outline);

        SetPixel(pixels, 4, 8, outline);
        SetPixel(pixels, 5, 8, wrapDark);
        SetPixel(pixels, 6, 8, goldAmulet);
        SetPixel(pixels, 7, 8, wrapBase);
        SetPixel(pixels, 8, 8, wrapLight);
        SetPixel(pixels, 9, 8, wrapLight);
        SetPixel(pixels, 10, 8, wrapLight);
        SetPixel(pixels, 11, 8, wrapBase);
        SetPixel(pixels, 12, 8, outline);

        for (int x = 4; x <= 8; x++)
        {
            SetPixel(pixels, x, 9, (x % 2 == 0) ? wrapBase : wrapDark);
            SetPixel(pixels, x, 10, (x % 2 == 0) ? wrapLight : wrapBase);
        }
        SetPixel(pixels, 3, 9, wrapDark); // Tira de lino colgante

        // --- Piernas paso B (filas 11 a 15) ---
        // Pierna izquierda adelantada
        SetPixel(pixels, 4, 11, wrapLight); SetPixel(pixels, 5, 11, wrapBase);
        SetPixel(pixels, 4, 12, wrapBase);  SetPixel(pixels, 5, 12, wrapDark);
        SetPixel(pixels, 5, 13, wrapLight); SetPixel(pixels, 6, 13, outline);
        SetPixel(pixels, 5, 14, wrapBase);  SetPixel(pixels, 6, 14, outline);
        SetPixel(pixels, 5, 15, wrapDark);  SetPixel(pixels, 6, 15, outline);

        // Pierna derecha atrasada
        SetPixel(pixels, 7, 11, wrapDark);  SetPixel(pixels, 8, 11, wrapBase);
        SetPixel(pixels, 7, 12, wrapBase);  SetPixel(pixels, 8, 12, wrapLight);
        SetPixel(pixels, 8, 13, wrapBase);  SetPixel(pixels, 9, 13, outline);
        SetPixel(pixels, 8, 14, wrapDark);  SetPixel(pixels, 9, 14, outline);
        SetPixel(pixels, 8, 15, wrapBase);  SetPixel(pixels, 9, 15, outline);

        texture.SetData(pixels);
        return texture;
    }

    private static void SetPixel(Color[] pixels, int x, int y, Color color)
    {
        if (x >= 0 && x < 16 && y >= 0 && y < 16)
        {
            pixels[y * 16 + x] = color;
        }
    }
}
