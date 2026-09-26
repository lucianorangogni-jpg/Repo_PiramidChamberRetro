using Microsoft.Xna.Framework.Input;

namespace RetroGamePiramid.Input;

/// <summary>
/// Representa el estado de entrada del jugador en un instante dado.
/// Readonly struct con cero asignaciones en heap y altamente desacoplable para pruebas unitarias.
/// </summary>
public readonly struct PlayerInput
{
    public readonly bool Left;
    public readonly bool Right;
    public readonly bool Up;
    public readonly bool Down;
    public readonly bool Jump;

    public PlayerInput(bool left, bool right, bool up, bool down, bool jump)
    {
        Left = left;
        Right = right;
        Up = up;
        Down = down;
        Jump = jump;
    }

    /// <summary>
    /// Muestrea el estado del teclado del jugador (flechas y barra espaciadora, con soporte adicional WASD).
    /// </summary>
    public static PlayerInput FromKeyboard(KeyboardState keyboard)
    {
        bool left = keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A);
        bool right = keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D);
        bool up = keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W);
        bool down = keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S);
        bool jump = keyboard.IsKeyDown(Keys.Space);

        return new PlayerInput(left, right, up, down, jump);
    }
}
