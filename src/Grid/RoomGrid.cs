using System;
using System.Collections.Generic;
using RetroGamePiramid.Core;

namespace RetroGamePiramid.Grid;

/// <summary>
/// Representa la cuadrícula de la cámara de la pirámide (20 columnas x 15 filas).
/// Utiliza un búfer plano unidimensional contiguo preasignado para garantizar cero asignaciones de memoria.
/// </summary>
public sealed class RoomGrid
{
    private readonly TileType[] _tiles = new TileType[GameConstants.GRID_COLUMNS * GameConstants.GRID_ROWS];
    private readonly List<PlatformInfo> _platforms = new(4);

    public int Columns => GameConstants.GRID_COLUMNS;
    public int Rows => GameConstants.GRID_ROWS;
    public string RoomName { get; private set; } = "Recamara 1";
    public IReadOnlyList<PlatformInfo> Platforms => _platforms;

    public static int GetIndex(int x, int y) => y * GameConstants.GRID_COLUMNS + x;

    /// <summary>
    /// Comprueba si la coordenada (x, y) se encuentra dentro de los límites de la cuadrícula.
    /// </summary>
    public bool InBounds(int x, int y) =>
        (uint)x < (uint)GameConstants.GRID_COLUMNS && (uint)y < (uint)GameConstants.GRID_ROWS;

    public bool InBounds(GridCoord coord) => InBounds(coord.X, coord.Y);

    /// <summary>
    /// Obtiene el tipo de baldosa en (x, y). Si está fuera de los límites, devuelve SolidWall para evitar salidas no permitidas.
    /// </summary>
    public TileType GetTile(int x, int y)
    {
        if (!InBounds(x, y))
            return TileType.SolidWall;

        return _tiles[GetIndex(x, y)];
    }

    public TileType GetTile(GridCoord coord) => GetTile(coord.X, coord.Y);

    /// <summary>
    /// Establece el tipo de baldosa en (x, y) si la posición está dentro de los límites.
    /// </summary>
    public void SetTile(int x, int y, TileType type)
    {
        if (InBounds(x, y))
        {
            _tiles[GetIndex(x, y)] = type;
        }
    }

    public void SetTile(GridCoord coord, TileType type) => SetTile(coord.X, coord.Y, type);

    /// <summary>
    /// Comprueba si la celda es impenetrable. Fuera de límites se considera sólido.
    /// </summary>
    public bool IsSolid(int x, int y)
    {
        if (!InBounds(x, y))
            return true;

        return _tiles[GetIndex(x, y)] == TileType.SolidWall;
    }

    public bool IsSolid(GridCoord coord) => IsSolid(coord.X, coord.Y);

    /// <summary>
    /// Comprueba si la celda contiene una escalera transitable en vertical.
    /// </summary>
    public bool IsLadder(int x, int y)
    {
        if (!InBounds(x, y))
            return false;

        return _tiles[GetIndex(x, y)] == TileType.Ladder;
    }

    public bool IsLadder(GridCoord coord) => IsLadder(coord.X, coord.Y);

    /// <summary>
    /// Comprueba si la celda contiene una placa de presión.
    /// </summary>
    public bool IsPressurePlate(int x, int y)
    {
        if (!InBounds(x, y))
            return false;

        return _tiles[GetIndex(x, y)] == TileType.PressurePlate;
    }

    public bool IsPressurePlate(GridCoord coord) => IsPressurePlate(coord.X, coord.Y);

    /// <summary>
    /// Comprueba si la celda contiene la puerta de salida.
    /// </summary>
    public bool IsExitDoor(int x, int y)
    {
        if (!InBounds(x, y))
            return false;

        return _tiles[GetIndex(x, y)] == TileType.ExitDoor;
    }

    public bool IsExitDoor(GridCoord coord) => IsExitDoor(coord.X, coord.Y);

    /// <summary>
    /// Comprueba si la celda está vacía.
    /// </summary>
    public bool IsEmpty(int x, int y)
    {
        if (!InBounds(x, y))
            return false;

        return _tiles[GetIndex(x, y)] == TileType.Empty;
    }

