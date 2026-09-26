using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RetroGamePiramid.Core;

namespace RetroGamePiramid.Graphics;

/// <summary>
/// Gestiona el búfer de renderizado virtual (320x240) y proyecta a la pantalla
/// preservando la relación de aspecto 4:3 retro mediante letterbox/pillarbox y SamplerState.PointClamp.
/// Garantiza cero asignaciones de memoria durante Update y Draw.
/// </summary>
public sealed class VirtualViewport : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private RenderTarget2D _renderTarget;
    private Rectangle _destinationRectangle;
    private int _lastBackBufferWidth;
    private int _lastBackBufferHeight;
    private bool _isDisposed;

    public int VirtualWidth { get; }
    public int VirtualHeight { get; }
    public RenderTarget2D RenderTarget => _renderTarget;
    public Rectangle DestinationRectangle => _destinationRectangle;
    public bool IntegerScalingOnly { get; set; }

    /// <summary>
    /// Color de fondo de la cámara retro (ambiente oscuro de tumba egipcia).
    /// </summary>
    public Color VirtualClearColor { get; set; } = new Color(18, 12, 22);

    /// <summary>
    /// Color de las bandas de letterbox/pillarbox exteriores.
    /// </summary>
    public Color LetterboxClearColor { get; set; } = Color.Black;

    public VirtualViewport(
        GraphicsDevice graphicsDevice,
        int virtualWidth = GameConstants.VIRTUAL_WIDTH,
        int virtualHeight = GameConstants.VIRTUAL_HEIGHT)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        VirtualWidth = virtualWidth;
        VirtualHeight = virtualHeight;

        _renderTarget = new RenderTarget2D(
            _graphicsDevice,
            VirtualWidth,
            VirtualHeight,
            mipMap: false,
            SurfaceFormat.Color,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);

        UpdateDestinationRectangle(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
    }

    /// <summary>
    /// Inicia el pase de renderizado hacia el RenderTarget2D virtual.
    /// Limpia el búfer virtual e inicializa el SpriteBatch con SamplerState.PointClamp.
    /// </summary>
    public void Begin(SpriteBatch spriteBatch)
    {
        _graphicsDevice.SetRenderTarget(_renderTarget);
        _graphicsDevice.Clear(VirtualClearColor);

        spriteBatch?.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
    }

    /// <summary>
    /// Sobrecarga para activar el RenderTarget2D sin invocar automáticamente el SpriteBatch.
    /// </summary>
    public void Begin()
    {
        _graphicsDevice.SetRenderTarget(_renderTarget);
        _graphicsDevice.Clear(VirtualClearColor);
    }

    /// <summary>
    /// Finaliza el renderizado virtual y proyecta el RenderTarget2D a la ventana final
    /// con escalado proporcional 4:3 y SamplerState.PointClamp.
    /// </summary>
    public void End(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        // Finaliza el dibujado en el render target virtual
        spriteBatch.End();

        // Conmuta al backbuffer de pantalla principal
        graphicsDevice.SetRenderTarget(null);
        graphicsDevice.Clear(LetterboxClearColor);

        // Recalcula el viewport solo si la ventana ha cambiado de resolución
        int screenW = graphicsDevice.Viewport.Width;
        int screenH = graphicsDevice.Viewport.Height;
        if (screenW != _lastBackBufferWidth || screenH != _lastBackBufferHeight)
        {
            UpdateDestinationRectangle(screenW, screenH);
        }

        // Proyecta la textura virtual a pantalla completa con PointClamp para píxeles nítidos
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        spriteBatch.Draw(_renderTarget, _destinationRectangle, Color.White);
        spriteBatch.End();
    }

    /// <summary>
    /// Recalcula el rectángulo de proyección respetando 4:3 con barras negras (letterbox/pillarbox).
    /// Cero allocations en heap.
    /// </summary>
    public void UpdateDestinationRectangle(int screenWidth, int screenHeight)
    {
        _lastBackBufferWidth = screenWidth;
        _lastBackBufferHeight = screenHeight;

        if (screenWidth <= 0 || screenHeight <= 0)
        {
            _destinationRectangle = Rectangle.Empty;
            return;
        }

        if (IntegerScalingOnly)
        {
            int scale = Math.Max(1, Math.Min(screenWidth / VirtualWidth, screenHeight / VirtualHeight));
            int destWidth = VirtualWidth * scale;
            int destHeight = VirtualHeight * scale;
            int destX = (screenWidth - destWidth) / 2;
            int destY = (screenHeight - destHeight) / 2;
            _destinationRectangle = new Rectangle(destX, destY, destWidth, destHeight);
        }
        else
        {
            float scaleX = (float)screenWidth / VirtualWidth;
            float scaleY = (float)screenHeight / VirtualHeight;
            float scale = MathF.Min(scaleX, scaleY);

            int destWidth = (int)MathF.Round(VirtualWidth * scale);
            int destHeight = (int)MathF.Round(VirtualHeight * scale);
            int destX = (screenWidth - destWidth) / 2;
            int destY = (screenHeight - destHeight) / 2;
            _destinationRectangle = new Rectangle(destX, destY, destWidth, destHeight);
        }
    }

    /// <summary>
    /// Convierte una coordenada de pantalla en píxeles virtuales (320x240).
    /// </summary>
    public Vector2 ScreenToVirtual(Vector2 screenPosition)
    {
        if (_destinationRectangle.Width <= 0 || _destinationRectangle.Height <= 0)
            return Vector2.Zero;

        float x = (screenPosition.X - _destinationRectangle.X) * VirtualWidth / _destinationRectangle.Width;
        float y = (screenPosition.Y - _destinationRectangle.Y) * VirtualHeight / _destinationRectangle.Height;
        return new Vector2(Math.Clamp(x, 0, VirtualWidth), Math.Clamp(y, 0, VirtualHeight));
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _renderTarget?.Dispose();
            _renderTarget = null!;
            _isDisposed = true;
        }
    }
}
