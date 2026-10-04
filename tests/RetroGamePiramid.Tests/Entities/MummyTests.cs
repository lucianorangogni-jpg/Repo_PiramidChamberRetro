using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;
using RetroGamePiramid.Grid;
using RetroGamePiramid.Input;
using Xunit;

namespace RetroGamePiramid.Tests.Entities;

public class MummyTests
{
    private const float SPAWN_X = 13 * GameConstants.TILE_SIZE; // 208f
    private const float SPAWN_Y = 4 * GameConstants.TILE_SIZE;  // 64f
    private const float MIN_X = 4 * GameConstants.TILE_SIZE;    // 64f (límite con el hueco vacío de columna 3)
    private const float MAX_X = 18 * GameConstants.TILE_SIZE - Mummy.WIDTH; // 274f (límite con el hueco vacío de columna 18)

    private Mummy CreateTestMummy(Direction initialFacing = Direction.Right)
    {
        return new Mummy(SPAWN_X, SPAWN_Y, MIN_X, MAX_X, initialFacing);
    }

    [Fact]
    public void InitialState_PositionAndFacing_MatchesConfiguration()
    {
        var mummy = CreateTestMummy();

        Assert.Equal(SPAWN_X, mummy.Position.X);
        Assert.Equal(SPAWN_Y, mummy.Position.Y);
        Assert.Equal(MIN_X, mummy.MinX);
        Assert.Equal(MAX_X, mummy.MaxX);
        Assert.Equal(Direction.Right, mummy.Facing);
        Assert.Equal(14, Mummy.WIDTH);
        Assert.Equal(16, Mummy.HEIGHT);
        Assert.Equal(Player.WALK_SPEED, Mummy.SPEED);
    }

    [Fact]
    public void Patrol_MovesRightAtSpeed()
    {
        var mummy = CreateTestMummy(Direction.Right);
        var player = new Player(new GridCoord(2, 13)); // Jugador lejos en plataforma 0

        mummy.Update(player);

        Assert.Equal(SPAWN_X + Mummy.SPEED, mummy.Position.X);
        Assert.Equal(Direction.Right, mummy.Facing);
    }

    [Fact]
    public void Patrol_ReversesAtMaxX()
    {
        var mummy = CreateTestMummy(Direction.Right);
        var player = new Player(new GridCoord(2, 13));

        // Colocar la momia a 1 píxel de maxX
        mummy.SetPosition(MAX_X - 1f, SPAWN_Y);

        mummy.Update(player);

        Assert.Equal(MAX_X, mummy.Position.X);
        Assert.Equal(Direction.Left, mummy.Facing);
    }

    [Fact]
    public void Patrol_MovesLeftAtSpeed()
    {
        var mummy = CreateTestMummy(Direction.Left);
        var player = new Player(new GridCoord(2, 13));

        mummy.Update(player);

        Assert.Equal(SPAWN_X - Mummy.SPEED, mummy.Position.X);
        Assert.Equal(Direction.Left, mummy.Facing);
    }

    [Fact]
    public void Patrol_ReversesAtMinX()
    {
        var mummy = CreateTestMummy(Direction.Left);
        var player = new Player(new GridCoord(2, 13));

        // Colocar la momia a 1 píxel de minX
        mummy.SetPosition(MIN_X + 1f, SPAWN_Y);

        mummy.Update(player);

        Assert.Equal(MIN_X, mummy.Position.X);
        Assert.Equal(Direction.Right, mummy.Facing);
    }

    [Fact]
    public void CollisionWithPlayer_EliminatesPlayerAndDeductsLife()
    {
        var mummy = CreateTestMummy();
        // Colocar al jugador exactamente sobre la momia
        var player = new Player(mummy.Position.X, mummy.Position.Y);
        Assert.Equal(3, player.Lives);
        Assert.False(player.IsEliminated);

        mummy.Update(player);

        Assert.True(player.IsEliminated, "El jugador debe quedar eliminado al colisionar con la momia");
        Assert.Equal(2, player.Lives);
    }

