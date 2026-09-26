namespace RetroGamePiramid.Grid;

/// <summary>
/// Tipos de baldosa en la cuadrícula de la cámara.
/// Respaldado por byte para minimizar consumo de memoria y evitar boxing.
/// </summary>
public enum TileType : byte
{
    Empty = 0,
    SolidWall = 1,
    Ladder = 2,
    PressurePlate = 3,
    ExitDoor = 4
}
