using System;
using System.Runtime.InteropServices;
using RetroGamePiramid.Grid;
using Xunit;

namespace RetroGamePiramid.Tests.Grid;

public class TileTypeTests
{
    [Fact]
    public void UnderlyingType_IsByte()
    {
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(TileType)));
        Assert.Equal(1, System.Runtime.CompilerServices.Unsafe.SizeOf<TileType>());
    }

    [Theory]
    [InlineData(TileType.Empty, 0)]
    [InlineData(TileType.SolidWall, 1)]
    [InlineData(TileType.Ladder, 2)]
    [InlineData(TileType.PressurePlate, 3)]
    [InlineData(TileType.ExitDoor, 4)]
    public void TileType_HasExpectedNumericValues(TileType tileType, byte expectedValue)
    {
        Assert.Equal(expectedValue, (byte)tileType);
    }

    [Theory]
    [InlineData(0, TileType.Empty)]
    [InlineData(1, TileType.SolidWall)]
    [InlineData(2, TileType.Ladder)]
    [InlineData(3, TileType.PressurePlate)]
    [InlineData(4, TileType.ExitDoor)]
    public void CastFromByte_ProducesCorrectTileType(byte value, TileType expectedTileType)
    {
        Assert.Equal(expectedTileType, (TileType)value);
    }

    [Fact]
    public void EnumValues_ContainAllExpectedDefinedMembers()
    {
        var values = Enum.GetValues<TileType>();
        Assert.Equal(5, values.Length);
        Assert.Contains(TileType.Empty, values);
        Assert.Contains(TileType.SolidWall, values);
        Assert.Contains(TileType.Ladder, values);
        Assert.Contains(TileType.PressurePlate, values);
        Assert.Contains(TileType.ExitDoor, values);
    }
}
