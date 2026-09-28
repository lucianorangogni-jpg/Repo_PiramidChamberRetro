using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Grid;

namespace RetroGamePiramid.Entities;

/// <summary>
/// Representa el cofre del tesoro del faraón oculto en la cámara.
/// Cero asignaciones en memoria heap.
/// </summary>
public sealed class Treasure
{
    public GridCoord Coord { get; private set; }
    public Vector2 Position { get; private set; }
    public bool IsCollected { get; private set; }

    public Treasure(GridCoord coord)
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

    public void Collect()
    {
        IsCollected = true;
    }

    public void Reset()
    {
        IsCollected = false;
    }
}

