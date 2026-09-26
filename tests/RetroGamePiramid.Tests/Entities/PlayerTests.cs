using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;
using RetroGamePiramid.Grid;
using RetroGamePiramid.Input;
using Xunit;

namespace RetroGamePiramid.Tests.Entities;

public class PlayerTests
{
    private RoomGrid CreateTestRoom()
    {
        var grid = new RoomGrid();
        grid.LoadDefaultRoom();
        return grid;
    }

    [Fact]
    public void InitialState_DefaultsToExpectedValues()
    {
        var player = new Player(new GridCoord(2, 13));

        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(Direction.Right, player.Facing);
        Assert.True(player.Position.X > 0);
        Assert.True(player.Position.Y > 0);
    }

    [Fact]
    public void MoveRight_UpdatesPositionAndState()
    {
        var grid = CreateTestRoom();
        var player = new Player(new GridCoord(2, 13));
        float startX = player.Position.X;

        var input = new PlayerInput(left: false, right: true, up: false, down: false, jump: false);
        player.Update(grid, in input);

        Assert.Equal(PlayerState.Walking, player.State);
        Assert.Equal(Direction.Right, player.Facing);
        Assert.Equal(startX + Player.WALK_SPEED, player.Position.X);
    }

    [Fact]
    public void MoveLeft_UpdatesPositionAndFacing()
    {
        var grid = CreateTestRoom();
        var player = new Player(new GridCoord(4, 13));
        float startX = player.Position.X;

        var input = new PlayerInput(left: true, right: false, up: false, down: false, jump: false);
        player.Update(grid, in input);

        Assert.Equal(PlayerState.Walking, player.State);
        Assert.Equal(Direction.Left, player.Facing);
        Assert.Equal(startX - Player.WALK_SPEED, player.Position.X);
    }

    [Fact]
    public void MoveLeft_IntoSolidWall_BlocksMovement()
    {
        var grid = CreateTestRoom();
        // Columna 0 es muro sólido (SolidWall en x=0..15)
        // Posicionamos al jugador en x=16 (borde derecho de la columna 0)
        var player = new Player(16f, 13 * GameConstants.TILE_SIZE);

        var input = new PlayerInput(left: true, right: false, up: false, down: false, jump: false);
        player.Update(grid, in input);

        // No debe penetrar el muro en x < 16
        Assert.True(player.Position.X >= 16f);
    }

    [Fact]
    public void NeutralInput_TransitionsToIdle()
    {
        var grid = CreateTestRoom();
        var player = new Player(new GridCoord(2, 13));

        // Primero camina
        player.Update(grid, new PlayerInput(left: false, right: true, up: false, down: false, jump: false));
        Assert.Equal(PlayerState.Walking, player.State);

        // Luego suelta controles
        player.Update(grid, new PlayerInput(left: false, right: false, up: false, down: false, jump: false));
        Assert.Equal(PlayerState.Idle, player.State);
    }

    [Fact]
    public void Jump_WhileGrounded_InitiatesJumpingState()
    {
        var grid = CreateTestRoom();
        var player = new Player(new GridCoord(2, 13));
        float startY = player.Position.Y;

        var jumpInput = new PlayerInput(left: false, right: false, up: false, down: false, jump: true);
        player.Update(grid, in jumpInput);

        Assert.Equal(PlayerState.Jumping, player.State);

        // En los siguientes frames sin input, el salto avanza en el arco parabólico ascendente
        var neutral = new PlayerInput(left: false, right: false, up: false, down: false, jump: false);
        player.Update(grid, in neutral);

        Assert.Equal(PlayerState.Jumping, player.State);
        Assert.True(player.Position.Y < startY); // El jugador se eleva
    }

    [Fact]
    public void Jump_FullArc_LandsBackOnGround()
    {
        var grid = CreateTestRoom();
        var player = new Player(new GridCoord(2, 13));
        float initialY = player.Position.Y;

        // Inicia el salto
        player.Update(grid, new PlayerInput(left: false, right: false, up: false, down: false, jump: true));

        // Ejecutar los frames del salto
        var neutral = new PlayerInput(false, false, false, false, false);
        for (int i = 0; i < Player.JUMP_DURATION_FRAMES; i++)
        {
            player.Update(grid, in neutral);
        }

        // Al finalizar el arco sobre suelo firme, debe volver a Idle
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(initialY, player.Position.Y, precision: 1);
    }

