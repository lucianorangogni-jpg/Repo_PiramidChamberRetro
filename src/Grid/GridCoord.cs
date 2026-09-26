using System;
using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;

namespace RetroGamePiramid.Grid;

/// <summary>
/// Coordenadas discretas de celda en la rejilla (X = columna, Y = fila).
/// Readonly struct con cero asignaciones en memoria heap y comparaciones directas por valor.
/// </summary>
public readonly struct GridCoord : IEquatable<GridCoord>
{
    public readonly int X;
    public readonly int Y;

    public GridCoord(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static GridCoord Zero => new(0, 0);
    public static GridCoord One => new(1, 1);
    public static GridCoord Up => new(0, -1);
    public static GridCoord Down => new(0, 1);
    public static GridCoord Left => new(-1, 0);
    public static GridCoord Right => new(1, 0);

    public static GridCoord operator +(GridCoord a, GridCoord b) => new(a.X + b.X, a.Y + b.Y);
    public static GridCoord operator -(GridCoord a, GridCoord b) => new(a.X - b.X, a.Y - b.Y);
    public static bool operator ==(GridCoord left, GridCoord right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(GridCoord left, GridCoord right) => left.X != right.X || left.Y != right.Y;

    public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is GridCoord other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X}, {Y})";

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    /// <summary>
    /// Convierte la coordenada de celda a posición en píxeles del espacio virtual nativo.
    /// </summary>
    public Vector2 ToPixelPosition(int tileSize = GameConstants.TILE_SIZE) =>
        new(X * tileSize, Y * tileSize);
}
