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
    public void Chamber2_DoorCycles_DoNotTriggerChamber1SecretPuzzle()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 2);

        var player = new Player(puzzle.StoneCoord);

        // Abrir puerta y recoger los 3 ítems en Recámara 2
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.KeyCoord.X * GameConstants.TILE_SIZE + 2f, puzzle.KeyCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        player.SetPosition(puzzle.Treasure2Coord.X * GameConstants.TILE_SIZE + 2f, puzzle.Treasure2Coord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);

        Assert.True(puzzle.Treasure.IsCollected);
        Assert.True(puzzle.Key.IsCollected);
        Assert.True(puzzle.Treasure2.IsCollected);

        // Realizar ciclos de abrir y cerrar
        for (int i = 0; i < 6; i++)
        {
            player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE + 1f, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
            player.SetPosition(10 * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
            puzzle.Update(grid, player);
        }

        // En Recámara 2 no se recarga
        Assert.True(puzzle.Treasure.IsCollected);
        Assert.False(puzzle.HasSecretTreasureReloaded);
        Assert.NotEqual(PuzzleManager.MSG_TREASURE_RELOAD, puzzle.NotificationMessage);
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

    [Fact]
    public void Initialize_Chamber2_DisablesTrap2()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);
        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid, 2);

        Assert.False(puzzle.HasTrap2);
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
            minX: 4 * GameConstants.TILE_SIZE,
            maxX: 18 * GameConstants.TILE_SIZE - Mummy.WIDTH,
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
            minX: 4 * GameConstants.TILE_SIZE,
            maxX: 18 * GameConstants.TILE_SIZE - Mummy.WIDTH,
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
    public void Chamber1_FallingThroughTrap2_SteeringOntoLevel1_DoesNotEliminatePlayer_WhenReachingLevel0()
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

        // El jugador ha aterrizado con éxito en Plataforma 1 (fila 9, Y = 128)
        Assert.Equal(128f, player.Position.Y);
        Assert.False(player.IsEliminated);
        Assert.Equal(3, player.Lives);
        Assert.False(puzzle.IsPlayerTrappedInTrap2, "La bandera de trampa debe reiniciarse porque el jugador aterrizó y pisó el Nivel 1");

        // Ahora el jugador continúa su camino y desciende al Nivel 0 (caminando al hueco col 11 para caer a Nivel 0)
        var walkLeft = new RetroGamePiramid.Input.PlayerInput(left: true, right: false, up: false, down: false, jump: false);
        for (int i = 0; i < 20; i++)
        {
            player.Update(grid, in walkLeft);
            puzzle.Update(grid, player);
        }

        var neutral = new RetroGamePiramid.Input.PlayerInput(false, false, false, false, false);
        for (int i = 0; i < 60; i++)
        {
            player.Update(grid, in neutral);
            puzzle.Update(grid, player);
        }

        // El jugador llega al Nivel 0 (Y = 208): NO debe perder vida ni ser eliminado
        Assert.Equal(208f, player.Position.Y);
        Assert.False(player.IsEliminated, "El jugador no debe ser eliminado al llegar a Nivel 0 tras haber pisado Nivel 1");
        Assert.Equal(3, player.Lives);
        Assert.False(puzzle.HasTrap2Eliminated);
    }
}
