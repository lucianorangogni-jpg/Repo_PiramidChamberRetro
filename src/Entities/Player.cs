using System;
using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Grid;
using RetroGamePiramid.Input;

namespace RetroGamePiramid.Entities;

/// <summary>
/// Entidad del protagonista (arqueólogo).
/// Gestiona la física por celdas, máquina de estados, escalado de escaleras y salto parabólico arcade.
/// Garantiza cero asignaciones en memoria heap (0 allocations) en cada llamada a Update.
/// </summary>
public sealed class Player
{
    // Constantes de dimensiones del hitbox (en píxeles virtuales)
    public const int WIDTH = 14;
    public const int HEIGHT = 16;

    // Constantes de físicas y balanceo arcade
    public const float WALK_SPEED = 1.5f;
    public const float CLIMB_SPEED = 1.2f;
    public const int JUMP_DURATION_FRAMES = 24;
    public const float JUMP_HEIGHT_PIXELS = 28f;
    public const float JUMP_HORIZONTAL_DISTANCE = 40f;
    public const float GRAVITY = 0.25f;
    public const float MAX_FALL_SPEED = 4.0f;
    public const float MAX_SAFE_FALL_DISTANCE = 5.5f * GameConstants.TILE_SIZE; // 88 píxeles (5.5 celdas)

    // Estado y posición
    private Vector2 _position;
    private PlayerState _state = PlayerState.Idle;
    private Direction _facing = Direction.Right;

    // Variables de cinemática y salto
    private float _verticalVelocity;
    private int _jumpTimer;
    private float _jumpStartX;
    private float _jumpStartY;
    private Direction _jumpDirection = Direction.None;

    // Control de caídas y letalidad
    private float _fallStartY;
    private int _fallStartPlatformLevel = -1;

    // Sistema de vidas (la que está jugando + 2 de reserva)
    public const int INITIAL_LIVES = 3;
    private int _lives = INITIAL_LIVES;

    // Animación (contador de frames para ciclar pasos)
    private int _animationTimer;

    public Vector2 Position => _position;
    public PlayerState State => _state;
    public Direction Facing => _facing;
    public int AnimationTimer => _animationTimer;
    public bool IsEliminated => _state == PlayerState.Eliminated;
    public float FallStartY => _fallStartY;
    public int FallStartPlatformLevel => _fallStartPlatformLevel;
    public int Lives => _lives;

    public void LoseLife()
    {
        if (_lives > 0)
        {
            _lives--;
        }
    }

    public void ResetLives()
    {
        _lives = INITIAL_LIVES;
    }

    /// <summary>
    /// Elimina al jugador de forma inmediata (por caída fatal, impacto con momia u otro peligro)
    /// descontando 1 vida sin duplicar descuentos si ya estaba eliminado.
    /// </summary>
    public void Eliminate()
    {
        if (_state != PlayerState.Eliminated)
        {
            _state = PlayerState.Eliminated;
            LoseLife();
        }
    }

    public Player(float startX, float startY)
    {
        _position = new Vector2(startX, startY);
    }

    public Player(GridCoord gridCoord)
        : this(gridCoord.X * GameConstants.TILE_SIZE + (GameConstants.TILE_SIZE - WIDTH) / 2f,
               gridCoord.Y * GameConstants.TILE_SIZE)
    {
    }

    /// <summary>
    /// Teletransporta o reubica al jugador en una coordenada de pantalla dada.
    /// </summary>
    public void SetPosition(float x, float y)
    {
        _position.X = x;
        _position.Y = y;
        _verticalVelocity = 0f;
        _jumpTimer = 0;
        _fallStartY = y;
        _fallStartPlatformLevel = -1;
        _state = PlayerState.Idle;
    }