    [Fact]
    public void CollisionWithAlreadyEliminatedPlayer_DoesNotDeductMultipleLives()
    {
        var mummy = CreateTestMummy();
        var player = new Player(mummy.Position.X, mummy.Position.Y);

        // Primer frame: colisión y descuento de 1 vida
        mummy.Update(player);
        Assert.True(player.IsEliminated);
        Assert.Equal(2, player.Lives);

        // Frames siguientes permaneciendo en contacto: no debe perder vidas adicionales
        for (int i = 0; i < 30; i++)
        {
            mummy.Update(player);
            Assert.Equal(2, player.Lives);
        }
    }

    [Fact]
    public void NoCollisionWhenDistant_DoesNotEliminatePlayer()
    {
        var mummy = CreateTestMummy();
        var player = new Player(new GridCoord(2, 13)); // En piso inferior

        mummy.Update(player);

        Assert.False(player.IsEliminated);
        Assert.Equal(3, player.Lives);
    }

    [Fact]
    public void Reset_RestoresSpawnPositionAndFacing()
    {
        var mummy = CreateTestMummy(Direction.Right);
        var player = new Player(new GridCoord(2, 13));

        // Avanzar la momia varias veces
        for (int i = 0; i < 50; i++)
        {
            mummy.Update(player);
        }
        Assert.NotEqual(SPAWN_X, mummy.Position.X);

        mummy.Reset();

        Assert.Equal(SPAWN_X, mummy.Position.X);
        Assert.Equal(SPAWN_Y, mummy.Position.Y);
        Assert.Equal(Direction.Right, mummy.Facing);
    }

    [Fact]
    public void PatrolStaysWithinPlatformLevel2Bounds_OverManyCycles()
    {
        var mummy = CreateTestMummy();
        var player = new Player(new GridCoord(2, 13));

        for (int frame = 0; frame < 1000; frame++)
        {
            mummy.Update(player);

            Assert.True(mummy.Position.X >= MIN_X, $"Momia se salió por la izquierda en frame {frame}: {mummy.Position.X} < {MIN_X}");
            Assert.True(mummy.Position.X <= MAX_X, $"Momia se salió por la derecha en frame {frame}: {mummy.Position.X} > {MAX_X}");
        }
    }

    [Fact]
    public void JumpOverMummy_OnPlatform2_DoesNotEliminatePlayer()
    {
        var grid = new RoomGrid();
        grid.LoadDefaultRoom();

        // Momia en plataforma nivel 2 (Y=64), avanzando hacia la izquierda desde x=200
        var mummy = new Mummy(200f, 64f, MIN_X, MAX_X, Direction.Left);

        // Jugador en plataforma nivel 2 (Y=64), posicionado a distancia para saltar hacia la derecha sobre la momia
        var player = new Player(170f, 64f);
        Assert.Equal(3, player.Lives);

        // Inicia el salto parabólico con dirección a la derecha
        var jumpInput = new PlayerInput(left: false, right: true, up: false, down: false, jump: true);
        player.Update(grid, in jumpInput);
        mummy.Update(player);
        Assert.False(player.IsEliminated);

        // Avanzar el arco completo del salto (24 frames) mientras la momia y el jugador se cruzan
        var neutral = new PlayerInput(false, false, false, false, false);
        for (int i = 0; i < Player.JUMP_DURATION_FRAMES; i++)
        {
            player.Update(grid, in neutral);
            mummy.Update(player);
            Assert.False(player.IsEliminated, $"El jugador no debe ser eliminado en frame {i} mientras salta sobre la momia");
        }

        // El jugador debe haber superado la posición de la momia y aterrizado ileso conservando sus 3 vidas
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.False(player.IsEliminated);
        Assert.Equal(3, player.Lives);
        Assert.True(player.Position.X > mummy.Position.X, "El jugador debe haber aterrizado al otro lado de la momia");
    }

