# Especificación Técnica: [US-012] Puzle 1 de Recámara 1: Recarga Secreta del Tesoro del Nivel 0

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-10-03 (Actualizado v2.0)  
**Componentes:** `src/Systems/PuzzleManager.cs`, `src/Entities/Treasure.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
En la **Recámara 1**, el arqueólogo puede descubrir el **Puzle 1** secreto accionando la losa rúnica del nivel 0 tras cumplir estrictas condiciones previas:
1. **Prerrequisito de Ítems:** El jugador debe haber recolectado previamente los **dos (2) tesoros** existentes (Tesoro 1 en `(17, 13)` y Tesoro 2 en `(8, 4)`) y la **llave sagrada** (en `(16, 6)`).
2. **Acción de Activación:** Tras tener todos los ítems en su poder, el jugador debe **abrir y cerrar la puerta secreta del nivel 0 tres (3) veces consecutivas** (es decir, pasar por la losa rúnica de conmutación en `(8, 13)` completando 3 ciclos completos de cierre).
3. **Activación Única:** El puzle se puede ejecutar **una única vez** por intento de recámara (`HasSecretTreasureReloaded`). No se puede repetir una vez recargado el nuevo tesoro.

---

## 2. Mecánica y Ciclo de Conmutación
1. **Disparador:** La losa rúnica en `StoneCoord = (8, 13)` sobre la Plataforma 0.
2. **Puerta conmutable:** El muro secreto en `WallCoord = (14, 13)`.
3. **Ciclo de Apertura y Cierre:**
   - La puerta comienza cerrada (`IsWallOpen == false`).
   - Al pisar la losa rúnica: la puerta se abre (`IsWallOpen = true`).
   - Al salir de la losa y volver a pisarla: la puerta se cierra (`IsWallOpen = false`).
   - Un ciclo se computa al cerrarse la puerta tras haber sido abierta.
4. **Condición de Activación:**
   - Debe ser en la **Recámara 1** (`CurrentChamber == 1`).
   - **No debe haberse ejecutado previamente:** `HasSecretTreasureReloaded == false`.
   - **Prerrequisitos completos:**
     - `Treasure.IsCollected == true` (Tesoro 1 recogido, +1000 pts).
     - `Treasure2.IsCollected == true` (Tesoro 2 recogido, +1000 pts).
     - `Key.IsCollected == true` (Llave recogida, +500 pts).
   - Al completar el **3.er ciclo consecutivo de conmutación**:
     - `Treasure.Reset()` vuelve a materializar el tesoro cerrado en `(17, 13)` (`IsCollected = false`).
     - `HasSecretTreasureReloaded = true`.
     - Se reinicia el contador de ciclos a 0 (`DoorCycleCount = 0`).
     - Se muestra la notificación retro: `"!TESORO RECARGADO! +1000 PTS"` durante 180 frames (3 segundos).
5. **Comportamiento Posterior y No Repetición:**
   - El jugador puede volver a abrir la puerta pisando la losa y recoger el nuevo tesoro por otros **+1000 pts** (puntaje total acumulable de 3500 pts en la recámara).
   - Tras recogerlo o continuar jugando, si el jugador vuelve a abrir y cerrar la puerta 3 veces, **el tesoro ya no volverá a recargarse**.

---

## 3. Parámetros y Constantes
- `CurrentChamber == 1`
- `StoneCoord = (8, 13)`
- `WallCoord = (14, 13)`
- `TreasureCoord = (17, 13)`
- `Treasure2Coord = (8, 4)`
- `KeyCoord = (16, 6)`
- `REQUIRED_DOOR_CYCLES = 3`
- `MSG_TREASURE_RELOAD = "!TESORO RECARGADO! +1000 PTS"`

---

## 4. Restricciones Técnicas (Zero Allocation)
- Cero asignaciones en memoria heap (`0 allocations`) en `Update()` y `Draw()`.
- Variables de estado ligeras (`int DoorCycleCount`, `bool HasSecretTreasureReloaded`) dentro de `PuzzleManager`.
- La notificación utiliza constantes `const string` internadas.

---

## 5. Criterios de Aceptación (QA)
1. Si falta cualquiera de los dos tesoros o la llave, abrir y cerrar la puerta 3 veces no recarga el tesoro ni incrementa ciclos válidos.
2. Tras recoger ambos tesoros y la llave, al completar 3 ciclos de abrir y cerrar la puerta en Recámara 1:
   - El tesoro reaparece cerrado (`Treasure.IsCollected == false`).
   - `HasSecretTreasureReloaded == true`.
   - Se muestra el mensaje retro `!TESORO RECARGADO! +1000 PTS`.
3. El jugador puede abrir la puerta nuevamente, ingresar y recolectar el tesoro recargado, alcanzando un puntaje de 3500 puntos.
4. Tras recargarse una vez, repetir los 3 ciclos de puerta no vuelve a recargar el tesoro.
5. Al reiniciar la recámara (`Initialize`), `HasSecretTreasureReloaded` vuelve a `false`.
6. En Recámara 2, los ciclos de puerta no activan este puzle.
7. El proyecto compila con 0 advertencias y 0 errores (`dotnet build -warnaserror`).
8. 100% de las pruebas unitarias pasan exitosamente (`dotnet test`).
