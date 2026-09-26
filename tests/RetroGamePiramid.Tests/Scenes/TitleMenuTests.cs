using RetroGamePiramid.Scenes;
using Xunit;

namespace RetroGamePiramid.Tests.Scenes;

public class TitleMenuTests
{
    [Fact]
    public void InitialState_SelectsNewGame()
    {
        var menu = new TitleMenu();
        Assert.Equal(MenuOption.NewGame, menu.SelectedOption);
        Assert.False(menu.CanContinue);
    }

    [Fact]
    public void NavigateDown_CyclesThroughOptionsInOrder()
    {
        var menu = new TitleMenu();

        menu.NavigateDown();
        Assert.Equal(MenuOption.Continue, menu.SelectedOption);

        menu.NavigateDown();
        Assert.Equal(MenuOption.Exit, menu.SelectedOption);

        menu.NavigateDown();
        Assert.Equal(MenuOption.NewGame, menu.SelectedOption);
    }

    [Fact]
    public void NavigateUp_CyclesThroughOptionsInReverse()
    {
        var menu = new TitleMenu();

        menu.NavigateUp();
        Assert.Equal(MenuOption.Exit, menu.SelectedOption);

        menu.NavigateUp();
        Assert.Equal(MenuOption.Continue, menu.SelectedOption);

        menu.NavigateUp();
        Assert.Equal(MenuOption.NewGame, menu.SelectedOption);
    }

    [Fact]
    public void Update_EdgeTriggering_PreventsRepeatedNavOnHeldKey()
    {
        var menu = new TitleMenu();

        // Primer frame con 'down' presionado
        bool confirmed = menu.Update(up: false, down: true, confirm: false, out var action);
        Assert.False(confirmed);
        Assert.Equal(MenuOption.Continue, menu.SelectedOption);

        // Segundo frame manteniendo 'down' presionado (no debe volver a saltar a Exit)
        confirmed = menu.Update(up: false, down: true, confirm: false, out action);
        Assert.False(confirmed);
        Assert.Equal(MenuOption.Continue, menu.SelectedOption);

        // Al soltar y volver a presionar, avanza a Exit
        menu.Update(up: false, down: false, confirm: false, out _);
        menu.Update(up: false, down: true, confirm: false, out action);
        Assert.Equal(MenuOption.Exit, menu.SelectedOption);
    }

    [Fact]
    public void Update_ConfirmKey_ReturnsSelectedAction()
    {
        var menu = new TitleMenu();
        menu.NavigateDown(); // Selecciona Continue

        // Pulsar confirm (Enter / Espacio)
        bool confirmed = menu.Update(up: false, down: false, confirm: true, out var action);

        Assert.True(confirmed);
        Assert.Equal(MenuOption.Continue, action);
    }

    [Fact]
    public void CanContinue_CanBeToggled()
    {
        var menu = new TitleMenu();
        Assert.False(menu.CanContinue);

        menu.CanContinue = true;
        Assert.True(menu.CanContinue);
    }
}

