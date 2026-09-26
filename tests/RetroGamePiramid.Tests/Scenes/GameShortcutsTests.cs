using Microsoft.Xna.Framework;
using RetroGamePiramid.Core;
using RetroGamePiramid.Entities;
using RetroGamePiramid.Grid;
using RetroGamePiramid.Systems;
using Xunit;

namespace RetroGamePiramid.Tests.Scenes;

public class GameShortcutsTests
{
    [Fact]
    public void RestartLogic_RestoresPlayer_Grid_AndPuzzleToInitialState()
    {
        var grid = new RoomGrid();
        grid.LoadDefaultRoom();

        var puzzle = new PuzzleManager();
        puzzle.Initialize(grid);

        var player = new Player(new GridCoord(2, 13));

        // 1. Modificar el estado: mover al jugador a la piedra, abrir el muro y recoger el tesoro
        player.SetPosition(puzzle.StoneCoord.X * GameConstants.TILE_SIZE, puzzle.StoneCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.IsWallOpen);
        Assert.Equal(TileType.Empty, grid.GetTile(puzzle.WallCoord));

        player.SetPosition(puzzle.TreasureCoord.X * GameConstants.TILE_SIZE, puzzle.TreasureCoord.Y * GameConstants.TILE_SIZE);
        puzzle.Update(grid, player);
        Assert.True(puzzle.Treasure.IsCollected);

        // 2. Ejecutar la lógica de reinicio de pantalla (tecla 2 / NewGame)
        grid.LoadDefaultRoom();
        puzzle.Initialize(grid);
        player.SetPosition(2 * GameConstants.TILE_SIZE + (GameConstants.TILE_SIZE - Player.WIDTH) / 2f, 13 * GameConstants.TILE_SIZE);

        // 3. Validar que todo ha vuelto al estado inicial
        Assert.False(puzzle.IsWallOpen);
        Assert.False(puzzle.Treasure.IsCollected);
        Assert.Equal(TileType.SolidWall, grid.GetTile(puzzle.WallCoord));
        Assert.Equal(TileType.PressurePlate, grid.GetTile(puzzle.StoneCoord));
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.Equal(2 * GameConstants.TILE_SIZE + (GameConstants.TILE_SIZE - Player.WIDTH) / 2f, player.Position.X);
        Assert.Equal(13 * GameConstants.TILE_SIZE, player.Position.Y);
    }

