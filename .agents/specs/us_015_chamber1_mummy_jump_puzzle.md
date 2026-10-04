# Especificación Técnica: [US-015] Puzle 5 de Recámara 1: Salto sobre la Momia, Persecución al Foso y Tesoro Ancestral

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-10-04  
**Componentes:** `src/Entities/Mummy.cs`, `src/Systems/PuzzleManager.cs`, `src/Graphics/PuzzleRenderer.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
En la **Recámara 1**, una vez que el jugador ha despertado a la momia guardiana de la **Plataforma Nivel 1** tras recoger la llave sagrada (`HasKey == true`), se activa la posibilidad de resolver un nuevo puzle acrobático de habilidad arcade:
1. **Desafío de Saltos:** El arqueólogo debe **saltar sobre la momia 4 veces consecutivas** en el Nivel 1.
2. **Modo Persecución:** Al completar el 4.º salto sobre ella, la momia se enfurece y entra en **Modo Persecución** (`IsChasing = true`), siguiendo horizontalmente la dirección del jugador a velocidad arcade.
3. **Caída al Nivel 0:** Si el jugador desciende o cae al Nivel 0 (a través de la trampa rúnica de la columna 16 o por los fosos/huecos de la plataforma), la momia lo sigue avanzando hacia el hueco y cae por gravedad al Nivel 0.
4. **Petrificación y Recompensa:** Al impactar contra el suelo del Nivel 0 ($Y = 208\text{ px}$, fila 13):
   - La momia se detiene y queda **inmóvil para siempre en el Nivel 0** (`IsAwake = false`, `IsChasing = false`, `HasReachedLevel0 = true`).
   - Se abre el muro de la cámara secreta y se materializa un **nuevo tesoro sagrado** en la misma posición donde ya existe en la cámara en el Nivel 0: **columna 17, fila 13** (`Treasure3Coord = (17, 13)`).
   - Se muestra la notificación: `"!MOMIA PETRIFICADA! TESORO EN CAMARA"`.
   - El cofre otorga **+1000 puntos** al ser recolectado y muestra: `"!TESORO ANCESTRAL REVELADO! +1000 PTS"`.

---

## 2. Parámetros y Constantes
- **Recámara Activa:** `CurrentChamber == 1`.
- **Condición Inicial del Puzle:** `_puzzleManager.HasKey == true` y `_mummyPlatform1.IsAwake == true`.
- **Saltos Requeridos:** `REQUIRED_JUMP_OVERS = 4`.
- **Velocidad de Caída de la Momia:** $2.5\text{ px/frame}$.
- **Destino del Tesoro Especial:** `Treasure3Coord = new GridCoord(17, 13)` (cámara del tesoro en Nivel 0).
- **Puntos por Tesoro:** `+1000 PTS`.
- **Mensaje al Enfurecer:** `"!MOMIA ENFURECIDA TE PERSIGUE!"` (120 frames).
- **Mensaje al Caer y Petrificarse:** `"!MOMIA PETRIFICADA! TESORO EN CAMARA"` (180 frames).
- **Mensaje al Recolectar Tesoro:** `"!TESORO ANCESTRAL REVELADO! +1000 PTS"` (180 frames).

---

## 3. Máquina de Estados y Lógica del Sistema
1. **Detección de Salto sobre la Momia:**
   - Durante `PlayerState.Jumping`, si el hitbox del jugador sobrevuela el hitbox horizontal de la momia ($X \in [\text{mummy.X}, \text{mummy.X} + \text{WIDTH}]$) estando por encima de ella ($Y + \text{Player.HEIGHT} \le \text{mummy.Y} + 4\text{ px}$), se marca `_jumpCrossedMummy = true`.
   - Al aterrizar con éxito el salto sin colisionar ni eliminarse, se computa `JumpOverCount++`.
   - Si `JumpOverCount >= 4`, se activa `_mummyPlatform1.StartChasing()`.
2. **Persecución y Gravedad de la Momia:**
   - En `IsChasing`:
     - La momia ajusta `_facing` hacia la posición $X$ del jugador.
     - Si no hay suelo sólido bajo la momia (`!grid.IsSolid(...)`), entra en `IsFalling = true` y desciende verticalmente.
     - Al alcanzar el suelo de Nivel 0 ($Y \ge 208\text{ px}$):
       - `_mummyPlatform1.LandOnLevel0()`.
       - `_puzzleManager.SpawnTreasure3()`.
3. **Reinicio de Cámara:**
   - Al morir o reiniciar recámara:
     - `JumpOverCount = 0`.
     - `_mummyPlatform1.Reset()`.
     - `Treasure3` queda oculto hasta que se vuelva a completar el puzle.

---

## 4. Restricciones Técnicas (Zero Allocation)
- Cero asignaciones en memoria heap (`0 allocations`) en `Update()` y `Draw()`.
- Cadenas constantes `const string` internadas para notificaciones.
- Variables de estado numéricas y booleanas en `Mummy.cs` y `PuzzleManager.cs`.

---

## 5. Criterios de Aceptación (QA)
1. Antes de recoger la llave, saltar sobre la momia no cuenta para el puzle.
2. Tras recoger la llave (momia despierta), saltar 4 veces sobre ella activa el modo persecución (`IsChasing == true`).
3. La momia persigue horizontalmente al jugador.
4. Cuando el jugador cae al Nivel 0 y la momia llega a un foso o a la trampa abierta, la momia cae por gravedad hasta el Nivel 0.
5. Al llegar la momia al Nivel 0, queda inmóvil (`IsAwake == false`, `IsChasing == false`, `HasReachedLevel0 == true`).
6. Se abre el muro y se materializa un nuevo cofre cerrado en la columna 17, fila 13 (`Treasure3`) en la cámara de Nivel 0.
7. El jugador puede saquear el cofre en `(17, 13)` sumando +1000 puntos.
8. En la Recámara 2 este puzle no existe y permanece inactivo.
9. Compilación limpia con `0` advertencias y `0` errores (`dotnet build -warnaserror`).
10. 100% de pruebas unitarias pasando (`dotnet test`).
