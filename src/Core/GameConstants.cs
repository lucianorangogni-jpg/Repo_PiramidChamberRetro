using System;

namespace RetroGamePiramid.Core;

/// <summary>
/// Constantes globales para la simulación, rejilla y resolución virtual retro.
/// </summary>
public static class GameConstants
{
    // Resolución virtual nativa fija (4:3 retro arcade)
    public const int VIRTUAL_WIDTH = 320;
    public const int VIRTUAL_HEIGHT = 240;

    // Dimensiones de celdas (tiles) y cuadrícula de la cámara
    public const int TILE_SIZE = 16;
    public const int GRID_COLUMNS = 20; // 20 * 16 = 320 px
    public const int GRID_ROWS = 15;    // 15 * 16 = 240 px

    // Cadencia de frames y física de tiempo fijo (60 FPS arcade)
    public const int TARGET_FPS = 60;
    public const float FIXED_TIME_STEP_SECONDS = 1f / TARGET_FPS;
    public static readonly TimeSpan TargetElapsedTime = TimeSpan.FromSeconds(1.0 / TARGET_FPS);

    // Tiempos y mecánicas de gameplay
    public const float STUN_DURATION_SECONDS = 3.5f;
    public const int WHIP_DURATION_FRAMES = 10;
}
