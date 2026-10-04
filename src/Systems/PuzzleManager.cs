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
    public GridCoord Treasure3Coord { get; private set; } = new(17, 13);
    public GridCoord Treasure4Coord { get; private set; } = new(17, 13);
    public GridCoord ExitDoorCoord { get; private set; } = new(1, 4);
    public GridCoord KeyCoord { get; private set; } = new(16, 6);
    public GridCoord TrapCoord { get; private set; } = new(16, 9);
    public GridCoord Trap2Coord { get; private set; } = new(13, 5);

    public const string MSG_TREASURE = "!TESORO ENCONTRADO! +1000 PTS";
    public const string MSG_TREASURE_RELOAD = "!TESORO RECARGADO! +1000 PTS";
    public const string MSG_TREASURE3_SPAWN = "!MOMIA PETRIFICADA! TESORO EN CAMARA";
    public const string MSG_TREASURE3 = "!TESORO ANCESTRAL REVELADO! +1000 PTS";
    public const string MSG_MUMMY_CHASE = "!MOMIA ENFURECIDA TE PERSIGUE!";
    public const string MSG_MUMMY_TRAPPED_1 = "!MOMIA ATRAPADA! SE REGENERA (1/3)";
    public const string MSG_MUMMY_TRAPPED_2 = "!MOMIA ATRAPADA! SE REGENERA (2/3)";
    public const string MSG_MUMMY_DEFEATED = "!MOMIA DESTRUIDA! NUEVO TESORO";
    public const string MSG_TREASURE4 = "!TESORO SAGRADO LIBERADO! +1000 PTS";
    public const string MSG_KEY = "!LLAVE ENCONTRADA! +500 PTS";
    public const string MSG_DOOR_LOCKED = "!PUERTA CERRADA! NECESITAS LA LLAVE";
    public const string MSG_TRAP = "!TRAMPA! EL PISO SE HA ABIERTO";
    public const string MSG_TRAP2 = "!TRAMPA MORTAL! QUEDAS ATRAPADO";

    public Treasure Treasure { get; }
    public Treasure Treasure2 { get; }
    public Treasure Treasure3 { get; }
    public Treasure Treasure4 { get; }
    public Key Key { get; }
    public FloorTrap Trap { get; }
    public FloorTrap Trap2 { get; }
    public bool HasKey => Key.IsCollected;
    public bool IsTrapOpen => Trap.IsOpen;
    public bool IsTrap2Open => Trap2.IsOpen;
    public bool HasTrap2 { get; private set; } = true;
    public bool IsTrap2Triggered => IsPlayerTrappedInTrap2;
    public bool IsPlayerTrappedInTrap2 { get; private set; }
    public bool HasTrap2Eliminated { get; private set; }
    public int Trap2OpenTimer { get; private set; }
    public int Mummy2TrapFallCount { get; private set; }
    public bool IsMummy2FallingInTrap { get; private set; }
    public bool IsTreasure4Spawned { get; private set; }
    public bool IsTreasure4Pending { get; private set; }
    public bool IsWallOpen { get; private set; }
    public bool WasPlayerOnStone { get; private set; }
    public bool IsChamberCompleted { get; private set; }
    public bool IsTreasure3Spawned { get; private set; }
    public int CurrentChamber { get; private set; } = 1;
    public int DoorCycleCount { get; private set; }
    public bool HasSecretTreasureReloaded { get; private set; }
    public int Score { get; private set; }
    public int NotificationTimer { get; private set; }
    public string NotificationMessage { get; private set; } = "";

    public PuzzleManager()
    {
        Treasure = new Treasure(TreasureCoord);
        Treasure2 = new Treasure(Treasure2Coord);
        Treasure3 = new Treasure(Treasure3Coord);
        Treasure4 = new Treasure(Treasure4Coord);
        Key = new Key(KeyCoord);
        Trap = new FloorTrap(TrapCoord);
        Trap2 = new FloorTrap(Trap2Coord);
    }

    public void ResetScore() => Score = 0;

    public void ResetTreasures()
    {
        Treasure.Reset();
        Treasure2.Reset();
        Treasure3.Reset();
        Treasure4.Reset();
    }

    public void SpawnTreasure3(RoomGrid? grid = null)
    {
        IsTreasure3Spawned = true;
        Treasure3.Reset();
        if (grid != null && !IsWallOpen)
        {
            IsWallOpen = true;
            grid.SetTile(WallCoord.X, WallCoord.Y, TileType.Empty);
        }
        NotificationTimer = 180;
        NotificationMessage = MSG_TREASURE3_SPAWN;
    }

    public void SpawnTreasure4(RoomGrid? grid = null)
    {
        IsTreasure4Spawned = true;
        IsTreasure4Pending = false;
        Treasure4.Reset();
        if (grid != null && !IsWallOpen)
        {
            IsWallOpen = true;
            grid.SetTile(WallCoord.X, WallCoord.Y, TileType.Empty);
        }
        NotificationTimer = 180;
        NotificationMessage = MSG_MUMMY_DEFEATED;
    }

    public void NotifyMummyChase()
    {
        NotificationTimer = 120;
        NotificationMessage = MSG_MUMMY_CHASE;
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
            Treasure3Coord = new(17, 13);
            Treasure4Coord = new(17, 13);
            ExitDoorCoord = new(1, 5);
            KeyCoord = new(16, 7);
            TrapCoord = new(16, 10);
            HasTrap2 = false;
        }
        else
        {
            StoneCoord = new(8, 13);
            WallCoord = new(14, 13);
            TreasureCoord = new(17, 13);
            Treasure2Coord = new(8, 4);
            Treasure3Coord = new(17, 13);
            Treasure4Coord = new(17, 13);
            ExitDoorCoord = new(1, 4);
            KeyCoord = new(16, 6);
            TrapCoord = new(16, 9);
            Trap2Coord = new(13, 5);
            HasTrap2 = true;
            Trap2.Configure(Trap2Coord);
            grid.SetTile(Trap2Coord.X, Trap2Coord.Y, TileType.SolidWall);
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
        CurrentChamber = chamberNumber;
        DoorCycleCount = 0;
        HasSecretTreasureReloaded = false;
        IsWallOpen = false;
        WasPlayerOnStone = false;
        NotificationTimer = 0;
        NotificationMessage = "";
        IsChamberCompleted = false;
        IsTreasure3Spawned = false;
        IsTreasure4Spawned = false;
        IsTreasure4Pending = false;
        Mummy2TrapFallCount = 0;
        IsMummy2FallingInTrap = false;
        Trap2OpenTimer = 0;
        IsPlayerTrappedInTrap2 = false;
        HasTrap2Eliminated = false;
        Treasure.Reset();
        Treasure2.Reset();
        Treasure3.Reset();
        Treasure4.Reset();
        Key.Reset();
        Trap.Reset();
        Trap2.Reset();
        Treasure3.Configure(Treasure3Coord);
        Treasure4.Configure(Treasure4Coord);
    }

    /// <summary>
    /// Actualiza la lógica de colisión con la piedra interruptora y la recolección del tesoro.
    /// </summary>
    public void Update(RoomGrid grid, Player player, Mummy? mummy = null)
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

            // Puzle 1 (Recámara 1):
            // Requisitos:
            // 1. Tener recogidos los 2 tesoros (Nivel 0 y Nivel 2) y la llave (Nivel 1).
            // 2. Pasar por la losa para abrir y cerrar la puerta del nivel 0 tres (3) veces consecutivas.
            // 3. Activación única: solo se puede hacer una vez, no se repite nuevamente.
            if (CurrentChamber == 1 && !HasSecretTreasureReloaded)
            {
                bool arePrerequisitesMet = Treasure.IsCollected && Treasure2.IsCollected && Key.IsCollected;

                if (!IsWallOpen)
                {
                    // La puerta acaba de cerrarse completando un ciclo de conmutación
                    if (arePrerequisitesMet)
                    {
                        DoorCycleCount++;
                        if (DoorCycleCount >= 3)
                        {
                            Treasure.Reset();
                            HasSecretTreasureReloaded = true;
                            DoorCycleCount = 0;
                            NotificationTimer = 180;
                            NotificationMessage = MSG_TREASURE_RELOAD;
                        }
                    }
                    else
                    {
                        DoorCycleCount = 0;
                    }
                }
            }
        }

        WasPlayerOnStone = isPlayerOnStone;

        // 3. Detección de recolección del tesoro de la Plataforma 0
        bool collectedTreasureThisFrame = false;
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
                collectedTreasureThisFrame = true;
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

        // 4b. Detección de recolección del tercer tesoro (Puzle 5: revelado por la momia en la cámara de Nivel 0)
        if (IsTreasure3Spawned && !Treasure3.IsCollected && !collectedTreasureThisFrame)
        {
            float treasure3Left = Treasure3Coord.X * GameConstants.TILE_SIZE;
            float treasure3Right = treasure3Left + GameConstants.TILE_SIZE;
            float treasure3Top = Treasure3Coord.Y * GameConstants.TILE_SIZE;
            float treasure3Bottom = treasure3Top + GameConstants.TILE_SIZE;

            bool isPlayerOnTreasure3 = playerRight > treasure3Left + 2f &&
                                       playerLeft < treasure3Right - 2f &&
                                       playerBottom > treasure3Top &&
                                       playerTop < treasure3Bottom;

            if (isPlayerOnTreasure3)
            {
                Treasure3.Collect();
                Score += 1000;
                NotificationTimer = 180;
                NotificationMessage = MSG_TREASURE3;
                collectedTreasureThisFrame = true;
            }
        }

        // 4c. Detección de recolección del cuarto tesoro (Puzle 6: liberado tras derrotar a la momia de Nivel 2)
        if (IsTreasure4Spawned && !Treasure4.IsCollected && !collectedTreasureThisFrame)
        {
            float treasure4Left = Treasure4Coord.X * GameConstants.TILE_SIZE;
            float treasure4Right = treasure4Left + GameConstants.TILE_SIZE;
            float treasure4Top = Treasure4Coord.Y * GameConstants.TILE_SIZE;
            float treasure4Bottom = treasure4Top + GameConstants.TILE_SIZE;

            bool isPlayerOnTreasure4 = playerRight > treasure4Left + 2f &&
                                       playerLeft < treasure4Right - 2f &&
                                       playerBottom > treasure4Top &&
                                       playerTop < treasure4Bottom;

            if (isPlayerOnTreasure4)
            {
                Treasure4.Collect();
                Score += 1000;
                NotificationTimer = 180;
                NotificationMessage = MSG_TREASURE4;
                collectedTreasureThisFrame = true;
            }
        }

        // Si Treasure4 estaba pendiente a que se recolecte el tesoro de la cámara
        if (IsTreasure4Pending)
        {
            bool chamberTreasureCollected = Treasure.IsCollected && (!IsTreasure3Spawned || Treasure3.IsCollected);
            if (chamberTreasureCollected)
            {
                IsTreasure4Pending = false;
                IsTreasure4Spawned = true;
                Treasure4.Reset();
                if (!IsWallOpen)
                {
                    IsWallOpen = true;
                    grid.SetTile(WallCoord.X, WallCoord.Y, TileType.Empty);
                }
                NotificationTimer = 180;
                NotificationMessage = MSG_MUMMY_DEFEATED;
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

        // 8. Detección y ciclo de la trampa mortal de suelo en Nivel 2 (columna 13, fila 5)
        if (HasTrap2)
        {
            float trap2Left = Trap2Coord.X * GameConstants.TILE_SIZE;
            float trap2Right = trap2Left + GameConstants.TILE_SIZE;
            bool isOverTrap2 = playerRight > trap2Left + 2f && playerLeft < trap2Right - 2f;
            float platform2PlayerY = (Trap2Coord.Y - 1) * GameConstants.TILE_SIZE;

            // El jugador está a la altura de la plataforma de Nivel 2 (caminando en Y=64 o saltando en el aire sobre ella)
            bool isPlayerAtLevel2Height = player.Position.Y <= platform2PlayerY + 4f &&
                                          player.Position.Y >= platform2PlayerY - Player.JUMP_HEIGHT_PIXELS - 4f;

            // Apertura de la trampa al pasar por arriba (caminando o saltando)
            if (isOverTrap2 && isPlayerAtLevel2Height)
            {
                if (!Trap2.IsOpen)
                {
                    Trap2.Open();
                    grid.SetTile(Trap2Coord.X, Trap2Coord.Y, TileType.Empty);
                    Trap2OpenTimer = 120; // 2 segundos abierta

                    if (player.State != PlayerState.Jumping)
                    {
                        // Si pasa caminando sobre la trampa, cae
                        player.ForceFall(grid);
                        IsPlayerTrappedInTrap2 = true;
                        NotificationTimer = 120;
                        NotificationMessage = MSG_TRAP2;
                    }
                }
                else
                {
                    // Si ya estaba abierta y el jugador entra caminando, cae
                    if (player.State != PlayerState.Jumping && !IsPlayerTrappedInTrap2)
                    {
                        player.ForceFall(grid);
                        IsPlayerTrappedInTrap2 = true;
                        NotificationTimer = 120;
                        NotificationMessage = MSG_TRAP2;
                    }
                }
            }

            // Temporizador de cierre automático de la trampilla
            if (Trap2.IsOpen)
            {
                if (Trap2OpenTimer > 0)
                {
                    Trap2OpenTimer--;
                    if (Trap2OpenTimer == 0)
                    {
                        Trap2.Close();
                        grid.SetTile(Trap2Coord.X, Trap2Coord.Y, TileType.SolidWall);
                    }
                }
            }

            // Si el jugador cayó por la trampa:
            if (IsPlayerTrappedInTrap2)
            {
                // Cierre a las espaldas cuando pasa Y >= 96
                if (Trap2.IsOpen && player.Position.Y >= (Trap2Coord.Y + 1) * GameConstants.TILE_SIZE)
                {
                    Trap2.Close();
                    grid.SetTile(Trap2Coord.X, Trap2Coord.Y, TileType.SolidWall);
                    Trap2OpenTimer = 0;
                }

                if (!HasTrap2Eliminated && (player.Position.Y >= 13 * GameConstants.TILE_SIZE || player.IsEliminated))
                {
                    HasTrap2Eliminated = true;
                    if (!player.IsEliminated)
                    {
                        player.Eliminate();
                    }
                    NotificationTimer = 120;
                    NotificationMessage = MSG_TRAP2;
                }
            }

            // 8b. Caída de la momia de Nivel 2 en la trampa abierta
            if (mummy != null && mummy.IsActive && CurrentChamber == 1)
            {
                float mummyLeft = mummy.Position.X;
                float mummyRight = mummy.Position.X + Mummy.WIDTH;
                bool isMummyOverTrap2 = mummyRight > trap2Left + 2f && mummyLeft < trap2Right - 2f;
                bool isMummyOnLevel2 = MathF.Abs(mummy.Position.Y - platform2PlayerY) <= 4f;

                if (!mummy.IsFalling && Trap2.IsOpen && isMummyOverTrap2 && isMummyOnLevel2)
                {
                    mummy.SetFalling(true);
                    IsMummy2FallingInTrap = true;
                }

                if (IsMummy2FallingInTrap && mummy.Position.Y >= 13f * GameConstants.TILE_SIZE)
                {
                    IsMummy2FallingInTrap = false;
                    Mummy2TrapFallCount++;

                    if (Mummy2TrapFallCount < 3)
                    {
                        mummy.Reset();
                        if (Mummy2TrapFallCount == 1)
                        {
                            NotificationTimer = 120;
                            NotificationMessage = MSG_MUMMY_TRAPPED_1;
                        }
                        else if (Mummy2TrapFallCount == 2)
                        {
                            NotificationTimer = 120;
                            NotificationMessage = MSG_MUMMY_TRAPPED_2;
                        }
                    }
                    else
                    {
                        mummy.IsActive = false;
                        mummy.SetFalling(false);
                        NotificationTimer = 180;
                        NotificationMessage = MSG_MUMMY_DEFEATED;

                        // Comprobar si el tesoro de la cámara ya fue recogido
                        bool chamberTreasureCollected = Treasure.IsCollected && (!IsTreasure3Spawned || Treasure3.IsCollected);
                        if (chamberTreasureCollected)
                        {
                            IsTreasure4Spawned = true;
                            IsTreasure4Pending = false;
                            Treasure4.Reset();
                            if (!IsWallOpen)
                            {
                                IsWallOpen = true;
                                grid.SetTile(WallCoord.X, WallCoord.Y, TileType.Empty);
                            }
                        }
                        else
                        {
                            IsTreasure4Pending = true;
                            IsTreasure4Spawned = false;
                        }
                    }
                }
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