    [Fact]
    public void WalkOffLedge_EntersFallingState()
    {
        var grid = CreateTestRoom();
        // Plataforma intermedia: fila 9, columnas 2 a 10.
        // Colocamos al jugador en el extremo derecho de la plataforma (columna 10)
        var player = new Player(10 * GameConstants.TILE_SIZE + 8f, 8 * GameConstants.TILE_SIZE);

        // Caminar a la derecha hacia fuera de la plataforma (columna 11 está vacía en fila 9)
        var rightInput = new PlayerInput(left: false, right: true, up: false, down: false, jump: false);
        for (int i = 0; i < 15; i++)
        {
            player.Update(grid, in rightInput);
        }

        // El jugador debe entrar en estado Falling
        Assert.True(player.State == PlayerState.Falling || player.State == PlayerState.Jumping);
    }

    [Fact]
    public void Ladder_UpInput_EntersClimbingState()
    {
        var grid = CreateTestRoom();
        // Escalera 1 está en la columna 5, filas 9 a 13.
        // Posicionamos al jugador en la base de la escalera (col 5, fila 13)
        var player = new Player(new GridCoord(5, 13));
        float startY = player.Position.Y;

        var upInput = new PlayerInput(left: false, right: false, up: true, down: false, jump: false);
        player.Update(grid, in upInput);

        Assert.Equal(PlayerState.Climbing, player.State);
        Assert.True(player.Position.Y < startY); // Se desplaza hacia arriba
    }

    [Fact]
    public void Ladder_DownInput_MovesDown()
    {
        var grid = CreateTestRoom();
        // Posicionamos al jugador a mitad de la escalera 1 (col 5, fila 11)
        var player = new Player(new GridCoord(5, 11));
        float startY = player.Position.Y;

        var downInput = new PlayerInput(left: false, right: false, up: false, down: true, jump: false);
        player.Update(grid, in downInput);

        Assert.Equal(PlayerState.Climbing, player.State);
        Assert.True(player.Position.Y > startY); // Se desplaza hacia abajo
    }

    [Fact]
    public void Ladder_JumpWithDirection_DisengagesLadder()
    {
        var grid = CreateTestRoom();
        var player = new Player(new GridCoord(5, 11));

        // Entrar en Climbing
        player.Update(grid, new PlayerInput(false, false, up: true, false, false));
        Assert.Equal(PlayerState.Climbing, player.State);

        // Saltar a la derecha
        player.Update(grid, new PlayerInput(false, right: true, false, false, jump: true));
        Assert.Equal(PlayerState.Jumping, player.State);
        Assert.Equal(Direction.Right, player.Facing);
    }

    [Fact]
    public void Ladder_ClimbToTop_AlightsOnPlatformAndRemainsGrounded()
    {
        var grid = CreateTestRoom();
        // Escalera 1 va de fila 13 a fila 9. La plataforma superior está en fila 9 (Y = 144, player Y = 128).
        var player = new Player(new GridCoord(5, 13));

        var upInput = new PlayerInput(left: false, right: false, up: true, down: false, jump: false);
        // Trepar suficientes frames para alcanzar el tope de la escalera
        for (int i = 0; i < 100; i++)
        {
            player.Update(grid, in upInput);
            if (player.State == PlayerState.Idle && player.Position.Y <= 128f)
                break;
        }

        // Debe posarse sobre la plataforma en Y = 128 (fila 8 con pies en fila 9)
        Assert.Equal(128f, player.Position.Y);
        Assert.Equal(PlayerState.Idle, player.State);

        // Sin input en el siguiente frame, no debe caer
        player.Update(grid, new PlayerInput(false, false, false, false, false));
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(128f, player.Position.Y);
    }

