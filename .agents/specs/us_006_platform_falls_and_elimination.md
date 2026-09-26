# Especificación Técnica: [US-006] Distinción de Plataformas, Física de Caídas en Vacío y Eliminación por Caída Fatal

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-26  
**Componentes:** `src/Grid/PlatformInfo.cs`, `src/Grid/RoomGrid.cs`, `src/Entities/Player.cs`, `src/Entities/PlayerState.cs`, `src/Graphics/PlayerRenderer.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/Entities/PlayerTests.cs`, `tests/RetroGamePiramid.Tests/Scenes/GameShortcutsTests.cs`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
Esta especificación resuelve los problemas de jugabilidad con el personaje al desplazarse por las plataformas de la recámara y establece la mecánica de caída libre y eliminación:
1. **Distinción de plataformas:** Cada recámara (comenzando por "Recámara 1") modela formalmente sus plataformas con sus niveles de altura y cotas transitables.
2. **Corrección de atascos en bordes:** Al caminar hacia el espacio vacío al final de una plataforma, el jugador debe caer suavemente en lugar de trabarse en la esquina.
3. **Caída segura desde Nivel 1:** Al caer por el vacío izquierdo de la Plataforma Nivel 1 hacia el Nivel 0, el jugador aterriza a salvo y continúa la partida.
4. **Caída fatal y Eliminación desde Nivel 2:** Al caer por el vacío derecho de la Plataforma Nivel 2 hacia el Nivel 0, la gran altura (9 celdas) resulta letal; el jugador queda en estado `Eliminated`, se presenta el mensaje de "ELIMINADO", se bloquea el control de movimiento y se habilita pulsar `1` para salir o `2` para reiniciar.

---

## 2. Modelado de Datos y Plataformas (Recámara 1)
Se define un struct inmutable de solo lectura en `src/Grid/PlatformInfo.cs`:
```csharp
public readonly record struct PlatformInfo(int Level, int Row, int StartCol, int EndCol, string Name);
```

En `RoomGrid.cs`, "Recámara 1" registra 3 plataformas:
- **Plataforma Nivel 0 (Suelo base):** Fila base 14, fila transitable 13 (Y = 208), columnas 1 a 18.
- **Plataforma Nivel 1 (Intermedia):** Fila base 9, fila transitable 8 (Y = 128), columnas 2 a 10.
- **Plataforma Nivel 2 (Superior):** Fila base 5, fila transitable 4 (Y = 64), columnas 8 a 17.

---

## 3. Dinámica de Saliente y Física de Caídas
- **Regla de Letalidad por Diferencia de Niveles (mínimo 2 niveles de altura):**
  - Caída de 1 nivel (`startLevel - landLevel < 2`): **Segura / Superada**.
    - Ejemplo: Nivel 1 a Nivel 0 (`1 - 0 = 1`): No se elimina.
    - Ejemplo: Nivel 2 a Nivel 1 (`2 - 1 = 1`): No se elimina.
    - Ejemplo: Nivel 3 a Nivel 2 (`3 - 2 = 1`): No se elimina.
  - Caída de 2 o más niveles (`startLevel - landLevel >= 2`): **Fatal / Eliminado**.
    - Ejemplo: Nivel 2 a Nivel 0 (`2 - 0 = 2`): Se elimina.
    - Ejemplo: Nivel 3 a Nivel 1 (`3 - 1 = 2`): Se elimina.
    - Ejemplo: Nivel 3 a Nivel 0 (`3 - 0 = 3`): Se elimina.
  - **Respaldo métrico:** `MAX_SAFE_FALL_DISTANCE = 5.5f * GameConstants.TILE_SIZE; // 88 píxeles` para superficies no tabuladas.

- **Liberación de salientes (Edge Ledge Slip):**
  - Al salir de la plataforma caminando hacia el vacío, si el jugador ya no tiene sustentación de suelo (`!IsGrounded`), el hitbox se acomoda inmediatamente en la columna vacía:
    - Hacia la izquierda (vacío col 1): `X = 2 * 16 - 14 = 18.0f`.
    - Hacia la derecha (vacío col 18): `X = 18 * 16 = 288.0f`.
  - En `UpdateFalling`, si el hitbox roza la esquina sólida de la plataforma por menos de 2 píxeles (`!IsGrounded`), se libera horizontalmente en lugar de computar un aterrizaje falso en el aire.

---

## 4. Máquina de Estados: `PlayerState.Eliminated`
Se amplía el enum `PlayerState`:
```csharp
public enum PlayerState : byte
{
    Idle = 0,
    Walking = 1,
    Climbing = 2,
    Jumping = 3,
    Falling = 4,
    Eliminated = 5
}
```

- **Comportamiento en `PlayerState.Eliminated`:**
  - `Update`: Se anula la gravedad y se deshabilitan las entradas de movimiento horizontal, salto o escalada.
  - `PlayerRenderer`: Dibuja al arqueólogo noqueado/derrotado sobre el suelo.
  - `Game1`:
    - Dibuja un banner modal central: `"ELIMINADO"`, con `"1: SALIR AL MENU"` y `"2: RE-INICIAR"`.
    - Pulsar `1` o `Escape`: Va a `GameScreen.TitleMenu`.
    - Pulsar `2`: Ejecuta `RestartCurrentRoom()`, devolviendo al jugador con vida a la posición inicial del Nivel 0.

---

## 5. Restricciones Técnicas (Zero Allocation)
- Estrictamente 0 asignaciones en heap durante los bucles de `Update()` y `Draw()`.
- Colores y textos cacheados como `static readonly` y `const`.
- Rectángulos de HUD y renderizado reutilizados.

---

## 6. Criterios de Aceptación (QA)
1. **Recámara 1 identificada:** La cuadrícula reporta su nombre y sus 3 plataformas (Nivel 0, Nivel 1, Nivel 2).
2. **Caída Nivel 1 izquierda:** Al caminar hacia la izquierda desde la Plataforma Nivel 1 hacia el vacío de la columna 1, el jugador no se traba; cae suavemente hasta la Plataforma Nivel 0, aterriza con vida y puede continuar moviéndose.
3. **Caída Nivel 2 derecha:** Al caminar hacia la derecha desde la Plataforma Nivel 2 hacia el vacío de la columna 18, el jugador no se traba; cae hasta la Plataforma Nivel 0 y al tocar el suelo queda en estado `Eliminated`.
4. **Mensaje de Eliminación:** Al ser eliminado, aparece en pantalla el mensaje "ELIMINADO" junto a las opciones "1: SALIR AL MENU" y "2: RE-INICIAR".
5. **Atajos tras Eliminación:** Pulsar `1` lleva al menú principal; pulsar `2` re-inicia la pantalla restaurando al jugador con vida en el Nivel 0.
6. **Compilación y Tests:** `dotnet build` compila con 0 errores y 0 advertencias, y `dotnet test` pasa todas las pruebas unitarias.
