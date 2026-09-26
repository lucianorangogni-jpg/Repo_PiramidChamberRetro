using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RetroGamePiramid.Core;
using RetroGamePiramid.Graphics;

namespace RetroGamePiramid.Scenes;

/// <summary>
/// Gestiona la lógica de selección y renderizado del menú principal de inicio.
/// Cero asignaciones en memoria heap en cada frame.
/// </summary>
public sealed class TitleMenu
{
    private MenuOption _selectedOption = MenuOption.NewGame;
    private bool _prevUp;
    private bool _prevDown;
    private bool _prevConfirm;

    public MenuOption SelectedOption
    {
        get => _selectedOption;
        set => _selectedOption = value;
    }

    public bool CanContinue { get; set; }

    public void NavigateUp()
    {
        _selectedOption = _selectedOption switch
        {
            MenuOption.NewGame => MenuOption.Exit,
            MenuOption.Continue => MenuOption.NewGame,
            MenuOption.Exit => MenuOption.Continue,
            _ => MenuOption.NewGame
        };
    }

    public void NavigateDown()
    {
        _selectedOption = _selectedOption switch
        {
            MenuOption.NewGame => MenuOption.Continue,
            MenuOption.Continue => MenuOption.Exit,
            MenuOption.Exit => MenuOption.NewGame,
            _ => MenuOption.NewGame
        };
    }

    /// <summary>
    /// Actualiza la navegación del menú con detección de flancos para evitar saltos múltiples por frame.
    /// Devuelve la opción confirmada si el usuario pulsó Enter o Espacio.
    /// </summary>
    public bool Update(bool up, bool down, bool confirm, out MenuOption selectedAction)
    {
        bool upJustPressed = up && !_prevUp;
        bool downJustPressed = down && !_prevDown;
        bool confirmJustPressed = confirm && !_prevConfirm;

        _prevUp = up;
        _prevDown = down;
        _prevConfirm = confirm;

        if (upJustPressed)
        {
            NavigateUp();
        }
        else if (downJustPressed)
        {
            NavigateDown();
        }

        if (confirmJustPressed)
        {
            selectedAction = _selectedOption;
            return true;
        }

        selectedAction = _selectedOption;
        return false;
    }

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
        font.DrawTextCentered(spriteBatch, "==============================", GameConstants.VIRTUAL_WIDTH, 84, borderGold, scale: 1);

        // 2. Opciones del menú (NUEVO, CONTINUAR, SALIR)
        int menuStartY = 112;
        int lineSpacing = 24;

        DrawOption(spriteBatch, font, MenuOption.NewGame, "NUEVO", menuStartY, frameCounter);
        DrawOption(spriteBatch, font, MenuOption.Continue, "CONTINUAR", menuStartY + lineSpacing, frameCounter);
        DrawOption(spriteBatch, font, MenuOption.Exit, "SALIR", menuStartY + (lineSpacing * 2), frameCounter);

        // 3. Indicaciones de control en la parte inferior
        font.DrawTextCentered(spriteBatch, "==============================", GameConstants.VIRTUAL_WIDTH, 192, borderGold, scale: 1);
        Color footerColor = new(170, 140, 95);
        font.DrawTextCentered(spriteBatch, "FLECHAS: ELEGIR   ENTER: CONFIRMAR", GameConstants.VIRTUAL_WIDTH, 208, footerColor, scale: 1);
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