    [Fact]
    public void Platform_WalkAcrossLadderTop_DoesNotFall()
    {
        var grid = CreateTestRoom();
        // Colocamos al jugador en la plataforma intermedia (columna 4, fila 8, Y = 128)
        var player = new Player(4 * GameConstants.TILE_SIZE, 8 * GameConstants.TILE_SIZE);

        var rightInput = new PlayerInput(left: false, right: true, up: false, down: false, jump: false);

        // Caminar hacia la derecha cruzando sobre la escalera en columna 5 hacia columna 6
        for (int i = 0; i < 30; i++)
        {
            player.Update(grid, in rightInput);
            // En ningún momento debe caer por el hueco de la escalera
            Assert.NotEqual(PlayerState.Falling, player.State);
            Assert.Equal(128f, player.Position.Y);
        }

        // El jugador debe haber avanzado a la columna 6 sobre la plataforma
        Assert.True(player.Position.X > 5 * GameConstants.TILE_SIZE);
    }

    [Fact]
    public void Platform_ClimbDownFromTop_EntersClimbingState()
    {
        var grid = CreateTestRoom();
        // Colocamos al jugador justo en el tope de la escalera en columna 5 (Y = 128)
        var player = new Player(5 * GameConstants.TILE_SIZE + 1f, 8 * GameConstants.TILE_SIZE);

        var downInput = new PlayerInput(left: false, right: false, up: false, down: true, jump: false);
        player.Update(grid, in downInput);

        Assert.Equal(PlayerState.Climbing, player.State);
        Assert.True(player.Position.Y > 128f);
    }

    [Fact]
    public void RoomGrid_DistinguishesPlatforms_InRecamara1()
    {
        var grid = CreateTestRoom();

        Assert.Equal("Recamara 1", grid.RoomName);
        Assert.Equal(3, grid.Platforms.Count);

        Assert.Equal(0, grid.Platforms[0].Level);
        Assert.Equal(14, grid.Platforms[0].Row);
        Assert.Equal("Plataforma Nivel 0", grid.Platforms[0].Name);

        Assert.Equal(1, grid.Platforms[1].Level);
        Assert.Equal(9, grid.Platforms[1].Row);
        Assert.Equal("Plataforma Nivel 1", grid.Platforms[1].Name);

        Assert.Equal(2, grid.Platforms[2].Level);
        Assert.Equal(5, grid.Platforms[2].Row);
        Assert.Equal("Plataforma Nivel 2", grid.Platforms[2].Name);

        // Verificación de resolución de altura Y a nivel de plataforma
        Assert.Equal(0, grid.GetPlatformLevel(208f)); // Fila 13, pies en fila 14
        Assert.Equal(1, grid.GetPlatformLevel(128f)); // Fila 8, pies en fila 9
        Assert.Equal(2, grid.GetPlatformLevel(64f));  // Fila 4, pies en fila 5
        Assert.Equal(-1, grid.GetPlatformLevel(100f)); // Altura en el aire
    }

    [Fact]
    public void Platform1_WalkLeftOffEdge_FallsSafelyToPlatform0()
    {
        var grid = CreateTestRoom();
        // Colocar al jugador en el extremo izquierdo de la Plataforma 1 (columna 2, fila 8, Y = 128)
        var player = new Player(2 * GameConstants.TILE_SIZE, 8 * GameConstants.TILE_SIZE);

        var leftInput = new PlayerInput(left: true, right: false, up: false, down: false, jump: false);

        // Simular frames suficientes para que camine al vacío de columna 1 y caiga hasta el suelo (Nivel 0)
        for (int i = 0; i < 80; i++)
        {
            player.Update(grid, in leftInput);
        }

        // El jugador no debe quedarse trabado en Y = 128; debe caer al suelo (Y = 208, fila 13 con pies en fila 14)
        Assert.Equal(208f, player.Position.Y);
        Assert.True(player.Position.X < 2 * GameConstants.TILE_SIZE, "Debe haber entrado a la columna 1");
        Assert.True(player.Position.X >= GameConstants.TILE_SIZE, "No debe atravesar el muro perimetral izquierdo");

        // Al provenir de la Plataforma Nivel 1 (caída de 5 celdas = 80 px), la caída es segura
        Assert.False(player.IsEliminated, "La caída desde Nivel 1 a Nivel 0 debe ser segura y no eliminar al jugador");
        Assert.NotEqual(PlayerState.Eliminated, player.State);
    }

