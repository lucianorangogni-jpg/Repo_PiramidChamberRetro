using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Grid;
using Xunit;

namespace RetroGamePiramid.Tests.Grid;

public class GridCoordTests
{
    [Fact]
    public void Constructor_SetsCoordinatesCorrectly()
    {
        var coord = new GridCoord(7, 12);
        Assert.Equal(7, coord.X);
        Assert.Equal(12, coord.Y);
    }

    [Fact]
    public void StaticDirections_HaveCorrectValues()
    {
        Assert.Equal(new GridCoord(0, 0), GridCoord.Zero);
        Assert.Equal(new GridCoord(1, 1), GridCoord.One);
        Assert.Equal(new GridCoord(0, -1), GridCoord.Up);
        Assert.Equal(new GridCoord(0, 1), GridCoord.Down);
        Assert.Equal(new GridCoord(-1, 0), GridCoord.Left);
        Assert.Equal(new GridCoord(1, 0), GridCoord.Right);
    }

    [Theory]
    [InlineData(2, 3, 4, 5, 6, 8)]
    [InlineData(-1, 5, 1, -5, 0, 0)]
    [InlineData(0, 0, 10, -20, 10, -20)]
    public void OperatorPlus_AddsCoordinates(int x1, int y1, int x2, int y2, int expectedX, int expectedY)
    {
        var a = new GridCoord(x1, y1);
        var b = new GridCoord(x2, y2);
        var result = a + b;

        Assert.Equal(expectedX, result.X);
        Assert.Equal(expectedY, result.Y);
    }

    [Theory]
    [InlineData(10, 15, 4, 5, 6, 10)]
    [InlineData(0, 0, 1, 1, -1, -1)]
    [InlineData(-5, -5, -3, -2, -2, -3)]
    public void OperatorMinus_SubtractsCoordinates(int x1, int y1, int x2, int y2, int expectedX, int expectedY)
    {
        var a = new GridCoord(x1, y1);
        var b = new GridCoord(x2, y2);
        var result = a - b;

        Assert.Equal(expectedX, result.X);
        Assert.Equal(expectedY, result.Y);
    }

    [Fact]
    public void EqualityAndInequality_WorkCorrectly()
    {
        var a = new GridCoord(3, 4);
        var b = new GridCoord(3, 4);
        var c = new GridCoord(4, 3);
        var d = new GridCoord(3, 5);

        Assert.True(a == b);
        Assert.False(a == c);
        Assert.False(a == d);

        Assert.False(a != b);
        Assert.True(a != c);
        Assert.True(a != d);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals((object)c));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("not a coord"));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Deconstruct_ExtractsXAndY()
    {
        var coord = new GridCoord(9, 14);
        var (x, y) = coord;

        Assert.Equal(9, x);
        Assert.Equal(14, y);
    }

    [Fact]
    public void ToString_FormatsExpectedOutput()
    {
        var coord = new GridCoord(5, 8);
        Assert.Equal("(5, 8)", coord.ToString());
    }

    [Theory]
    [InlineData(0, 0, 16, 0f, 0f)]
    [InlineData(1, 1, 16, 16f, 16f)]
    [InlineData(5, 9, 16, 80f, 144f)]
    [InlineData(19, 14, 16, 304f, 224f)]
    [InlineData(2, 3, 32, 64f, 96f)]
    public void ToPixelPosition_CalculatesCorrectVector2(int gridX, int gridY, int tileSize, float expX, float expY)
    {
        var coord = new GridCoord(gridX, gridY);
        Vector2 pixelPos = coord.ToPixelPosition(tileSize);

        Assert.Equal(expX, pixelPos.X);
        Assert.Equal(expY, pixelPos.Y);
    }

    [Fact]
    public void ToPixelPosition_DefaultUsesGameConstantsTileSize()
    {
        var coord = new GridCoord(2, 3);
        Vector2 pixelPos = coord.ToPixelPosition();

        Assert.Equal(new Vector2(2 * GameConstants.TILE_SIZE, 3 * GameConstants.TILE_SIZE), pixelPos);
    }
}