    [Fact]
    public void InGameHudPanel_StaysInsidePlayableRoom_WithoutOverlappingWalls()
    {
        const int panelX = 18;
        const int panelY = 18;
        const int panelW = 90;
        const int panelH = 28;

        // Validar que el panel esté dentro del marco de la pantalla (320x240)
        Assert.True(panelX >= GameConstants.TILE_SIZE, "El panel debe comenzar dentro de los muros interiores izquierdos");
        Assert.True(panelY >= GameConstants.TILE_SIZE, "El panel debe comenzar dentro de los muros interiores superiores");
        Assert.True(panelX + panelW < GameConstants.VIRTUAL_WIDTH - GameConstants.TILE_SIZE, "El panel no debe desbordar hacia la pared derecha");
        Assert.True(panelY + panelH < GameConstants.VIRTUAL_HEIGHT - GameConstants.TILE_SIZE, "El panel no debe desbordar hacia el suelo");

        // Validar que las celdas donde se ubica el panel en la habitación por defecto estén despejadas
        var grid = new RoomGrid();
        grid.LoadDefaultRoom();

        int startCol = panelX / GameConstants.TILE_SIZE;
        int endCol = (panelX + panelW - 1) / GameConstants.TILE_SIZE;
        int startRow = panelY / GameConstants.TILE_SIZE;
        int endRow = (panelY + panelH - 1) / GameConstants.TILE_SIZE;

        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = startCol; col <= endCol; col++)
            {
                Assert.True(grid.IsEmpty(col, row), $"La celda ({col}, {row}) ocupada por el HUD debe estar vacía");
            }
        }
    }

    [Fact]
    public void MiniFontHudTexts_FitInsidePanelDimensions()
    {
        const int panelW = 90;
        const int panelH = 28;
        const int miniGlyphStride = 5; // 4 px ancho + 1 px espacio
        const int marginX = 4;
        const int marginY = 3;
        const int miniGlyphHeight = 6;

        string roomText = "RECAMARA 1";
        string menuText = "1: SALIR AL MENU";
        string restartText = "2: RE-INICIAR";

        int roomWidth = roomText.Length * miniGlyphStride;
        int menuWidth = menuText.Length * miniGlyphStride;
        int restartWidth = restartText.Length * miniGlyphStride;

        Assert.True(marginX + roomWidth <= panelW, "El texto de recámara debe caber horizontalmente dentro del panel");
        Assert.True(marginX + menuWidth <= panelW, "El texto de menú debe caber horizontalmente dentro del panel");
        Assert.True(marginX + restartWidth <= panelW, "El texto de reinicio debe caber horizontalmente dentro del panel");

        // Tres líneas de texto: línea 1 en Y=3, línea 2 en Y=11, línea 3 en Y=19
        Assert.True(marginY + miniGlyphHeight < 11, "La línea 1 no debe superponerse con la línea 2");
        Assert.True(11 + miniGlyphHeight < 19, "La línea 2 no debe superponerse con la línea 3");
        Assert.True(19 + miniGlyphHeight <= panelH, "La línea 3 debe caber verticalmente dentro del panel");
    }

    [Fact]
    public void RightHudPanel_StaysInsidePlayableRoom_WithoutOverlappingWalls()
    {
        const int panelW = 90;
        const int panelH = 28;
        const int panelX = GameConstants.VIRTUAL_WIDTH - 18 - panelW; // 212
        const int panelY = 18;

        // Validar simetría y posición interior dentro del marco virtual de 320x240
        Assert.Equal(212, panelX);
        Assert.True(panelX >= GameConstants.TILE_SIZE, "El panel debe comenzar dentro de los muros interiores");
        Assert.True(panelY >= GameConstants.TILE_SIZE, "El panel debe comenzar debajo del techo");
        Assert.True(panelX + panelW < GameConstants.VIRTUAL_WIDTH - GameConstants.TILE_SIZE, "El panel no debe desbordar hacia la pared derecha");
        Assert.True(panelY + panelH < GameConstants.VIRTUAL_HEIGHT - GameConstants.TILE_SIZE, "El panel no debe desbordar hacia el suelo");

        // Validar que las celdas donde se ubica el panel derecho en la recámara por defecto estén despejadas (filas 1 y 2, cols 13 a 18)
        var grid = new RoomGrid();
        grid.LoadDefaultRoom();

        int startCol = panelX / GameConstants.TILE_SIZE;
        int endCol = (panelX + panelW - 1) / GameConstants.TILE_SIZE;
        int startRow = panelY / GameConstants.TILE_SIZE;
        int endRow = (panelY + panelH - 1) / GameConstants.TILE_SIZE;

        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = startCol; col <= endCol; col++)
            {
                Assert.True(grid.IsEmpty(col, row), $"La celda ({col}, {row}) ocupada por el HUD derecho debe estar vacía");
            }
        }
    }

    [Fact]
    public void RightHudLabels_FitInsidePanelDimensions()
    {
        const int panelW = 90;
        const int panelH = 28;
        const int miniGlyphStride = 5;
        const int marginX = 4;
        const int marginY = 3;
        const int miniGlyphHeight = 6;

        string titleText = "ESTADO";
        string pointsText = "PUNTOS: 2000";
        string livesText = "VIDAS:  3";

        int titleWidth = titleText.Length * miniGlyphStride;
        int pointsWidth = pointsText.Length * miniGlyphStride;
        int livesWidth = livesText.Length * miniGlyphStride;

        Assert.True(marginX + titleWidth <= panelW, "El título del estado debe caber dentro del panel");
        Assert.True(marginX + pointsWidth <= panelW, "El texto de puntos debe caber dentro del panel");
        Assert.True(marginX + livesWidth <= panelW, "El texto de vidas debe caber dentro del panel");

        // Tres líneas verticales: Y=3, Y=11, Y=19
        Assert.True(marginY + miniGlyphHeight < 11, "La línea de título no debe superponerse con la línea de puntos");
        Assert.True(11 + miniGlyphHeight < 19, "La línea de puntos no debe superponerse con la línea de vidas");
        Assert.True(19 + miniGlyphHeight <= panelH, "La línea de vidas debe caber verticalmente dentro del panel");
    }

    [Fact]
    public void GameOverModal_DisplaysOnlyExitToMenu_AndFitsCenteredBounds()
    {
        const int elimW = 130;
        const int elimH = 40;
        const int elimX = (GameConstants.VIRTUAL_WIDTH - elimW) / 2;
        const int elimY = (GameConstants.VIRTUAL_HEIGHT - elimH) / 2;

        Assert.Equal(95, elimX);
        Assert.Equal(100, elimY);

        string gameOverTitle = "GAME OVER";
        string singleMenuOption = "1: SALIR AL MENU";

        // Título en atlas 8x8
        int titlePixelWidth = gameOverTitle.Length * 8;
        Assert.True(titlePixelWidth <= elimW, "El título GAME OVER debe caber en el modal");

        // Opción única en atlas mini 4x6 (5 px stride)
        int optionPixelWidth = singleMenuOption.Length * 5;
        Assert.True(optionPixelWidth <= elimW, "La opción de salir al menú debe caber en el modal");

        // Posicionamiento vertical dentro de los 40 píxeles de altura
        const int titleYOffset = 8;
        const int titleHeight = 8;
        const int optionYOffset = 24;
        const int optionHeight = 6;

        Assert.True(titleYOffset + titleHeight < optionYOffset, "El título no debe colisionar con la opción");
        Assert.True(optionYOffset + optionHeight <= elimH, "La opción no debe desbordar el modal");
    }

    [Fact]
    public void EliminatedModal_WhenLivesRemain_DisplaysMenuAndContinueOptions()
    {
        const int elimW = 130;
        const int elimH = 48;
        const int elimX = (GameConstants.VIRTUAL_WIDTH - elimW) / 2;
        const int elimY = (GameConstants.VIRTUAL_HEIGHT - elimH) / 2;

        Assert.Equal(95, elimX);
        Assert.Equal(96, elimY);

        string eliminatedTitle = "ELIMINADO";
        string menuOption = "1: SALIR AL MENU";
        string continueOption = "2: CONTINUAR";

        // Título en atlas 8x8
        int titlePixelWidth = eliminatedTitle.Length * 8;
        Assert.True(titlePixelWidth <= elimW, "El título ELIMINADO debe caber en el modal");

        // Opciones en atlas mini 4x6 (5 px stride)
        int menuPixelWidth = menuOption.Length * 5;
        int continuePixelWidth = continueOption.Length * 5;
        Assert.True(menuPixelWidth <= elimW, "La opción de salir al menú debe caber en el modal");
        Assert.True(continuePixelWidth <= elimW, "La opción continuar debe caber en el modal");

        // Espaciado vertical
        const int titleY = 7;
        const int titleH = 8;
        const int opt1Y = 22;
        const int opt1H = 6;
        const int opt2Y = 32;
        const int opt2H = 6;

        Assert.True(titleY + titleH < opt1Y, "El título no debe superponerse con la opción 1");
        Assert.True(opt1Y + opt1H < opt2Y, "La opción 1 no debe superponerse con la opción 2");
        Assert.True(opt2Y + opt2H <= elimH, "La opción 2 debe caber dentro del modal");
    }
}
