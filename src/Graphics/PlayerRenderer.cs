using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;

namespace RetroGamePiramid.Graphics;

/// <summary>
/// Renderiza al arqueólogo protagonista mediante sprites pixel-art procedurales de 16x16.
/// Cero asignaciones en memoria heap (0 allocations) en cada llamada a Draw.
/// </summary>
public sealed class PlayerRenderer : IDisposable
{
    private readonly Texture2D _idleTexture;
    private readonly Texture2D _walk1Texture;
    private readonly Texture2D _walk2Texture;
    private readonly Texture2D _climb1Texture;
    private readonly Texture2D _climb2Texture;
    private readonly Texture2D _jumpTexture;
    private readonly Texture2D _eliminatedTexture;

    // Rectángulo reutilizable para dibujar sin generar garbage
    private Rectangle _destRect = new(0, 0, GameConstants.TILE_SIZE, GameConstants.TILE_SIZE);
    private bool _isDisposed;

    public PlayerRenderer(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _idleTexture = GenerateIdleTexture(graphicsDevice);
        _walk1Texture = GenerateWalk1Texture(graphicsDevice);
        _walk2Texture = GenerateWalk2Texture(graphicsDevice);
        _climb1Texture = GenerateClimb1Texture(graphicsDevice);
        _climb2Texture = GenerateClimb2Texture(graphicsDevice);
        _jumpTexture = GenerateJumpTexture(graphicsDevice);
        _eliminatedTexture = GenerateEliminatedTexture(graphicsDevice);
    }

    /// <summary>
    /// Dibuja al arqueólogo con su frame de animación y orientación actual.
    /// Totalmente libre de allocations.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Player player)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(player);

        Texture2D texture = SelectFrame(player);
        SpriteEffects effects = player.Facing == Direction.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        // Centrado respecto al hitbox de 14x16 en el tile de 16x16
        _destRect.X = (int)MathF.Round(player.Position.X - 1f);
        _destRect.Y = (int)MathF.Round(player.Position.Y);

