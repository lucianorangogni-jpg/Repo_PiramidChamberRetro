using System;
using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;

namespace RetroGamePiramid.Entities;

/// <summary>
/// Entidad del enemigo Momia (Mommy).
/// Patrulla de forma autónoma horizontalmente sobre la plataforma nivel 2 a la velocidad del jugador.
/// Si colisiona con el jugador, lo elimina y le descuenta 1 vida.
/// Garantiza cero asignaciones de memoria heap en cada frame.
/// </summary>
public sealed class Mummy
{
    public const int WIDTH = 14;
    public const int HEIGHT = 16;
    public const float SPEED = Player.WALK_SPEED; // 1.5f px/frame

    private Vector2 _position;
    private Direction _facing;
    private readonly float _minX;
    private readonly float _maxX;
    private readonly Vector2 _spawnPosition;
    private readonly Direction _spawnFacing;

    public Vector2 Position => _position;
    public Direction Facing => _facing;
    public float MinX => _minX;
    public float MaxX => _maxX;

    public Mummy(float spawnX, float spawnY, float minX, float maxX, Direction initialFacing = Direction.Right)
    {
        _spawnPosition = new Vector2(spawnX, spawnY);
        _position = _spawnPosition;
        _minX = minX;
        _maxX = maxX;
        _spawnFacing = initialFacing;
        _facing = initialFacing;
    }

    /// <summary>
    /// Restablece la momia a su posición y orientación iniciales de spawn.
    /// </summary>
    public void Reset()
    {
        _position = _spawnPosition;
        _facing = _spawnFacing;
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
    /// Actualiza la posición de la momia invirtiendo el avance en los extremos de la plataforma,
    /// y verifica colisión AABB letal con el arqueólogo.
    /// Cero allocations.
    /// </summary>
    public void Update(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        // 1. Desplazamiento autónomo sobre la plataforma nivel 2
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

        // 2. Comprobación de colisión AABB con el jugador
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
