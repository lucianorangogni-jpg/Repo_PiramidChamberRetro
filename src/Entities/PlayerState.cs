namespace RetroGamePiramid.Entities;

/// <summary>
/// Estados discretos de la máquina de estados del jugador (arqueólogo).
/// Tipo byte para optimización de memoria.
/// </summary>
public enum PlayerState : byte
{
    Idle = 0,
    Walking = 1,
    Climbing = 2,
    Jumping = 3,
    Falling = 4,
    Eliminated = 5
}
