using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;
using RetroGamePiramid.Grid;
using Xunit;

namespace RetroGamePiramid.Tests.Entities;

public class FloorTrapTests
{
    [Fact]
    public void InitialState_TrapIsClosed_WithCorrectCoordinates()
    {
        var coord = new GridCoord(16, 9);
        var trap = new FloorTrap(coord);

        Assert.Equal(coord, trap.Coord);
        Assert.Equal(new Vector2(16 * 16, 9 * 16), trap.Position);
        Assert.False(trap.IsOpen);
    }

    [Fact]
    public void Open_SetsIsOpenTrue()
    {
        var trap = new FloorTrap(new GridCoord(16, 9));
        trap.Open();

        Assert.True(trap.IsOpen);
    }

    [Fact]
    public void Reset_RestoresIsOpenToFalse()
    {
        var trap = new FloorTrap(new GridCoord(16, 9));
        trap.Open();
        Assert.True(trap.IsOpen);

        trap.Reset();
        Assert.False(trap.IsOpen);
    }

    [Fact]
    public void Close_SetsIsOpenFalse()
    {
        var trap = new FloorTrap(new GridCoord(13, 5));
        trap.Open();
        Assert.True(trap.IsOpen);

        trap.Close();
        Assert.False(trap.IsOpen);
    }
}
