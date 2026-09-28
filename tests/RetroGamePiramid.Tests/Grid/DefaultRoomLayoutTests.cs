using RetroGamePiramid.Core;
using RetroGamePiramid.Grid;
using Xunit;

namespace RetroGamePiramid.Tests.Grid;

public class DefaultRoomLayoutTests
{
    private readonly RoomGrid _grid;

    public DefaultRoomLayoutTests()
    {
        _grid = new RoomGrid();
        _grid.LoadDefaultRoom();
    }

    [Fact]
    public void Perimeter_IsCompletelyEnclosedBySolidWalls()
    {
        // Techo (fila 0) y Suelo (fila 14)
        for (int x = 0; x < GameConstants.GRID_COLUMNS; x++)
        {
            Assert.Equal(TileType.SolidWall, _grid.GetTile(x, 0));
            Assert.Equal(TileType.SolidWall, _grid.GetTile(x, GameConstants.GRID_ROWS - 1));
        }

        // Paredes laterales (columna 0 y columna 19)
        for (int y = 0; y < GameConstants.GRID_ROWS; y++)
        {
            Assert.Equal(TileType.SolidWall, _grid.GetTile(0, y));
            Assert.Equal(TileType.SolidWall, _grid.GetTile(GameConstants.GRID_COLUMNS - 1, y));
        }
    }

    [Fact]
    public void IntermediatePlatform_ContainsSolidWallsExceptLadderPositions()
    {
        // Plataforma en fila 9, columnas 2 a 10
        for (int x = 2; x <= 10; x++)
        {
            if (x == 5 || x == 10)
            {
                // Escaleras sobre la plataforma
                Assert.Equal(TileType.Ladder, _grid.GetTile(x, 9));
            }
            else
            {
                Assert.Equal(TileType.SolidWall, _grid.GetTile(x, 9));
            }
        }
    }

    [Fact]
    public void UpperPlatform_ContainsSolidWallsExceptLadderPosition()
    {
        // Plataforma superior en fila 5, columnas 8 a 17
        for (int x = 8; x <= 17; x++)
        {
            if (x == 10)
            {
                // Escalera conectando a la plataforma superior
                Assert.Equal(TileType.Ladder, _grid.GetTile(x, 5));
            }
            else
            {
                Assert.Equal(TileType.SolidWall, _grid.GetTile(x, 5));
            }
        }
    }

    [Fact]
    public void Ladders_AreContinuousAndInCorrectPositions()
    {
        // Escalera 1: Columna 5, filas 9 a 13
        for (int y = 9; y <= 13; y++)
        {
            Assert.Equal(TileType.Ladder, _grid.GetTile(5, y));
            Assert.True(_grid.IsLadder(5, y));
        }

        // Escalera 2: Columna 10, filas 5 a 9
        for (int y = 5; y <= 9; y++)
        {
            Assert.Equal(TileType.Ladder, _grid.GetTile(10, y));
            Assert.True(_grid.IsLadder(10, y));
        }
    }

    [Fact]
    public void PuzzleElements_AreCorrectlyPlaced()
    {
        // Única losa de presión para activar el muro en (8, 13)
        Assert.Equal(TileType.PressurePlate, _grid.GetTile(8, 13));
        Assert.True(_grid.IsPressurePlate(8, 13));

        // (3, 13) es espacio libre transitable (sin placa duplicada)
        Assert.Equal(TileType.Empty, _grid.GetTile(3, 13));
        Assert.True(_grid.IsEmpty(3, 13));

        // Puerta de salida en (1, 4) descansando sobre la plataforma de la fila 5
        Assert.Equal(TileType.ExitDoor, _grid.GetTile(1, 4));
        Assert.True(_grid.IsExitDoor(1, 4));

        // Comprobar que el suelo debajo de la puerta es sólido (1, 5)
        Assert.Equal(TileType.SolidWall, _grid.GetTile(1, 5));
    }

