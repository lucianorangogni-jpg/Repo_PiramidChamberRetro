using System;
using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;
using RetroGamePiramid.Grid;

namespace RetroGamePiramid.Systems;

/// <summary>
/// Gestiona el desafío del puzle de la plataforma 0 (piso inferior).
/// Controla la losa de piedra conmutable, el muro secreto del tesoro y la recolección del cofre.
/// Adapta dinámicamente la configuración según la recámara activa.
/// Garantiza cero asignaciones en memoria heap en cada frame.
/// </summary>
public sealed class PuzzleManager
{
    public GridCoord StoneCoord { get; private set; } = new(8, 13);
    public GridCoord WallCoord { get; private set; } = new(14, 13);
    public GridCoord TreasureCoord { get; private set; } = new(17, 13);
    public GridCoord Treasure2Coord { get; private set; } = new(8, 4);
    public GridCoord ExitDoorCoord { get; private set; } = new(1, 4);
    public GridCoord KeyCoord { get; private set; } = new(16, 6);
    public GridCoord TrapCoord { get; private set; } = new(16, 9);

    public const string MSG_TREASURE = "!TESORO ENCONTRADO! +1000 PTS";
    public const string MSG_KEY = "!LLAVE ENCONTRADA! +500 PTS";
    public const string MSG_DOOR_LOCKED = "!PUERTA CERRADA! NECESITAS LA LLAVE";
    public const string MSG_TRAP = "!TRAMPA! EL PISO SE HA ABIERTO";

    public Treasure Treasure { get; }
    public Treasure Treasure2 { get; }
    public Key Key { get; }
    public FloorTrap Trap { get; }
    public bool HasKey => Key.IsCollected;
    public bool IsTrapOpen => Trap.IsOpen;
    public bool IsWallOpen { get; private set; }
    public bool WasPlayerOnStone { get; private set; }
    public bool IsChamberCompleted { get; private set; }
    public int Score { get; private set; }
    public int NotificationTimer { get; private set; }
    public string NotificationMessage { get; private set; } = "";

    public PuzzleManager()
    {
        Treasure = new Treasure(TreasureCoord);
        Treasure2 = new Treasure(Treasure2Coord);
        Key = new Key(KeyCoord);
        Trap = new FloorTrap(TrapCoord);
    }

    public void ResetScore() => Score = 0;

    public void ResetTreasures()
    {
        Treasure.Reset();
        Treasure2.Reset();
    }

    /// <summary>
    /// Configura el puzle en la cuadrícula de la cámara.
    /// Crea el dintel de muro impenetrable en fila 10 a 12, el muro conmutable en fila 13 y la losa rúnica en col 8.
    /// Adapta dinámicamente las coordenadas según la recámara activa.
    /// </summary>
    public void Initialize(RoomGrid grid, int chamberNumber = 1)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (chamberNumber == 2)
        {
            StoneCoord = new(8, 13);
            WallCoord = new(14, 13);
            TreasureCoord = new(17, 13);
            Treasure2Coord = new(8, 5);
            ExitDoorCoord = new(1, 5);
            KeyCoord = new(16, 7);
            TrapCoord = new(16, 10);
        }
        else
        {
            StoneCoord = new(8, 13);
            WallCoord = new(14, 13);
            TreasureCoord = new(17, 13);
            Treasure2Coord = new(8, 4);
            ExitDoorCoord = new(1, 4);
            KeyCoord = new(16, 6);
            TrapCoord = new(16, 9);
        }

        Treasure.Configure(TreasureCoord);
        Treasure2.Configure(Treasure2Coord);
        Key.Configure(KeyCoord);
        Trap.Configure(TrapCoord);

        // 1. Dintel superior sobre la puerta secreta para evitar saltar por encima
        grid.SetTile(WallCoord.X, 10, TileType.SolidWall);
        grid.SetTile(WallCoord.X, 11, TileType.SolidWall);
        grid.SetTile(WallCoord.X, 12, TileType.SolidWall);