    [Fact]
    public void Configure_UpdatesSpawnAndPatrolBounds_AndResets()
    {
        var mummy = CreateTestMummy(Direction.Left);
        var player = new Player(new GridCoord(2, 13));

        // Mover la momia
        mummy.Update(player);

        // Reconfigurar con nuevos límites (por ejemplo para Recámara 2)
        mummy.Configure(
            spawnX: 10 * GameConstants.TILE_SIZE,
            spawnY: 5 * GameConstants.TILE_SIZE,
            minX: 4 * GameConstants.TILE_SIZE,
            maxX: 16 * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right);

        Assert.Equal(10 * GameConstants.TILE_SIZE, mummy.Position.X);
        Assert.Equal(5 * GameConstants.TILE_SIZE, mummy.Position.Y);
        Assert.Equal(4 * GameConstants.TILE_SIZE, mummy.MinX);
        Assert.Equal(16 * GameConstants.TILE_SIZE - Mummy.WIDTH, mummy.MaxX);
        Assert.Equal(Direction.Right, mummy.Facing);
    }

    [Fact]
    public void PatrolReachesEdgeOfGapAtColumn3_AndReversesWithoutFalling()
    {
        var mummy = CreateTestMummy(Direction.Left);
        var player = new Player(new GridCoord(2, 13));

        // Acercar la momia al borde izquierdo de la plataforma continua (columna 4, X=64)
        mummy.SetPosition(MIN_X + 2f, SPAWN_Y); // 66f
        mummy.Update(player); // 66 - 1.5 = 64.5f
        Assert.Equal(64.5f, mummy.Position.X);
        Assert.Equal(Direction.Left, mummy.Facing);

        mummy.Update(player); // Al intentar cruzar 64f, rebota exactamente a MIN_X (64f)
        Assert.Equal(MIN_X, mummy.Position.X);
        Assert.Equal(Direction.Right, mummy.Facing);

        // La momia nunca invade la columna 3 (X < 64f), evitando caer en el hueco sin piso
        Assert.True(mummy.Position.X >= 4 * GameConstants.TILE_SIZE);
    }

    [Fact]
    public void PatrolReachesEdgeOfGapAtColumn18_AndReversesWithoutFalling()
    {
        var mummy = CreateTestMummy(Direction.Right);
        var player = new Player(new GridCoord(2, 13));

        // Acercar la momia al borde derecho de la plataforma continua (columna 17, X=274)
        mummy.SetPosition(MAX_X - 2f, SPAWN_Y); // 272f
        mummy.Update(player); // 272 + 1.5 = 273.5f
        Assert.Equal(273.5f, mummy.Position.X);
        Assert.Equal(Direction.Right, mummy.Facing);

        mummy.Update(player); // Al intentar cruzar MAX_X, rebota exactamente a MAX_X (274f)
        Assert.Equal(MAX_X, mummy.Position.X);
        Assert.Equal(Direction.Left, mummy.Facing);

        // Su lado derecho (X + 14 = 288f) no sobrepasa la columna 17, evitando caer en la columna 18 (X >= 288f)
        Assert.True(mummy.Position.X + Mummy.WIDTH <= 18 * GameConstants.TILE_SIZE);
    }

    [Fact]
    public void Mummy_InitiallyDormant_DoesNotMoveOnUpdate()
    {
        var mummy = new Mummy(
            spawnX: 2 * GameConstants.TILE_SIZE,
            spawnY: 8 * GameConstants.TILE_SIZE,
            minX: 2 * GameConstants.TILE_SIZE,
            maxX: 11 * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right,
            initiallyAwake: false);

        var player = new Player(new GridCoord(2, 13)); // Lejos en nivel 0

        Assert.False(mummy.IsAwake);
        Assert.True(mummy.IsActive);
        Assert.Equal(32f, mummy.Position.X);

        // Varios frames de actualización sin despertar
        for (int i = 0; i < 20; i++)
        {
            mummy.Update(player);
        }

        Assert.Equal(32f, mummy.Position.X);
        Assert.False(mummy.IsAwake);
    }

    [Fact]
    public void Mummy_WakeUp_StartsMovingAndPatrolling()
    {
        var mummy = new Mummy(
            spawnX: 2 * GameConstants.TILE_SIZE,
            spawnY: 8 * GameConstants.TILE_SIZE,
            minX: 2 * GameConstants.TILE_SIZE,
            maxX: 11 * GameConstants.TILE_SIZE - Mummy.WIDTH,
            initialFacing: Direction.Right,
            initiallyAwake: false);

        var player = new Player(new GridCoord(2, 13));

        mummy.Update(player);
        Assert.Equal(32f, mummy.Position.X);

        // Despertar la momia (al recoger la llave)
        mummy.WakeUp();
        Assert.True(mummy.IsAwake);

        mummy.Update(player);
        Assert.Equal(32f + Mummy.SPEED, mummy.Position.X);
    }

