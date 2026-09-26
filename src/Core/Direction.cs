namespace RetroGamePiramid.Core;

/// <summary>
/// Direcciones cardinales para movimiento, orientación e inputs.
/// Tipo byte para optimización de memoria y cero boxing.
/// </summary>
public enum Direction : byte
{
    None = 0,
    Left = 1,
    Right = 2,
    Up = 3,
    Down = 4
}
