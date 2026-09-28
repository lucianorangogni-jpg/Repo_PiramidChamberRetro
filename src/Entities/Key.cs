using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Grid;

namespace RetroGamePiramid.Entities;

/// <summary>
/// Representa la llave egipcia dorada colgada que permite abrir la puerta de salida de la recámara.
/// Cero asignaciones en memoria heap.
/// </summary>
public sealed class Key
{
    public GridCoord Coord { get; private set; }
    public Vector2 Position { get; private set; }
    public bool IsCollected { get; private set; }

    public Key(GridCoord coord)
    {
        Coord = coord;
        Position = coord.ToPixelPosition();
        IsCollected = false;
    }

    public void Configure(GridCoord coord)
    {
        Coord = coord;
        Position = coord.ToPixelPosition();
        IsCollected = false;
    }

    public void Collect() => IsCollected = true;
    public void Reset() => IsCollected = false;
}
