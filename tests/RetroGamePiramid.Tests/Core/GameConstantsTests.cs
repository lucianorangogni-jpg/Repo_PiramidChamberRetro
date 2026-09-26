using System;
using RetroGamePiramid.Core;
using Xunit;

namespace RetroGamePiramid.Tests.Core;

public class GameConstantsTests
{
    [Fact]
    public void NativeResolution_Is320x240()
    {
        Assert.Equal(320, GameConstants.VIRTUAL_WIDTH);
        Assert.Equal(240, GameConstants.VIRTUAL_HEIGHT);
    }

    [Fact]
    public void GridDimensions_MatchVirtualResolution()
    {
        Assert.Equal(16, GameConstants.TILE_SIZE);
        Assert.Equal(20, GameConstants.GRID_COLUMNS);
        Assert.Equal(15, GameConstants.GRID_ROWS);

        Assert.Equal(GameConstants.VIRTUAL_WIDTH, GameConstants.GRID_COLUMNS * GameConstants.TILE_SIZE);
        Assert.Equal(GameConstants.VIRTUAL_HEIGHT, GameConstants.GRID_ROWS * GameConstants.TILE_SIZE);
    }

    [Fact]
    public void FrameRateAndTimings_Are60FpsArcadeFixed()
    {
        Assert.Equal(60, GameConstants.TARGET_FPS);
        Assert.Equal(1f / 60f, GameConstants.FIXED_TIME_STEP_SECONDS, 5);
        Assert.Equal(TimeSpan.FromSeconds(1.0 / 60.0), GameConstants.TargetElapsedTime);
    }
}
