namespace RetroGamePiramid.Grid;

/// <summary>
/// Representa la información estructural de una plataforma dentro de una recámara.
/// Tipo inmutable de solo lectura para garantizar cero asignaciones en memoria heap.
/// </summary>
public readonly record struct PlatformInfo(int Level, int Row, int StartCol, int EndCol, string Name);
