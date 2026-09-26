# Especificación Técnica: [US-004] Desafío de la Plataforma 0: Muro Conmutable y Tesoro del Faraón

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-26  
**Componentes:** `src/Entities/Treasure.cs`, `src/Systems/PuzzleManager.cs`, `src/Graphics/TreasureRenderer.cs`, `Game1.cs`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
Implementar el puzle de la plataforma 0 (piso inferior de la cámara):
1. Un **Muro Secreto** bloquea el paso hacia el fondo de la plataforma en la columna 14.
2. Detrás del muro, en la columna 17, yace el **Tesoro del Faraón** (cofre sagrado de oro).
3. En la misma plataforma 0, se encuentra una **Piedra / Losa Rúnica** de activación en la columna 3 (o columna 8).
4. **Mecánica de Conmutación (Toggle por pisada):**
   - **Primera pisada:** Al pisar la piedra por primera vez, el muro se abre (se retrae o se vuelve transitable), permitiendo el paso hacia el tesoro.
   - **Segunda pisada:** Al volver a pisar la piedra (tras salir de ella y volver a entrar), el muro se cierra de nuevo, bloqueando el paso.
   - **Pisadas sucesivas:** Alterna el estado (abierto <-> cerrado).
   - **Filtrado de permanencia:** Quedarse quieto sobre la piedra no conmuta el muro repetidamente; requiere entrar nuevamente en contacto con ella.

---

## 2. Parámetros de la Cuadrícula (Plataforma 0)
- **Posición del Arqueólogo (Inicio):** `(2, 13)`.
- **Posición de la Piedra Conmutable (`SwitchStone`):** `(8, 13)` (entre la escalera en col 5 y el muro en col 14).
- **Posición del Muro del Tesoro (`TreasureWall`):** `(14, 13)` (columna 14, fila 13).
- **Posición del Tesoro (`Treasure`):** `(17, 13)` (columna 17, fila 13).

---

## 3. Estados del Sistema
- `WallState`:
  - `Closed`: El tile en `(14, 13)` es sólido (`SolidWall`).
  - `Open`: El tile en `(14, 13)` es transitable (`Empty`).
- `TreasureState`:
  - `Available`: Visible y recolectable.
  - `Collected`: Ya recogido por el arqueólogo, sumando puntaje y mostrando notificación retro.

---

## 4. Restricciones Técnicas (Zero Allocation)
- Cero asignaciones en memoria heap (`0 allocations`) en `Update()` y `Draw()`.
- Texturas pixel-art del cofre del tesoro y la losa rúnica generadas en la carga inicial.
- Lógica desacoplada y comprobable mediante pruebas unitarias (`dotnet test`).

---

## 5. Criterios de Aceptación (QA)
1. Al iniciar la partida, el muro en `(14, 13)` está cerrado y no permite el paso del jugador.
2. Al pisar la piedra en `(8, 13)`, el muro se abre inmediatamente y la celda se vuelve transitable.
3. El jugador puede cruzar la columna 14 y recoger el tesoro en `(17, 13)`.
4. Si el jugador vuelve a pisar la piedra por segunda vez, el muro se cierra volviendo a ser sólido.
5. El proyecto compila con 0 advertencias y 0 errores (`dotnet build`).
6. Pruebas unitarias validan la lógica de conmutación y recolección (`dotnet test`).