    [Fact]
    public void Platform2_WalkRightOffEdge_FallsToPlatform0_TriggersEliminated()
    {
        var grid = CreateTestRoom();
        // Colocar al jugador en el extremo derecho de la Plataforma 2 (columna 17, fila 4, Y = 64)
        var player = new Player(17 * GameConstants.TILE_SIZE, 4 * GameConstants.TILE_SIZE);

        var rightInput = new PlayerInput(left: false, right: true, up: false, down: false, jump: false);

        // Simular frames suficientes para caminar al vacío de columna 18 y caer hasta el suelo (Nivel 0)
        for (int i = 0; i < 100; i++)
        {
            player.Update(grid, in rightInput);
        }

        // El jugador debe haber caído hasta el suelo inferior (Y = 208)
        Assert.Equal(208f, player.Position.Y);
        Assert.True(player.Position.X >= 18 * GameConstants.TILE_SIZE, "Debe haber avanzado hacia la columna vacía 18");

        // Al caer desde la altura de la Plataforma Nivel 2 (9 celdas = 144 px), el jugador queda eliminado
        Assert.True(player.IsEliminated, "El jugador debe quedar eliminado al caer desde la altura de la Plataforma Nivel 2");
        Assert.Equal(PlayerState.Eliminated, player.State);

        // Los controles no deben mover al jugador una vez eliminado
        float elimX = player.Position.X;
        float elimY = player.Position.Y;
        player.Update(grid, new PlayerInput(left: true, right: true, up: true, down: true, jump: true));
        Assert.Equal(elimX, player.Position.X);
        Assert.Equal(elimY, player.Position.Y);
        Assert.Equal(PlayerState.Eliminated, player.State);
    }

    [Fact]
    public void EliminatedPlayer_SetPositionRestoresToIdle()
    {
        var grid = CreateTestRoom();
        var player = new Player(17 * GameConstants.TILE_SIZE, 4 * GameConstants.TILE_SIZE);

        // Caer al vacío desde Nivel 2 hasta ser eliminado
        var rightInput = new PlayerInput(left: false, right: true, up: false, down: false, jump: false);
        for (int i = 0; i < 100; i++)
        {
            player.Update(grid, in rightInput);
        }
        Assert.True(player.IsEliminated);

        // Reiniciar posición (equivalente a pulsar tecla 2)
        player.SetPosition(2 * GameConstants.TILE_SIZE, 13 * GameConstants.TILE_SIZE);

        Assert.False(player.IsEliminated);
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(2 * GameConstants.TILE_SIZE, player.Position.X);
        Assert.Equal(13 * GameConstants.TILE_SIZE, player.Position.Y);
    }

    [Fact]
    public void Fall_FromLevel2_ToLevel1_DoesNotEliminatePlayer()
    {
        var grid = CreateTestRoom();
        // Columna 4 es el extremo izquierdo de la sección principal de Plataforma 2 (fila 4, Y = 64).
        // Al caminar a la izquierda, entra al hueco de columna 3 y cae hacia la Plataforma 1 (fila 9, Y = 128).
        // Caída de 1 nivel (Nivel 2 a Nivel 1): no debe eliminar al jugador.
        var player = new Player(4 * GameConstants.TILE_SIZE, 4 * GameConstants.TILE_SIZE);

        var leftInput = new PlayerInput(left: true, right: false, up: false, down: false, jump: false);
        for (int i = 0; i < 10; i++)
        {
            player.Update(grid, in leftInput);
        }

        var neutral = new PlayerInput(false, false, false, false, false);
        for (int i = 0; i < 50; i++)
        {
            player.Update(grid, in neutral);
        }

        // Debe aterrizar sobre la Plataforma 1 (Y = 128)
        Assert.Equal(128f, player.Position.Y);

        // Al ser caída de solo 1 nivel (2 a 1), NO debe eliminarse
        Assert.False(player.IsEliminated, "La caída de 1 nivel (Nivel 2 a Nivel 1) no debe eliminar al jugador");
        Assert.NotEqual(PlayerState.Eliminated, player.State);
    }

