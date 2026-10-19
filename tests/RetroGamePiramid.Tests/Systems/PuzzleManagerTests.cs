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
        Assert.Equal(new GridCoord(18, 4), puzzle.ExitDoorCoord);
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
        // Colocar al arqueólogo en la puerta de salida (18, 4) sin la llave
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

        // 2. Llegar a la puerta de salida (18, 4) con la llave
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
    public void PassingUnderKey_PlayerFallsToLevel0_LosesLife_AndIsEliminated()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(16 * GameConstants.TILE_SIZE + 1f, 8 * GameConstants.TILE_SIZE);
        Assert.Equal(3, player.Lives);

        // Al pasar por debajo, se activa la trampa
        puzzle.Update(grid, player);
        Assert.Equal(PlayerState.Falling, player.State);
        Assert.True(puzzle.IsPlayerTrappedInTrap);

        // Simular la caída paso a paso hasta que aterriza en el suelo del Nivel 0 (fila 14, Y = 208)
        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        for (int frame = 0; frame < 60; frame++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
            if (player.IsEliminated)
                break;
        }

        // El jugador cayó por la trampa: pierde 1 vida y queda en estado Eliminado
        Assert.Equal(PlayerState.Eliminated, player.State);
        Assert.Equal(2, player.Lives);
        Assert.True(player.IsEliminated);
        Assert.True(puzzle.HasTrapEliminated);

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

    [Fact]
    public void Chamber1_SecretPuzzle_WhenTreasureNotCollected_DoorCyclesDoNotReloadTreasure()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(puzzle.StoneCoord);

        Assert.False(puzzle.Treasure.IsCollected);
        Assert.Equal(0, puzzle.DoorCycleCount);

        // Realizar 3 ciclos completos de abrir y cerrar la puerta
        for (int cycle = 0; cycle < 3; cycle++)
        {
            // Abrir
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
            Assert.True(puzzle.IsWallOpen);

            // Salir de la piedra
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);

            // Cerrar
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
            Assert.False(puzzle.IsWallOpen);

            // Salir de la piedra
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
        }

        // El tesoro sigue sin haber sido recogido y el contador permanece en 0
        Assert.False(puzzle.Treasure.IsCollected);
        Assert.Equal(0, puzzle.DoorCycleCount);
        Assert.NotEqual(PuzzleManager.MSG_TREASURE_RELOAD, puzzle.NotificationMessage);
    }

    [Fact]
    public void Chamber1_SecretPuzzle_WhenAllItemsCollected_OpeningAndClosingDoorThreeTimes_ReloadsTreasure()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(puzzle.StoneCoord);

        // 1. Abrir puerta y recoger tesoro 1 (Nivel 0)
        puzzle.Update(grid, player); // Abre el muro
        Assert.True(puzzle.IsWallOpen);

        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.Equal(1000, puzzle.Score);

        // 2. Recoger la llave (Nivel 1)
        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Key.IsCollected);
        Assert.Equal(1500, puzzle.Score);

        // 3. Recoger tesoro 2 (Nivel 2)
        player.SetPosition(puzzle.Treasure2Coord.X * GameConstants.TILE_SIZE + 2f, puzzle.Treasure2Coord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Treasure2.IsCollected);
        Assert.Equal(2500, puzzle.Score);
        Assert.False(puzzle.HasSecretTreasureReloaded);

        // 4. Realizar 3 ciclos completos de abrir y cerrar con todos los prerrequisitos cumplidos
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            // Pisar la piedra: si estaba abierta se cierra; si estaba cerrada se abre.
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);

            // Salir de la piedra
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);

            // Si cycle < 3, abrir nuevamente para continuar el ciclo siguiente
            if (cycle < 3)
            {
                player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
                puzzle.Update(grid, player); // Abre
                Assert.True(puzzle.IsWallOpen);

                player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
                puzzle.Update(grid, player); // Sale
            }
        }

        // Al cerrarse por 3.ª vez:
        Assert.False(puzzle.IsWallOpen);
        Assert.False(puzzle.Treasure.IsCollected, "El tesoro 1 debió recargarse");
        Assert.True(puzzle.HasSecretTreasureReloaded);
        Assert.Equal(0, puzzle.DoorCycleCount);
        Assert.Equal(PuzzleManager.MSG_TREASURE_RELOAD, puzzle.NotificationMessage);

        // 5. Volver a abrir la puerta y recoger el nuevo tesoro por otros +1000 puntos
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player); // Abre puerta
        Assert.True(puzzle.IsWallOpen);

        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player); // Recoge segundo tesoro

        Assert.True(puzzle.Treasure.IsCollected);
        Assert.Equal(3500, puzzle.Score);
    }

    [Fact]
    public void Chamber1_SecretPuzzle_MissingTreasure2_DoesNotReloadTreasure()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(puzzle.StoneCoord);

        // Recoger tesoro 1 y llave, pero NO tesoro 2
        puzzle.Update(grid, player); // Abre puerta
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player); // Recoge tesoro 1

        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player); // Recoge llave

        Assert.True(puzzle.Treasure.IsCollected);
        Assert.True(puzzle.Key.IsCollected);
        Assert.False(puzzle.Treasure2.IsCollected);

        // 3 ciclos de puerta
        for (int i = 0; i < 6; i++)
        {
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
        }

        // No se recarga porque falta tesoro 2
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.False(puzzle.HasSecretTreasureReloaded);
        Assert.NotEqual(PuzzleManager.MSG_TREASURE_RELOAD, puzzle.NotificationMessage);
    }

    [Fact]
    public void Chamber1_SecretPuzzle_MissingKey_DoesNotReloadTreasure()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(puzzle.StoneCoord);

        // Recoger tesoro 1 y tesoro 2, pero NO la llave
        puzzle.Update(grid, player); // Abre puerta
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player); // Recoge tesoro 1

        player.SetPosition(puzzle.Treasure2Coord.X * GameConstants.TILE_SIZE + 2f, puzzle.Treasure2Coord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player); // Recoge tesoro 2

        Assert.True(puzzle.Treasure.IsCollected);
        Assert.False(puzzle.Key.IsCollected);
        Assert.True(puzzle.Treasure2.IsCollected);

        // 3 ciclos de puerta
        for (int i = 0; i < 6; i++)
        {
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
        }

        // No se recarga porque falta la llave
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.False(puzzle.HasSecretTreasureReloaded);
        Assert.NotEqual(PuzzleManager.MSG_TREASURE_RELOAD, puzzle.NotificationMessage);
    }

    [Fact]
    public void Chamber1_SecretPuzzle_CanOnlyBeTriggeredOnce()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(puzzle.StoneCoord);

        // 1. Recoger los 3 ítems
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.Treasure2Coord.X * GameConstants.TILE_SIZE + 2f, puzzle.Treasure2Coord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);

        // 2. Primeros 3 ciclos -> activa recarga
        for (int i = 0; i < 6; i++)
        {
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
        }

        Assert.False(puzzle.Treasure.IsCollected);
        Assert.True(puzzle.HasSecretTreasureReloaded);

        // 3. Recoger el tesoro recargado
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player); // Abre
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player); // Recoge
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.Equal(3500, puzzle.Score);

        // 4. Intentar realizar otros 3 ciclos completos de puerta
        for (int i = 0; i < 6; i++)
        {
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
        }

        // NO debe volver a recargarse (activación única)
        Assert.True(puzzle.Treasure.IsCollected, "El tesoro no debe recargarse una segunda vez");
        Assert.Equal(0, puzzle.DoorCycleCount);
        Assert.Equal(3500, puzzle.Score);
    }

    [Fact]
    public void Chamber2_Puzzle1_RequiresTreasure0_ThenKey_ThenMummyJump_Then2DoorCycles()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 2);

        var player = new Player(puzzle.StoneCoord);

        // 1. Abrir puerta y agarrar el tesoro del Nivel 0 primero
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.Equal(1, puzzle.Chamber2Puzzle1Stage);

        // Cerrar puerta al salir de la cámara del tesoro
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.False(puzzle.IsWallOpen);

        // 2. Agarrar la llave en el Nivel 1
        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Key.IsCollected);
        Assert.Equal(2, puzzle.Chamber2Puzzle1Stage);

        // 3. Saltar la momia del Nivel 1
        puzzle.RegisterLevel1MummyJump();
        Assert.True(puzzle.HasJumpedOverLevel1Mummy);
        Assert.Equal(3, puzzle.Chamber2Puzzle1Stage);

        // 4. Pasar por el activador de la puerta para abrir y cerrar la puerta 2 veces (4 pases por la losa)
        // Ciclo 1: Abrir y Cerrar
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);

        // Con 1 solo ciclo aún no debe haberse recargado
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.False(puzzle.HasSecretTreasureReloaded);
        Assert.Equal(1, puzzle.DoorCycleCount);

        // Ciclo 2: Abrir y Cerrar
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);

        // Al completar el 2do ciclo de abrir y cerrar, se cumple el Puzle 1 de la Recámara 2
        Assert.False(puzzle.Treasure.IsCollected);
        Assert.True(puzzle.HasSecretTreasureReloaded);
        Assert.Equal(PuzzleManager.MSG_TREASURE_RELOAD, puzzle.NotificationMessage);
    }

    [Fact]
    public void Chamber2_Puzzle1_DoesNotTrigger_IfKeyCollectedBeforeTreasure0_OrWithoutJumpingMummy()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 2);

        var player = new Player(puzzle.KeyCoord);

        // Agarrar primero la llave ANTES que el tesoro del Nivel 0 (orden incorrecto)
        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Key.IsCollected);
        Assert.Equal(0, puzzle.Chamber2Puzzle1Stage);

        // Luego agarrar el tesoro del Nivel 0 y saltar la momia
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        puzzle.RegisterLevel1MummyJump();

        // Realizar 2 ciclos de puerta
        for (int i = 0; i < 6; i++)
        {
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
        }

        // No debe haberse activado por no respetar el orden (primero tesoro Nivel 0, luego llave, luego saltar momia)
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.False(puzzle.HasSecretTreasureReloaded);
    }

    [Fact]
    public void Initialize_Chamber2_SetsTrapAtRow10_AndKeyAtRow7()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 2);

        Assert.Equal(new GridCoord(16, 10), puzzle.TrapCoord);
        Assert.Equal(new GridCoord(16, 7), puzzle.KeyCoord);
        Assert.Equal(new GridCoord(1, 5), puzzle.ExitDoorCoord);
        Assert.Equal(new GridCoord(8, 5), puzzle.Treasure2Coord);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.TrapCoord));
        Assert.Equal(TileType.ExitDoor, grid.GetTile(puzzle.ExitDoorCoord));
    }

    [Fact]
    public void Chamber2_PassingUnderKey_OnPlatform1Row10_TriggersTrapAndForcesFalling()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 2);

        // En Recámara 2, la plataforma 1 está en fila 10 (Y suelo = 160, jugador Y = 144)
        var player = new Player(16 * GameConstants.TILE_SIZE, 9 * GameConstants.TILE_SIZE);
        Assert.Equal(PlayerState.Idle, player.State);

        puzzle.Update(grid, player);

        Assert.True(puzzle.Trap.IsOpen, "La trampa debió abrirse en fila 10");
        Assert.Equal(TileType.Empty, grid.GetTile(puzzle.TrapCoord));
        Assert.Equal(PlayerState.Falling, player.State);
        Assert.Equal(PuzzleManager.MSG_TRAP, puzzle.NotificationMessage);
    }

    [Fact]
    public void Chamber2_PassingUnderKey_PlayerFallsToLevel0_LosesLife_AndIsEliminated()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 2);

        var player = new Player(16 * GameConstants.TILE_SIZE, 9 * GameConstants.TILE_SIZE);
        Assert.Equal(3, player.Lives);

        puzzle.Update(grid, player);
        Assert.Equal(PlayerState.Falling, player.State);

        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        for (int frame = 0; frame < 60; frame++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
            if (player.IsEliminated)
                break;
        }

        Assert.Equal(PlayerState.Eliminated, player.State);
        Assert.Equal(2, player.Lives);
        Assert.True(player.IsEliminated);
        Assert.True(puzzle.HasTrapEliminated);
    }

    [Fact]
    public void Chamber2_JumpingBeforeKey_CollectsKeyAtRow7_AndLandsOnColumn17()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 2);

        // Colocar al jugador en columna 15 en la plataforma 1 (fila 10, Y = 144)
        var player = new Player(15 * GameConstants.TILE_SIZE + 4f, 9 * GameConstants.TILE_SIZE);
        Assert.Equal(3, player.Lives);

        // Iniciar salto hacia la derecha
        var jumpInput = new RetroGamePiramid.Input.PlayerInput(left: false, right: true, up: false, down: false, jump: true);
        player.Update(grid, in jumpInput);
        puzzle.Update(grid, player);

        Assert.Equal(PlayerState.Jumping, player.State);

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

        // 1. Recoge la llave colgada en fila 7 en pleno salto
        Assert.True(keyCollectedMidAir, "La llave debió ser recogida en el aire en Recámara 2");
        Assert.True(puzzle.HasKey);

        // 2. Aterriza a salvo en columna 17 sin caer en la trampa
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(9 * GameConstants.TILE_SIZE, player.Position.Y);
        Assert.True(player.Position.X >= 17 * GameConstants.TILE_SIZE);
        Assert.False(puzzle.IsTrapOpen, "La trampa debió permanecer cerrada tras saltar sobre ella");
        Assert.Equal(3, player.Lives);
    }

    [Fact]
    public void Treasure3_InitiallyNotSpawned_MatchesChamberTreasureLocation()
    {
        var (grid, puzzle) = CreateTestSetup();
        Assert.False(puzzle.IsTreasure3Spawned);
        Assert.Equal(new GridCoord(17, 13), puzzle.Treasure3Coord);
        Assert.Equal(puzzle.TreasureCoord, puzzle.Treasure3Coord);
    }

    [Fact]
    public void SpawnTreasure3_SetsSpawned_OpensWallAndShowsNotification()
    {
        var (grid, puzzle) = CreateTestSetup();
        Assert.False(puzzle.IsWallOpen);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.WallCoord));

        puzzle.SpawnTreasure3(grid);

        Assert.True(puzzle.IsTreasure3Spawned);
        Assert.False(puzzle.Treasure3.IsCollected);
        Assert.True(puzzle.IsWallOpen, "El muro de la cámara debe abrirse al caer la momia y petrificarse");
        Assert.Equal(TileType.Empty, grid.GetTile(puzzle.WallCoord));
        Assert.Equal(PuzzleManager.MSG_TREASURE3_SPAWN, puzzle.NotificationMessage);
        Assert.Equal(180, puzzle.NotificationTimer);
    }

    [Fact]
    public void CollectTreasure3_InSameChamberLocation_AwardsPointsSequentially()
    {
        var (grid, puzzle) = CreateTestSetup();

        // 1. Recoger tesoro original en (17, 13)
        var player = new Player(puzzle.TreasureCoord);
        puzzle.Update(grid, player);

        Assert.True(puzzle.Treasure.IsCollected);
        Assert.Equal(1000, puzzle.Score);

        // 2. Al caer la momia al Nivel 0, se genera un nuevo tesoro en el mismo lugar de la cámara
        puzzle.SpawnTreasure3(grid);
        Assert.True(puzzle.IsTreasure3Spawned);
        Assert.False(puzzle.Treasure3.IsCollected);
        Assert.Equal(PuzzleManager.MSG_TREASURE3_SPAWN, puzzle.NotificationMessage);

        // 3. Recoger el nuevo cofre creado en (17, 13)
        puzzle.Update(grid, player);

        Assert.True(puzzle.Treasure3.IsCollected);
        Assert.Equal(2000, puzzle.Score);
        Assert.Equal(PuzzleManager.MSG_TREASURE3, puzzle.NotificationMessage);
        Assert.True(puzzle.NotificationTimer > 0);
    }

    [Fact]
    public void NotifyMummyChase_SetsMessageAndTimer()
    {
        var (grid, puzzle) = CreateTestSetup();

        puzzle.NotifyMummyChase();

        Assert.Equal(PuzzleManager.MSG_MUMMY_CHASE, puzzle.NotificationMessage);
        Assert.Equal(120, puzzle.NotificationTimer);
    }

    [Fact]
    public void Initialize_ResetsTreasure3State()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(puzzle.TreasureCoord);
        puzzle.Update(grid, player);
        puzzle.SpawnTreasure3(grid);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Treasure3.IsCollected);

        puzzle.Initialize(grid, 1);

        Assert.False(puzzle.IsTreasure3Spawned);
        Assert.False(puzzle.Treasure3.IsCollected);
    }

    [Fact]
    public void Initialize_Chamber1_EnablesTrap2_AtRow5Col13()
    {
        var (grid, puzzle) = CreateTestSetup();

        Assert.True(puzzle.HasTrap2);
        Assert.Equal(new GridCoord(13, 5), puzzle.Trap2Coord);
        Assert.False(puzzle.IsTrap2Open);
        Assert.False(puzzle.IsTrap2Triggered);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.Trap2Coord));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    [InlineData(6, 2)]
    [InlineData(7, 2)]
    [InlineData(8, 3)]
    [InlineData(9, 3)]
    [InlineData(10, 4)]
    public void Initialize_All10Chambers_EnableTrap2_AndConfigureExitKeyAndTreasures(int chamberNumber, int expectedPyramidLevel)
    {
        var grid = new RoomGrid();
        grid.LoadChamber(chamberNumber);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, chamberNumber);

        Assert.Equal(chamberNumber, puzzle.CurrentChamber);
        Assert.Equal(expectedPyramidLevel, puzzle.PyramidLevel);
        Assert.True(puzzle.HasTrap2);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.TrapCoord));
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.Trap2Coord));
        Assert.Equal(TileType.ExitDoor, grid.GetTile(puzzle.ExitDoorCoord));
        Assert.False(puzzle.Key.IsCollected);
    }

    [Fact]
    public void Chamber1_JumpingOverColumn13_OnPlatform2_OpensTrap_AndPlayerSurvives()
    {
        var (grid, puzzle) = CreateTestSetup();

        // Colocar al jugador en col 12 en plataforma 2 (fila 4, Y = 64)
        var player = new Player(12 * GameConstants.TILE_SIZE, 4 * GameConstants.TILE_SIZE);
        Assert.Equal(3, player.Lives);

        // Iniciar salto hacia la derecha sobrevolando la columna 13
        var jumpInput = new RetroGamePiramid.Input.PlayerInput(left: false, right: true, up: false, down: false, jump: true);
        player.Update(grid, in jumpInput);
        puzzle.Update(grid, player);

        Assert.Equal(PlayerState.Jumping, player.State);

        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        for (int frame = 0; frame < Player.JUMP_DURATION_FRAMES; frame++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
        }

        // 1. Al pasar saltando por arriba, la trampa se abrió
        Assert.True(puzzle.IsTrap2Open, "La trampa 2 debió abrirse al pasar el jugador saltando por arriba");
        Assert.True(puzzle.Trap2OpenTimer > 0);
        Assert.Equal(TileType.Empty, grid.GetTile(puzzle.Trap2Coord));

        // 2. Aterriza a salvo en columna 14 sin caer al vacío
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(4 * GameConstants.TILE_SIZE, player.Position.Y);
        Assert.True(player.Position.X >= 14 * GameConstants.TILE_SIZE);
        Assert.Equal(3, player.Lives);
        Assert.False(player.IsEliminated);
    }

    [Fact]
    public void Chamber1_Trap2_TimerClosesAutomaticallyAfter120Frames()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(12 * GameConstants.TILE_SIZE, 4 * GameConstants.TILE_SIZE);

        // Abrir la trampa saltando sobre ella
        var jumpInput = new RetroGamePiramid.Input.PlayerInput(left: false, right: true, up: false, down: false, jump: true);
        player.Update(grid, in jumpInput);
        puzzle.Update(grid, player);

        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        for (int frame = 0; frame < Player.JUMP_DURATION_FRAMES; frame++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
        }

        Assert.True(puzzle.IsTrap2Open);

        // Avanzar los frames restantes del temporizador (120 frames totales)
        while (puzzle.Trap2OpenTimer > 0)
        {
            puzzle.Update(grid, player);
        }

        // La trampa debe haberse cerrado automáticamente
        Assert.False(puzzle.IsTrap2Open, "La trampa debió cerrarse tras expirar el temporizador");
        Assert.Equal(0, puzzle.Trap2OpenTimer);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.Trap2Coord));
    }

    [Fact]
    public void Chamber1_WalkingOnColumn13_OnPlatform2_TriggersTrap_ForcesFall_ClosesBehindAndEliminatesPlayer()
    {
        var (grid, puzzle) = CreateTestSetup();

        // Colocar al jugador caminando sobre la baldosa de columna 13 en plataforma 2
        var player = new Player(13 * GameConstants.TILE_SIZE + 1f, 4 * GameConstants.TILE_SIZE);
        var walkRight = new RetroGamePiramid.Input.PlayerInput(left: false, right: true, up: false, down: false, jump: false);

        player.Update(grid, in walkRight);
        puzzle.Update(grid, player);

        // 1. La trampa se abre y fuerza al jugador a caer
        Assert.True(puzzle.Trap2.IsOpen, "La trampa debió abrirse al pisarla caminando");
        Assert.True(puzzle.IsTrap2Triggered);
        Assert.Equal(TileType.Empty, grid.GetTile(puzzle.Trap2Coord));
        Assert.Equal(PlayerState.Falling, player.State);
        Assert.Equal(PuzzleManager.MSG_TRAP2, puzzle.NotificationMessage);
        Assert.True(puzzle.NotificationTimer > 0);

        // 2. Simular frames de caída libre
        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        int maxFrames = 100;
        int frame = 0;
        while (player.State == PlayerState.Falling && frame < maxFrames)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
            frame++;

            // Cuando desciende por debajo de la trampa (Y >= 96), la trampa se cierra a sus espaldas
            if (player.Position.Y >= ((puzzle.Trap2Coord.Y + 1) * GameConstants.TILE_SIZE))
            {
                Assert.False(puzzle.Trap2.IsOpen, "La trampa debió cerrarse tras franquearla el jugador");
                Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.Trap2Coord));
            }
        }

        // 3. Al impactar en el suelo de Nivel 0, el jugador queda eliminado y pierde 1 vida
        Assert.True(player.IsEliminated, "El jugador debió ser eliminado por caída fatal desde Nivel 2 a Nivel 0");
        Assert.Equal(2, player.Lives); // Pierde 1 vida
        Assert.Equal(PuzzleManager.MSG_TRAP2, puzzle.NotificationMessage);
        Assert.True(puzzle.NotificationTimer > 0);
    }

    [Fact]
    public void RestartChamber1_AfterTrap2Death_ResetsTrap2State()
    {
        var (grid, puzzle) = CreateTestSetup();

        var player = new Player(13 * GameConstants.TILE_SIZE + 1f, 4 * GameConstants.TILE_SIZE);
        var walkRight = new RetroGamePiramid.Input.PlayerInput(left: false, right: true, up: false, down: false, jump: false);
        player.Update(grid, in walkRight);
        puzzle.Update(grid, player);

        Assert.True(puzzle.IsTrap2Triggered);

        // Reinicio de recámara
        puzzle.Initialize(grid, 1);

        Assert.False(puzzle.IsTrap2Open);
        Assert.False(puzzle.IsTrap2Triggered);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.Trap2Coord));
    }

    [Fact]
    public void Chamber1_MummyFallsInTrap_RespawnsTwice_AndIsEliminatedThirdTime_SpawningTreasure4()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(14 * GameConstants.TILE_SIZE, 13 * GameConstants.TILE_SIZE);

        // 1. Recoger previamente el tesoro de la cámara
        puzzle.Update(grid, new Player(puzzle.TreasureCoord));
        Assert.True(puzzle.Treasure.IsCollected);

        var mummy = new Mummy(
            spawnX: 15 * GameConstants.TILE_SIZE,
            spawnY: 4 * GameConstants.TILE_SIZE,
            minX: 2 * GameConstants.TILE_SIZE,
            maxX: 16 * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Left);

        // --- CAÍDA 1 ---
        // Abrir la trampa
        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);

        // La momia pisa columna 13
        mummy.SetPosition(13 * GameConstants.TILE_SIZE + 1f, 4 * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummy);
        Assert.True(mummy.IsFalling);
        Assert.True(puzzle.IsMummy2FallingInTrap);

        // Simular caída hasta el Nivel 0
        mummy.SetPosition(13 * GameConstants.TILE_SIZE + 1f, 13 * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummy);

        Assert.Equal(1, puzzle.Mummy2TrapFallCount);
        Assert.True(mummy.IsActive);
        Assert.False(mummy.IsFalling);
        Assert.Equal(mummy.SpawnPosition, mummy.Position);
        Assert.Equal(PuzzleManager.MSG_MUMMY_TRAPPED_1, puzzle.NotificationMessage);

        // --- CAÍDA 2 ---
        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);

        mummy.SetPosition(13 * GameConstants.TILE_SIZE + 1f, 4 * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummy);
        Assert.True(mummy.IsFalling);

        mummy.SetPosition(13 * GameConstants.TILE_SIZE + 1f, 13 * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummy);

        Assert.Equal(2, puzzle.Mummy2TrapFallCount);
        Assert.True(mummy.IsActive);
        Assert.False(mummy.IsFalling);
        Assert.Equal(mummy.SpawnPosition, mummy.Position);
        Assert.Equal(PuzzleManager.MSG_MUMMY_TRAPPED_2, puzzle.NotificationMessage);

        // --- CAÍDA 3 ---
        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);

        mummy.SetPosition(13 * GameConstants.TILE_SIZE + 1f, 4 * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummy);
        Assert.True(mummy.IsFalling);

        mummy.SetPosition(13 * GameConstants.TILE_SIZE + 1f, 13 * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummy);

        Assert.Equal(3, puzzle.Mummy2TrapFallCount);
        Assert.False(mummy.IsActive, "La momia debió ser eliminada definitivamente tras la 3.ª caída");
        Assert.Equal(PuzzleManager.MSG_MUMMY_DEFEATED, puzzle.NotificationMessage);
        Assert.True(puzzle.IsTreasure4Spawned, "Como el tesoro ya estaba cogido, se crea de inmediato Treasure4");
        Assert.False(puzzle.IsTreasure4Pending);
        Assert.True(puzzle.IsWallOpen);

        // --- RECOLECCIÓN DE TREASURE 4 ---
        int prevScore = puzzle.Score;
        var playerAtTreasure4 = new Player(puzzle.Treasure4Coord);
        puzzle.Update(grid, playerAtTreasure4, mummy);

        Assert.True(puzzle.Treasure4.IsCollected);
        Assert.Equal(prevScore + 1000, puzzle.Score);
        Assert.Equal(PuzzleManager.MSG_TREASURE4, puzzle.NotificationMessage);
    }

    [Fact]
    public void Chamber1_MummyFall3_WhenChamberTreasureNotCollected_SetsTreasure4Pending_SpawnsOnCollection()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(14 * GameConstants.TILE_SIZE, 13 * GameConstants.TILE_SIZE);

        Assert.False(puzzle.Treasure.IsCollected, "El tesoro de la cámara aún no ha sido recogido");

        var mummy = new Mummy(
            spawnX: 15 * GameConstants.TILE_SIZE,
            spawnY: 4 * GameConstants.TILE_SIZE,
            minX: 2 * GameConstants.TILE_SIZE,
            maxX: 16 * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Left);

        // Ejecutar 3 caídas de la momia
        for (int i = 0; i < 3; i++)
        {
            puzzle.Trap2.Open();
            grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);
            mummy.SetPosition(13 * GameConstants.TILE_SIZE + 1f, 4 * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player, mummy);

            mummy.SetPosition(13 * GameConstants.TILE_SIZE + 1f, 13 * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player, mummy);
        }

        Assert.Equal(3, puzzle.Mummy2TrapFallCount);
        Assert.False(mummy.IsActive);
        Assert.True(puzzle.IsTreasure4Pending, "Treasure4 debe quedar en estado pendiente");
        Assert.False(puzzle.IsTreasure4Spawned);

        // Ahora el jugador recolecta el cofre original en (17, 13)
        var playerAtChamber = new Player(puzzle.TreasureCoord);
        puzzle.Update(grid, playerAtChamber, mummy);

        Assert.True(puzzle.Treasure.IsCollected);
        Assert.False(puzzle.IsTreasure4Pending, "Tras recolectar el cofre actual, se resuelve el estado pendiente");
        Assert.True(puzzle.IsTreasure4Spawned, "Treasure4 ahora está materializado en la cámara");
        Assert.False(puzzle.Treasure4.IsCollected);

        // En la siguiente actualización, el jugador recolecta el nuevo cofre
        puzzle.Update(grid, playerAtChamber, mummy);
        Assert.True(puzzle.Treasure4.IsCollected);
        Assert.Equal(PuzzleManager.MSG_TREASURE4, puzzle.NotificationMessage);
    }

    [Fact]
    public void Chamber1_FallingThroughTrap2_SteeringOntoLevel1_EliminatesPlayerAndLosesLife()
    {
        var (grid, puzzle) = CreateTestSetup();

        // Colocar al jugador en plataforma 2 sobre la trampa (columna 13)
        var player = new Player(13 * GameConstants.TILE_SIZE + 1f, 4 * GameConstants.TILE_SIZE);
        var walkRight = new RetroGamePiramid.Input.PlayerInput(left: false, right: true, up: false, down: false, jump: false);

        player.Update(grid, in walkRight);
        puzzle.Update(grid, player);

        // La trampa se abre y el jugador entra en caída
        Assert.True(puzzle.Trap2.IsOpen);
        Assert.Equal(PlayerState.Falling, player.State);
        Assert.True(puzzle.IsPlayerTrappedInTrap2);

        // Mientras cae, el jugador maniobra hacia la izquierda (retro drift) aterrizando en columna 12 (Nivel 1, Y = 128)
        var driftLeft = new RetroGamePiramid.Input.PlayerInput(left: true, right: false, up: false, down: false, jump: false);
        int frame = 0;
        while (player.State == PlayerState.Falling && frame < 50)
        {
            player.Update(grid, in driftLeft);
            puzzle.Update(grid, player);
            frame++;
        }

        // Al completar la caída por la trampa y aterrizar en Plataforma 1 (fila 9, Y = 128), el jugador pierde una vida
        Assert.Equal(128f, player.Position.Y);
        Assert.True(player.IsEliminated, "El jugador que cae por la trampa del Nivel 2 debe ser eliminado y perder una vida");
        Assert.Equal(2, player.Lives); // Pierde 1 vida
        Assert.True(puzzle.HasTrap2Eliminated);
        Assert.Equal(PuzzleManager.MSG_TRAP2, puzzle.NotificationMessage);
    }

    [Fact]
    public void Chamber1_FallFromLevel2_ViaColumn16Gap_LandingOnLevel1_ThenDescendingToLevel0_DoesNotLoseLife()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(15 * GameConstants.TILE_SIZE, 4 * GameConstants.TILE_SIZE);
        Assert.Equal(Player.INITIAL_LIVES, player.Lives);

        // 1. Caminar a la derecha desde Nivel 2 (col 15) hacia el espacio vacío de col 16 y caer al Nivel 1
        var rightInput = new RetroGamePiramid.Input.PlayerInput(left: false, right: true, up: false, down: false, jump: false);
        for (int i = 0; i < 10; i++)
        {
            player.Update(grid, in rightInput);
            puzzle.Update(grid, player);
        }

        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        for (int i = 0; i < 50; i++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
        }

        // Debe haber aterrizado en Nivel 1 (Y = 128) sin ser eliminado ni quedar marcado como atrapado
        Assert.Equal(128f, player.Position.Y);
        Assert.False(player.IsEliminated);
        Assert.False(puzzle.IsPlayerTrappedInTrap);
        Assert.Equal(Player.INITIAL_LIVES, player.Lives);

        // 2. Moverse por el Nivel 1 hacia la derecha y bajar al Nivel 0 por el hueco de columna 18
        for (int i = 0; i < 25; i++)
        {
            player.Update(grid, in rightInput);
            puzzle.Update(grid, player);
        }

        for (int i = 0; i < 60; i++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
        }

        // Aterriza en Nivel 0 (Y = 208) conservando todas sus vidas
        Assert.Equal(208f, player.Position.Y);
        Assert.False(player.IsEliminated, "Si el jugador pisa el Nivel 1 y luego baja al Nivel 0, no debe perder una vida");
        Assert.Equal(Player.INITIAL_LIVES, player.Lives);
        Assert.Equal(PlayerState.Idle, player.State);
    }

    [Fact]
    public void Chamber1_FallFromLevel2_StraightDownColumn16_WalkOnLevel1_ThenClimbOrDropToLevel0_DoesNotLoseLife()
    {
        var (grid, puzzle) = CreateTestSetup();
        var player = new Player(15 * GameConstants.TILE_SIZE + 12f, 4 * GameConstants.TILE_SIZE);
        Assert.Equal(Player.INITIAL_LIVES, player.Lives);

        // 1. Dar 2 pasos a la derecha para caer verticalmente por la columna 16 (X = 256) hacia Nivel 1
        var rightInput = new RetroGamePiramid.Input.PlayerInput(left: false, right: true, up: false, down: false, jump: false);
        var leftInput = new RetroGamePiramid.Input.PlayerInput(left: true, right: false, up: false, down: false, jump: false);
        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);

        while (player.State != PlayerState.Falling)
        {
            player.Update(grid, in rightInput);
            puzzle.Update(grid, player);
        }

        for (int i = 0; i < 50; i++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
        }

        // Aterriza sobre Nivel 1 (Y = 128) en columna 16 sin que se active letalmente la trampa de Nivel 1
        Assert.Equal(128f, player.Position.Y);
        Assert.False(player.IsEliminated);
        Assert.Equal(Player.INITIAL_LIVES, player.Lives);

        // 2. Caminar a la izquierda por Nivel 1 (hacia col 14) y bajar al Nivel 0 por el hueco de col 13
        for (int i = 0; i < 40; i++)
        {
            player.Update(grid, in leftInput);
            puzzle.Update(grid, player);
        }

        for (int i = 0; i < 60; i++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
        }

        // Toca el Nivel 0 (Y = 208) sin perder ninguna vida
        Assert.Equal(208f, player.Position.Y);
        Assert.False(player.IsEliminated, "Bajar un nivel a la vez (Nivel 2 -> Nivel 1 -> Nivel 0) jamás debe costar una vida");
        Assert.Equal(Player.INITIAL_LIVES, player.Lives);
    }

    [Fact]
    public void Chamber2_Puzzle3_RequiresPuzzles1And2_ThenJumpOverLevel2MummyAndTrapFall_Repeated2Times()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 2);

        var player = new Player(puzzle.StoneCoord);
        var mummyLevel2 = new Mummy(
            spawnX: 9f * GameConstants.TILE_SIZE,
            spawnY: 5f * GameConstants.TILE_SIZE,
            minX: 4f * GameConstants.TILE_SIZE,
            maxX: 16f * GameConstants.TILE_SIZE,
            initialFacing: Direction.Right,
            initiallyAwake: true,
            initiallyActive: true);

        // Si la momia de Nivel 2 cae en la trampa SIN haber completado los puzles 1 y 2, no cuenta
        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 5f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        Assert.True(puzzle.IsMummy2FallingInTrap);

        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 13f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        Assert.Equal(0, puzzle.Mummy2TrapFallCount);

        // 1. Completar Puzle 1 de Recámara 2 (Tesoro Nivel 0 -> Llave -> Salto Momia Nivel 1 -> 2 ciclos de puerta)
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        puzzle.RegisterLevel1MummyJump();

        player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        for (int i = 0; i < 4; i++)
        {
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player, mummyLevel2);
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player, mummyLevel2);
        }
        Assert.True(puzzle.HasSecretTreasureReloaded);

        // Recoger el tesoro recargado del Puzle 1
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        Assert.True(puzzle.Treasure.IsCollected);

        // 2. Completar Puzle 2 de Recámara 2 y recoger Treasure3
        puzzle.SpawnTreasure3(grid);
        Assert.True(puzzle.IsTreasure3Spawned);
        player.SetPosition(puzzle.Treasure3Coord.X * GameConstants.TILE_SIZE + 2f, puzzle.Treasure3Coord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        Assert.True(puzzle.Treasure3.IsCollected);

        // Si cae en la trampa sin haber saltado antes sobre la momia de Nivel 2, no cuenta
        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 5f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 13f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        Assert.Equal(0, puzzle.Mummy2TrapFallCount);

        // 3. Repetición 1: Saltar 1 vez sobre la momia del Nivel 2 y hacerla caer por la trampa
        puzzle.RegisterLevel2MummyJump();
        Assert.True(puzzle.HasJumpedOverLevel2Mummy);

        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 5f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 13f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);

        Assert.Equal(1, puzzle.Mummy2TrapFallCount);
        Assert.False(puzzle.HasJumpedOverLevel2Mummy);
        Assert.True(mummyLevel2.IsActive);
        Assert.False(puzzle.IsTreasure4Spawned);

        // 4. Repetición 2: Saltar 1 vez sobre la momia del Nivel 2 y hacerla caer por la trampa
        puzzle.RegisterLevel2MummyJump();
        Assert.True(puzzle.HasJumpedOverLevel2Mummy);

        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 5f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 13f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);

        Assert.Equal(2, puzzle.Mummy2TrapFallCount);
        Assert.False(mummyLevel2.IsActive);
        Assert.True(puzzle.IsTreasure4Spawned);
    }

    [Fact]
    public void Chamber3_Puzzle1_RequiresKeyFirst_ThenBothChests_Then4DoorCycles()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(3);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 3);
        var player = new Player(puzzle.StoneCoord);

        // 1. Recoger primero la llave (antes de ambos cofres)
        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Chamber3KeyCollectedFirst);

        // 2. Recoger ambos cofres
        player.SetPosition(puzzle.Treasure2Coord.X * GameConstants.TILE_SIZE + 2f, puzzle.Treasure2Coord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player); // Abre puerta
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.True(puzzle.Treasure2.IsCollected);

        // 3. Cerrar la puerta que estaba abierta (ciclo 1) y hacer 3 ciclos más = 4 ciclos
        player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        for (int i = 0; i < 7; i++)
        {
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
        }

        Assert.True(puzzle.HasSecretTreasureReloaded);
        Assert.False(puzzle.Treasure.IsCollected);
    }

    [Fact]
    public void Chamber3_Puzzle2_Requires1JumpOnLevel1_ThenFall_Then3JumpsOnLevel0()
    {
        var mummy = new Mummy(
            spawnX: 2f * GameConstants.TILE_SIZE,
            spawnY: 8f * GameConstants.TILE_SIZE,
            minX: 2f * GameConstants.TILE_SIZE,
            maxX: 11f * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right,
            initiallyAwake: true,
            initiallyActive: true,
            requiredJumpOvers: 1,
            requiredLevel0JumpOvers: 3);

        Assert.True(mummy.RegisterJumpOver());
        Assert.True(mummy.IsChasing);

        mummy.LandOnLevel0();
        Assert.True(mummy.HasReachedLevel0);
        Assert.False(mummy.IsPuzzleCompleted);

        mummy.RegisterJumpOver();
        mummy.RegisterJumpOver();
        Assert.False(mummy.IsPuzzleCompleted);

        mummy.RegisterJumpOver();
        Assert.True(mummy.IsPuzzleCompleted);
    }

    [Fact]
    public void Chamber3_Puzzle3_RequiresTreasure2_Then3JumpsOverLevel2Mummy_Then1TrapFall()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(3);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 3);
        var player = new Player(puzzle.StoneCoord);
        var mummyLevel2 = new Mummy(
            spawnX: 14f * GameConstants.TILE_SIZE,
            spawnY: 4f * GameConstants.TILE_SIZE,
            minX: 2f * GameConstants.TILE_SIZE,
            maxX: 15f * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right,
            initiallyAwake: true,
            initiallyActive: true);

        // Recoger Treasure (Nivel 0) y Treasure2 (Nivel 2)
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        player.SetPosition(puzzle.Treasure2Coord.X * GameConstants.TILE_SIZE + 2f, puzzle.Treasure2Coord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        Assert.True(puzzle.Treasure2.IsCollected);

        // Saltar 3 veces sobre la momia de Nivel 2
        puzzle.RegisterLevel2MummyJump();
        puzzle.RegisterLevel2MummyJump();
        puzzle.RegisterLevel2MummyJump();
        Assert.Equal(3, puzzle.Level2MummyJumpCount);
        Assert.True(puzzle.HasJumpedOverLevel2Mummy);

        // Hacerla caer 1 sola vez por la trampa de Nivel 2
        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 4f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 13f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);

        Assert.Equal(1, puzzle.Mummy2TrapFallCount);
        Assert.False(mummyLevel2.IsActive);
        Assert.True(puzzle.IsTreasure4Spawned);
    }

    [Fact]
    public void Chamber4_Puzzle1_RequiresBothChestsKeyAnd2MummyJumps_Then1DoorCycle()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(4);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 4);
        var player = new Player(puzzle.StoneCoord);

        puzzle.Update(grid, player); // Abre puerta
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.Treasure2Coord.X * GameConstants.TILE_SIZE + 2f, puzzle.Treasure2Coord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);

        puzzle.RegisterLevel1MummyJump();
        puzzle.RegisterLevel1MummyJump();
        Assert.Equal(2, puzzle.Chamber4MummyJumpCount);

        // Cerrar la puerta (1 ciclo) recarga el tesoro secreto
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);

        Assert.True(puzzle.HasSecretTreasureReloaded);
        Assert.False(puzzle.Treasure.IsCollected);
    }

    [Fact]
    public void Chamber4_Puzzle2_Requires3JumpsOnLevel1_ThenFall_Then1JumpOnLevel0()
    {
        var mummy = new Mummy(
            spawnX: 2f * GameConstants.TILE_SIZE,
            spawnY: 9f * GameConstants.TILE_SIZE,
            minX: 2f * GameConstants.TILE_SIZE,
            maxX: 11f * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right,
            initiallyAwake: true,
            initiallyActive: true,
            requiredJumpOvers: 3,
            requiredLevel0JumpOvers: 1);

        Assert.False(mummy.RegisterJumpOver());
        Assert.False(mummy.RegisterJumpOver());
        Assert.True(mummy.RegisterJumpOver());
        Assert.True(mummy.IsChasing);

        mummy.LandOnLevel0();
        Assert.True(mummy.HasReachedLevel0);
        Assert.False(mummy.IsPuzzleCompleted);

        mummy.RegisterJumpOver();
        Assert.True(mummy.IsPuzzleCompleted);
    }

    [Fact]
    public void Chamber4_Puzzle3_RequiresKey_Then2JumpsOverLevel2MummyAndTrapFall_Repeated2Times()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(4);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 4);
        var player = new Player(puzzle.StoneCoord);
        var mummyLevel2 = new Mummy(
            spawnX: 15f * GameConstants.TILE_SIZE,
            spawnY: 5f * GameConstants.TILE_SIZE,
            minX: 4f * GameConstants.TILE_SIZE,
            maxX: 18f * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right,
            initiallyAwake: true,
            initiallyActive: true);

        // Recoger Treasure y Llave
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        Assert.True(puzzle.HasKey);

        // Repetición 1: 2 saltos sobre la momia de Nivel 2 + 1 caída en trampa
        puzzle.RegisterLevel2MummyJump();
        puzzle.RegisterLevel2MummyJump();
        Assert.Equal(2, puzzle.Level2MummyJumpCount);

        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 5f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 13f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);

        Assert.Equal(1, puzzle.Mummy2TrapFallCount);
        Assert.True(mummyLevel2.IsActive);

        // Repetición 2: 2 saltos sobre la momia de Nivel 2 + 2da caída en trampa
        puzzle.RegisterLevel2MummyJump();
        puzzle.RegisterLevel2MummyJump();
        puzzle.Trap2.Open();
        grid.SetTile(puzzle.Trap2Coord.X, puzzle.Trap2Coord.Y, TileType.Empty);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 5f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);
        mummyLevel2.SetPosition(puzzle.Trap2Coord.X * GameConstants.TILE_SIZE + 2f, 13f * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player, mummyLevel2);

        Assert.Equal(2, puzzle.Mummy2TrapFallCount);
        Assert.False(mummyLevel2.IsActive);
        Assert.True(puzzle.IsTreasure4Spawned);
    }
}
