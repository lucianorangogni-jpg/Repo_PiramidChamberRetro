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
    public GridCoord PlayerSpawnCoord { get; private set; } = new(2, 13);
    public GridCoord StoneCoord { get; private set; } = new(8, 13);
    public GridCoord WallCoord { get; private set; } = new(14, 13);
    public GridCoord Stone2Coord { get; private set; } = new(7, 10);
    public GridCoord Wall2Coord { get; private set; } = new(4, 10);
    public GridCoord Stone3Coord { get; private set; } = new(12, 7);
    public GridCoord Wall3Coord { get; private set; } = new(14, 7);
    public GridCoord TreasureCoord { get; private set; } = new(17, 13);
    public GridCoord Treasure2Coord { get; private set; } = new(8, 4);
    public GridCoord Treasure3Coord { get; private set; } = new(17, 13);
    public GridCoord Treasure4Coord { get; private set; } = new(17, 13);
    public GridCoord Treasure5Coord { get; private set; } = new(11, 3);
    public GridCoord Treasure6Coord { get; private set; } = new(11, 3);
    public GridCoord ExitDoorCoord { get; private set; } = new(18, 4);
    public GridCoord KeyCoord { get; private set; } = new(16, 6);
    public GridCoord TrapCoord { get; private set; } = new(16, 9);
    public GridCoord Trap2Coord { get; private set; } = new(13, 5);
    public GridCoord Trap3Coord { get; private set; } = new(9, 4);

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
    public const string MSG_CHAMBER10_LOCKED = "!CUSPIDE BLOQUEADA! COMPLETA PUZZLES 1-9";

    private readonly bool[] _chamberPuzzlesCompleted = new bool[GameConstants.TOTAL_CHAMBERS + 1];

    public Treasure Treasure { get; }
    public Treasure Treasure2 { get; }
    public Treasure Treasure3 { get; }
    public Treasure Treasure4 { get; }
    public Treasure Treasure5 { get; }
    public Treasure Treasure6 { get; }
    public bool HasExtraTreasure { get; private set; }
    public Key Key { get; }
    public FloorTrap Trap { get; }
    public FloorTrap Trap2 { get; }
    public FloorTrap Trap3 { get; }
    public bool HasKey => Key.IsCollected;
    public bool IsTrapOpen => Trap.IsOpen;
    public bool IsPlayerTrappedInTrap { get; private set; }
    public bool HasTrapEliminated { get; private set; }
    private bool _suppressTrap1FromUpperFall;
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
    public bool HasTrap3 { get; private set; }
    public bool IsTrap3Open => Trap3.IsOpen;
    public bool IsTrap3Triggered => IsPlayerTrappedInTrap3;
    public bool IsPlayerTrappedInTrap3 { get; private set; }
    public bool HasTrap3Eliminated { get; private set; }
    public int Trap3OpenTimer { get; private set; }
    public int Mummy3TrapFallCount { get; private set; }
    public bool IsMummy3FallingInTrap { get; private set; }
    public bool IsTreasure6Spawned { get; private set; }
    public bool HasJumpedOverLevel3Mummy { get; private set; }
    public int Level3MummyJumpCount { get; private set; }
    private bool _wasJumpingOverMummy3;
    public bool IsWallOpen { get; private set; }
    public bool WasPlayerOnStone { get; private set; }
    public bool HasMultipleTreasureDoors { get; private set; }
    public bool IsWall2Open { get; private set; }
    public bool WasPlayerOnStone2 { get; private set; }
    public bool IsWall3Open { get; private set; }
    public bool WasPlayerOnStone3 { get; private set; }
    public bool IsChamberCompleted { get; private set; }
    public bool IsTreasure3Spawned { get; private set; }
    public int CurrentChamber { get; private set; } = 1;
    public int PyramidLevel => RoomGrid.GetPyramidLevel(CurrentChamber);
    public int DoorCycleCount { get; private set; }
    public bool HasSecretTreasureReloaded { get; private set; }
    public int Chamber2Puzzle1Stage { get; private set; }
    public bool Chamber3KeyCollectedFirst { get; private set; }
    public int Chamber4MummyJumpCount { get; private set; }
    public bool HasJumpedOverLevel1Mummy { get; private set; }
    public bool HasJumpedOverLevel2Mummy { get; private set; }
    public int Level2MummyJumpCount { get; private set; }
    private bool _wasJumpingOverMummy2;
    public int Score { get; private set; }
    public bool JustCollectedTreasure { get; private set; }
    public int NotificationTimer { get; private set; }
    public string NotificationMessage { get; private set; } = "";

    /// <summary>
    /// Indica si todos los puzles de la recámara actual han sido completados:
    /// Puzle 1 (recarga secreta), Puzle 2 (momia petrificada), Puzle 3 (momia derrotada en trampa)
    /// y, en caso de existir trampa en Nivel 3 (Recámara 5), el Puzle de Nivel 3.
    /// </summary>
    public bool AreAllChamberPuzzlesCompleted =>
        HasSecretTreasureReloaded &&
        IsTreasure3Spawned &&
        (IsTreasure4Spawned || IsTreasure4Pending) &&
        (!HasTrap3 || IsTreasure6Spawned);

    /// <summary>
    /// Indica cuántas recámaras de la 1 a la 9 tienen todos sus puzles completados.
    /// </summary>
    public int CompletedChambersCount
    {
        get
        {
            int count = 0;
            for (int c = 1; c < GameConstants.TOTAL_CHAMBERS; c++)
            {
                if (_chamberPuzzlesCompleted[c])
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// La Recámara 10 (Cúspide) solo se desbloquea cuando se han completado todos los puzles de las Recámaras 1 a 9.
    /// </summary>
    public bool IsChamber10Unlocked => CompletedChambersCount == GameConstants.TOTAL_CHAMBERS - 1;

    public bool IsChamberPuzzlesCompleted(int chamberNumber) =>
        chamberNumber >= 1 && chamberNumber <= GameConstants.TOTAL_CHAMBERS && _chamberPuzzlesCompleted[chamberNumber];

    public void SetChamberPuzzlesCompleted(int chamberNumber, bool completed = true)
    {
        if (chamberNumber >= 1 && chamberNumber <= GameConstants.TOTAL_CHAMBERS)
        {
            _chamberPuzzlesCompleted[chamberNumber] = completed;
        }
    }

    public void ResetChamberPuzzlesProgress()
    {
        Array.Clear(_chamberPuzzlesCompleted, 0, _chamberPuzzlesCompleted.Length);
    }

    public bool CanAccessChamber(int chamberNumber)
    {
        if (chamberNumber < 1 || chamberNumber > GameConstants.TOTAL_CHAMBERS)
            return false;
        if (chamberNumber == GameConstants.TOTAL_CHAMBERS)
            return IsChamber10Unlocked;
        return true;
    }

    public int GetFirstIncompleteChamber()
    {
        for (int c = 1; c < GameConstants.TOTAL_CHAMBERS; c++)
        {
            if (!_chamberPuzzlesCompleted[c])
            {
                return c;
            }
        }
        return GameConstants.TOTAL_CHAMBERS;
    }

    public void NotifyChamber10Locked()
    {
        NotificationTimer = 180;
        NotificationMessage = MSG_CHAMBER10_LOCKED;
    }

    private void UpdateChamberCompletionStatus()
    {
        if (CurrentChamber >= 1 && CurrentChamber <= GameConstants.TOTAL_CHAMBERS && AreAllChamberPuzzlesCompleted)
        {
            _chamberPuzzlesCompleted[CurrentChamber] = true;
        }
    }

    public PuzzleManager()
    {
        Treasure = new Treasure(TreasureCoord);
        Treasure2 = new Treasure(Treasure2Coord);
        Treasure3 = new Treasure(Treasure3Coord);
        Treasure4 = new Treasure(Treasure4Coord);
        Treasure5 = new Treasure(Treasure5Coord);
        Treasure6 = new Treasure(Treasure6Coord);
        Key = new Key(KeyCoord);
        Trap = new FloorTrap(TrapCoord);
        Trap2 = new FloorTrap(Trap2Coord);
        Trap3 = new FloorTrap(Trap3Coord);
    }

    public void ResetScore() => Score = 0;

    public void ResetTreasures()
    {
        Treasure.Reset();
        Treasure2.Reset();
        Treasure3.Reset();
        Treasure4.Reset();
        Treasure5.Reset();
        Treasure6.Reset();
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
        UpdateChamberCompletionStatus();
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
        UpdateChamberCompletionStatus();
    }

    public void NotifyMummyChase()
    {
        NotificationTimer = 120;
        NotificationMessage = MSG_MUMMY_CHASE;
    }

    public void RegisterLevel1MummyJump()
    {
        HasJumpedOverLevel1Mummy = true;
        Chamber4MummyJumpCount++;
        if (CurrentChamber == 2 && Chamber2Puzzle1Stage == 2 && Treasure.IsCollected && Key.IsCollected)
        {
            Chamber2Puzzle1Stage = 3;
        }
    }

    public void RegisterLevel2MummyJump()
    {
        Chamber4MummyJumpCount++;
        if (CurrentChamber == 2)
        {
            if (HasSecretTreasureReloaded && IsTreasure3Spawned)
            {
                HasJumpedOverLevel2Mummy = true;
                Level2MummyJumpCount++;
            }
        }
        else if (CurrentChamber == 3)
        {
            if (Treasure2.IsCollected)
            {
                Level2MummyJumpCount++;
                if (Level2MummyJumpCount >= 3)
                {
                    HasJumpedOverLevel2Mummy = true;
                }
            }
        }
        else if (CurrentChamber == 4)
        {
            if (Key.IsCollected)
            {
                Level2MummyJumpCount++;
                if (Level2MummyJumpCount >= 2)
                {
                    HasJumpedOverLevel2Mummy = true;
                }
            }
        }
        else
        {
            HasJumpedOverLevel2Mummy = true;
            Level2MummyJumpCount++;
        }
    }

    public void RegisterLevel3MummyJump()
    {
        if (Treasure5.IsCollected)
        {
            HasJumpedOverLevel3Mummy = true;
            Level3MummyJumpCount++;
        }
    }

    public void SpawnTreasure6()
    {
        IsTreasure6Spawned = true;
        Treasure6.Reset();
        NotificationTimer = 180;
        NotificationMessage = MSG_MUMMY_DEFEATED;
        UpdateChamberCompletionStatus();
    }

    /// <summary>
    /// Configura el puzle en la cuadrícula de la recámara (1 a 10).
    /// Crea el dintel de muro impenetrable en fila 10 a 12, el muro conmutable en fila 13 y la losa rúnica.
    /// Adapta dinámicamente las coordenadas según la recámara activa de la pirámide.
    /// </summary>
    public void Initialize(RoomGrid grid, int chamberNumber = 1)
    {
        ArgumentNullException.ThrowIfNull(grid);

        int normalized = (chamberNumber >= 1 && chamberNumber <= GameConstants.TOTAL_CHAMBERS) ? chamberNumber : 1;
        PlayerSpawnCoord = new(2, 13);
        WallCoord = new(14, 13);
        TreasureCoord = new(17, 13);
        Treasure3Coord = new(17, 13);
        Treasure4Coord = new(17, 13);
        Treasure5Coord = new(11, 3);
        Treasure6Coord = new(11, 3);
        Trap3Coord = new(9, 4);
        Stone2Coord = new(7, 10);
        Wall2Coord = new(4, 10);
        Stone3Coord = new(12, 7);
        Wall3Coord = new(14, 7);
        HasExtraTreasure = normalized is >= 5 and <= 9;
        HasMultipleTreasureDoors = normalized is 8 or 9;
        HasTrap2 = true;
        HasTrap3 = normalized == 5;

        switch (normalized)
        {
            case 1:
                StoneCoord = new(8, 13);
                Treasure2Coord = new(8, 4);
                ExitDoorCoord = new(18, 4);
                KeyCoord = new(16, 6);
                TrapCoord = new(16, 9);
                Trap2Coord = new(13, 5);
                break;
            case 2:
                StoneCoord = new(8, 13);
                Treasure2Coord = new(8, 5);
                ExitDoorCoord = new(1, 5);
                KeyCoord = new(16, 7);
                TrapCoord = new(16, 10);
                Trap2Coord = new(11, 6);
                break;
            case 3:
                StoneCoord = new(7, 13);
                Treasure2Coord = new(6, 4);
                ExitDoorCoord = new(18, 4);
                KeyCoord = new(15, 6);
                TrapCoord = new(15, 9);
                Trap2Coord = new(12, 5);
                break;
            case 4:
                StoneCoord = new(9, 13);
                Treasure2Coord = new(7, 5);
                ExitDoorCoord = new(1, 5);
                KeyCoord = new(15, 7);
                TrapCoord = new(15, 10);
                Trap2Coord = new(10, 6);
                break;
            case 5:
                StoneCoord = new(8, 13);
                WallCoord = new(14, 10);
                TreasureCoord = new(17, 10);
                Treasure3Coord = new(17, 10);
                Treasure4Coord = new(17, 10);
                Treasure2Coord = new(6, 7);
                Treasure5Coord = new(11, 3);
                Treasure6Coord = new(11, 3);
                ExitDoorCoord = new(18, 3);
                KeyCoord = new(11, 9);
                TrapCoord = new(11, 11);
                Trap2Coord = new(13, 8);
                Trap3Coord = new(9, 4);
                break;
            case 6:
                StoneCoord = new(7, 13);
                Treasure2Coord = new(8, 7);
                Treasure5Coord = new(12, 3);
                ExitDoorCoord = new(1, 3);
                KeyCoord = new(16, 9);
                TrapCoord = new(16, 11);
                Trap2Coord = new(13, 8);
                break;
            case 7:
                StoneCoord = new(9, 13);
                Treasure2Coord = new(7, 7);
                Treasure5Coord = new(10, 3);
                ExitDoorCoord = new(18, 3);
                KeyCoord = new(15, 9);
                TrapCoord = new(15, 11);
                Trap2Coord = new(12, 8);
                break;
            case 8:
                StoneCoord = new(8, 13);
                Stone2Coord = new(7, 10);
                Wall2Coord = new(4, 10);
                Treasure2Coord = new(2, 10);
                Stone3Coord = new(12, 7);
                Wall3Coord = new(14, 7);
                Treasure5Coord = new(17, 7);
                ExitDoorCoord = new(1, 2);
                KeyCoord = new(15, 9);
                TrapCoord = new(15, 11);
                Trap2Coord = new(6, 8);
                break;
            case 9:
                StoneCoord = new(7, 13);
                Stone2Coord = new(6, 10);
                Wall2Coord = new(4, 10);
                Treasure2Coord = new(2, 10);
                Stone3Coord = new(12, 7);
                Wall3Coord = new(14, 7);
                Treasure5Coord = new(17, 7);
                ExitDoorCoord = new(18, 2);
                KeyCoord = new(15, 9);
                TrapCoord = new(15, 11);
                Trap2Coord = new(10, 8);
                break;
            case 10:
                StoneCoord = new(8, 13);
                Treasure2Coord = new(4, 4);
                ExitDoorCoord = new(18, 4);
                KeyCoord = new(16, 6);
                TrapCoord = new(16, 9);
                Trap2Coord = new(13, 5);
                break;
        }

        Trap2.Configure(Trap2Coord);
        grid.SetTile(Trap2Coord.X, Trap2Coord.Y, TileType.SolidWall);

        Trap3.Configure(Trap3Coord);
        if (HasTrap3)
        {
            grid.SetTile(Trap3Coord.X, Trap3Coord.Y, TileType.SolidWall);
        }

        Treasure.Configure(TreasureCoord);
        Treasure2.Configure(Treasure2Coord);
        Treasure5.Configure(Treasure5Coord);
        Treasure6.Configure(Treasure6Coord);
        Key.Configure(KeyCoord);
        Trap.Configure(TrapCoord);

        // 1. Dintel superior sobre la puerta secreta para evitar saltar por encima
        if (normalized == 5)
        {
            // En Recámara 5 la cámara secreta está en el Nivel 1 (fila 10, bajo el Nivel 2 en fila 8)
            grid.SetTile(WallCoord.X, 9, TileType.SolidWall);
        }
        else
        {
            if (!HasExtraTreasure)
            {
                grid.SetTile(WallCoord.X, 10, TileType.SolidWall);
            }
            grid.SetTile(WallCoord.X, 11, TileType.SolidWall);
            grid.SetTile(WallCoord.X, 12, TileType.SolidWall);
        }

        // 2. Muro conmutable inicialmente cerrado
        grid.SetTile(WallCoord.X, WallCoord.Y, TileType.SolidWall);

        // 3. Piedra de activación en el suelo
        grid.SetTile(StoneCoord.X, StoneCoord.Y, TileType.PressurePlate);

        // 3b. Puertas y losas adicionales para acceder a los tesoros en Recámaras 8 y 9
        if (HasMultipleTreasureDoors)
        {
            grid.SetTile(Wall2Coord.X, 9, TileType.SolidWall);
            grid.SetTile(Wall2Coord.X, Wall2Coord.Y, TileType.SolidWall);
            grid.SetTile(Stone2Coord.X, Stone2Coord.Y, TileType.PressurePlate);

            grid.SetTile(Wall3Coord.X, 6, TileType.SolidWall);
            grid.SetTile(Wall3Coord.X, Wall3Coord.Y, TileType.SolidWall);
            grid.SetTile(Stone3Coord.X, Stone3Coord.Y, TileType.PressurePlate);
        }

        // 4. Trampa de suelo bajo la llave inicialmente cerrada
        grid.SetTile(TrapCoord.X, TrapCoord.Y, TileType.SolidWall);

        // 5. Puerta de salida
        grid.SetTile(ExitDoorCoord.X, ExitDoorCoord.Y, TileType.ExitDoor);

        // 6. Reinicio de variables de estado
        CurrentChamber = normalized;
        DoorCycleCount = 0;
        HasSecretTreasureReloaded = false;
        Chamber2Puzzle1Stage = 0;
        Chamber3KeyCollectedFirst = false;
        Chamber4MummyJumpCount = 0;
        HasJumpedOverLevel1Mummy = false;
        HasJumpedOverLevel2Mummy = false;
        Level2MummyJumpCount = 0;
        _wasJumpingOverMummy2 = false;
        IsWallOpen = false;
        WasPlayerOnStone = false;
        IsWall2Open = false;
        WasPlayerOnStone2 = false;
        IsWall3Open = false;
        WasPlayerOnStone3 = false;
        NotificationTimer = 0;
        NotificationMessage = "";
        IsChamberCompleted = false;
        IsTreasure3Spawned = false;
        IsTreasure4Spawned = false;
        IsTreasure4Pending = false;
        Mummy2TrapFallCount = 0;
        IsMummy2FallingInTrap = false;
        Trap2OpenTimer = 0;
        IsPlayerTrappedInTrap = false;
        HasTrapEliminated = false;
        _suppressTrap1FromUpperFall = false;
        IsPlayerTrappedInTrap2 = false;
        HasTrap2Eliminated = false;
        IsPlayerTrappedInTrap3 = false;
        HasTrap3Eliminated = false;
        Trap3OpenTimer = 0;
        Mummy3TrapFallCount = 0;
        IsMummy3FallingInTrap = false;
        IsTreasure6Spawned = false;
        HasJumpedOverLevel3Mummy = false;
        Level3MummyJumpCount = 0;
        _wasJumpingOverMummy3 = false;
        JustCollectedTreasure = false;
        Treasure.Reset();
        Treasure2.Reset();
        Treasure3.Reset();
        Treasure4.Reset();
        Treasure5.Reset();
        Treasure6.Reset();
        Key.Reset();
        Trap.Reset();
        Trap2.Reset();
        Trap3.Reset();
        Treasure3.Configure(Treasure3Coord);
        Treasure4.Configure(Treasure4Coord);
        Treasure5.Configure(Treasure5Coord);
        Treasure6.Configure(Treasure6Coord);
    }

    /// <summary>
    /// Actualiza la lógica de colisión con la piedra interruptora y la recolección del tesoro.
    /// </summary>
    public void Update(RoomGrid grid, Player player, Mummy? mummy = null, Mummy? mummyLevel3 = null)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(player);

        JustCollectedTreasure = false;

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

            // Puzle 1 (Nivel 0):
            // - Recámara 2: 1) Tesoro de Nivel 0, 2) Llave, 3) Saltar momia de Nivel 1, 4) Abrir y cerrar puerta 2 veces.
            // - Recámara 3: 1) Llave primero, 2) Ambos cofres de tesoro, 3) Abrir y cerrar puerta 4 veces.
            // - Recámara 4: Ambos cofres + Llave + 2 saltos sobre momia (cualquier orden) -> Abrir y cerrar puerta 1 vez.
            // - Resto de recámaras (1, 5..10): Tener los 2 tesoros y la llave, y abrir y cerrar puerta 3 veces.
            if (!HasSecretTreasureReloaded)
            {
                bool arePrerequisitesMet = CurrentChamber switch
                {
                    2 => Chamber2Puzzle1Stage == 3 && Treasure.IsCollected && Key.IsCollected && HasJumpedOverLevel1Mummy,
                    3 => Chamber3KeyCollectedFirst && Key.IsCollected && Treasure.IsCollected && Treasure2.IsCollected,
                    4 => Treasure.IsCollected && Treasure2.IsCollected && Key.IsCollected && Chamber4MummyJumpCount >= 2,
                    >= 5 and <= 9 => Treasure.IsCollected && Treasure2.IsCollected && Treasure5.IsCollected && Key.IsCollected,
                    _ => Treasure.IsCollected && Treasure2.IsCollected && Key.IsCollected
                };

                int requiredCycles = CurrentChamber switch
                {
                    2 => 2,
                    3 => 4,
                    4 => 1,
                    _ => 3
                };

                if (!IsWallOpen)
                {
                    // La puerta acaba de cerrarse completando un ciclo de conmutación
                    if (arePrerequisitesMet)
                    {
                        DoorCycleCount++;
                        if (DoorCycleCount >= requiredCycles)
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

        // 2b. Conmutación de las puertas adicionales de tesoros en Nivel 1 y Nivel 2 (Recámaras 8 y 9)
        if (HasMultipleTreasureDoors)
        {
            float stone2Left = Stone2Coord.X * GameConstants.TILE_SIZE;
            float stone2Right = stone2Left + GameConstants.TILE_SIZE;
            float stone2Top = Stone2Coord.Y * GameConstants.TILE_SIZE;
            float stone2Bottom = stone2Top + GameConstants.TILE_SIZE;

            bool isPlayerOnStone2 = playerRight > stone2Left + 2f &&
                                    playerLeft < stone2Right - 2f &&
                                    playerBottom > stone2Top &&
                                    playerTop < stone2Bottom;

            if (isPlayerOnStone2 && !WasPlayerOnStone2)
            {
                IsWall2Open = !IsWall2Open;
                grid.SetTile(Wall2Coord.X, Wall2Coord.Y, IsWall2Open ? TileType.Empty : TileType.SolidWall);
            }
            WasPlayerOnStone2 = isPlayerOnStone2;

            float stone3Left = Stone3Coord.X * GameConstants.TILE_SIZE;
            float stone3Right = stone3Left + GameConstants.TILE_SIZE;
            float stone3Top = Stone3Coord.Y * GameConstants.TILE_SIZE;
            float stone3Bottom = stone3Top + GameConstants.TILE_SIZE;

            bool isPlayerOnStone3 = playerRight > stone3Left + 2f &&
                                    playerLeft < stone3Right - 2f &&
                                    playerBottom > stone3Top &&
                                    playerTop < stone3Bottom;

            if (isPlayerOnStone3 && !WasPlayerOnStone3)
            {
                IsWall3Open = !IsWall3Open;
                grid.SetTile(Wall3Coord.X, Wall3Coord.Y, IsWall3Open ? TileType.Empty : TileType.SolidWall);
            }
            WasPlayerOnStone3 = isPlayerOnStone3;
        }

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
                JustCollectedTreasure = true;

                // En Recámara 2, el paso 1 del Puzle 1 es agarrar primero el tesoro del Nivel 0
                if (CurrentChamber == 2 && Chamber2Puzzle1Stage == 0 && !Key.IsCollected)
                {
                    Chamber2Puzzle1Stage = 1;
                }
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
                JustCollectedTreasure = true;
            }
        }

        // 4a. Detección de recolección del cofre de tesoro adicional en Plataforma Nivel 3 (Recámaras 5, 6 y 7)
        bool collectedTreasure5ThisFrame = false;
        if (HasExtraTreasure && !Treasure5.IsCollected)
        {
            float treasure5Left = Treasure5Coord.X * GameConstants.TILE_SIZE;
            float treasure5Right = treasure5Left + GameConstants.TILE_SIZE;
            float treasure5Top = Treasure5Coord.Y * GameConstants.TILE_SIZE;
            float treasure5Bottom = treasure5Top + GameConstants.TILE_SIZE;

            bool isPlayerOnTreasure5 = playerRight > treasure5Left + 2f &&
                                       playerLeft < treasure5Right - 2f &&
                                       playerBottom > treasure5Top &&
                                       playerTop < treasure5Bottom;

            if (isPlayerOnTreasure5)
            {
                Treasure5.Collect();
                Score += 1000;
                NotificationTimer = 180;
                NotificationMessage = MSG_TREASURE;
                collectedTreasure5ThisFrame = true;
                JustCollectedTreasure = true;
            }
        }

        // 4a-bis. Detección de recolección del sexto tesoro (Puzle de Nivel 3 en Recámara 5)
        if (IsTreasure6Spawned && !Treasure6.IsCollected && !collectedTreasure5ThisFrame)
        {
            float treasure6Left = Treasure6Coord.X * GameConstants.TILE_SIZE;
            float treasure6Right = treasure6Left + GameConstants.TILE_SIZE;
            float treasure6Top = Treasure6Coord.Y * GameConstants.TILE_SIZE;
            float treasure6Bottom = treasure6Top + GameConstants.TILE_SIZE;

            bool isPlayerOnTreasure6 = playerRight > treasure6Left + 2f &&
                                       playerLeft < treasure6Right - 2f &&
                                       playerBottom > treasure6Top &&
                                       playerTop < treasure6Bottom;

            if (isPlayerOnTreasure6)
            {
                Treasure6.Collect();
                Score += 1000;
                NotificationTimer = 180;
                NotificationMessage = MSG_TREASURE4;
                JustCollectedTreasure = true;
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
                JustCollectedTreasure = true;
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
                JustCollectedTreasure = true;
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

                // En Recámara 2, el paso 2 del Puzle 1 es agarrar la llave después del tesoro del Nivel 0
                if (CurrentChamber == 2 && Chamber2Puzzle1Stage == 1 && Treasure.IsCollected)
                {
                    Chamber2Puzzle1Stage = 2;
                }

                // En Recámara 3, el paso 1 del Puzle 1 es agarrar la llave antes de recoger los dos cofres
                if (CurrentChamber == 3 && !Treasure.IsCollected && !Treasure2.IsCollected)
                {
                    Chamber3KeyCollectedFirst = true;
                }
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
        float trapLeft = TrapCoord.X * GameConstants.TILE_SIZE;
        float trapRight = trapLeft + GameConstants.TILE_SIZE;
        float platformPlayerY = (TrapCoord.Y - 1) * GameConstants.TILE_SIZE;

        // Si el jugador viene cayendo desde el Nivel 2 por el hueco de columna 16 hacia el Nivel 1,
        // su aterrizaje sobre Nivel 1 no debe activar la trampa mortal de Nivel 1.
        bool isHorizNearTrap1 = playerRight > trapLeft - 2f && playerLeft < trapRight + 2f;
        if (player.State == PlayerState.Falling && player.Position.Y < platformPlayerY - 4f && isHorizNearTrap1)
        {
            _suppressTrap1FromUpperFall = true;
        }
        else if (_suppressTrap1FromUpperFall)
        {
            if (!isHorizNearTrap1 || player.State == PlayerState.Jumping || (player.State != PlayerState.Falling && MathF.Abs(player.Position.Y - platformPlayerY) > 4f))
            {
                _suppressTrap1FromUpperFall = false;
            }
        }

        // El jugador está horizontalmente situado sobre el vano de la trampa (ambos puntos de apoyo en columna 16)
        bool isUnderKey = playerLeft >= trapLeft - 2f && playerRight <= trapRight + 2f;
        // El jugador está caminando o apoyado sobre el piso de la plataforma 1 (no en pleno salto ni cayendo desde arriba)
        bool isGroundedOnPlatform1 = MathF.Abs(player.Position.Y - platformPlayerY) <= 4f &&
                                     (player.State == PlayerState.Idle || player.State == PlayerState.Walking);

        if (!_suppressTrap1FromUpperFall)
        {
            if (!Trap.IsOpen)
            {
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
                    IsPlayerTrappedInTrap = true;
                    HasTrapEliminated = false;

                    NotificationTimer = 120;
                    NotificationMessage = MSG_TRAP;
                }
            }
            else
            {
                // Si la trampa ya estaba abierta y el jugador entra en ella caminando
                if (isUnderKey && isGroundedOnPlatform1 && !IsPlayerTrappedInTrap)
                {
                    player.ForceFall(grid);
                    IsPlayerTrappedInTrap = true;
                    HasTrapEliminated = false;
                    NotificationTimer = 120;
                    NotificationMessage = MSG_TRAP;
                }
            }
        }

        // Si el jugador cayó por la trampa, pierde una vida al completar la caída;
        // pero si permanece o camina sobre el Nivel 1, se limpia el estado de atrapado.
        if (IsPlayerTrappedInTrap)
        {
            if (!player.IsEliminated && player.State != PlayerState.Falling && player.Position.Y <= platformPlayerY + 4f)
            {
                IsPlayerTrappedInTrap = false;
                HasTrapEliminated = false;
            }
            else if (!HasTrapEliminated && (player.Position.Y >= 13 * GameConstants.TILE_SIZE || (player.State == PlayerState.Idle && player.Position.Y > TrapCoord.Y * GameConstants.TILE_SIZE) || player.IsEliminated))
            {
                HasTrapEliminated = true;
                if (!player.IsEliminated)
                {
                    player.Eliminate();
                }
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
            bool isOverTrap2Hole = playerLeft >= trap2Left - 2f && playerRight <= trap2Right + 2f;
            float platform2PlayerY = (Trap2Coord.Y - 1) * GameConstants.TILE_SIZE;

            // El jugador está a la altura de la plataforma de Nivel 2 (caminando en Y=64 o saltando en el aire sobre ella)
            bool isPlayerAtLevel2Height = player.Position.Y <= platform2PlayerY + 4f &&
                                          player.Position.Y >= platform2PlayerY - Player.JUMP_HEIGHT_PIXELS - 4f;
            bool isGroundedOnPlatform2 = MathF.Abs(player.Position.Y - platform2PlayerY) <= 4f &&
                                         (player.State == PlayerState.Idle || player.State == PlayerState.Walking);

            // Apertura de la trampa al pasar por arriba (caminando o saltando)
            if (isOverTrap2 && isPlayerAtLevel2Height && !Trap2.IsOpen)
            {
                Trap2.Open();
                grid.SetTile(Trap2Coord.X, Trap2Coord.Y, TileType.Empty);
                Trap2OpenTimer = 120; // 2 segundos abierta
            }

            // Si la trampa está abierta y el jugador pisa el vano de la trampa en Nivel 2, cae
            if (Trap2.IsOpen && isOverTrap2Hole && isGroundedOnPlatform2 && !IsPlayerTrappedInTrap2)
            {
                player.ForceFall(grid);
                IsPlayerTrappedInTrap2 = true;
                HasTrap2Eliminated = false;
                NotificationTimer = 120;
                NotificationMessage = MSG_TRAP2;
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
                if (!player.IsEliminated && player.State != PlayerState.Falling && player.Position.Y <= platform2PlayerY + 4f)
                {
                    IsPlayerTrappedInTrap2 = false;
                    HasTrap2Eliminated = false;
                }
                else
                {
                    // Cierre a las espaldas cuando pasa Y >= 96
                    if (Trap2.IsOpen && player.Position.Y >= (Trap2Coord.Y + 1) * GameConstants.TILE_SIZE)
                    {
                        Trap2.Close();
                        grid.SetTile(Trap2Coord.X, Trap2Coord.Y, TileType.SolidWall);
                        Trap2OpenTimer = 0;
                    }

                    // Si el jugador cayó por la trampa del Nivel 2, pierde una vida al completar la caída (ya sea aterrizando en Nivel 1 o en Nivel 0)
                    if (!HasTrap2Eliminated && (player.Position.Y >= 13 * GameConstants.TILE_SIZE || (player.State == PlayerState.Idle && player.Position.Y > Trap2Coord.Y * GameConstants.TILE_SIZE) || player.IsEliminated))
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
            }

            // 8b. Salto sobre la momia de Nivel 2 y caída en la trampa abierta
            if (mummy != null && mummy.IsActive)
            {
                float mummyLeft = mummy.Position.X;
                float mummyRight = mummy.Position.X + Mummy.WIDTH;
                bool isMummyOverTrap2 = mummyRight > trap2Left + 2f && mummyLeft < trap2Right - 2f;
                bool isMummyOnLevel2 = MathF.Abs(mummy.Position.Y - platform2PlayerY) <= 4f;

                if (!mummy.IsFalling && isMummyOnLevel2)
                {
                    if (player.State == PlayerState.Jumping)
                    {
                        bool horizOverlap = playerRight > mummyLeft && playerLeft < mummyRight;
                        bool isAbove = playerBottom <= (mummy.Position.Y + 4f);
                        if (horizOverlap && isAbove)
                        {
                            _wasJumpingOverMummy2 = true;
                        }
                    }
                    else if (_wasJumpingOverMummy2)
                    {
                        _wasJumpingOverMummy2 = false;
                        if (!player.IsEliminated)
                        {
                            RegisterLevel2MummyJump();
                        }
                    }
                }
                else
                {
                    _wasJumpingOverMummy2 = false;
                }

                if (!mummy.IsFalling && Trap2.IsOpen && isMummyOverTrap2 && isMummyOnLevel2)
                {
                    mummy.SetFalling(true);
                    IsMummy2FallingInTrap = true;
                }

                if (IsMummy2FallingInTrap && mummy.Position.Y >= 13f * GameConstants.TILE_SIZE)
                {
                    IsMummy2FallingInTrap = false;

                    bool isValidFall = CurrentChamber switch
                    {
                        2 => HasSecretTreasureReloaded && IsTreasure3Spawned && HasJumpedOverLevel2Mummy,
                        3 => Treasure2.IsCollected && Level2MummyJumpCount >= 3,
                        4 => Key.IsCollected && Level2MummyJumpCount >= 2,
                        _ => true
                    };
                    HasJumpedOverLevel2Mummy = false;
                    Level2MummyJumpCount = 0;

                    if (!isValidFall)
                    {
                        mummy.Reset();
                    }
                    else
                    {
                        Mummy2TrapFallCount++;
                        int requiredFalls = CurrentChamber switch
                        {
                            2 => 2,
                            3 => 1,
                            4 => 2,
                            _ => 3
                        };

                        if (Mummy2TrapFallCount < requiredFalls)
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
        }

        // 9. Detección y ciclo de la trampa mortal de suelo y puzle en Nivel 3 (Recámara 5)
        if (HasTrap3)
        {
            float trap3Left = Trap3Coord.X * GameConstants.TILE_SIZE;
            float trap3Right = trap3Left + GameConstants.TILE_SIZE;
            bool isOverTrap3 = playerRight > trap3Left + 2f && playerLeft < trap3Right - 2f;
            bool isOverTrap3Hole = playerLeft >= trap3Left - 2f && playerRight <= trap3Right + 2f;
            float platform3PlayerY = (Trap3Coord.Y - 1) * GameConstants.TILE_SIZE;

            bool isPlayerAtLevel3Height = player.Position.Y <= platform3PlayerY + 4f &&
                                          player.Position.Y >= platform3PlayerY - Player.JUMP_HEIGHT_PIXELS - 4f;
            bool isGroundedOnPlatform3 = MathF.Abs(player.Position.Y - platform3PlayerY) <= 4f &&
                                         (player.State == PlayerState.Idle || player.State == PlayerState.Walking);

            if (isOverTrap3 && isPlayerAtLevel3Height && !Trap3.IsOpen)
            {
                Trap3.Open();
                grid.SetTile(Trap3Coord.X, Trap3Coord.Y, TileType.Empty);
                Trap3OpenTimer = 120;
            }

            if (Trap3.IsOpen && isOverTrap3Hole && isGroundedOnPlatform3 && !IsPlayerTrappedInTrap3)
            {
                player.ForceFall(grid);
                IsPlayerTrappedInTrap3 = true;
                HasTrap3Eliminated = false;
                NotificationTimer = 120;
                NotificationMessage = MSG_TRAP2;
            }

            if (Trap3.IsOpen)
            {
                if (Trap3OpenTimer > 0)
                {
                    Trap3OpenTimer--;
                    if (Trap3OpenTimer == 0)
                    {
                        Trap3.Close();
                        grid.SetTile(Trap3Coord.X, Trap3Coord.Y, TileType.SolidWall);
                    }
                }
            }

            if (IsPlayerTrappedInTrap3)
            {
                if (!player.IsEliminated && player.State != PlayerState.Falling && player.Position.Y <= platform3PlayerY + 4f)
                {
                    IsPlayerTrappedInTrap3 = false;
                    HasTrap3Eliminated = false;
                }
                else
                {
                    if (Trap3.IsOpen && player.Position.Y >= (Trap3Coord.Y + 1) * GameConstants.TILE_SIZE)
                    {
                        Trap3.Close();
                        grid.SetTile(Trap3Coord.X, Trap3Coord.Y, TileType.SolidWall);
                        Trap3OpenTimer = 0;
                    }

                    if (!HasTrap3Eliminated && (player.Position.Y >= 13 * GameConstants.TILE_SIZE || (player.State == PlayerState.Idle && player.Position.Y > Trap3Coord.Y * GameConstants.TILE_SIZE) || player.IsEliminated))
                    {
                        HasTrap3Eliminated = true;
                        if (!player.IsEliminated)
                        {
                            player.Eliminate();
                        }
                        NotificationTimer = 120;
                        NotificationMessage = MSG_TRAP2;
                    }
                }
            }

            // 9b. Puzle de Nivel 3 (Recámara 5): recoger Treasure5 -> saltar momia de Nivel 3 + hacerla caer en Trap3 (x2)
            if (mummyLevel3 != null && mummyLevel3.IsActive)
            {
                float mummy3Left = mummyLevel3.Position.X;
                float mummy3Right = mummyLevel3.Position.X + Mummy.WIDTH;
                bool isMummy3OverTrap3 = mummy3Right > trap3Left + 2f && mummy3Left < trap3Right - 2f;
                bool isMummy3OnLevel3 = MathF.Abs(mummyLevel3.Position.Y - platform3PlayerY) <= 4f;

                if (!mummyLevel3.IsFalling && isMummy3OnLevel3)
                {
                    if (player.State == PlayerState.Jumping)
                    {
                        bool horizOverlap = playerRight > mummy3Left && playerLeft < mummy3Right;
                        bool isAbove = playerBottom <= (mummyLevel3.Position.Y + 4f);
                        if (horizOverlap && isAbove)
                        {
                            _wasJumpingOverMummy3 = true;
                        }
                    }
                    else if (_wasJumpingOverMummy3)
                    {
                        _wasJumpingOverMummy3 = false;
                        if (!player.IsEliminated)
                        {
                            RegisterLevel3MummyJump();
                        }
                    }
                }
                else
                {
                    _wasJumpingOverMummy3 = false;
                }

                if (!mummyLevel3.IsFalling && Trap3.IsOpen && isMummy3OverTrap3 && isMummy3OnLevel3)
                {
                    mummyLevel3.SetFalling(true);
                    IsMummy3FallingInTrap = true;
                }

                if (IsMummy3FallingInTrap && mummyLevel3.Position.Y >= 13f * GameConstants.TILE_SIZE)
                {
                    IsMummy3FallingInTrap = false;

                    bool isValidFall3 = Treasure5.IsCollected && HasJumpedOverLevel3Mummy;
                    HasJumpedOverLevel3Mummy = false;
                    Level3MummyJumpCount = 0;

                    if (!isValidFall3)
                    {
                        mummyLevel3.Reset();
                    }
                    else
                    {
                        Mummy3TrapFallCount++;
                        if (Mummy3TrapFallCount < 2)
                        {
                            mummyLevel3.Reset();
                            NotificationTimer = 120;
                            NotificationMessage = MSG_MUMMY_TRAPPED_1;
                        }
                        else
                        {
                            mummyLevel3.IsActive = false;
                            mummyLevel3.SetFalling(false);
                            SpawnTreasure6();
                        }
                    }
                }
            }
        }

        UpdateChamberCompletionStatus();

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