    [Fact]
    public void Fall_FromLevel3_Scenarios_ValidateTwoLevelLethalityRule()
    {
        var grid = new RoomGrid();
        grid.Clear(TileType.Empty);

        // Paredes exteriores
        for (int x = 0; x < GameConstants.GRID_COLUMNS; x++)
        {
            grid.SetTile(x, 0, TileType.SolidWall);
            grid.SetTile(x, 14, TileType.SolidWall); // Nivel 0
        }
        for (int y = 0; y < GameConstants.GRID_ROWS; y++)
        {
            grid.SetTile(0, y, TileType.SolidWall);
            grid.SetTile(GameConstants.GRID_COLUMNS - 1, y, TileType.SolidWall);
        }

        // Construir 4 niveles de plataformas:
        // Nivel 0: Fila 14
        // Nivel 1: Fila 11 (Y = 160)
        // Nivel 2: Fila 8 (Y = 112)
        // Nivel 3: Fila 4 (Y = 48)
        for (int x = 2; x <= 16; x++)
        {
            grid.SetTile(x, 11, TileType.SolidWall);
            grid.SetTile(x, 8, TileType.SolidWall);
            grid.SetTile(x, 4, TileType.SolidWall);
        }

        // Registrar las 4 plataformas en la recámara
        grid.Platforms.GetType().GetMethod("Clear")?.Invoke(grid.Platforms, null);
        var addMethod = grid.Platforms.GetType().GetMethod("Add");
        addMethod?.Invoke(grid.Platforms, new object[] { new PlatformInfo(0, 14, 1, 18, "Nivel 0") });
        addMethod?.Invoke(grid.Platforms, new object[] { new PlatformInfo(1, 11, 2, 16, "Nivel 1") });
        addMethod?.Invoke(grid.Platforms, new object[] { new PlatformInfo(2, 8, 2, 16, "Nivel 2") });
        addMethod?.Invoke(grid.Platforms, new object[] { new PlatformInfo(3, 4, 2, 16, "Nivel 3") });

        // Caso A: Caída de Nivel 3 a Nivel 2 (1 nivel de diferencia: 3 - 2 = 1) -> NO se elimina
        grid.SetTile(6, 4, TileType.Empty);
        grid.SetTile(7, 4, TileType.Empty);
        var playerA = new Player(6 * GameConstants.TILE_SIZE + 1f, 3 * GameConstants.TILE_SIZE);
        for (int i = 0; i < 40; i++)
        {
            playerA.Update(grid, new PlayerInput(false, false, false, false, false));
        }
        Assert.Equal(8 * GameConstants.TILE_SIZE - Player.HEIGHT, playerA.Position.Y); // Fila 7 (pies en fila 8 sólida = 112 px)
        Assert.False(playerA.IsEliminated, "Caída de 1 nivel (Nivel 3 a Nivel 2) debe ser segura");

        // Caso B: Caída de Nivel 3 a Nivel 1 (2 niveles de diferencia: 3 - 1 = 2) -> ELIMINADO
        grid.SetTile(6, 4, TileType.SolidWall);
        grid.SetTile(7, 4, TileType.SolidWall);
        grid.SetTile(10, 4, TileType.Empty);
        grid.SetTile(11, 4, TileType.Empty);
        grid.SetTile(10, 8, TileType.Empty);
        grid.SetTile(11, 8, TileType.Empty);
        var playerB = new Player(10 * GameConstants.TILE_SIZE + 1f, 3 * GameConstants.TILE_SIZE);
        for (int i = 0; i < 60; i++)
        {
            playerB.Update(grid, new PlayerInput(false, false, false, false, false));
        }
        Assert.Equal(11 * GameConstants.TILE_SIZE - Player.HEIGHT, playerB.Position.Y); // Fila 10 (pies en fila 11 sólida = 160 px)
        Assert.True(playerB.IsEliminated, "Caída de 2 niveles (Nivel 3 a Nivel 1) debe eliminar al jugador");

        // Caso C: Caída de Nivel 3 a Nivel 0 (3 niveles de diferencia: 3 - 0 = 3) -> ELIMINADO
        grid.SetTile(10, 4, TileType.SolidWall);
        grid.SetTile(11, 4, TileType.SolidWall);
        grid.SetTile(10, 8, TileType.SolidWall);
        grid.SetTile(11, 8, TileType.SolidWall);
        grid.SetTile(14, 4, TileType.Empty);
        grid.SetTile(15, 4, TileType.Empty);
        grid.SetTile(14, 8, TileType.Empty);
        grid.SetTile(15, 8, TileType.Empty);
        grid.SetTile(14, 11, TileType.Empty);
        grid.SetTile(15, 11, TileType.Empty);
        var playerC = new Player(14 * GameConstants.TILE_SIZE + 1f, 3 * GameConstants.TILE_SIZE);
        for (int i = 0; i < 80; i++)
        {
            playerC.Update(grid, new PlayerInput(false, false, false, false, false));
        }
        Assert.Equal(14 * GameConstants.TILE_SIZE - Player.HEIGHT, playerC.Position.Y); // Fila 13 (pies en fila 14 sólida = 208 px)
        Assert.True(playerC.IsEliminated, "Caída de 3 niveles (Nivel 3 a Nivel 0) debe eliminar al jugador");
    }