        spriteBatch.Draw(texture, _destRect, null, Color.White, 0f, Vector2.Zero, effects, 0f);
    }

    private Texture2D SelectFrame(Player player)
    {
        switch (player.State)
        {
            case PlayerState.Eliminated:
                return _eliminatedTexture;

            case PlayerState.Jumping:
            case PlayerState.Falling:
                return _jumpTexture;

            case PlayerState.Climbing:
                // Alternar cada 10 frames si se está moviendo en la escalera
                return (player.AnimationTimer / 10) % 2 == 0 ? _climb1Texture : _climb2Texture;

            case PlayerState.Walking:
                // Alternar pasos de caminata cada 8 frames
                return (player.AnimationTimer / 8) % 2 == 0 ? _walk1Texture : _walk2Texture;

            case PlayerState.Idle:
            default:
                return _idleTexture;
        }
    }

    private static Texture2D GenerateIdleTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color hatTop = new(220, 195, 140);
        Color hatBrim = new(190, 165, 110);
        Color hatBand = new(85, 45, 20);
        Color skin = new(255, 210, 165);
        Color eye = new(25, 15, 10);
        Color mustache = new(70, 38, 16);
        Color shirt = new(195, 160, 105);
        Color belt = new(75, 40, 18);
        Color buckle = new(255, 215, 60);
        Color pants = new(135, 100, 60);
        Color boot = new(50, 30, 15);

        // Sombrero salacot (filas 1 a 4)
        SetLine(pixels, 2, 5, 10, hatTop);
        SetLine(pixels, 3, 4, 11, hatTop);
        SetLine(pixels, 4, 3, 12, hatBand);
        SetLine(pixels, 5, 2, 13, hatBrim);

        // Cara y cuello (filas 6 a 8)
        SetLine(pixels, 6, 5, 10, skin);
        pixels[6 * 16 + 8] = eye;
        SetLine(pixels, 7, 5, 10, skin);
        pixels[7 * 16 + 8] = mustache;
        pixels[7 * 16 + 9] = mustache;
        SetLine(pixels, 8, 6, 9, skin);

        // Torso / Chaqueta safari con cinturón (filas 9 a 11)
        SetLine(pixels, 9, 4, 11, shirt);
        SetLine(pixels, 10, 4, 11, shirt);
        SetLine(pixels, 11, 4, 11, belt);
        pixels[11 * 16 + 7] = buckle;
        pixels[11 * 16 + 8] = buckle;

        // Pantalones (filas 12 y 13)
        SetLine(pixels, 12, 5, 10, pants);
        pixels[13 * 16 + 5] = pants;
        pixels[13 * 16 + 6] = pants;
        pixels[13 * 16 + 9] = pants;
        pixels[13 * 16 + 10] = pants;

        // Botas (filas 14 y 15)
        SetLine(pixels, 14, 5, 7, boot);
        SetLine(pixels, 14, 9, 11, boot);
        SetLine(pixels, 15, 5, 8, boot);
        SetLine(pixels, 15, 9, 12, boot);

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateWalk1Texture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color hatTop = new(220, 195, 140);
        Color hatBrim = new(190, 165, 110);
        Color hatBand = new(85, 45, 20);
        Color skin = new(255, 210, 165);
        Color eye = new(25, 15, 10);
        Color mustache = new(70, 38, 16);
        Color shirt = new(195, 160, 105);
        Color belt = new(75, 40, 18);
        Color buckle = new(255, 215, 60);
        Color pants = new(135, 100, 60);
        Color boot = new(50, 30, 15);

        // Sombrero
        SetLine(pixels, 2, 5, 10, hatTop);
        SetLine(pixels, 3, 4, 11, hatTop);
        SetLine(pixels, 4, 3, 12, hatBand);
        SetLine(pixels, 5, 2, 13, hatBrim);

        // Cara
        SetLine(pixels, 6, 5, 10, skin);
        pixels[6 * 16 + 8] = eye;
        SetLine(pixels, 7, 5, 10, skin);
        pixels[7 * 16 + 8] = mustache;
        pixels[7 * 16 + 9] = mustache;
        SetLine(pixels, 8, 6, 9, skin);

        // Torso
        SetLine(pixels, 9, 4, 11, shirt);
        SetLine(pixels, 10, 4, 11, shirt);
        SetLine(pixels, 11, 4, 11, belt);
        pixels[11 * 16 + 7] = buckle;
        pixels[11 * 16 + 8] = buckle;

        // Piernas en paso adelante (zancada)
        pixels[12 * 16 + 5] = pants;
        pixels[12 * 16 + 6] = pants;
        pixels[12 * 16 + 9] = pants;
        pixels[12 * 16 + 10] = pants;

        pixels[13 * 16 + 4] = pants;
        pixels[13 * 16 + 5] = pants;
        pixels[13 * 16 + 10] = pants;
        pixels[13 * 16 + 11] = pants;

        SetLine(pixels, 14, 3, 5, boot);
        SetLine(pixels, 14, 11, 13, boot);
        SetLine(pixels, 15, 3, 6, boot);
        SetLine(pixels, 15, 11, 14, boot);

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateWalk2Texture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color hatTop = new(220, 195, 140);
        Color hatBrim = new(190, 165, 110);
        Color hatBand = new(85, 45, 20);
        Color skin = new(255, 210, 165);
        Color eye = new(25, 15, 10);
        Color mustache = new(70, 38, 16);
        Color shirt = new(195, 160, 105);
        Color belt = new(75, 40, 18);
        Color buckle = new(255, 215, 60);
        Color pants = new(135, 100, 60);
        Color boot = new(50, 30, 15);

        // Sombrero
        SetLine(pixels, 2, 5, 10, hatTop);
        SetLine(pixels, 3, 4, 11, hatTop);
        SetLine(pixels, 4, 3, 12, hatBand);
        SetLine(pixels, 5, 2, 13, hatBrim);

        // Cara
        SetLine(pixels, 6, 5, 10, skin);
        pixels[6 * 16 + 8] = eye;
        SetLine(pixels, 7, 5, 10, skin);
        pixels[7 * 16 + 8] = mustache;
        pixels[7 * 16 + 9] = mustache;
        SetLine(pixels, 8, 6, 9, skin);

        // Torso
        SetLine(pixels, 9, 4, 11, shirt);
        SetLine(pixels, 10, 4, 11, shirt);
        SetLine(pixels, 11, 4, 11, belt);
        pixels[11 * 16 + 7] = buckle;
        pixels[11 * 16 + 8] = buckle;

        // Piernas juntas en transición
        SetLine(pixels, 12, 6, 9, pants);
        SetLine(pixels, 13, 6, 9, pants);
        SetLine(pixels, 14, 6, 9, boot);
        SetLine(pixels, 15, 6, 10, boot);

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateClimb1Texture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color hatTop = new(220, 195, 140);
        Color hatBrim = new(190, 165, 110);
        Color hatBand = new(85, 45, 20);
        Color hair = new(70, 38, 16);
        Color shirt = new(195, 160, 105);
        Color belt = new(75, 40, 18);
        Color pants = new(135, 100, 60);
        Color boot = new(50, 30, 15);

        // Sombrero visto desde atrás
        SetLine(pixels, 2, 5, 10, hatTop);
        SetLine(pixels, 3, 4, 11, hatTop);
        SetLine(pixels, 4, 3, 12, hatBand);
        SetLine(pixels, 5, 2, 13, hatBrim);

        // Pelo trasero
        SetLine(pixels, 6, 5, 10, hair);
        SetLine(pixels, 7, 5, 10, hair);

        // Espalda con brazo izquierdo alzado
        pixels[6 * 16 + 3] = shirt;
        pixels[7 * 16 + 3] = shirt;
        SetLine(pixels, 8, 4, 11, shirt);
        SetLine(pixels, 9, 4, 11, shirt);
        SetLine(pixels, 10, 4, 11, belt);

        // Piernas en escalera
        SetLine(pixels, 11, 4, 7, pants);
        SetLine(pixels, 12, 4, 6, pants);
        SetLine(pixels, 13, 9, 11, pants);
        SetLine(pixels, 14, 4, 6, boot);
        SetLine(pixels, 15, 9, 11, boot);

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateClimb2Texture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color hatTop = new(220, 195, 140);
        Color hatBrim = new(190, 165, 110);
        Color hatBand = new(85, 45, 20);
        Color hair = new(70, 38, 16);
        Color shirt = new(195, 160, 105);
        Color belt = new(75, 40, 18);
        Color pants = new(135, 100, 60);
        Color boot = new(50, 30, 15);

        // Sombrero visto desde atrás
        SetLine(pixels, 2, 5, 10, hatTop);
        SetLine(pixels, 3, 4, 11, hatTop);
        SetLine(pixels, 4, 3, 12, hatBand);
        SetLine(pixels, 5, 2, 13, hatBrim);

        // Pelo trasero
        SetLine(pixels, 6, 5, 10, hair);
        SetLine(pixels, 7, 5, 10, hair);

        // Espalda con brazo derecho alzado
        pixels[6 * 16 + 12] = shirt;
        pixels[7 * 16 + 12] = shirt;
        SetLine(pixels, 8, 4, 11, shirt);
        SetLine(pixels, 9, 4, 11, shirt);
        SetLine(pixels, 10, 4, 11, belt);

        // Piernas alternadas en escalera
        SetLine(pixels, 11, 8, 11, pants);
        SetLine(pixels, 12, 9, 11, pants);
        SetLine(pixels, 13, 4, 6, pants);
        SetLine(pixels, 14, 9, 11, boot);
        SetLine(pixels, 15, 4, 6, boot);

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateJumpTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color hatTop = new(220, 195, 140);
        Color hatBrim = new(190, 165, 110);
        Color hatBand = new(85, 45, 20);
        Color skin = new(255, 210, 165);
        Color eye = new(25, 15, 10);
        Color mustache = new(70, 38, 16);
        Color shirt = new(195, 160, 105);
        Color belt = new(75, 40, 18);
        Color pants = new(135, 100, 60);
        Color boot = new(50, 30, 15);

        // Sombrero algo inclinado por la velocidad
        SetLine(pixels, 1, 5, 10, hatTop);
        SetLine(pixels, 2, 4, 11, hatTop);
        SetLine(pixels, 3, 3, 12, hatBand);
        SetLine(pixels, 4, 2, 13, hatBrim);

        // Cara
        SetLine(pixels, 5, 5, 10, skin);
        pixels[5 * 16 + 8] = eye;
        SetLine(pixels, 6, 5, 10, skin);
        pixels[6 * 16 + 8] = mustache;
        pixels[6 * 16 + 9] = mustache;
        SetLine(pixels, 7, 6, 9, skin);

        // Torso con brazos abiertos
        pixels[7 * 16 + 3] = shirt;
        pixels[7 * 16 + 12] = shirt;
        SetLine(pixels, 8, 4, 11, shirt);
        SetLine(pixels, 9, 4, 11, shirt);
        SetLine(pixels, 10, 4, 11, belt);

        // Piernas encogidas en el salto
        SetLine(pixels, 11, 4, 6, pants);
        SetLine(pixels, 11, 9, 11, pants);
        SetLine(pixels, 12, 3, 6, boot);
        SetLine(pixels, 12, 9, 12, boot);

        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D GenerateEliminatedTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Transparent);

        Color hatTop = new(220, 195, 140);
        Color hatBrim = new(190, 165, 110);
        Color hatBand = new(85, 45, 20);
        Color skin = new(255, 210, 165);
        Color eyeX = new(180, 40, 30);
        Color mustache = new(70, 38, 16);
        Color shirt = new(195, 160, 105);
        Color belt = new(75, 40, 18);
        Color pants = new(135, 100, 60);
        Color boot = new(50, 30, 15);

        // Sombrero caído al costado en el suelo
        SetLine(pixels, 10, 1, 3, hatTop);
        SetLine(pixels, 11, 0, 4, hatBand);
        SetLine(pixels, 12, 0, 5, hatBrim);

        // Cabeza noqueada apoyada sobre el suelo
        SetLine(pixels, 11, 5, 8, skin);
        pixels[11 * 16 + 6] = eyeX; // Ojo en X / noqueado
        pixels[11 * 16 + 8] = eyeX;
        SetLine(pixels, 12, 5, 8, mustache);
        SetLine(pixels, 13, 5, 8, skin);

        // Torso tumbado sobre el suelo
        SetLine(pixels, 12, 9, 12, shirt);
        SetLine(pixels, 13, 9, 12, shirt);
        SetLine(pixels, 14, 9, 12, belt);

        // Piernas y botas estiradas horizontalmente
        SetLine(pixels, 13, 13, 15, pants);
        SetLine(pixels, 14, 13, 15, pants);
        SetLine(pixels, 15, 12, 15, boot);

        texture.SetData(pixels);
        return texture;
    }

    private static void SetLine(Color[] pixels, int y, int startX, int endX, Color color)
    {
        for (int x = startX; x <= endX; x++)
        {
            pixels[y * 16 + x] = color;
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _idleTexture?.Dispose();
            _walk1Texture?.Dispose();
            _walk2Texture?.Dispose();
            _climb1Texture?.Dispose();
            _climb2Texture?.Dispose();
            _jumpTexture?.Dispose();
            _eliminatedTexture?.Dispose();
            _isDisposed = true;
        }
    }
}
