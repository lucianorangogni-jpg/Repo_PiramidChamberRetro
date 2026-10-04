using System;
using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Grid;

namespace RetroGamePiramid.Entities;

/// <summary>
/// Entidad del enemigo Momia (Mommy).
/// Patrulla de forma autónoma horizontalmente sobre la plataforma nivel 2 a la velocidad del jugador.
/// Si colisiona con el jugador, lo elimina y le descuenta 1 vida.
/// En Recámara 1, soporta el Puzle 5: tras 4 saltos sobre ella, entra en persecución, cae al Nivel 0 y se petrifica.
/// Garantiza cero asignaciones de memoria heap en cada frame.
/// </summary>
public sealed class Mummy
{
    public const int WIDTH = 14;
    public const int HEIGHT = 16;
    public const float SPEED = Player.WALK_SPEED; // 1.5f px/frame
    public const float FALL_SPEED = 2.5f;
    public const int REQUIRED_JUMP_OVERS = 4;

    private Vector2 _position;
    private Direction _facing;
    private float _minX;
    private float _maxX;
    private Vector2 _spawnPosition;
    private Direction _spawnFacing;
    private bool _initialAwake;
    private bool _initialActive;

    public Vector2 Position => _position;
    public Direction Facing => _facing;
    public float MinX => _minX;
    public float MaxX => _maxX;
    public Vector2 SpawnPosition => _spawnPosition;
    public Direction SpawnFacing => _spawnFacing;
    public bool IsActive { get; set; } = true;
    public bool IsAwake { get; set; } = true;
    public bool IsChasing { get; private set; }
    public bool IsFalling { get; private set; }
    public bool HasReachedLevel0 { get; private set; }
    public int JumpOverCount { get; private set; }

    public Mummy(
        float spawnX,
        float spawnY,
        float minX,
        float maxX,
        Direction initialFacing = Direction.Right,
        bool initiallyAwake = true,
        bool initiallyActive = true)
    {
        _spawnPosition = new Vector2(spawnX, spawnY);
        _position = _spawnPosition;
        _minX = minX;
        _maxX = maxX;
        _spawnFacing = initialFacing;
        _facing = initialFacing;
        _initialAwake = initiallyAwake;
        _initialActive = initiallyActive;
        IsAwake = initiallyAwake;
        IsActive = initiallyActive;
    }

    /// <summary>
    /// Reconfigura los parámetros de spawn, límites de patrulla y estados iniciales de la momia.
    /// </summary>
    public void Configure(
        float spawnX,
        float spawnY,
        float minX,
        float maxX,
        Direction initialFacing = Direction.Right,
        bool initiallyAwake = true,
        bool initiallyActive = true)
    {
        _spawnPosition = new Vector2(spawnX, spawnY);
        _minX = minX;
        _maxX = maxX;
        _spawnFacing = initialFacing;
        _initialAwake = initiallyAwake;
        _initialActive = initiallyActive;
        Reset();
    }

    /// <summary>
    /// Despierta a la momia permitiéndole patrullar activamente.
    /// </summary>
    public void WakeUp()
    {
        IsAwake = true;
    }

    /// <summary>
    /// Restablece la momia a su posición, orientación y estado iniciales de spawn.
    /// </summary>
    public void Reset()
    {
        _position = _spawnPosition;
        _facing = _spawnFacing;
        IsAwake = _initialAwake;
        IsActive = _initialActive;
        IsChasing = false;
        IsFalling = false;
        HasReachedLevel0 = false;
        JumpOverCount = 0;
    }