    [Fact]
    public void InitialLives_StartsAtThree()
    {
        var player = new Player(new GridCoord(2, 13));
        Assert.Equal(Player.INITIAL_LIVES, player.Lives);
        Assert.Equal(3, player.Lives);
    }

    [Fact]
    public void LethalFall_DecrementsLifeByOne()
    {
        var grid = CreateTestRoom();
        // Colocar al jugador en plataforma nivel 2 (fila 4) sobre el vacío en x=290 (col 18)
        var player = new Player(290f, 4 * GameConstants.TILE_SIZE);
        Assert.Equal(3, player.Lives);

        // Dejar caer hasta el suelo inferior (nivel 2 a nivel 0 = caída fatal de 2 niveles)
        for (int i = 0; i < 90; i++)
        {
            player.Update(grid, new PlayerInput(false, false, false, false, false));
        }

        Assert.True(player.IsEliminated);
        Assert.Equal(2, player.Lives);
    }

    [Fact]
    public void ThreeLethalFalls_DepletesLivesToZero()
    {
        var grid = CreateTestRoom();
        var player = new Player(290f, 4 * GameConstants.TILE_SIZE);

        for (int fall = 0; fall < 3; fall++)
        {
            player.SetPosition(290f, 4 * GameConstants.TILE_SIZE);
            for (int i = 0; i < 90; i++)
            {
                player.Update(grid, new PlayerInput(false, false, false, false, false));
            }
            Assert.True(player.IsEliminated);
            Assert.Equal(2 - fall, player.Lives);
        }

        Assert.Equal(0, player.Lives);
    }

    [Fact]
    public void LoseLife_DoesNotUnderflowBelowZero()
    {
        var player = new Player(new GridCoord(2, 13));
        for (int i = 0; i < 10; i++)
        {
            player.LoseLife();
        }
        Assert.Equal(0, player.Lives);
    }

    [Fact]
    public void ResetLives_RestoresToInitialLives()
    {
        var player = new Player(new GridCoord(2, 13));
        player.LoseLife();
        player.LoseLife();
        Assert.Equal(1, player.Lives);

        player.ResetLives();
        Assert.Equal(Player.INITIAL_LIVES, player.Lives);
        Assert.Equal(3, player.Lives);
    }

    [Fact]
    public void Eliminate_SetsStateEliminatedAndDeductsLife()
    {
        var player = new Player(new GridCoord(2, 13));
        Assert.Equal(3, player.Lives);
        Assert.False(player.IsEliminated);

        player.Eliminate();

        Assert.True(player.IsEliminated);
        Assert.Equal(2, player.Lives);
    }

    [Fact]
    public void Eliminate_WhenAlreadyEliminated_DoesNotDeductExtraLife()
    {
        var player = new Player(new GridCoord(2, 13));
        player.Eliminate();
        Assert.Equal(2, player.Lives);

        // Llamar a Eliminate() de nuevo mientras sigue eliminado
        player.Eliminate();
        Assert.Equal(2, player.Lives);
    }

