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
    private static readonly string[] ChamberNames =
    [
        "Recamara 1",
        "Recamara 2",
        "Recamara 3",
        "Recamara 4",
        "Recamara 5",
        "Recamara 6",
        "Recamara 7",
        "Recamara 8",
        "Recamara 9",
        "Recamara 10"
    ];

    private readonly TileType[] _tiles = new TileType[GameConstants.GRID_COLUMNS * GameConstants.GRID_ROWS];
    private readonly List<PlatformInfo> _platforms = new(4);

    public int Columns => GameConstants.GRID_COLUMNS;
    public int Rows => GameConstants.GRID_ROWS;
    public int CurrentChamber { get; private set; } = 1;
    public int PyramidLevel => GetPyramidLevel(CurrentChamber);
    public string RoomName { get; private set; } = "Recamara 1";
    public IReadOnlyList<PlatformInfo> Platforms => _platforms;

    /// <summary>
    /// Devuelve el nivel de la pirámide (1 a 4) al que pertenece una recámara (1 a 10):
    /// - Nivel 1 (Base): Recámaras 1, 2, 3, 4
    /// - Nivel 2: Recámaras 5, 6, 7
    /// - Nivel 3: Recámaras 8, 9
    /// - Nivel 4 (Cúspide): Recámara 10
    /// </summary>
    public static int GetPyramidLevel(int chamberNumber) => chamberNumber switch
    {
        >= 1 and <= 4 => 1,
        >= 5 and <= 7 => 2,
        8 or 9 => 3,
        10 => 4,
        _ => 1
    };

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
    /// Configura una recámara por su número ordinal (1 a 10) dentro de la estructura piramidal.
    /// </summary>
    public void LoadChamber(int chamberNumber)
    {
        int normalized = (chamberNumber >= 1 && chamberNumber <= GameConstants.TOTAL_CHAMBERS) ? chamberNumber : 1;
        CurrentChamber = normalized;
        Clear(TileType.Empty);

        switch (normalized)
        {
            case 1:
                LoadChamber1();
                break;
            case 2:
                LoadChamber2();
                break;
            case 3:
                LoadChamberVariant(
                    chamberNumber: 3,
                    level1Row: 9, level2Row: 5,
                    ladder1Col: 4, ladder2Col: 9,
                    doorOnRight: true,
                    stoneCol: 7,
                    level1GapColA: 11, level1GapColB: 13);
                break;
            case 4:
                LoadChamberVariant(
                    chamberNumber: 4,
                    level1Row: 10, level2Row: 6,
                    ladder1Col: 5, ladder2Col: 13,
                    doorOnRight: false,
                    stoneCol: 9,
                    level1GapColA: 11, level1GapColB: -1);
                break;
            case 5:
                LoadChamberVariant(
                    chamberNumber: 5,
                    level1Row: 9, level2Row: 5,
                    ladder1Col: 6, ladder2Col: 10,
                    doorOnRight: true,
                    stoneCol: 8,
                    level1GapColA: 12, level1GapColB: -1);
                break;
            case 6:
                LoadChamberVariant(
                    chamberNumber: 6,
                    level1Row: 10, level2Row: 6,
                    ladder1Col: 4, ladder2Col: 14,
                    doorOnRight: false,
                    stoneCol: 7,
                    level1GapColA: 12, level1GapColB: -1);
                break;
            case 7:
                LoadChamberVariant(
                    chamberNumber: 7,
                    level1Row: 9, level2Row: 5,
                    ladder1Col: 5, ladder2Col: 9,
                    doorOnRight: true,
                    stoneCol: 9,
                    level1GapColA: 11, level1GapColB: -1);
                break;
            case 8:
                LoadChamberVariant(
                    chamberNumber: 8,
                    level1Row: 10, level2Row: 6,
                    ladder1Col: 6, ladder2Col: 13,
                    doorOnRight: false,
                    stoneCol: 8,
                    level1GapColA: 11, level1GapColB: -1);
                break;
            case 9:
                LoadChamberVariant(
                    chamberNumber: 9,
                    level1Row: 9, level2Row: 5,
                    ladder1Col: 4, ladder2Col: 10,
                    doorOnRight: true,
                    stoneCol: 7,
                    level1GapColA: 11, level1GapColB: 13);
                break;
            case 10:
                LoadChamberVariant(
                    chamberNumber: 10,
                    level1Row: 9, level2Row: 5,
                    ladder1Col: 5, ladder2Col: 10,
                    doorOnRight: true,
                    stoneCol: 8,
                    level1GapColA: 11, level1GapColB: 13);
                break;
        }
    }

    /// <summary>
    /// Carga la recámara 1 por defecto con todas sus plataformas, escaleras, puzle y puerta.
    /// </summary>
    public void LoadDefaultRoom() => LoadChamber(1);

    private void BuildPerimeterWalls()
    {
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
    }

    private void LoadChamber1()
    {
        // 1. Paredes perimetrales exteriores (bordes indestructibles de la cámara)
        BuildPerimeterWalls();

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
        // Columna 1 es espacio vacío (foso directo hacia Nivel 0)

        // Plataforma principal Nivel 2: columnas 2 a 15
        for (int x = 2; x <= 15; x++)
        {
            SetTile(x, 5, TileType.SolidWall);
        }

        // Columna 16 es espacio vacío para salto hacia el descansillo de la puerta (16 px)

        // Descansillo de la puerta de salida a la derecha: columnas 17 y 18
        SetTile(17, 5, TileType.SolidWall);
        SetTile(18, 5, TileType.SolidWall);

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
        // Situada en (18, 4), reposando sobre el descansillo de la celda (18, 5)
        SetTile(18, 4, TileType.ExitDoor);

        // 8. Registro de plataformas de la recámara (distinción por pantalla)
        RoomName = ChamberNames[0];
        _platforms.Clear();
        _platforms.Add(new PlatformInfo(0, GameConstants.GRID_ROWS - 1, 1, GameConstants.GRID_COLUMNS - 2, "Plataforma Nivel 0"));
        _platforms.Add(new PlatformInfo(1, 9, 2, 17, "Plataforma Nivel 1"));
        _platforms.Add(new PlatformInfo(2, 5, 2, 18, "Plataforma Nivel 2"));
    }

    private void LoadChamber2()
    {
        BuildPerimeterWalls();

        // Plataforma intermedia Nivel 1 (fila 10, columnas 2 a 17 con foso en columna 12)
        for (int x = 2; x <= 17; x++)
        {
            if (x == 12)
                continue;
            SetTile(x, 10, TileType.SolidWall);
        }

        // Escalera 1: columna 6, filas 10 a 13
        for (int y = 10; y <= 13; y++)
        {
            SetTile(6, y, TileType.Ladder);
        }

        // Plataforma superior Nivel 2 (fila 6)
        // Descansillo de puerta a la izquierda: columnas 1 y 2
        SetTile(1, 6, TileType.SolidWall);
        SetTile(2, 6, TileType.SolidWall);
        // Columna 3 es hueco de salto
        for (int x = 4; x <= 17; x++)
        {
            SetTile(x, 6, TileType.SolidWall);
        }

        // Escalera 2: columna 13, filas 6 a 10
        for (int y = 6; y <= 10; y++)
        {
            SetTile(13, y, TileType.Ladder);
        }

        // Losa de activación en (8, 13)
        SetTile(8, 13, TileType.PressurePlate);

        // Puerta de salida en (1, 5)
        SetTile(1, 5, TileType.ExitDoor);

        // Registro de plataformas de Recámara 2
        RoomName = ChamberNames[1];
        _platforms.Clear();
        _platforms.Add(new PlatformInfo(0, GameConstants.GRID_ROWS - 1, 1, GameConstants.GRID_COLUMNS - 2, "Plataforma Nivel 0"));
        _platforms.Add(new PlatformInfo(1, 10, 2, 17, "Plataforma Nivel 1"));
        _platforms.Add(new PlatformInfo(2, 6, 1, 17, "Plataforma Nivel 2"));
    }

    private void LoadChamberVariant(
        int chamberNumber,
        int level1Row,
        int level2Row,
        int ladder1Col,
        int ladder2Col,
        bool doorOnRight,
        int stoneCol,
        int level1GapColA,
        int level1GapColB)
    {
        BuildPerimeterWalls();

        // 1. Plataforma intermedia Nivel 1 (columnas 2 a 17 con fosos intermedios configurables)
        for (int x = 2; x <= 17; x++)
        {
            if (x == level1GapColA || x == level1GapColB)
                continue;
            SetTile(x, level1Row, TileType.SolidWall);
        }

        // 2. Escalera 1: conecta el suelo (fila 13) con Nivel 1
        for (int y = level1Row; y <= 13; y++)
        {
            SetTile(ladder1Col, y, TileType.Ladder);
        }

        // 3. Plataforma superior Nivel 2
        int level2MinCol;
        int level2MaxCol;
        if (doorOnRight)
        {
            // Plataforma principal: columnas 2 a 15, hueco en 16, descansillo de puerta en 17 y 18
            for (int x = 2; x <= 15; x++)
            {
                SetTile(x, level2Row, TileType.SolidWall);
            }
            SetTile(17, level2Row, TileType.SolidWall);
            SetTile(18, level2Row, TileType.SolidWall);
            SetTile(18, level2Row - 1, TileType.ExitDoor);
            level2MinCol = 2;
            level2MaxCol = 18;
        }
        else
        {
            // Descansillo de puerta a la izquierda en 1 y 2, hueco en 3, plataforma principal de 4 a 17
            SetTile(1, level2Row, TileType.SolidWall);
            SetTile(2, level2Row, TileType.SolidWall);
            for (int x = 4; x <= 17; x++)
            {
                SetTile(x, level2Row, TileType.SolidWall);
            }
            SetTile(1, level2Row - 1, TileType.ExitDoor);
            level2MinCol = 1;
            level2MaxCol = 17;
        }

        // 4. Escalera 2: conecta Nivel 1 con Nivel 2
        for (int y = level2Row; y <= level1Row; y++)
        {
            SetTile(ladder2Col, y, TileType.Ladder);
        }

        // 5. Losa de activación en Nivel 0
        SetTile(stoneCol, 13, TileType.PressurePlate);

        // 6. Registro de plataformas
        RoomName = ChamberNames[chamberNumber - 1];
        _platforms.Clear();
        _platforms.Add(new PlatformInfo(0, GameConstants.GRID_ROWS - 1, 1, GameConstants.GRID_COLUMNS - 2, "Plataforma Nivel 0"));
        _platforms.Add(new PlatformInfo(1, level1Row, 2, 17, "Plataforma Nivel 1"));
        _platforms.Add(new PlatformInfo(2, level2Row, level2MinCol, level2MaxCol, "Plataforma Nivel 2"));
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
