using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RetroGamePiramid.Core;
using RetroGamePiramid.Graphics;
using RetroGamePiramid.Grid;

namespace RetroGamePiramid.Scenes;

/// <summary>
/// Gestiona la lógica de selección y renderizado del menú principal de inicio.
/// Cero asignaciones en memoria heap en cada frame.
/// </summary>
public sealed class TitleMenu
{
    private MenuOption _selectedOption = MenuOption.NewGame;
    private int _selectedChamber = 1;
    private bool _prevUp;
    private bool _prevDown;
    private bool _prevLeft;
    private bool _prevRight;
    private bool _prevConfirm;

    private static readonly string[] ChamberTexts =
    {
        "RECAMARA: < 1 >",
        "RECAMARA: < 1 >",
        "RECAMARA: < 2 >",
        "RECAMARA: < 3 >",
        "RECAMARA: < 4 >",
        "RECAMARA: < 5 >",
        "RECAMARA: < 6 >",
        "RECAMARA: < 7 >",
        "RECAMARA: < 8 >",
        "RECAMARA: < 9 >",
        "RECAMARA: < 10 >"
    };

    private static readonly string[] PyramidLevelTexts =
    {
        "NIVEL PIRAMIDE: 1 (BASE)",
        "NIVEL PIRAMIDE: 1 (BASE)",
        "NIVEL PIRAMIDE: 2",
        "NIVEL PIRAMIDE: 3",
        "NIVEL PIRAMIDE: 4 (CUSPIDE)"
    };

    public const string TEXT_CHAMBER10_LOCKED = "CUSPIDE BLOQUEADA: COMPLETA PUZZLES 1-9";

    public MenuOption SelectedOption
    {
        get => _selectedOption;
        set => _selectedOption = value;
    }

    public int SelectedChamber
    {
        get => _selectedChamber;
        set
        {
            _selectedChamber = (value >= 1 && value <= GameConstants.TOTAL_CHAMBERS) ? value : 1;
            ShowChamber10LockedWarning = false;
        }
    }

    public int SelectedPyramidLevel => RoomGrid.GetPyramidLevel(_selectedChamber);

    public bool CanContinue { get; set; }

    /// <summary>
    /// La Recámara 10 (Cúspide) solo puede iniciarse si todos los puzles de las Recámaras 1 a 9 fueron completados.
    /// </summary>
    public bool IsChamber10Unlocked { get; set; }

    public bool ShowChamber10LockedWarning { get; private set; }

    public bool CanStartSelectedChamber => _selectedChamber < GameConstants.TOTAL_CHAMBERS || IsChamber10Unlocked;

    public void NavigateUp()
    {
        _selectedOption = _selectedOption switch
        {
            MenuOption.NewGame => MenuOption.Exit,
            MenuOption.Continue => MenuOption.NewGame,
            MenuOption.SelectChamber => MenuOption.Continue,
            MenuOption.Exit => MenuOption.SelectChamber,
            _ => MenuOption.NewGame
        };
        ShowChamber10LockedWarning = false;
    }

    public void NavigateDown()
    {
        _selectedOption = _selectedOption switch
        {
            MenuOption.NewGame => MenuOption.Continue,
            MenuOption.Continue => MenuOption.SelectChamber,
            MenuOption.SelectChamber => MenuOption.Exit,
            MenuOption.Exit => MenuOption.NewGame,
            _ => MenuOption.NewGame
        };
        ShowChamber10LockedWarning = false;
    }

    /// <summary>
    /// Actualiza la navegación del menú con detección de flancos para evitar saltos múltiples por frame.
    /// Permite cambiar de recámara (1 a 10) con izquierda/derecha o enter sobre la opción de recámara.
    /// Devuelve la opción confirmada si el usuario pulsó Enter o Espacio para iniciar o salir.
    /// </summary>
    public bool Update(bool up, bool down, bool left, bool right, bool confirm, out MenuOption selectedAction)
    {
        bool upJustPressed = up && !_prevUp;
        bool downJustPressed = down && !_prevDown;
        bool leftJustPressed = left && !_prevLeft;
        bool rightJustPressed = right && !_prevRight;
        bool confirmJustPressed = confirm && !_prevConfirm;

        _prevUp = up;
        _prevDown = down;
        _prevLeft = left;
        _prevRight = right;
        _prevConfirm = confirm;

        if (upJustPressed)
        {
            NavigateUp();
            ShowChamber10LockedWarning = false;
        }
        else if (downJustPressed)
        {
            NavigateDown();
            ShowChamber10LockedWarning = false;
        }

        if (_selectedOption == MenuOption.SelectChamber)
        {
            if (leftJustPressed)
            {
                SelectedChamber = _selectedChamber > 1 ? _selectedChamber - 1 : GameConstants.TOTAL_CHAMBERS;
            }
            else if (rightJustPressed)
            {
                SelectedChamber = _selectedChamber < GameConstants.TOTAL_CHAMBERS ? _selectedChamber + 1 : 1;
            }
        }

        if (confirmJustPressed)
        {
            if (_selectedOption == MenuOption.SelectChamber)
            {
                SelectedChamber = _selectedChamber < GameConstants.TOTAL_CHAMBERS ? _selectedChamber + 1 : 1;
                selectedAction = _selectedOption;
                return false;
            }

            if ((_selectedOption == MenuOption.NewGame || _selectedOption == MenuOption.Continue) &&
                _selectedChamber == GameConstants.TOTAL_CHAMBERS &&
                !IsChamber10Unlocked)
            {
                ShowChamber10LockedWarning = true;
                selectedAction = _selectedOption;
                return false;
            }

            selectedAction = _selectedOption;
            return true;
        }

        selectedAction = _selectedOption;
        return false;
    }