    [Fact]
    public void Jump_FromPlatform1_AcrossGapsToExtension_LandsSafely()
    {
        var grid = CreateTestRoom();
        // Iniciar en el extremo de la sección 1 de Plataforma 1 (columna 9, fila 8, Y = 128)
        var player = new Player(9 * GameConstants.TILE_SIZE + 8f, 8 * GameConstants.TILE_SIZE);
        var neutral = new PlayerInput(false, false, false, false, false);

        // Salto 1: desde col 9 sobre el hueco de col 11 hacia el descansillo en col 12
        player.Update(grid, new PlayerInput(left: false, right: true, up: false, down: false, jump: true));
        for (int i = 0; i < Player.JUMP_DURATION_FRAMES; i++)
        {
            player.Update(grid, in neutral);
        }

        Assert.Equal(128f, player.Position.Y);
        Assert.False(player.IsEliminated);
        Assert.NotEqual(PlayerState.Falling, player.State);
        Assert.True(player.Position.X >= 11 * GameConstants.TILE_SIZE && player.Position.X <= 13 * GameConstants.TILE_SIZE,
            "El jugador debe haber aterrizado sobre el descansillo de la columna 12");

        // Salto 2: desde el descansillo de col 12 sobre el hueco de col 13 hacia la extensión (col 14-17)
        player.Update(grid, new PlayerInput(left: false, right: true, up: false, down: false, jump: true));
        for (int i = 0; i < Player.JUMP_DURATION_FRAMES; i++)
        {
            player.Update(grid, in neutral);
        }

        Assert.Equal(128f, player.Position.Y);
        Assert.False(player.IsEliminated);
        Assert.NotEqual(PlayerState.Falling, player.State);
        Assert.True(player.Position.X >= 14 * GameConstants.TILE_SIZE,
            "El jugador debe haber aterrizado sobre la nueva sección de la plataforma (col 14-17)");
    }

    [Fact]
    public void Jump_FromPlatform1_ExtensionBackToSection1_LandsSafely()
    {
        var grid = CreateTestRoom();
        // Iniciar en la extensión de Plataforma 1 (columna 14, fila 8, Y = 128)
        var player = new Player(14 * GameConstants.TILE_SIZE + 10f, 8 * GameConstants.TILE_SIZE);
        var neutral = new PlayerInput(false, false, false, false, false);

        // Salto 1 hacia la izquierda: desde col 14 sobre el hueco de col 13 hacia el descansillo en col 12
        player.Update(grid, new PlayerInput(left: true, right: false, up: false, down: false, jump: true));
        for (int i = 0; i < Player.JUMP_DURATION_FRAMES; i++)
        {
            player.Update(grid, in neutral);
        }

        Assert.Equal(128f, player.Position.Y);
        Assert.False(player.IsEliminated);
        Assert.NotEqual(PlayerState.Falling, player.State);
        Assert.True(player.Position.X >= 11 * GameConstants.TILE_SIZE && player.Position.X <= 13 * GameConstants.TILE_SIZE,
            "El jugador debe haber aterrizado de regreso en el descansillo de la columna 12");

        // Salto 2 hacia la izquierda: desde col 12 sobre el hueco de col 11 hacia la sección 1 (col 9)
        player.Update(grid, new PlayerInput(left: true, right: false, up: false, down: false, jump: true));
        for (int i = 0; i < Player.JUMP_DURATION_FRAMES; i++)
        {
            player.Update(grid, in neutral);
        }

        Assert.Equal(128f, player.Position.Y);
        Assert.False(player.IsEliminated);
        Assert.NotEqual(PlayerState.Falling, player.State);
        Assert.True(player.Position.X <= 10 * GameConstants.TILE_SIZE,
            "El jugador debe haber aterrizado de regreso en la sección 1 de la plataforma");
    }

