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

    [Fact]
    public void InitialState_TrapIsClosed_GridTileIsSolid()
    {
        var (grid, puzzle) = CreateTestSetup();

        Assert.False(puzzle.IsTrapOpen);
        Assert.False(puzzle.Trap.IsOpen);
        Assert.Equal(new GridCoord(16, 9), puzzle.TrapCoord);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.TrapCoord));
    }

    [Fact]
    public void PassingUnderKey_WhileGrounded_OpensTrapAndForcesFalling()
    {
        var (grid, puzzle) = CreateTestSetup();
        // Colocar al jugador caminando sobre la plataforma 1 bajo la llave (col 16, fila 8-9)
        var player = new Player(16 * GameConstants.TILE_SIZE + 1f, 8 * GameConstants.TILE_SIZE);
        Assert.Equal(PlayerState.Idle, player.State);

        puzzle.Update(grid, player);

        // La trampa debe abrirse, el piso volverse Empty y el jugador entrar en Falling
        Assert.True(puzzle.IsTrapOpen);
        Assert.Equal(TileType.Empty, grid.GetTile(puzzle.TrapCoord));
        Assert.Equal(PlayerState.Falling, player.State);
        Assert.Equal(PuzzleManager.MSG_TRAP, puzzle.NotificationMessage);

        // La llave NO debe ser recolectada
        Assert.False(puzzle.Key.IsCollected);
        Assert.False(puzzle.HasKey);

        // El muro secreto debe haberse abierto para permitir la salida del jugador
        Assert.True(puzzle.IsWallOpen);
        Assert.Equal(TileType.Empty, grid.GetTile(puzzle.WallCoord));
    }

    [Fact]
    public void PassingUnderKey_PlayerFallsToLevel0_WithoutLosingLives_AndDoesNotGetKey()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(16 * GameConstants.TILE_SIZE + 1f, 8 * GameConstants.TILE_SIZE);
        Assert.Equal(3, player.Lives);

        // Al pasar por debajo, se activa la trampa
        puzzle.Update(grid, player);
        Assert.Equal(PlayerState.Falling, player.State);

        // Simular la caída paso a paso hasta que aterriza en el suelo del Nivel 0 (fila 14, Y = 208)
        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        for (int frame = 0; frame < 60; frame++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
            if (player.State == PlayerState.Idle)
                break;
        }

        // El jugador debe aterrizar en el suelo inferior (Nivel 0) ileso con sus 3 vidas intactas
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(3, player.Lives);
        Assert.False(player.IsEliminated);
        Assert.Equal(13 * GameConstants.TILE_SIZE, player.Position.Y); // Sobre fila 14 (suelo nivel 0)

        // La llave NO fue recogida durante la caída
        Assert.False(puzzle.Key.IsCollected);
        Assert.False(puzzle.HasKey);

        // El muro conmutable está abierto, permitiendo al jugador caminar a la izquierda hacia la escalera
        Assert.True(puzzle.IsWallOpen);
        Assert.Equal(TileType.Empty, grid.GetTile(puzzle.WallCoord));
    }

    [Fact]
    public void JumpingBeforeReachingKey_CollectsKeyInMidAir_AndLandsSafelyOnPlatform1()
    {
        var (grid, puzzle) = CreateTestSetup();
        // Colocar al jugador en la columna 15 (antes de la llave) sobre la plataforma 1
        var player = new Player(15 * GameConstants.TILE_SIZE + 4f, 8 * GameConstants.TILE_SIZE);
        Assert.Equal(3, player.Lives);

        // Iniciar salto hacia la derecha antes de llegar a la posición de la trampa/llave
        var jumpInput = new RetroGamePiramid.Input.PlayerInput(left: false, right: true, up: false, down: false, jump: true);
        player.Update(grid, in jumpInput);
        puzzle.Update(grid, player);

        Assert.Equal(PlayerState.Jumping, player.State);

        // Avanzar el salto mientras sobrevuela en el aire
        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        bool keyCollectedMidAir = false;

        for (int frame = 0; frame < Player.JUMP_DURATION_FRAMES; frame++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);

            if (puzzle.Key.IsCollected)
            {
                keyCollectedMidAir = true;
            }
        }

        // 1. Debe haber recogido la llave en el aire
        Assert.True(keyCollectedMidAir, "La llave debió ser recogida en pleno vuelo");
        Assert.True(puzzle.HasKey);
        Assert.Equal(500, puzzle.Score);

        // 2. Debe haber aterrizado a salvo sobre la columna 17 (al otro lado de la trampa)
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(8 * GameConstants.TILE_SIZE, player.Position.Y);
        Assert.True(player.Position.X >= 17 * GameConstants.TILE_SIZE, "El jugador debió aterrizar en columna 17");
        Assert.Equal(3, player.Lives);
    }

    [Fact]
    public void JumpingBackFromColumn17_CrossesOpenTrap_AndLandsOnColumn15()
    {
        var (grid, puzzle) = CreateTestSetup();
        // Abrir la trampa manualmente (como si hubiese sido activada previamente)
        puzzle.Trap.Open();
        grid.SetTile(puzzle.TrapCoord.X, puzzle.TrapCoord.Y, TileType.Empty);

        // Colocar al jugador en la columna 17
        var player = new Player(17 * GameConstants.TILE_SIZE + 4f, 8 * GameConstants.TILE_SIZE);

        // Saltar hacia la izquierda
        var jumpLeft = new RetroGamePiramid.Input.PlayerInput(left: true, right: false, up: false, down: false, jump: true);
        player.Update(grid, in jumpLeft);
        puzzle.Update(grid, player);

        Assert.Equal(PlayerState.Jumping, player.State);

        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        for (int frame = 0; frame < Player.JUMP_DURATION_FRAMES; frame++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
        }

        // Debe aterrizar a salvo sobre la columna 15
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(8 * GameConstants.TILE_SIZE, player.Position.Y);
        Assert.True(player.Position.X < 16 * GameConstants.TILE_SIZE, "El jugador debió aterrizar en columna 15");
        Assert.Equal(3, player.Lives);
    }

    [Fact]
    public void Initialize_ResetsTrapToClosed_AndGridToSolid()
    {
        var (grid, puzzle) = CreateTestSetup();

        // Abrir la trampa
        puzzle.Trap.Open();
        grid.SetTile(puzzle.TrapCoord.X, puzzle.TrapCoord.Y, TileType.Empty);
        Assert.True(puzzle.IsTrapOpen);

        // Reinicializar
        puzzle.Initialize(grid);

        Assert.False(puzzle.IsTrapOpen);
        Assert.False(puzzle.Trap.IsOpen);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.TrapCoord));
    }
}