    [Fact]
    public void Mummy_Dormant_StillEliminatesPlayerOnCollision()
    {
        var mummy = new Mummy(
            spawnX: 32f,
            spawnY: 128f,
            minX: 32f,
            maxX: 162f,
            initialFacing: Direction.Right,
            initiallyAwake: false);

        // Colocar al jugador directamente sobre la momia dormida
        var player = new Player(mummy.Position.X, mummy.Position.Y);
        Assert.Equal(3, player.Lives);
        Assert.False(player.IsEliminated);

        mummy.Update(player);

        Assert.True(player.IsEliminated, "La momia debe ser letal incluso estando dormida/inmóvil");
        Assert.Equal(2, player.Lives);
    }

    [Fact]
    public void Mummy_Inactive_DoesNotUpdatePositionNorEliminatePlayer()
    {
        var mummy = new Mummy(
            spawnX: 32f,
            spawnY: 128f,
            minX: 32f,
            maxX: 162f,
            initialFacing: Direction.Right,
            initiallyAwake: false,
            initiallyActive: false);

        var player = new Player(mummy.Position.X, mummy.Position.Y);

        mummy.Update(player);

        Assert.False(player.IsEliminated, "Una momia inactiva no debe eliminar al jugador");
        Assert.Equal(32f, mummy.Position.X);
    }

    [Fact]
    public void Mummy_Reset_RestoresInitialAwakeState()
    {
        var mummy = new Mummy(
            spawnX: 32f,
            spawnY: 128f,
            minX: 32f,
            maxX: 162f,
            initialFacing: Direction.Right,
            initiallyAwake: false);

        var player = new Player(new GridCoord(2, 13));

        mummy.WakeUp();
        mummy.Update(player);
        Assert.True(mummy.IsAwake);
        Assert.NotEqual(32f, mummy.Position.X);

        mummy.Reset();

        Assert.False(mummy.IsAwake);
        Assert.Equal(32f, mummy.Position.X);
        Assert.Equal(128f, mummy.Position.Y);
        Assert.Equal(Direction.Right, mummy.Facing);
    }

    [Fact]
    public void Mummy_RegisterJumpOver_TriggersChaseOnFourthJump()
    {
        var mummy = new Mummy(32f, 128f, 32f, 162f, Direction.Right, initiallyAwake: true);

        Assert.Equal(0, mummy.JumpOverCount);
        Assert.False(mummy.IsChasing);

        Assert.False(mummy.RegisterJumpOver());
        Assert.Equal(1, mummy.JumpOverCount);
        Assert.False(mummy.IsChasing);

        Assert.False(mummy.RegisterJumpOver());
        Assert.Equal(2, mummy.JumpOverCount);
        Assert.False(mummy.IsChasing);

        Assert.False(mummy.RegisterJumpOver());
        Assert.Equal(3, mummy.JumpOverCount);
        Assert.False(mummy.IsChasing);

        Assert.True(mummy.RegisterJumpOver());
        Assert.Equal(4, mummy.JumpOverCount);
        Assert.True(mummy.IsChasing);
        Assert.True(mummy.IsAwake);
    }

    [Fact]
    public void Mummy_ResetJumpOver_ResetsCounter()
    {
        var mummy = new Mummy(32f, 128f, 32f, 162f);
        mummy.RegisterJumpOver();
        mummy.RegisterJumpOver();
        Assert.Equal(2, mummy.JumpOverCount);

        mummy.ResetJumpOver();
        Assert.Equal(0, mummy.JumpOverCount);
    }

