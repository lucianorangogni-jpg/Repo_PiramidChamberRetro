using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Grid;

namespace RetroGamePiramid.Entities;

/// <summary>
/// Representa la trampa de suelo situada bajo la llave colgada en la plataforma nivel 1.
/// Al pasar el jugador por debajo caminando sobre el piso, la trampa se abre y hace caer al jugador al nivel inferior.
/// Cero asignaciones en memoria heap.
/// </summary>
public sealed class FloorTrap
{
    public GridCoord Coord { get; private set; }
    public Vector2 Position { get; private set; }
    public bool IsOpen { get; private set; }

    public FloorTrap(GridCoord coord)
    {
        Coord = coord;
        Position = coord.ToPixelPosition();
        IsOpen = false;
    }

    public void Configure(GridCoord coord)
    {
        Coord = coord;
        Position = coord.ToPixelPosition();
        IsOpen = false;
    }

    public void Open() => IsOpen = true;
    public void Reset() => IsOpen = false;
}