    public bool IsEmpty(GridCoord coord) => IsEmpty(coord.X, coord.Y);

    /// <summary>
    /// Limpia todas las celdas de la cámara con el tipo especificado.
    /// </summary>
    public void Clear(TileType tileType = TileType.Empty)
    {
        Array.Fill(_tiles, tileType);
    }

    /// <summary>
    /// Configura una cámara por su número ordinal (1: Recamara 1, 2: Recamara 2).
    /// </summary>
    public void LoadChamber(int chamberNumber)
    {
        Clear(TileType.Empty);
        if (chamberNumber == 2)
        {
            LoadChamber2();
        }
        else
        {
            LoadChamber1();
        }
    }

    /// <summary>
    /// Carga la recámara 1 por defecto con todas sus plataformas, escaleras, puzle y puerta.
    /// </summary>
    public void LoadDefaultRoom() => LoadChamber(1);

    private void LoadChamber1()
    {
        // 1. Paredes perimetrales exteriores (bordes indestructibles de la cámara)
        for (int x = 0; x < GameConstants.GRID_COLUMNS; x++)
        {
            SetTile(x, 0, TileType.SolidWall); // Techo
            SetTile(x, GameConstants.GRID_ROWS - 1, TileType.SolidWall); // Base / Suelo inferior (fila 14)
        }

        for (int y = 0; y < GameConstants.GRID_ROWS; y++)
        {
            SetTile(0, y, TileType.SolidWall); // Pared izquierda
            SetTile(GameConstants.GRID_COLUMNS - 1, y, TileType.SolidWall); // Pared derecha
        }

        // 2. Plataforma intermedia Nivel 1 (fila 9)
        // Sección 1: columnas 2 a 10
        for (int x = 2; x <= 10; x++)
        {
            SetTile(x, 9, TileType.SolidWall);
        }

        // Descansillo de salto intermedio: columna 12 (espacios vacíos en columna 11 y columna 13)
        SetTile(12, 9, TileType.SolidWall);

        // Sección 2: suelo sobre el tesoro del nivel 0 desde la puerta (columna 14) hasta columna 17
        // dejando columna 18 vacía (idéntico al espacio vacío en columna 1 a la izquierda)
        for (int x = 14; x <= 17; x++)
        {
            SetTile(x, 9, TileType.SolidWall);
        }

        // 3. Escalera 1: conecta el suelo (fila 13) con la plataforma intermedia (fila 9)
        // Columna 5: desde la fila 9 hasta la fila 13
        for (int y = 9; y <= 13; y++)
        {
            SetTile(5, y, TileType.Ladder);
        }

        // 4. Plataforma superior Nivel 2 (fila 5)
        // Descansillo de la puerta de salida a la izquierda: columnas 1 y 2
        SetTile(1, 5, TileType.SolidWall);
        SetTile(2, 5, TileType.SolidWall);

        // Columna 3 es espacio vacío para salto hacia la puerta (16 px)

        // Plataforma principal Nivel 2: columnas 4 a 17
        for (int x = 4; x <= 17; x++)
        {
            SetTile(x, 5, TileType.SolidWall);
        }

        // 5. Escalera 2: conecta la plataforma intermedia (fila 9) con la plataforma superior (fila 5)
        // Columna 10: desde la fila 5 hasta la fila 9
        for (int y = 5; y <= 9; y++)
        {
            SetTile(10, y, TileType.Ladder);
        }

        // 6. Única losa de activación sobre el suelo inferior
        // Situada en (8, 13) para conmutar el muro de la cámara del tesoro
        SetTile(8, 13, TileType.PressurePlate);

        // 7. Puerta de salida en la plataforma superior Nivel 2
        // Situada en (1, 4), reposando sobre el descansillo de la celda (1, 5)
        SetTile(1, 4, TileType.ExitDoor);

        // 8. Registro de plataformas de la recámara (distinción por pantalla)
        RoomName = "Recamara 1";
        _platforms.Clear();
        _platforms.Add(new PlatformInfo(0, GameConstants.GRID_ROWS - 1, 1, GameConstants.GRID_COLUMNS - 2, "Plataforma Nivel 0"));
        _platforms.Add(new PlatformInfo(1, 9, 2, 17, "Plataforma Nivel 1"));
        _platforms.Add(new PlatformInfo(2, 5, 1, 17, "Plataforma Nivel 2"));
    }

