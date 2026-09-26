# Especificación Técnica: [US-005] Atajos de Pantalla: Re-iniciar y Salir al Menú Principal

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-26  
**Componentes:** `Game1.cs`, `src/Graphics/PixelFont.cs`, `tests/RetroGamePiramid.Tests/Scenes/GameShortcutsTests.cs`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
Permitir al jugador en cualquier momento durante la partida:
1. **Salir al menú principal** pulsando la tecla `1` (o `NumPad1` / `Escape`).
2. **Re-iniciar la pantalla/cámara actual** pulsando la tecla `2` (o `NumPad2`), restableciendo la posición inicial del arqueólogo, la cuadrícula de la cámara y el estado de los puzles y tesoros.

Asimismo, mostrar un indicador visual sutil y legible en el lateral superior izquierdo con tipografía pequeña (escala 1x) que recuerde estos atajos.

---

## 2. Controles y Mapeo
- **Tecla 1 (`Keys.D1` / `Keys.NumPad1`):** Salir al Menú Principal.
  - Guarda la capacidad de "CONTINUAR" en el menú.
  - Conmuta `_currentScreen` a `GameScreen.TitleMenu`.
- **Tecla 2 (`Keys.D2` / `Keys.NumPad2`):** Re-iniciar la pantalla actual.
  - Restablece `_roomGrid.LoadDefaultRoom()`.
  - Restablece el puzle `_puzzleManager.Initialize(_roomGrid)`.
  - Reubica al arqueólogo en su punto de partida `(2, 13)`.
- **Detección por flanco (Edge Triggering):**
  - Solo se activa en el frame exacto de pulsación para evitar disparos repetidos.

---

## 3. Presentación Visual (HUD Superior Izquierdo)
- **Posición:** `X = 4, Y = 4` y `X = 4, Y = 14`.
- **Textos:**
  - `1: SALIR AL MENU`
  - `2: RE-INICIAR`
- **Tipografía:** `PixelFont` a escala 1x (8x8 píxeles por glifo) con sombra de contraste oscura (`new Color(20, 10, 5)`) y tono arena dorado (`new Color(240, 215, 140)`).

---

## 4. Restricciones Técnicas (Zero Allocation)
- Cero asignaciones en memoria heap (`0 allocations`) en `Update()` y `Draw()`.
- Textos estáticos preasignados internados.

---

## 5. Criterios de Aceptación (QA)
1. Durante el juego, pulsar `1` devuelve al jugador al Menú Principal.
2. Durante el juego, tras modificar la pantalla (recoger tesoro, abrir puerta, mover jugador), pulsar `2` re-inicia la pantalla al estado inicial.
3. El HUD lateral superior izquierdo muestra claramente los atajos `1: SALIR AL MENU` y `2: RE-INICIAR` en tamaño pequeño.
4. `dotnet build` compila con 0 errores y 0 advertencias.
5. Pruebas automatizadas validan la lógica de reinicio (`dotnet test`).
