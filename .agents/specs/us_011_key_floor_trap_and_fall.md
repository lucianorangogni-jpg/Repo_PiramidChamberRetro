# Especificación Técnica: [US-011] Trampa de Suelo Bajo la Llave y Caída al Nivel Inferior

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-28  
**Componentes:** `src/Entities/FloorTrap.cs`, `src/Systems/PuzzleManager.cs`, `src/Entities/Player.cs`, `src/Graphics/PuzzleRenderer.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
1. **Trampa de Suelo Bajo la Llave (`FloorTrap`):**
   - En la Plataforma Nivel 1 (fila 9), directamente debajo de la llave colgada (`KeyCoord = (16, 6)`), se sitúa una trampa de suelo en la baldosa `(16, 9)`.
   - Inicialmente la trampa se encuentra **cerrada**, presentando la apariencia de una trampilla de piedra rúnica sólida sobre la cual no hay hueco visible.
2. **Activación de la Trampa y Caída al Nivel Inferior:**
   - Si el arqueólogo pasa por debajo de la llave caminando o apoyado sobre el suelo de la Plataforma 1 (fila 9, columnas 15-16):
     - La trampa se **abre inmediatamente** (`Trap.Open()`, `IsTrapOpen = true`).
     - La celda `(16, 9)` de la cuadrícula pasa a ser transitable sin suelo (`grid.SetTile(16, 9, TileType.Empty)`).
     - El jugador pierde la sustentación y entra de inmediato en estado de caída libre (`PlayerState.Falling`).
     - El jugador cae por gravedad a través del hueco abierto desde la Plataforma 1 ($Y = 128$) hasta el suelo del Nivel 0 ($Y = 208$, fila 14).
     - Al ser una caída de 1 solo nivel ($1 - 0 = 1$), la caída es **no letal**: el jugador aterriza ileso en el Nivel 0 conservando todas sus vidas.
     - Como el jugador iba por el suelo ($Y = 128$) y la llave cuelga a $Y = 96$, el jugador nunca llega a alcanzar la llave.
     - Para asegurar que el jugador pueda regresar sin quedar atrapado, la apertura de la trampa abre también el pasaje del muro secreto `(14, 13)` si estuviese cerrado, permitiendo al jugador caminar hacia la izquierda, subir por la Escalera 1 (columna 5) y reintentar.
3. **Mecánica Exclusiva de Salto para Recoger la Llave:**
   - Para tomar la llave colgada en `(16, 6)`, el jugador **debe saltar antes de llegar a la posición donde cuelga** (es decir, iniciar el salto desde la columna 14 o 15 dirigiéndose a la derecha).
   - Al saltar antes de llegar:
     - El jugador entra en `PlayerState.Jumping` y describe su parábola arcade con elevación de $28\text{ px}$.
     - En la cúspide del salto (frame 12, sobrevolando la columna 16 a $Y \approx 100$), el jugador colisiona en el aire con la llave colgada ($Y \in [96, 112]$) y la recolecta exitosamente (`Key.Collect()`, +500 PTS).
     - El desplazamiento horizontal del salto ($40\text{ px}$) transporta al jugador por el aire sobre la trampa y lo hace aterrizar de forma segura sobre la columna 17 ($X \approx 284$), donde el piso es firme.
     - Al estar en el aire durante el salto, la trampa no lo hace caer.
     - Para regresar desde la columna 17, el jugador puede saltar de vuelta hacia la izquierda sobrevolando el hueco de la trampa y aterrizando en la columna 15.

---

## 2. Parámetros y Constantes
- **Coordenada de la Llave:** `KeyCoord = new GridCoord(16, 6)` ($X = 256$, $Y = 96$).
- **Coordenada de la Trampa:** `TrapCoord = new GridCoord(16, 9)` ($X = 256$, $Y = 144$).
- **Dimensiones:** Baldosa de $16 \times 16\text{ px}$. Hitbox jugador $14 \times 16\text{ px}$.
- **Físicas de Salto:** Altura $28\text{ px}$, Distancia horizontal $40\text{ px}$ (2.5 celdas).
- **Caída de 1 Nivel:** Desde Nivel 1 ($Y = 128$) a Nivel 0 ($Y = 208$), diferencia $= 1 < 2$, no letal.
- **Mensaje retro:** `"!TRAMPA ACTIVADA! EL PISO SE HA ABIERTO"` durante 120 frames (2 segundos).

---

## 3. Restricciones Técnicas (Zero Allocations)
- Cero asignaciones en memoria heap (`0 allocations`) durante `Update()` y `Draw()`.
- La entidad `FloorTrap.cs` utiliza propiedades directas y métodos mutadores sin crear objetos en el bucle de juego.
- `Player.ForceFall(RoomGrid grid)` transiciona el estado sin allocar memoria.
- `PuzzleRenderer` genera las texturas de la trampilla cerrada y abierta proceduralmente durante `LoadContent` (`GraphicsDevice`).

---

## 4. Criterios de Aceptación (QA)
1. **Apertura de Trampa al Pasar por Debajo:** Cuando el jugador camina por la Plataforma 1 hacia la columna 16, la trampa se abre y el jugador cae inmediatamente al Nivel 0 sin pérdida de vidas y sin haber tomado la llave.
2. **Llave Intacta tras Caída:** Tras caer el jugador por la trampa, la llave sigue sin recoger (`Key.IsCollected == false`).
3. **Muro de Escape:** Al abrirse la trampa, el muro `(14, 13)` queda transitable para que el jugador pueda salir del recinto inferior y volver a subir por la Escalera 1.
4. **Recolección en Salto:** Al saltar antes de llegar a la columna 16 (desde columna 15), el jugador recoge la llave en el aire (`Key.IsCollected == true`, +500 PTS) y aterriza a salvo en la columna 17.
5. **Salto de Regreso:** Desde la columna 17, el jugador puede saltar hacia la izquierda sobre el hueco de la trampa y aterrizar en la columna 15.
6. **Reinicio de Cámara:** Al reiniciar o recargar la recámara, la trampa vuelve a estar cerrada y la celda `(16, 9)` vuelve a ser sólida.
7. **Compilación y Tests:** `dotnet build -warnaserror` pasa con 0 errores y 0 advertencias, y `dotnet test` pasa el 100% de las pruebas automatizadas.