    /// <summary>
    /// Registra un salto limpio sobre la momia. Al alcanzar 4 saltos, desata el modo persecución.
    /// Retorna true si activó el modo persecución en esta invocación.
    /// </summary>
    public bool RegisterJumpOver()
    {
        JumpOverCount++;
        if (JumpOverCount >= REQUIRED_JUMP_OVERS && !IsChasing)
        {
            StartChasing();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reinicia el contador de saltos consecutivos sobre la momia.
    /// </summary>
    public void ResetJumpOver()
    {
        JumpOverCount = 0;
    }

    /// <summary>
    /// Activa el modo de persecución directa del jugador.
    /// </summary>
    public void StartChasing()
    {
        IsChasing = true;
        IsAwake = true;
    }

    /// <summary>
    /// Configura el estado de caída (útil para pruebas unitarias).
    /// </summary>
    public void SetFalling(bool falling)
    {
        IsFalling = falling;
    }

    /// <summary>
    /// Detiene la caída de la momia al impactar en el suelo del Nivel 0, petrificándola para siempre.
    /// </summary>
    public void LandOnLevel0(float groundY = 13f * GameConstants.TILE_SIZE)
    {
        _position.Y = groundY;
        IsFalling = false;
        IsChasing = false;
        IsAwake = false;
        HasReachedLevel0 = true;
    }

    /// <summary>
    /// Permite reposicionar la momia (útil para pruebas unitarias).
    /// </summary>
    public void SetPosition(float x, float y)
    {
        _position.X = x;
        _position.Y = y;
    }

    /// <summary>
    /// Permite reorientar la momia.
    /// </summary>
    public void SetFacing(Direction facing)
    {
        _facing = facing;
    }

    /// <summary>
    /// Actualiza la posición de la momia (patrulla, persecución o caída libre),
    /// y verifica colisión AABB letal con el arqueólogo si no está petrificada.
    /// Cero allocations.
    /// </summary>
    public void Update(Player player, RoomGrid? grid = null)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!IsActive)
            return;

        // Si ya llegó al Nivel 0, queda inmóvil y petrificada de por vida
        if (HasReachedLevel0)
            return;

        // 1. Descenso vertical por gravedad si está cayendo
        if (IsFalling)
        {
            _position.Y += FALL_SPEED;
            const float level0FloorY = 13f * GameConstants.TILE_SIZE;
            if (_position.Y >= level0FloorY)
            {
                LandOnLevel0(level0FloorY);
                return;
            }
        }
        else
        {
            // 2. Modo Persecución enfurecida (Puzle 5)
            if (IsChasing)
            {
                float mummyCenterX = _position.X + (WIDTH / 2f);
                float playerCenterX = player.Position.X + (Player.WIDTH / 2f);

                if (playerCenterX < mummyCenterX - 1f)
                {
                    _facing = Direction.Left;
                    _position.X -= SPEED;
                }
                else if (playerCenterX > mummyCenterX + 1f)
                {
                    _facing = Direction.Right;
                    _position.X += SPEED;
                }

                _position.X = Math.Clamp(_position.X, 0f, GameConstants.VIRTUAL_WIDTH - WIDTH);

                // Detección de foso o trampa abierta bajo los pies
                if (grid != null)
                {
                    int centerCol = (int)((_position.X + (WIDTH / 2f)) / GameConstants.TILE_SIZE);
                    int footRow = (int)MathF.Round((_position.Y + HEIGHT) / GameConstants.TILE_SIZE);

                    if (!grid.IsSolid(centerCol, footRow) && !grid.IsLadder(centerCol, footRow))
                    {
                        IsFalling = true;
                    }
                }
            }
            // 3. Patrulla estándar autónoma sobre la plataforma si está despierta
            else if (IsAwake)
            {
                if (_facing == Direction.Right)
                {
                    _position.X += SPEED;
                    if (_position.X >= _maxX)
                    {
                        _position.X = _maxX;
                        _facing = Direction.Left;
                    }
                }
                else if (_facing == Direction.Left)
                {
                    _position.X -= SPEED;
                    if (_position.X <= _minX)
                    {
                        _position.X = _minX;
                        _facing = Direction.Right;
                    }
                }

                // Detección de foso o trampa abierta bajo los pies durante la patrulla
                if (grid != null)
                {
                    int centerCol = (int)((_position.X + (WIDTH / 2f)) / GameConstants.TILE_SIZE);
                    int footRow = (int)MathF.Round((_position.Y + HEIGHT) / GameConstants.TILE_SIZE);

                    if (!grid.IsSolid(centerCol, footRow) && !grid.IsLadder(centerCol, footRow))
                    {
                        IsFalling = true;
                    }
                }
            }
        }

        // 4. Comprobación de colisión AABB letal con el jugador
        if (!player.IsEliminated)
        {
            // Margen de tolerancia retro de 2 px para permitir saltar sobre la momia limpiamente
            const float margin = 2f;
            bool overlaps = player.Position.X + margin < _position.X + WIDTH - margin &&
                            player.Position.X + Player.WIDTH - margin > _position.X + margin &&
                            player.Position.Y + margin < _position.Y + HEIGHT &&
                            player.Position.Y + Player.HEIGHT - margin > _position.Y + margin;

            if (overlaps)
            {
                player.Eliminate();
            }
        }
    }
}