    [Fact]
    public void WalkableSpaces_AreEmpty()
    {
        // Posiciones libres conocidas
        Assert.Equal(TileType.Empty, _grid.GetTile(1, 13));
        Assert.Equal(TileType.Empty, _grid.GetTile(2, 13));
        Assert.Equal(TileType.Empty, _grid.GetTile(3, 13));
        Assert.Equal(TileType.Empty, _grid.GetTile(4, 13));
        Assert.Equal(TileType.Empty, _grid.GetTile(1, 1));
        Assert.Equal(TileType.Empty, _grid.GetTile(18, 1));
    }

    [Fact]
    public void Level0_HasOnlyOnePressurePlate_AtCol8Row13()
    {
        int plateCount = 0;
        for (int x = 0; x < GameConstants.GRID_COLUMNS; x++)
        {
            if (_grid.GetTile(x, 13) == TileType.PressurePlate)
            {
                plateCount++;
                Assert.Equal(8, x);
            }
        }

        Assert.Equal(1, plateCount);
    }

    [Fact]
    public void Platform1_ExtensionAndGaps_AreConfiguredCorrectly()
    {
        // Espacio vacío a la izquierda (columna 1)
        Assert.Equal(TileType.Empty, _grid.GetTile(1, 9));
        Assert.True(_grid.IsEmpty(1, 9));

        // Huecos y descansillo intermedio para saltos
        Assert.Equal(TileType.Empty, _grid.GetTile(11, 9));
        Assert.True(_grid.IsEmpty(11, 9));

        Assert.Equal(TileType.SolidWall, _grid.GetTile(12, 9));
        Assert.True(_grid.IsSolid(12, 9));

        Assert.Equal(TileType.Empty, _grid.GetTile(13, 9));
        Assert.True(_grid.IsEmpty(13, 9));

        // Suelo sobre el tesoro del nivel 0 desde la puerta (columna 14) hasta casi el final (columna 17)
        for (int x = 14; x <= 17; x++)
        {
            Assert.Equal(TileType.SolidWall, _grid.GetTile(x, 9));
            Assert.True(_grid.IsSolid(x, 9));
        }

        // Espacio vacío a la derecha (columna 18) simétrico al de la izquierda (columna 1)
        Assert.Equal(TileType.Empty, _grid.GetTile(18, 9));
        Assert.True(_grid.IsEmpty(18, 9));

        // Muro perimetral derecho (columna 19)
        Assert.Equal(TileType.SolidWall, _grid.GetTile(19, 9));
        Assert.True(_grid.IsSolid(19, 9));
    }

    [Fact]
    public void Platform1_PlatformInfo_SpansFromCol2ToCol17()
    {
        var platform1 = _grid.Platforms[1];
        Assert.Equal(1, platform1.Level);
        Assert.Equal(9, platform1.Row);
        Assert.Equal(2, platform1.StartCol);
        Assert.Equal(17, platform1.EndCol);
        Assert.Equal("Plataforma Nivel 1", platform1.Name);
    }

    [Fact]
    public void Platform2_Layout_And_PlatformInfo_AreConfiguredCorrectly()
    {
        // Descansillo de la puerta a la izquierda (fila 5, columnas 1 y 2)
        Assert.Equal(TileType.SolidWall, _grid.GetTile(1, 5));
        Assert.Equal(TileType.SolidWall, _grid.GetTile(2, 5));

        // Hueco de salto hacia la puerta (columna 3)
        Assert.Equal(TileType.Empty, _grid.GetTile(3, 5));
        Assert.True(_grid.IsEmpty(3, 5));

        // Plataforma principal extendida (columnas 4 a 17)
        for (int x = 4; x <= 17; x++)
        {
            if (x == 10)
                Assert.Equal(TileType.Ladder, _grid.GetTile(x, 5));
            else
                Assert.Equal(TileType.SolidWall, _grid.GetTile(x, 5));
        }

        // Espacio vacío a la derecha (columna 18)
        Assert.Equal(TileType.Empty, _grid.GetTile(18, 5));

        // Registro de Plataforma Nivel 2
        var platform2 = _grid.Platforms[2];
        Assert.Equal(2, platform2.Level);
        Assert.Equal(5, platform2.Row);
        Assert.Equal(1, platform2.StartCol);
        Assert.Equal(17, platform2.EndCol);
        Assert.Equal("Plataforma Nivel 2", platform2.Name);
    }
}