        // 2. Muro conmutable inicialmente cerrado
        grid.SetTile(WallCoord.X, WallCoord.Y, TileType.SolidWall);

        // 3. Piedra de activación en el suelo
        grid.SetTile(StoneCoord.X, StoneCoord.Y, TileType.PressurePlate);

        // 4. Trampa de suelo bajo la llave inicialmente cerrada
        grid.SetTile(TrapCoord.X, TrapCoord.Y, TileType.SolidWall);

        // 5. Puerta de salida
        grid.SetTile(ExitDoorCoord.X, ExitDoorCoord.Y, TileType.ExitDoor);

        // 6. Reinicio de variables de estado
        IsWallOpen = false;
        WasPlayerOnStone = false;
        NotificationTimer = 0;
        NotificationMessage = "";
        IsChamberCompleted = false;
        Treasure.Reset();
        Treasure2.Reset();
        Key.Reset();
        Trap.Reset();
    }

    /// <summary>
    /// Actualiza la lógica de colisión con la piedra interruptora y la recolección del tesoro.
    /// </summary>
    public void Update(RoomGrid grid, Player player)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(player);

        // 1. Detección AABB de contacto entre el arqueólogo y la piedra rúnica
        float stoneLeft = StoneCoord.X * GameConstants.TILE_SIZE;
        float stoneRight = stoneLeft + GameConstants.TILE_SIZE;
        float stoneTop = StoneCoord.Y * GameConstants.TILE_SIZE;
        float stoneBottom = stoneTop + GameConstants.TILE_SIZE;

        float playerLeft = player.Position.X;
        float playerRight = player.Position.X + Player.WIDTH;
        float playerTop = player.Position.Y;
        float playerBottom = player.Position.Y + Player.HEIGHT;

        bool isPlayerOnStone = playerRight > stoneLeft + 2f &&
                               playerLeft < stoneRight - 2f &&
                               playerBottom > stoneTop &&
                               playerTop < stoneBottom;

        // 2. Mecánica de conmutación: al pisar la piedra por primera vez (o tras salir y volver a entrar)
        if (isPlayerOnStone && !WasPlayerOnStone)
        {
            IsWallOpen = !IsWallOpen;

            // Si está abierto, la celda se vuelve transitable (Empty); si está cerrado, vuelve a ser SolidWall
            grid.SetTile(WallCoord.X, WallCoord.Y, IsWallOpen ? TileType.Empty : TileType.SolidWall);
        }

        WasPlayerOnStone = isPlayerOnStone;

        // 3. Detección de recolección del tesoro de la Plataforma 0
        if (!Treasure.IsCollected)
        {
            float treasureLeft = TreasureCoord.X * GameConstants.TILE_SIZE;
            float treasureRight = treasureLeft + GameConstants.TILE_SIZE;
            float treasureTop = TreasureCoord.Y * GameConstants.TILE_SIZE;
            float treasureBottom = treasureTop + GameConstants.TILE_SIZE;

            bool isPlayerOnTreasure = playerRight > treasureLeft + 2f &&
                                      playerLeft < treasureRight - 2f &&
                                      playerBottom > treasureTop &&
                                      playerTop < treasureBottom;

            if (isPlayerOnTreasure)
            {
                Treasure.Collect();
                Score += 1000;
                NotificationTimer = 180; // 3 segundos a 60 FPS
                NotificationMessage = MSG_TREASURE;
            }
        }

        // 4. Detección de recolección del segundo tesoro (Plataforma Nivel 2 a la izquierda)
        if (!Treasure2.IsCollected)
        {
            float treasure2Left = Treasure2Coord.X * GameConstants.TILE_SIZE;
            float treasure2Right = treasure2Left + GameConstants.TILE_SIZE;
            float treasure2Top = Treasure2Coord.Y * GameConstants.TILE_SIZE;
            float treasure2Bottom = treasure2Top + GameConstants.TILE_SIZE;

            bool isPlayerOnTreasure2 = playerRight > treasure2Left + 2f &&
                                       playerLeft < treasure2Right - 2f &&
                                       playerBottom > treasure2Top &&
                                       playerTop < treasure2Bottom;

            if (isPlayerOnTreasure2)
            {
                Treasure2.Collect();
                Score += 1000;
                NotificationTimer = 180; // 3 segundos a 60 FPS
                NotificationMessage = MSG_TREASURE;
            }
        }

        // 5. Detección de recolección de la llave (Plataforma 1 a la derecha, colgada)
        if (!Key.IsCollected)
        {
            float keyLeft = KeyCoord.X * GameConstants.TILE_SIZE;
            float keyRight = keyLeft + GameConstants.TILE_SIZE;
            float keyTop = KeyCoord.Y * GameConstants.TILE_SIZE;
            float keyBottom = keyTop + GameConstants.TILE_SIZE;

            bool isPlayerOnKey = playerRight > keyLeft + 2f &&
                                 playerLeft < keyRight - 2f &&
                                 playerBottom > keyTop &&
                                 playerTop < keyBottom;

            if (isPlayerOnKey)
            {
                Key.Collect();
                Score += 500;
                NotificationTimer = 180;
                NotificationMessage = MSG_KEY;
            }
        }

        // 6. Detección de llegada a la puerta de salida (Plataforma 2 a la izquierda)
        float doorLeft = ExitDoorCoord.X * GameConstants.TILE_SIZE;
        float doorRight = doorLeft + GameConstants.TILE_SIZE;
        float doorTop = ExitDoorCoord.Y * GameConstants.TILE_SIZE;
        float doorBottom = doorTop + GameConstants.TILE_SIZE;

        bool isPlayerOnDoor = playerRight > doorLeft + 2f &&
                              playerLeft < doorRight - 2f &&
                              playerBottom > doorTop &&
                              playerTop < doorBottom;

        if (isPlayerOnDoor)
        {
            if (HasKey)
            {
                IsChamberCompleted = true;
            }
            else
            {
                NotificationTimer = 90;
                NotificationMessage = MSG_DOOR_LOCKED;
            }
        }

        // 7. Detección de activación de la trampa en el piso bajo la llave
        if (!Trap.IsOpen)
        {
            float trapLeft = TrapCoord.X * GameConstants.TILE_SIZE;
            float trapRight = trapLeft + GameConstants.TILE_SIZE;

            // El jugador está horizontalmente debajo de la llave (columna 16)
            bool isUnderKey = playerRight > trapLeft + 2f && playerLeft < trapRight - 2f;
            // El jugador está caminando o apoyado sobre el piso de la plataforma 1
            // y no está en el aire sobrevolando en salto
            float platformPlayerY = (TrapCoord.Y - 1) * GameConstants.TILE_SIZE;
            bool isGroundedOnPlatform1 = MathF.Abs(player.Position.Y - platformPlayerY) <= 4f &&
                                         player.State != PlayerState.Jumping;

            if (isUnderKey && isGroundedOnPlatform1)
            {
                Trap.Open();
                grid.SetTile(TrapCoord.X, TrapCoord.Y, TileType.Empty);

                // Abre el muro secreto en el piso inferior para permitir salir al jugador
                if (!IsWallOpen)
                {
                    IsWallOpen = true;
                    grid.SetTile(WallCoord.X, WallCoord.Y, TileType.Empty);
                }

                player.ForceFall(grid);

                NotificationTimer = 120;
                NotificationMessage = MSG_TRAP;
            }
        }

        if (NotificationTimer > 0)
        {
            NotificationTimer--;
            if (NotificationTimer == 0)
            {
                NotificationMessage = "";
            }
        }
    }
}