    /// <summary>
    /// Fuerza al jugador a entrar inmediatamente en estado de caída libre (por ejemplo al abrirse una trampa bajo sus pies).
    /// Si el jugador está en el aire durante un salto, la parábola no se interrumpe.
    /// Cero allocations.
    /// </summary>
    public void ForceFall(RoomGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (_state == PlayerState.Walking || _state == PlayerState.Idle)
        {
            _state = PlayerState.Falling;
            _verticalVelocity = 0f;
            _fallStartY = _position.Y;
            _fallStartPlatformLevel = grid.GetPlatformLevel(_position.Y);
        }
    }

    /// <summary>
    /// Actualiza la simulación del jugador en función de la entrada y la cuadrícula de la cámara.
    /// Completamente libre de allocations (sin new, sin LINQ, sin boxing).
    /// </summary>
    public void Update(RoomGrid grid, in PlayerInput input)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (_state == PlayerState.Eliminated)
            return;

        _animationTimer++;

        switch (_state)
        {
            case PlayerState.Idle:
            case PlayerState.Walking:
                UpdateGrounded(grid, in input);
                break;

            case PlayerState.Climbing:
                UpdateClimbing(grid, in input);
                break;

            case PlayerState.Jumping:
                UpdateJumping(grid);
                break;

            case PlayerState.Falling:
                UpdateFalling(grid, in input);
                break;
        }
    }

    private void UpdateGrounded(RoomGrid grid, in PlayerInput input)
    {
        // 1. Agarre de escalera vertical
        if (input.Up || input.Down)
        {
            if (TryEnterLadder(grid, input.Up, input.Down))
            {
                _state = PlayerState.Climbing;
                float moveY = input.Up ? -CLIMB_SPEED : CLIMB_SPEED;
                _position.Y += moveY;
                return;
            }
        }

        // Si ya está en una celda de escalera y no hay suelo firme debajo, agarrarse a la escalera
        if (IsOnLadder(grid, _position.X, _position.Y) && !IsGrounded(grid, _position.X, _position.Y))
        {
            SnapToLadderCenter();
            _state = PlayerState.Climbing;
            return;
        }

        // 2. Detección de caída (si no hay suelo firme debajo)
        if (!IsGrounded(grid, _position.X, _position.Y))
        {
            _state = PlayerState.Falling;
            _verticalVelocity = 0f;
            _fallStartY = _position.Y;
            _fallStartPlatformLevel = grid.GetPlatformLevel(_position.Y);
            return;
        }

        // 3. Disparo del salto con barra espaciadora
        if (input.Jump)
        {
            StartJump(input, grid);
            return;
        }

        // 4. Desplazamiento horizontal
        float moveX = 0f;
        if (input.Left)
        {
            moveX -= WALK_SPEED;
            _facing = Direction.Left;
        }
        if (input.Right)
        {
            moveX += WALK_SPEED;
            _facing = Direction.Right;
        }

        if (moveX != 0f)
        {
            _state = PlayerState.Walking;
            ApplyHorizontalMovement(grid, moveX);

            // Tras caminar, verificar si cayó al vacío
            if (!IsGrounded(grid, _position.X, _position.Y) && !IsOnLadder(grid, _position.X, _position.Y))
            {
                _state = PlayerState.Falling;
                _verticalVelocity = 0f;
                _fallStartY = _position.Y;
                _fallStartPlatformLevel = grid.GetPlatformLevel(_position.Y);

                // Liberar el borde (slip off ledge): acomodar hitbox en la columna vacía
                float footY = _position.Y + HEIGHT;
                int rowBelow = (int)MathF.Round(footY / GameConstants.TILE_SIZE);

                if (moveX < 0f)
                {
                    int rightLedgeCol = (int)((_position.X + WIDTH) / GameConstants.TILE_SIZE);
                    if (grid.IsSolid(rightLedgeCol, rowBelow))
                    {
                        _position.X = rightLedgeCol * GameConstants.TILE_SIZE - WIDTH;
                    }
                }
                else if (moveX > 0f)
                {
                    int leftLedgeCol = (int)(_position.X / GameConstants.TILE_SIZE);
                    if (grid.IsSolid(leftLedgeCol, rowBelow))
                    {
                        _position.X = (leftLedgeCol + 1) * GameConstants.TILE_SIZE;
                    }
                }
            }
        }
        else
        {
            _state = PlayerState.Idle;
        }
    }

    private void UpdateClimbing(RoomGrid grid, in PlayerInput input)
    {
        // Si el jugador pulsa Jump, salta soltándose de la escalera
        if (input.Jump)
        {
            StartJump(input, grid);
            return;
        }

        float moveY = 0f;
        if (input.Up)
            moveY -= CLIMB_SPEED;
        if (input.Down)
            moveY += CLIMB_SPEED;

        int centerCol = (int)((_position.X + WIDTH / 2f) / GameConstants.TILE_SIZE);

        if (moveY < 0f)
        {
            // Subiendo: verificar si alcanza el tope superior de la escalera para posarse sobre la plataforma
            int currentFootRow = (int)MathF.Round((_position.Y + HEIGHT) / GameConstants.TILE_SIZE);
            int topLadderRow = GetTopLadderRow(grid, centerCol, currentFootRow);
            float platformTopY = topLadderRow * GameConstants.TILE_SIZE - HEIGHT;

            float newY = _position.Y + moveY;

            if (newY <= platformTopY)
            {
                // Llegó al nivel superior de la plataforma: desmontar y posar limpiamente
                _position.Y = platformTopY;
                _state = PlayerState.Idle;
                return;
            }

            // Colisión con techo
            if (CollidesWithSolid(grid, _position.X, newY))
            {
                int topTileY = (int)(newY / GameConstants.TILE_SIZE);
                newY = (topTileY + 1) * GameConstants.TILE_SIZE;
            }

            _position.Y = newY;
        }
        else if (moveY > 0f)
        {
            float newY = _position.Y + moveY;

            // Bajando: colisión con suelo firme en la base
            float footY = newY + HEIGHT;
            int bottomTileY = (int)(footY / GameConstants.TILE_SIZE);

            if (grid.IsSolid(centerCol, bottomTileY))
            {
                _position.Y = bottomTileY * GameConstants.TILE_SIZE - HEIGHT;
                _state = PlayerState.Idle;
                return;
            }

            if (CollidesWithSolid(grid, _position.X, newY))
            {
                newY = bottomTileY * GameConstants.TILE_SIZE - HEIGHT;
                _position.Y = newY;
                _state = PlayerState.Idle;
                return;
            }

            _position.Y = newY;
        }

        // Permite desmontarse a los lados si camina en dirección horizontal hacia suelo firme
        if (input.Left || input.Right)
        {
            float sideMove = input.Left ? -WALK_SPEED : WALK_SPEED;
            float targetX = _position.X + sideMove;

            if (!CollidesWithSolid(grid, targetX, _position.Y))
            {
                _position.X = targetX;
                _facing = input.Left ? Direction.Left : Direction.Right;
                if (!IsOnLadder(grid, _position.X, _position.Y))
                {
                    if (IsGrounded(grid, _position.X, _position.Y))
                    {
                        _state = PlayerState.Walking;
                    }
                    else
                    {
                        _state = PlayerState.Falling;
                        _verticalVelocity = 0f;
                        _fallStartY = _position.Y;
                        _fallStartPlatformLevel = grid.GetPlatformLevel(_position.Y);
                    }
                }
            }
        }
    }

    private void StartJump(in PlayerInput input, RoomGrid? grid = null)
    {
        _state = PlayerState.Jumping;
        _jumpTimer = 0;
        _jumpStartX = _position.X;
        _jumpStartY = _position.Y;
        _fallStartY = _position.Y;
        if (grid != null)
        {
            _fallStartPlatformLevel = grid.GetPlatformLevel(_position.Y);
        }

        if (input.Right)
        {
            _jumpDirection = Direction.Right;
            _facing = Direction.Right;
        }
        else if (input.Left)
        {
            _jumpDirection = Direction.Left;
            _facing = Direction.Left;
        }
        else
        {
            _jumpDirection = Direction.None;
        }
    }

    private void UpdateJumping(RoomGrid grid)
    {
        _jumpTimer++;
        float t = (float)_jumpTimer / JUMP_DURATION_FRAMES;

        if (t >= 1f)
        {
            // Fin del salto arcade: comprobar estado de aterrizaje en la línea base
            _jumpTimer = 0;
            _position.Y = _jumpStartY;

            if (IsGrounded(grid, _position.X, _position.Y))
            {
                _state = PlayerState.Idle;
            }
            else
            {
                _state = PlayerState.Falling;
                _verticalVelocity = 0f;
                _fallStartY = _jumpStartY;
                _fallStartPlatformLevel = grid.GetPlatformLevel(_jumpStartY);
            }
            return;
        }

        // Cálculo de la parábola matemática arcade: y = -4 * H * t * (1 - t)
        float parabolaY = -4f * JUMP_HEIGHT_PIXELS * t * (1f - t);
        float targetY = _jumpStartY + parabolaY;

        // Comprobación de choque contra techo
        if (CollidesWithSolid(grid, _position.X, targetY))
        {
            // Interrumpir el ascenso del salto y caer inmediatamente
            _state = PlayerState.Falling;
            _verticalVelocity = 0.5f;
            _fallStartY = _jumpStartY;
            _fallStartPlatformLevel = grid.GetPlatformLevel(_jumpStartY);
            return;
        }

        _position.Y = targetY;

        // Desplazamiento horizontal durante el salto
        if (_jumpDirection != Direction.None)
        {
            float targetOffset = JUMP_HORIZONTAL_DISTANCE * t;
            float targetX = _jumpDirection == Direction.Right
                ? _jumpStartX + targetOffset
                : _jumpStartX - targetOffset;

            float deltaX = targetX - _position.X;
            if (!CollidesWithSolid(grid, _position.X + deltaX, _position.Y))
            {
                _position.X = targetX;
            }
            else
            {
                // Choque con pared lateral durante el salto: detiene avance horizontal
                _jumpDirection = Direction.None;
            }
        }
    }

    private void UpdateFalling(RoomGrid grid, in PlayerInput input)
    {
        // 1. Comprobar si puede agarrarse a una escalera en plena caída
        if ((input.Up || input.Down) && IsOnLadder(grid, _position.X, _position.Y))
        {
            SnapToLadderCenter();
            _state = PlayerState.Climbing;
            _verticalVelocity = 0f;
            return;
        }

        // 2. Aplicar gravedad y aceleración vertical
        _verticalVelocity = MathF.Min(_verticalVelocity + GRAVITY, MAX_FALL_SPEED);
        float newY = _position.Y + _verticalVelocity;

        // 3. Comprobación de impacto con el suelo firme
        if (CollidesWithSolid(grid, _position.X, newY))
        {
            if (IsGrounded(grid, _position.X, newY))
            {
                int landTileY = (int)MathF.Round((newY + HEIGHT) / GameConstants.TILE_SIZE);
                float landingY = landTileY * GameConstants.TILE_SIZE - HEIGHT;
                LandOnSurface(grid, landingY);
                return;
            }
            else
            {
                // No está sustentado: rozamiento de esquina sólida por menos de 2 píxeles
                // Ajustar horizontalmente para librar la esquina y seguir cayendo
                int rowBelow = (int)((newY + HEIGHT - 0.01f) / GameConstants.TILE_SIZE);
                int rightCol = (int)((_position.X + WIDTH - 0.01f) / GameConstants.TILE_SIZE);
                int leftCol = (int)(_position.X / GameConstants.TILE_SIZE);

                if (grid.IsSolid(rightCol, rowBelow) && !grid.IsSolid(leftCol, rowBelow))
                {
                    _position.X = rightCol * GameConstants.TILE_SIZE - WIDTH;
                }
                else if (grid.IsSolid(leftCol, rowBelow) && !grid.IsSolid(rightCol, rowBelow))
                {
                    _position.X = (leftCol + 1) * GameConstants.TILE_SIZE;
                }

                if (!CollidesWithSolid(grid, _position.X, newY))
                {
                    _position.Y = newY;
                }
                else
                {
                    int landTileY = (int)MathF.Round((newY + HEIGHT) / GameConstants.TILE_SIZE);
                    float landingY = landTileY * GameConstants.TILE_SIZE - HEIGHT;
                    LandOnSurface(grid, landingY);
                    return;
                }
            }
        }
        else if (_verticalVelocity > 0f && IsGrounded(grid, _position.X, newY))
        {
            // Aterriza sobre superficie transitable (ej. tope de escalera)
            float footY = newY + HEIGHT;
            int landTileY = (int)MathF.Round(footY / GameConstants.TILE_SIZE);
            float landingY = landTileY * GameConstants.TILE_SIZE - HEIGHT;
            LandOnSurface(grid, landingY);
            return;
        }
        else
        {
            _position.Y = newY;
        }

        // 4. Control horizontal limitado en el aire (retro drift)
        float airMove = 0f;
        if (input.Left)
            airMove -= WALK_SPEED * 0.75f;
        if (input.Right)
            airMove += WALK_SPEED * 0.75f;

        if (airMove != 0f)
        {
            ApplyHorizontalMovement(grid, airMove);
        }
    }

    private void LandOnSurface(RoomGrid grid, float landingY)
    {
        float fallDistance = landingY - _fallStartY;
        _position.Y = landingY;
        _verticalVelocity = 0f;

        int landLevel = grid.GetPlatformLevel(landingY);
        if (landLevel == -1)
        {
            landLevel = grid.GetClosestPlatformLevel(landingY);
        }

        int startLevel = _fallStartPlatformLevel;
        if (startLevel == -1)
        {
            startLevel = grid.GetClosestPlatformLevel(_fallStartY);
        }

        bool isLethalFall = false;

        // Regla general retro: la caída debe ser de mínimo 2 niveles de altura para ser letal
        // Caída de 1 nivel (ej. 1 a 0, 2 a 1, 3 a 2) NO elimina al jugador.
        // Caída de 2 o más niveles (ej. 2 a 0, 3 a 1, 3 a 0) ELIMINA al jugador.
        if (startLevel != -1 && landLevel != -1)
        {
            int levelDifference = startLevel - landLevel;
            if (levelDifference >= 2)
            {
                isLethalFall = true;
            }
        }
        else
        {
            // Respaldo métrico en caso de superficies no tabuladas
            if (fallDistance > MAX_SAFE_FALL_DISTANCE)
            {
                isLethalFall = true;
            }
        }

        if (isLethalFall)
        {
            Eliminate();
        }
        else
        {
            _state = PlayerState.Idle;
            _fallStartY = landingY;
            _fallStartPlatformLevel = landLevel;
        }
    }

    private void ApplyHorizontalMovement(RoomGrid grid, float deltaX)
    {
        float targetX = _position.X + deltaX;

        if (!CollidesWithSolid(grid, targetX, _position.Y))
        {
            _position.X = targetX;
        }
        else
        {
            // Ajustar contra el borde de la pared sólida
            if (deltaX > 0f)
            {
                int wallCol = (int)((targetX + WIDTH) / GameConstants.TILE_SIZE);
                _position.X = wallCol * GameConstants.TILE_SIZE - WIDTH;
            }
            else if (deltaX < 0f)
            {
                int wallCol = (int)(targetX / GameConstants.TILE_SIZE);
                _position.X = (wallCol + 1) * GameConstants.TILE_SIZE;
            }
        }
    }

    private bool CollidesWithSolid(RoomGrid grid, float testX, float testY)
    {
        int leftCol = (int)(testX / GameConstants.TILE_SIZE);
        int rightCol = (int)((testX + WIDTH - 0.01f) / GameConstants.TILE_SIZE);
        int topRow = (int)(testY / GameConstants.TILE_SIZE);
        int bottomRow = (int)((testY + HEIGHT - 0.01f) / GameConstants.TILE_SIZE);

        for (int row = topRow; row <= bottomRow; row++)
        {
            for (int col = leftCol; col <= rightCol; col++)
            {
                if (grid.IsSolid(col, row))
                    return true;
            }
        }

        return false;
    }

    private bool IsGrounded(RoomGrid grid, float testX, float testY)
    {
        float footY = testY + HEIGHT;
        int rowBelow = (int)MathF.Round(footY / GameConstants.TILE_SIZE);

        // Si los pies no están cerca del inicio de la celda inferior (dentro de 2 píxeles), no hay contacto
        float distToRow = MathF.Abs(footY - (rowBelow * GameConstants.TILE_SIZE));
        if (distToRow > 2.0f)
            return false;

        int leftCol = (int)((testX + 2f) / GameConstants.TILE_SIZE);
        int rightCol = (int)((testX + WIDTH - 2f) / GameConstants.TILE_SIZE);

        bool leftSupported = grid.IsSolid(leftCol, rowBelow) || IsLadderTop(grid, leftCol, rowBelow);
        bool rightSupported = grid.IsSolid(rightCol, rowBelow) || IsLadderTop(grid, rightCol, rowBelow);

        return leftSupported || rightSupported;
    }

    private static bool IsLadderTop(RoomGrid grid, int col, int row)
    {
        if (!grid.InBounds(col, row))
            return false;

        return grid.IsLadder(col, row) && (row == 0 || !grid.IsLadder(col, row - 1));
    }

    private static int GetTopLadderRow(RoomGrid grid, int col, int fromRow)
    {
        int r = Math.Clamp(fromRow, 0, GameConstants.GRID_ROWS - 1);
        while (r > 0 && grid.IsLadder(col, r - 1))
        {
            r--;
        }
        return r;
    }

    private bool IsOnLadder(RoomGrid grid, float testX, float testY)
    {
        int centerCol = (int)((testX + WIDTH / 2f) / GameConstants.TILE_SIZE);
        int topRow = (int)(testY / GameConstants.TILE_SIZE);
        int centerRow = (int)((testY + HEIGHT / 2f) / GameConstants.TILE_SIZE);
        int feetRow = (int)((testY + HEIGHT - 0.5f) / GameConstants.TILE_SIZE);

        return grid.IsLadder(centerCol, topRow) || grid.IsLadder(centerCol, centerRow) || grid.IsLadder(centerCol, feetRow);
    }

    private bool TryEnterLadder(RoomGrid grid, bool upPressed, bool downPressed)
    {
        int centerCol = (int)((_position.X + WIDTH / 2f) / GameConstants.TILE_SIZE);
        int centerRow = (int)((_position.Y + HEIGHT / 2f) / GameConstants.TILE_SIZE);
        int feetRow = (int)MathF.Round((_position.Y + HEIGHT) / GameConstants.TILE_SIZE);
        int topRow = (int)(_position.Y / GameConstants.TILE_SIZE);

        if (upPressed)
        {
            // Subir: debe haber escalera en el torso o cabeza (no sube si ya está en el tope)
            if (grid.IsLadder(centerCol, centerRow) || grid.IsLadder(centerCol, topRow))
            {
                SnapToLadderColumn(centerCol);
                return true;
            }
        }
        else if (downPressed)
        {
            // Bajar: si hay escalera bajo los pies o en el torso
            if (grid.IsLadder(centerCol, feetRow) || grid.IsLadder(centerCol, centerRow))
            {
                SnapToLadderColumn(centerCol);
                return true;
            }
        }

        return false;
    }

    private void SnapToLadderCenter()
    {
        int col = (int)((_position.X + WIDTH / 2f) / GameConstants.TILE_SIZE);
        SnapToLadderColumn(col);
    }

    private void SnapToLadderColumn(int col)
    {
        _position.X = col * GameConstants.TILE_SIZE + (GameConstants.TILE_SIZE - WIDTH) / 2f;
    }
}