    private void LoadChamber2()
    {
        // Paredes perimetrales exteriores
        for (int x = 0; x < GameConstants.GRID_COLUMNS; x++)
        {
            SetTile(x, 0, TileType.SolidWall);
            SetTile(x, GameConstants.GRID_ROWS - 1, TileType.SolidWall);
        }

        for (int y = 0; y < GameConstants.GRID_ROWS; y++)
        {
            SetTile(0, y, TileType.SolidWall);
            SetTile(GameConstants.GRID_COLUMNS - 1, y, TileType.SolidWall);
        }

        // Plataforma intermedia Nivel 1 (fila 10, columnas 3 a 16)
        for (int x = 3; x <= 16; x++)
        {
            SetTile(x, 10, TileType.SolidWall);
        }

        // Escalera 1: columna 6, filas 10 a 13
        for (int y = 10; y <= 13; y++)
        {
            SetTile(6, y, TileType.Ladder);
        }

        // Plataforma superior Nivel 2 (fila 6)
        SetTile(1, 6, TileType.SolidWall);
        SetTile(2, 6, TileType.SolidWall);
        // Columna 3 es hueco de salto
        for (int x = 4; x <= 15; x++)
        {
            SetTile(x, 6, TileType.SolidWall);
        }

        // Escalera 2: columna 12, filas 6 a 10
        for (int y = 6; y <= 10; y++)
        {
            SetTile(12, y, TileType.Ladder);
        }

        // Puerta de salida en (1, 5)
        SetTile(1, 5, TileType.ExitDoor);

        // Registro de plataformas de Recámara 2
        RoomName = "Recamara 2";
        _platforms.Clear();
        _platforms.Add(new PlatformInfo(0, GameConstants.GRID_ROWS - 1, 1, GameConstants.GRID_COLUMNS - 2, "Plataforma Nivel 0"));
        _platforms.Add(new PlatformInfo(1, 10, 3, 16, "Plataforma Nivel 1"));
        _platforms.Add(new PlatformInfo(2, 6, 1, 15, "Plataforma Nivel 2"));
    }

    /// <summary>
    /// Determina el nivel de plataforma (0, 1, 2) a partir de la altura vertical Y del personaje.
    /// Retorna -1 si no se encuentra sobre ninguna plataforma conocida.
    /// </summary>
    public int GetPlatformLevel(float posY)
    {
        int footRow = (int)MathF.Round((posY + GameConstants.TILE_SIZE) / GameConstants.TILE_SIZE);
        for (int i = 0; i < _platforms.Count; i++)
        {
            if (_platforms[i].Row == footRow)
                return _platforms[i].Level;
        }
        return -1;
    }

    /// <summary>
    /// Encuentra el nivel de la plataforma más cercana a una altura vertical Y dada.
    /// Retorna -1 si no hay plataformas registradas en la recámara.
    /// </summary>
    public int GetClosestPlatformLevel(float posY)
    {
        if (_platforms.Count == 0)
            return -1;

        int footRow = (int)MathF.Round((posY + GameConstants.TILE_SIZE) / GameConstants.TILE_SIZE);
        int closestLevel = _platforms[0].Level;
        int minDistance = Math.Abs(_platforms[0].Row - footRow);

        for (int i = 1; i < _platforms.Count; i++)
        {
            int dist = Math.Abs(_platforms[i].Row - footRow);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestLevel = _platforms[i].Level;
            }
        }

        return closestLevel;
    }
}
