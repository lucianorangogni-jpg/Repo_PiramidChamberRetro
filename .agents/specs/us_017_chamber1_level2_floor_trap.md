# Especificación Técnica: [US-017] Trampa de Suelo en Nivel 2: Trampilla Mortal y Pérdida de Vida

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-10-04  
**Componentes:** `src/Entities/FloorTrap.cs`, `src/Systems/PuzzleManager.cs`, `src/Graphics/PuzzleRenderer.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN TÉCNICA APROBADA  

---

## 1. Descripción
En la **Recámara 1**, sobre la plataforma del **Nivel 2** (fila 5), se añade una nueva trampa rúnica de suelo en la **columna 13** (`Trap2Coord = new GridCoord(13, 5)`), ubicada exactamente sobre el **segundo espacio vacío del Nivel 1** (el foso entre el descansillo de columna 12 y la plataforma derecha de columna 14):
1. **Paso Caminando (Activación):** Si el arqueólogo pisa la baldosa de la columna 13 caminando sobre el suelo de Nivel 2 ($Y = 64\text{ px}$), la trampilla se abre inmediatamente y el jugador cae hacia el vacío.
2. **Cierre de la Trampa (Quedar Atrapado):** En cuanto el cuerpo del jugador desciende por completo por debajo de la baldosa ($Y \ge 96\text{ px}$, cota inferior de fila 5), la trampilla se cierra de golpe a sus espaldas (`SolidWall`), dejándolo sellado y atrapado sin posibilidad de retorno.
3. **Pérdida de Vida:** Al precipitarse por la trampa y caer a través del hueco del Nivel 1 hasta el Nivel 0, el jugador sufre una caída fatal ineludible, perdiendo 1 vida (`Eliminate()`) y mostrando la notificación: `"!TRAMPA MORTAL! QUEDAS ATRAPADO"`.
4. **Habilidad de Evasión (Salto):** Si el jugador salta con la barra espaciadora antes de pisarla, puede sobrevolar la columna 13 y aterrizar a salvo al otro lado sin disparar la trampa.

---

## 2. Parámetros y Constantes
- **Recámara Activa:** `CurrentChamber == 1`.
- **Coordenada de la Trampa:** `Trap2Coord = new GridCoord(13, 5)`.
- **Superficie de Contacto Nivel 2:** $Y = 64\text{ px}$ (Fila 4).
- **Umbral de Cierre:** $Y \ge 96\text{ px}$ (cota inferior de fila 5, despejando el torso del jugador).
- **Mensaje Retro de Notificación:** `"!TRAMPA MORTAL! QUEDAS ATRAPADO"` (120 frames).
- **Penalización:** Eliminación del jugador y descuento de 1 vida (`player.Lives - 1`).

---

## 3. Máquina de Estados de la Trampa
- **Inicial / Cerrada:** La baldosa en `(13, 5)` es `TileType.SolidWall`, renderizada como trampilla cerrada (`_trapClosedTexture`).
- **Abierta (Disparada):** Al pisar caminando, `Trap2.Open()`, la baldosa en `(13, 5)` se convierte en `TileType.Empty`, y `player.ForceFall(grid)`.
- **Cerrada (Atrapado):** Cuando `player.Position.Y >= 96`, `Trap2.Close()`, la baldosa en `(13, 5)` vuelve a ser `TileType.SolidWall`.
- **Fatalidad:** Al impactar contra el fondo o suelo de Nivel 0, se ejecuta `player.Eliminate()`.
- **Reinicio:** Al reiniciar la recámara tras morir o pulsar Continuar, la trampa vuelve a su estado inicial.

---

## 4. Restricciones Técnicas (Zero Allocation)
- Cero asignaciones en memoria heap (`0 allocations`) en `Update()` y `Draw()`.
- Reutilización de la textura de trampilla `_trapClosedTexture` y `_trapOpenTexture` de `PuzzleRenderer`.
- Cadenas constantes internadas para notificaciones.

---

## 5. Criterios de Aceptación (QA)
1. En Recámara 1, la trampa de Nivel 2 se ubica en columna 13, fila 5.
2. Si el jugador salta sobre la columna 13, la trampa no se abre y el jugador aterriza sano y salvo.
3. Si el jugador camina sobre la columna 13, la trampa se abre y el jugador cae.
4. En cuanto el jugador desciende por debajo de la trampa ($Y \ge 96\text{ px}$), la trampilla se cierra de nuevo (`SolidWall`).
5. El jugador cae hasta el Nivel 0 y queda eliminado, descontándole 1 vida.
6. Se muestra la notificación: `"!TRAMPA MORTAL! QUEDAS ATRAPADO"`.
7. En la Recámara 2 esta trampa permanece inactiva (`HasTrap2 == false`).
8. `dotnet build -warnaserror` compila con 0 advertencias y 0 errores.
9. 100% de pruebas unitarias pasando (`dotnet test`).