    [Fact]
    public void Mummy_Reset_RestoresAllChaseAndPuzzleStates()
    {
        var mummy = new Mummy(32f, 128f, 32f, 162f, Direction.Right, initiallyAwake: false);
        mummy.WakeUp();
        mummy.StartChasing();
        mummy.SetFalling(true);
        mummy.LandOnLevel0();

        Assert.True(mummy.HasReachedLevel0);
        Assert.False(mummy.IsChasing);
        Assert.False(mummy.IsFalling);

        mummy.Reset();

        Assert.False(mummy.HasReachedLevel0);
        Assert.False(mummy.IsChasing);
        Assert.False(mummy.IsFalling);
        Assert.False(mummy.IsAwake);
        Assert.Equal(0, mummy.JumpOverCount);
        Assert.Equal(32f, mummy.Position.X);
        Assert.Equal(128f, mummy.Position.Y);
    }

    [Fact]
    public void Mummy_Chasing_FollowsPlayerHorizontally()
    {
        var mummy = new Mummy(100f, 128f, 32f, 200f);
        mummy.StartChasing();

        // Jugador a la izquierda (x = 50)
        var playerLeft = new Player(50f, 128f);
        mummy.Update(playerLeft);

        Assert.Equal(Direction.Left, mummy.Facing);
        Assert.Equal(100f - Mummy.SPEED, mummy.Position.X);

        // Jugador a la derecha (x = 150)
        var playerRight = new Player(150f, 128f);
        mummy.Update(playerRight);

        Assert.Equal(Direction.Right, mummy.Facing);
        Assert.Equal(100f, mummy.Position.X);
    }

    [Fact]
    public void Mummy_Chasing_EntersFallingWhenReachingGap()
    {
        var grid = new RoomGrid();
        grid.LoadDefaultRoom(); // En fila 9, col 11 está vacía (gap entre col 10 y col 12)

        var mummy = new Mummy(10 * GameConstants.TILE_SIZE, 8 * GameConstants.TILE_SIZE, 32f, 250f);
        mummy.StartChasing();

        // Jugador en nivel 0 a la derecha, obligando a la momia a avanzar hacia columna 11
        var player = new Player(14 * GameConstants.TILE_SIZE, 13 * GameConstants.TILE_SIZE);

        Assert.False(mummy.IsFalling);

        // Avanzar varios frames hasta que el centro de la momia entre en columna 11 (gap)
        for (int i = 0; i < 20 && !mummy.IsFalling; i++)
        {
            mummy.Update(player, grid);
        }

        Assert.True(mummy.IsFalling, "La momia debe empezar a caer al pisar el foso vacío de columna 11");
    }

    [Fact]
    public void Mummy_Falling_DescendsAndLandsOnLevel0Petrified()
    {
        var mummy = new Mummy(11 * GameConstants.TILE_SIZE, 8 * GameConstants.TILE_SIZE, 32f, 250f);
        mummy.SetFalling(true);

        var player = new Player(2 * GameConstants.TILE_SIZE, 13 * GameConstants.TILE_SIZE);

        float initialY = mummy.Position.Y;
        mummy.Update(player);
        Assert.Equal(initialY + Mummy.FALL_SPEED, mummy.Position.Y);

        // Simular frames hasta que caiga al nivel 0 (Y >= 208)
        while (!mummy.HasReachedLevel0)
        {
            mummy.Update(player);
        }

        Assert.True(mummy.HasReachedLevel0);
        Assert.False(mummy.IsFalling);
        Assert.False(mummy.IsChasing);
        Assert.False(mummy.IsAwake);
        Assert.Equal(13 * GameConstants.TILE_SIZE, mummy.Position.Y);
    }

    [Fact]
    public void Mummy_PetrifiedOnLevel0_DoesNotEliminatePlayer()
    {
        var mummy = new Mummy(11 * GameConstants.TILE_SIZE, 8 * GameConstants.TILE_SIZE, 32f, 250f);
        mummy.LandOnLevel0();

        Assert.True(mummy.HasReachedLevel0);

        // El jugador se coloca directamente sobre la momia petrificada
        var player = new Player(mummy.Position.X, mummy.Position.Y);
        Assert.Equal(3, player.Lives);
        Assert.False(player.IsEliminated);

        mummy.Update(player);

        // No debe eliminar al arqueólogo
        Assert.False(player.IsEliminated);
        Assert.Equal(3, player.Lives);
    }
}
