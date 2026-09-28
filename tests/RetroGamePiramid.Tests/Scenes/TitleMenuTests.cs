using RetroGamePiramid.Scenes;
using Xunit;

namespace RetroGamePiramid.Tests.Scenes;

public class TitleMenuTests
{
    [Fact]
    public void InitialState_SelectsNewGame_AndDefaultsToChamber1()
    {
        var menu = new TitleMenu();
        Assert.Equal(MenuOption.NewGame, menu.SelectedOption);
        Assert.Equal(1, menu.SelectedChamber);
        Assert.False(menu.CanContinue);
    }

    [Fact]
    public void NavigateDown_CyclesThroughOptionsInOrder()
    {
        var menu = new TitleMenu();

        menu.NavigateDown();
        Assert.Equal(MenuOption.Continue, menu.SelectedOption);

        menu.NavigateDown();
        Assert.Equal(MenuOption.SelectChamber, menu.SelectedOption);

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
        Assert.Equal(MenuOption.SelectChamber, menu.SelectedOption);

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

        // Segundo frame manteniendo 'down' presionado (no debe volver a saltar a SelectChamber)
        confirmed = menu.Update(up: false, down: true, confirm: false, out action);
        Assert.False(confirmed);
        Assert.Equal(MenuOption.Continue, menu.SelectedOption);

        // Al soltar y volver a presionar, avanza a SelectChamber
        menu.Update(up: false, down: false, confirm: false, out _);
        menu.Update(up: false, down: true, confirm: false, out action);
        Assert.Equal(MenuOption.SelectChamber, menu.SelectedOption);

        // Al soltar y volver a presionar de nuevo, avanza a Exit
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
    public void SelectChamber_ToggleWithLeftAndRight()
    {
        var menu = new TitleMenu();
        menu.NavigateDown(); // Continue
        menu.NavigateDown(); // SelectChamber
        Assert.Equal(MenuOption.SelectChamber, menu.SelectedOption);
        Assert.Equal(1, menu.SelectedChamber);

        // Presionar derecha cambia a recámara 2
        menu.Update(up: false, down: false, left: false, right: true, confirm: false, out _);
        Assert.Equal(2, menu.SelectedChamber);

        // Mantener derecha no vuelve a cambiar por edge-triggering
        menu.Update(up: false, down: false, left: false, right: true, confirm: false, out _);
        Assert.Equal(2, menu.SelectedChamber);

        // Soltar y presionar izquierda vuelve a recámara 1
        menu.Update(up: false, down: false, left: false, right: false, confirm: false, out _);
        menu.Update(up: false, down: false, left: true, right: false, confirm: false, out _);
        Assert.Equal(1, menu.SelectedChamber);
    }

    [Fact]
    public void SelectChamber_ToggleWithConfirm_StaysInMenu()
    {
        var menu = new TitleMenu();
        menu.NavigateDown(); // Continue
        menu.NavigateDown(); // SelectChamber
        Assert.Equal(1, menu.SelectedChamber);

        // Presionar Confirm sobre la opción de recámara alterna a 2 y retorna false (permanece en menú)
        bool confirmed = menu.Update(up: false, down: false, left: false, right: false, confirm: true, out var action);
        Assert.False(confirmed);
        Assert.Equal(2, menu.SelectedChamber);
        Assert.Equal(MenuOption.SelectChamber, action);

        // Soltar y presionar Confirm nuevamente alterna a 1
        menu.Update(up: false, down: false, left: false, right: false, confirm: false, out _);
        confirmed = menu.Update(up: false, down: false, left: false, right: false, confirm: true, out action);
        Assert.False(confirmed);
        Assert.Equal(1, menu.SelectedChamber);
    }

    [Fact]
    public void SelectedChamber_Property_ClampsTo1Or2()
    {
        var menu = new TitleMenu();
        menu.SelectedChamber = 2;
        Assert.Equal(2, menu.SelectedChamber);

        menu.SelectedChamber = 99;
        Assert.Equal(1, menu.SelectedChamber);

        menu.SelectedChamber = 1;
        Assert.Equal(1, menu.SelectedChamber);
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