    /// <summary>
    /// Sobrecarga compatible con versiones anteriores de Update.
    /// </summary>
    public bool Update(bool up, bool down, bool confirm, out MenuOption selectedAction) =>
        Update(up, down, false, false, confirm, out selectedAction);

    /// <summary>
    /// Dibuja el menú de inicio con estilo arcade y estética egipcia.
    /// Libre de asignaciones en heap.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, PixelFont font, int frameCounter)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(font);

        // 1. Título principal centrado con sombra retro (escala 2x)
        Color titleShadow = new(60, 35, 15);
        Color titleGold = new(255, 215, 60);
        Color subtitleColor = new(210, 175, 110);
        Color borderGold = new(135, 95, 45);

        // Borde decorativo superior
        font.DrawTextCentered(spriteBatch, "********************", GameConstants.VIRTUAL_WIDTH, 16, borderGold, scale: 2);

        // Sombra y texto del título
        font.DrawTextCentered(spriteBatch, "RETRO GAME PIRAMID", GameConstants.VIRTUAL_WIDTH + 2, 42, titleShadow, scale: 2);
        font.DrawTextCentered(spriteBatch, "RETRO GAME PIRAMID", GameConstants.VIRTUAL_WIDTH, 40, titleGold, scale: 2);

        // Subtítulo temático
        font.DrawTextCentered(spriteBatch, "- EXPEDICION EGIPCIA -", GameConstants.VIRTUAL_WIDTH, 66, subtitleColor, scale: 1);

        // Borde decorativo
        font.DrawTextCentered(spriteBatch, "==============================", GameConstants.VIRTUAL_WIDTH, 82, borderGold, scale: 1);

        // 2. Opciones del menú (NUEVO, CONTINUAR, RECAMARA, SALIR)
        int menuStartY = 98;
        int lineSpacing = 18;

        DrawOption(spriteBatch, font, MenuOption.NewGame, "NUEVO", menuStartY, frameCounter);
        DrawOption(spriteBatch, font, MenuOption.Continue, "CONTINUAR", menuStartY + lineSpacing, frameCounter);
        string chamberText = ChamberTexts[_selectedChamber];
        DrawOption(spriteBatch, font, MenuOption.SelectChamber, chamberText, menuStartY + (lineSpacing * 2), frameCounter);
        DrawOption(spriteBatch, font, MenuOption.Exit, "SALIR", menuStartY + (lineSpacing * 3), frameCounter);

        // Indicador del Nivel de la Pirámide correspondiente a la recámara seleccionada
        if (_selectedChamber == GameConstants.TOTAL_CHAMBERS && !IsChamber10Unlocked)
        {
            Color lockedColor = ShowChamber10LockedWarning ? new Color(255, 100, 75) : subtitleColor;
            font.DrawTextCentered(spriteBatch, TEXT_CHAMBER10_LOCKED, GameConstants.VIRTUAL_WIDTH, 174, lockedColor, scale: 1);
        }
        else
        {
            string pyramidLevelText = PyramidLevelTexts[SelectedPyramidLevel];
            font.DrawTextCentered(spriteBatch, pyramidLevelText, GameConstants.VIRTUAL_WIDTH, 174, subtitleColor, scale: 1);
        }

        // 3. Indicaciones de control en la parte inferior
        font.DrawTextCentered(spriteBatch, "==============================", GameConstants.VIRTUAL_WIDTH, 188, borderGold, scale: 1);
        Color footerColor = new(170, 140, 95);
        font.DrawTextCentered(spriteBatch, "FLECHAS: ELEGIR   ENTER: CONFIRMAR", GameConstants.VIRTUAL_WIDTH, 202, footerColor, scale: 1);
        font.DrawTextCentered(spriteBatch, "IZQ/DER: CAMBIAR RECAMARA (1-10)", GameConstants.VIRTUAL_WIDTH, 216, footerColor, scale: 1);
    }

    private void DrawOption(SpriteBatch spriteBatch, PixelFont font, MenuOption option, string text, int y, int frameCounter)
    {
        bool isSelected = _selectedOption == option;

        string cursor = "  ";
        Color textColor;

        if (isSelected)
        {
            // Cursor parpadeante cada 15 frames para efecto arcade
            bool blink = (frameCounter / 15) % 2 == 0;
            cursor = blink ? ">>" : " >";
            textColor = new Color(255, 240, 130); // Oro brillante activo
        }
        else
        {
            textColor = new Color(150, 120, 80); // Tono piedra inactivo
        }

        int charWidth = PixelFont.GLYPH_WIDTH * 2;
        int totalChars = 3 + text.Length; // 2 caracteres de cursor + 1 espacio + texto
        int startX = (GameConstants.VIRTUAL_WIDTH - (totalChars * charWidth)) / 2;
        int textX = startX + (3 * charWidth);

        if (isSelected)
        {
            // Sombra para la opción seleccionada
            font.DrawText(spriteBatch, cursor, startX + 1, y + 1, new Color(40, 20, 10), scale: 2);
            font.DrawText(spriteBatch, text, textX + 1, y + 1, new Color(40, 20, 10), scale: 2);

            font.DrawText(spriteBatch, cursor, startX, y, new Color(255, 215, 60), scale: 2);
            font.DrawText(spriteBatch, text, textX, y, textColor, scale: 2);
        }
        else
        {
            font.DrawText(spriteBatch, text, textX, y, textColor, scale: 2);
        }
    }
}