    [Theory]
    [InlineData(11)] // Hueco entre sección 1 y descansillo
    [InlineData(13)] // Hueco entre descansillo y sección 2
    [InlineData(18)] // Espacio vacío simétrico en el extremo derecho
    public void Fall_FromPlatform1_Gaps_LandsOnLevel0SafelyWithoutElimination(int gapCol)
    {
        var grid = CreateTestRoom();
        // Colocar al jugador cayendo en el hueco a la altura de la Plataforma 1 (fila 8, Y = 128)
        var player = new Player(gapCol * GameConstants.TILE_SIZE + 1f, 8 * GameConstants.TILE_SIZE);
        var neutral = new PlayerInput(false, false, false, false, false);

        // Simular frames de caída hasta el suelo (Nivel 0, Y = 208)
        for (int i = 0; i < 80; i++)
        {
            player.Update(grid, in neutral);
        }

        // Debe aterrizar en el suelo firme de Nivel 0 (Y = 208)
        Assert.Equal(208f, player.Position.Y);

        // Caída de 1 nivel (Nivel 1 a Nivel 0): NO letal, conserva sus vidas intactas
        Assert.False(player.IsEliminated, $"La caída desde el hueco col {gapCol} en Nivel 1 no debe eliminar al jugador");
        Assert.Equal(Player.INITIAL_LIVES, player.Lives);
        Assert.Equal(PlayerState.Idle, player.State);
    }

    [Fact]
    public void Jump_AcrossPlatform2_Col3Gap_ToExitDoor_LandsSafely()
    {
        var grid = CreateTestRoom();
        // Iniciar en el extremo izquierdo de Plataforma 2 (columna 4, fila 4, Y = 64)
        var player = new Player(4 * GameConstants.TILE_SIZE + 2f, 4 * GameConstants.TILE_SIZE);
        var neutral = new PlayerInput(false, false, false, false, false);

        // Saltar hacia la izquierda sobre el hueco de columna 3
        player.Update(grid, new PlayerInput(left: true, right: false, up: false, down: false, jump: true));
        for (int i = 0; i < Player.JUMP_DURATION_FRAMES; i++)
        {
            player.Update(grid, in neutral);
        }

        // Debe aterrizar sobre el descansillo de la puerta (columna 1 o 2, fila 4, Y = 64)
        Assert.Equal(64f, player.Position.Y);
        Assert.False(player.IsEliminated);
        Assert.NotEqual(PlayerState.Falling, player.State);
        Assert.True(player.Position.X < 3 * GameConstants.TILE_SIZE,
            "El jugador debe haber aterrizado sobre la repisa de la puerta de salida");
    }

    [Fact]
    public void Jump_AcrossPlatform2_FromExitDoor_BackToCol4_LandsSafely()
    {
        var grid = CreateTestRoom();
        // Iniciar en el descansillo de la puerta de salida (columna 2, fila 4, Y = 64)
        var player = new Player(2 * GameConstants.TILE_SIZE + 2f, 4 * GameConstants.TILE_SIZE);
        var neutral = new PlayerInput(false, false, false, false, false);

        // Saltar hacia la derecha sobre el hueco de columna 3
        player.Update(grid, new PlayerInput(left: false, right: true, up: false, down: false, jump: true));
        for (int i = 0; i < Player.JUMP_DURATION_FRAMES; i++)
        {
            player.Update(grid, in neutral);
        }

        // Debe aterrizar de regreso en la sección principal de Plataforma 2 (Y = 64)
        Assert.Equal(64f, player.Position.Y);
        Assert.False(player.IsEliminated);
        Assert.NotEqual(PlayerState.Falling, player.State);
        Assert.True(player.Position.X >= 4 * GameConstants.TILE_SIZE,
            "El jugador debe haber aterrizado de regreso en la plataforma nivel 2");
    }

    [Fact]
    public void Chamber2_LoadChamber2_ConfiguresExpectedRoomGridAndPlatforms()
    {
        var grid = new RoomGrid();
        grid.LoadChamber(2);

        Assert.Equal("Recamara 2", grid.RoomName);
        Assert.Equal(3, grid.Platforms.Count);
        Assert.Equal(TileType.ExitDoor, grid.GetTile(1, 5));
        Assert.True(grid.IsExitDoor(1, 5));
    }
}
