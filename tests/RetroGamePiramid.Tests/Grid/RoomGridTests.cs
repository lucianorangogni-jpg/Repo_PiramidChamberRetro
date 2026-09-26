using RetroGamePiramid.Core;
using RetroGamePiramid.Grid;
using Xunit;

namespace RetroGamePiramid.Tests.Grid;

public class RoomGridTests
{
    [Fact]
    public void Dimensions_MatchGameConstants()
    {
        var grid = new RoomGrid();
        Assert.Equal(20, grid.Columns);
        Assert.Equal(15, grid.Rows);
        Assert.Equal(GameConstants.GRID_COLUMNS, grid.Columns);
        Assert.Equal(GameConstants.GRID_ROWS, grid.Rows);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(19, 0, 19)]
    [InlineData(0, 1, 20)]
    [InlineData(5, 2, 45)]
    [InlineData(19, 14, 299)]
    public void GetIndex_CalculatesCorrectFlatIndex(int x, int y, int expectedIndex)
    {
        Assert.Equal(expectedIndex, RoomGrid.GetIndex(x, y));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(19, 14, true)]
    [InlineData(10, 7, true)]
    [InlineData(0, 14, true)]
    [InlineData(19, 0, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(-1, -1, false)]
    [InlineData(20, 0, false)]
    [InlineData(0, 15, false)]
    [InlineData(20, 15, false)]
    [InlineData(100, 50, false)]
    public void InBounds_ValidatesGridBoundaries(int x, int y, bool expectedInBounds)
    {
        var grid = new RoomGrid();
        Assert.Equal(expectedInBounds, grid.InBounds(x, y));
        Assert.Equal(expectedInBounds, grid.InBounds(new GridCoord(x, y)));
    }

    [Fact]
    public void InitialState_DefaultsToEmpty()
    {
        var grid = new RoomGrid();
        for (int y = 0; y < grid.Rows; y++)
        {
            for (int x = 0; x < grid.Columns; x++)
            {
                Assert.Equal(TileType.Empty, grid.GetTile(x, y));
                Assert.True(grid.IsEmpty(x, y));
            }
        }
    }

    [Fact]
    public void SetTileAndGetTile_WorkCorrectlyWithinBounds()
    {
        var grid = new RoomGrid();
        var coord = new GridCoord(3, 4);

        grid.SetTile(coord, TileType.Ladder);
        Assert.Equal(TileType.Ladder, grid.GetTile(coord));
        Assert.Equal(TileType.Ladder, grid.GetTile(3, 4));

        grid.SetTile(3, 4, TileType.SolidWall);
        Assert.Equal(TileType.SolidWall, grid.GetTile(3, 4));
        Assert.Equal(TileType.SolidWall, grid.GetTile(coord));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(20, 5)]
    [InlineData(5, 15)]
    [InlineData(100, 100)]
    public void GetTile_OutOfBounds_ReturnsSolidWall(int x, int y)
    {
        var grid = new RoomGrid();
        Assert.Equal(TileType.SolidWall, grid.GetTile(x, y));
        Assert.Equal(TileType.SolidWall, grid.GetTile(new GridCoord(x, y)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(20, 5)]
    [InlineData(5, 15)]
    [InlineData(-10, -10)]
    public void SetTile_OutOfBounds_DoesNotThrowOrCorrupt(int x, int y)
    {
        var grid = new RoomGrid();
        // Debe ignorar la operación sin lanzar excepción
        grid.SetTile(x, y, TileType.Ladder);
        grid.SetTile(new GridCoord(x, y), TileType.Ladder);

        // Los tiles válidos siguen en Empty
        Assert.Equal(TileType.Empty, grid.GetTile(0, 0));
    }

    [Fact]
    public void Clear_SetsAllTilesToSpecifiedType()
    {
        var grid = new RoomGrid();
        grid.SetTile(5, 5, TileType.Ladder);
        grid.Clear(TileType.SolidWall);

        for (int y = 0; y < grid.Rows; y++)
        {
            for (int x = 0; x < grid.Columns; x++)
            {
                Assert.Equal(TileType.SolidWall, grid.GetTile(x, y));
            }
        }
    }

    [Fact]
    public void IsSolid_ReturnsExpectedValues()
    {
        var grid = new RoomGrid();
        grid.SetTile(2, 2, TileType.SolidWall);
        grid.SetTile(3, 2, TileType.Ladder);
        grid.SetTile(4, 2, TileType.Empty);
        grid.SetTile(5, 2, TileType.PressurePlate);
        grid.SetTile(6, 2, TileType.ExitDoor);

        Assert.True(grid.IsSolid(2, 2));
        Assert.False(grid.IsSolid(3, 2));
        Assert.False(grid.IsSolid(4, 2));
        Assert.False(grid.IsSolid(5, 2));
        Assert.False(grid.IsSolid(6, 2));

        // Fuera de límites siempre es sólido
        Assert.True(grid.IsSolid(-1, 0));
        Assert.True(grid.IsSolid(20, 0));
        Assert.True(grid.IsSolid(0, -1));
        Assert.True(grid.IsSolid(0, 15));
    }

    [Fact]
    public void IsLadder_ReturnsExpectedValues()
    {
        var grid = new RoomGrid();
        grid.SetTile(2, 2, TileType.Ladder);
        grid.SetTile(3, 2, TileType.SolidWall);

        Assert.True(grid.IsLadder(2, 2));
        Assert.True(grid.IsLadder(new GridCoord(2, 2)));
        Assert.False(grid.IsLadder(3, 2));
        Assert.False(grid.IsLadder(0, 0));
        Assert.False(grid.IsLadder(-1, 0));
        Assert.False(grid.IsLadder(20, 15));
    }

    [Fact]
    public void IsPressurePlate_ReturnsExpectedValues()
    {
        var grid = new RoomGrid();
        grid.SetTile(3, 13, TileType.PressurePlate);

        Assert.True(grid.IsPressurePlate(3, 13));
        Assert.True(grid.IsPressurePlate(new GridCoord(3, 13)));
        Assert.False(grid.IsPressurePlate(3, 12));
        Assert.False(grid.IsPressurePlate(-1, 0));
    }

    [Fact]
    public void IsExitDoor_ReturnsExpectedValues()
    {
        var grid = new RoomGrid();
        grid.SetTile(16, 4, TileType.ExitDoor);

        Assert.True(grid.IsExitDoor(16, 4));
        Assert.True(grid.IsExitDoor(new GridCoord(16, 4)));
        Assert.False(grid.IsExitDoor(16, 5));
        Assert.False(grid.IsExitDoor(20, 4));
    }

    [Fact]
    public void IsEmpty_ReturnsExpectedValues()
    {
        var grid = new RoomGrid();
        grid.SetTile(1, 1, TileType.Empty);
        grid.SetTile(2, 2, TileType.SolidWall);

        Assert.True(grid.IsEmpty(1, 1));
        Assert.False(grid.IsEmpty(2, 2));
        Assert.False(grid.IsEmpty(-1, -1)); // Fuera de límites no es Empty
    }
}
