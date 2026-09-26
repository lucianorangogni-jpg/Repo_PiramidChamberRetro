using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;
using RetroGamePiramid.Grid;
using RetroGamePiramid.Systems;
using Xunit;

namespace RetroGamePiramid.Tests.Systems;

public class PuzzleManagerTests
{
    private (RoomGrid grid, PuzzleManager puzzle) CreateTestSetup()
    {
        var grid = new RoomGrid();
        grid.LoadDefaultRoom();

        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid);

        return (grid, puzzle);
    }

    [Fact]
    public void InitialState_WallClosed_TreasureAvailable()
    {
        var (grid, puzzle) = CreateTestSetup();

        Assert.False(puzzle.IsWallOpen);
        Assert.False(puzzle.Treasure.IsCollected);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.WallCoord));
        Assert.Equal(TileType.PressurePlate, grid.GetTile(puzzle.StoneCoord));
        Assert.Equal(0, puzzle.Score);
    }

    [Fact]
    public void StepOnStone_FirstTime_OpensWall()
    {
        var (grid, puzzle) = CreateTestSetup();
        // Colocar al jugador encima de la piedra en (8, 13)
        var player = new Player(puzzle.StoneCoord);

        puzzle.Update(grid, player);

        Assert.True(puzzle.IsWallOpen);
        Assert.Equal(TileType.Empty, grid.GetTile(puzzle.WallCoord));
    }

    [Fact]
    public void StayingOnStone_DoesNotToggleContinuously()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(puzzle.StoneCoord);

        // Primer frame: abre el muro
        puzzle.Update(grid, player);
        Assert.True(puzzle.IsWallOpen);

        // Frames subsiguientes permaneciendo sobre la piedra: debe seguir abierto
        for (int i = 0; i < 10; i++)
        {
            puzzle.Update(grid, player);
            Assert.True(puzzle.IsWallOpen);
            Assert.Equal(TileType.Empty, grid.GetTile(puzzle.WallCoord));
        }
    }

    [Fact]
    public void StepOffAndStepOnAgain_SecondTime_ClosesWall()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(puzzle.StoneCoord);

        // 1. Primera pisada: abre el muro
        puzzle.Update(grid, player);
        Assert.True(puzzle.IsWallOpen);

        // 2. Salir de la piedra hacia la columna 10
        player.SetPosition(10 * GameConstants.TILE_SIZE, 13 * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.IsWallOpen); // Sigue abierto mientras avanza

        // 3. Segunda pisada: vuelve a pisar la piedra en (8, 13)
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, 13 * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);

        // Debe haberse cerrado el muro
        Assert.False(puzzle.IsWallOpen);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.WallCoord));
    }

    [Fact]
    public void CollectTreasure_AwardsPointsAndMarksCollected()
    {
        var (grid, puzzle) = CreateTestSetup();
        // Colocar al jugador sobre el cofre en (17, 13)
        var player = new Player(puzzle.TreasureCoord);

        puzzle.Update(grid, player);

        Assert.True(puzzle.Treasure.IsCollected);
        Assert.Equal(1000, puzzle.Score);
        Assert.True(puzzle.NotificationTimer > 0);
    }

    [Fact]
    public void InitialState_Treasure2_AvailableAtPlatformLevel2()
    {
        var (grid, puzzle) = CreateTestSetup();

        Assert.Equal(new GridCoord(8, 4), puzzle.Treasure2Coord);
        Assert.False(puzzle.Treasure2.IsCollected);
    }

    [Fact]
    public void CollectTreasure2_OnPlatformLevel2_AwardsPointsAndMarksCollected()
    {
        var (grid, puzzle) = CreateTestSetup();
        // Colocar al jugador sobre el segundo cofre en (8, 4)
        var player = new Player(puzzle.Treasure2Coord);

        puzzle.Update(grid, player);

        Assert.True(puzzle.Treasure2.IsCollected);
        Assert.Equal(1000, puzzle.Score);
        Assert.True(puzzle.NotificationTimer > 0);
    }

    [Fact]
    public void CollectBothTreasures_AwardsTotal2000Points()
    {
        var (grid, puzzle) = CreateTestSetup();

        // 1. Recoger primer tesoro en (17, 13)
        var player = new Player(puzzle.TreasureCoord);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.Equal(1000, puzzle.Score);

        // 2. Moverse y recoger segundo tesoro en (8, 4)
        player.SetPosition(puzzle.Treasure2Coord.X * GameConstants.TILE_SIZE, puzzle.Treasure2Coord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Treasure2.IsCollected);
        Assert.Equal(2000, puzzle.Score);
    }

    [Fact]
    public void ResetScore_ResetsScoreToZero()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(puzzle.TreasureCoord);
        puzzle.Update(grid, player);
        Assert.Equal(1000, puzzle.Score);

        puzzle.ResetScore();
        Assert.Equal(0, puzzle.Score);
    }

    [Fact]
    public void InitialState_KeyAvailableAndDoorLocked()
    {
        var (grid, puzzle) = CreateTestSetup();

        Assert.Equal(new GridCoord(16, 6), puzzle.KeyCoord);
        Assert.Equal(new GridCoord(1, 4), puzzle.ExitDoorCoord);
        Assert.False(puzzle.HasKey);
        Assert.False(puzzle.IsChamberCompleted);
    }

    [Fact]
    public void Key_WalkingUnderneathAtRow8_DoesNotCollectKey()
    {
        var (grid, puzzle) = CreateTestSetup();
        // Colocar al arqueólogo caminando sobre la Plataforma 1 (fila 8, Y = 128)
        var player = new Player(16 * GameConstants.TILE_SIZE + 1f, 8 * GameConstants.TILE_SIZE);

        puzzle.Update(grid, player);

        // La llave cuelga en fila 6 (Y = 96 a 112). Caminando no la alcanza
        Assert.False(puzzle.HasKey);
        Assert.False(puzzle.Key.IsCollected);
        Assert.Equal(0, puzzle.Score);
    }

    [Fact]
    public void Key_JumpingAtCol16_ReachesHeightAndCollectsKey()
    {
        var (grid, puzzle) = CreateTestSetup();
        // Colocar al arqueólogo en el pico de su salto en columna 16 (Y = 100)
        var player = new Player(16 * GameConstants.TILE_SIZE + 1f, 100f);

        puzzle.Update(grid, player);

        Assert.True(puzzle.HasKey);
        Assert.True(puzzle.Key.IsCollected);
        Assert.Equal(500, puzzle.Score);
        Assert.Equal(PuzzleManager.MSG_KEY, puzzle.NotificationMessage);
    }

    [Fact]
    public void ExitDoor_WithoutKey_RemainsLocked()
    {
        var (grid, puzzle) = CreateTestSetup();
        // Colocar al arqueólogo en la puerta de salida (1, 4) sin la llave
        var player = new Player(puzzle.ExitDoorCoord);

        puzzle.Update(grid, player);

        Assert.False(puzzle.IsChamberCompleted);
        Assert.Equal(PuzzleManager.MSG_DOOR_LOCKED, puzzle.NotificationMessage);
    }

    [Fact]
    public void ExitDoor_WithKey_CompletesChamber()
    {
        var (grid, puzzle) = CreateTestSetup();
        // 1. Recoger la llave
        var player = new Player(16 * GameConstants.TILE_SIZE + 1f, 100f);
        puzzle.Update(grid, player);
        Assert.True(puzzle.HasKey);

        // 2. Llegar a la puerta de salida (1, 4) con la llave
        player.SetPosition(puzzle.ExitDoorCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.ExitDoorCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);

        Assert.True(puzzle.IsChamberCompleted);
    }
}
