namespace RetroGamePiramid.Scenes;

/// <summary>
/// Opciones disponibles en el menú principal.
/// Tipo byte para optimización de memoria y cero boxing.
/// </summary>
public enum MenuOption : byte
{
    NewGame = 0,
    Continue = 1,
    SelectChamber = 2,
    Exit = 3
}
