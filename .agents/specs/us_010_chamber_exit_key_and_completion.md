# Especificación Técnica: [US-010] Puerta de Salida en Plataforma 2, Llave Colgada en Plataforma 1 y Fin de Recámara

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-26  
**Componentes:** `src/Entities/Key.cs`, `src/Grid/RoomGrid.cs`, `src/Systems/PuzzleManager.cs`, `src/Graphics/PuzzleRenderer.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción de la Mecánica

1. **Ampliación de Plataforma 2 a la Izquierda y Puerta de Salida:**
   - En la **Plataforma Nivel 2** (fila 5), se extiende el suelo hacia el extremo izquierdo:
     - `Columnas 4 a 7`: Suelo firme (`SolidWall`), conectando con la sección principal preexistente (`Columnas 8 a 17`).
     - `Columna 3`: **Hueco de salto** (`Empty`, 16 px de ancho).
     - `Columnas 1 y 2`: Descansillo elevado (`SolidWall`) sobre el que se ubica la puerta de salida.
   - **Puerta de Salida:**
     - Ubicada en `(1, 4)` descansando sobre el suelo de la celda `(1, 5)`.
     - Permite salir de la recámara y dar por terminada la expedición en la Recámara 1.
   - **Mecánica de Salto y Caída:**
     - El salto arcade de 40 px permite saltar desde la Columna 4 sobre el hueco de la Columna 3 para aterrizar en la repisa de las Columnas 1 y 2, y retornar saltando a la derecha.
     - Si el jugador cae por el hueco de la Columna 3, desciende de la fila 5 a la fila 9 (Plataforma Nivel 1).
     - Al ser una diferencia de $2 - 1 = 1$ nivel, la caída es **segura**: aterriza en sus pies sin perder vidas.

2. **Llave Colgada en la Plataforma Nivel 1 a la Derecha:**
   - Nueva entidad [`Key`](file:///c:/Users/rob_r/Sources/Mono/RetrosGames/RetroGamePiramid/src/Entities/Key.cs) ubicada en `(16, 6)`.
   - **Mecánica de Salto Requerido:**
     - El suelo de la Plataforma 1 está en la fila 9 ($Y = 144$).
     - El jugador caminando mide 16 px de alto y sus pies están en $Y = 144$, por lo que su cabeza alcanza $Y = 128$ (fila 8).
     - La llave cuelga a la altura de la fila 6 ($Y = 96$ a $112$).
     - Al caminar, la distancia libre entre la cabeza del jugador ($Y = 128$) y la base de la llave ($Y = 112$) es de 16 px, impidiendo recogerla caminando.
     - Al saltar con la altura arcade calibrada de 28 px, el jugador alcanza $Y = 100$, intersectando el hitbox de la llave y recolectándola.
   - **Efectos al Recoger la Llave:**
     - `HasKey = true`.
     - Otorga 500 puntos de bonificación.
     - Muestra notificación retro: `"!LLAVE ENCONTRADA! PUERTA ABIERTA"`.
     - La llave desaparece del escenario y se refleja en el HUD.

3. **Interacción con la Puerta de Salida:**
   - Si el jugador llega a la puerta en `(1, 4)` **sin la llave**, la puerta permanece bloqueada y se notifica: `"!PUERTA CERRADA! NECESITAS LA LLAVE"`.
   - Si el jugador llega a la puerta **con la llave**, la puerta se abre, finalizando la recámara (`IsChamberCompleted = true`).

4. **Pantalla / Modal de Fin de Recámara:**
   - Al finalizar la recámara, se pausa el juego activo y se despliega un modal centralizado estilo arcade retro:
     - Título: `!RECAMARA 1 COMPLETADA!`
     - Resumen de puntos: `PUNTOS OBTENIDOS: XXXX`
     - Vidas restantes: `VIDAS RESTANTES: X`
     - Opciones interactivas:
       - `1: PASAR A OTRA RECAMARA` $\rightarrow$ Avanza a la siguiente recámara (Recámara 2).
       - `2: VOLVER A JUGAR` $\rightarrow$ Re-inicia la misma recámara para mejorar puntaje.
       - `3: SALIR AL MENU` $\rightarrow$ Regresa al menú principal guardando opción de continuar.

---

## 2. Parámetros y Constantes

- **Coordenadas Puerta de Salida:** `GridCoord(1, 4)`, descansando en `(1, 5)`.
- **Coordenadas Llave Colgada:** `GridCoord(16, 6)`.
- **Hitbox Llave:** $X \in [256, 272]$, $Y \in [96, 112]$.
- **Puntaje por Llave:** `+500 puntos`.
- **Hueco de Salto Plataforma 2:** `Columna 3, Fila 5` (`Empty`, 16 px).
- **Descansillo Puerta Plataforma 2:** `Columnas 1 y 2, Fila 5` (`SolidWall`).
- **Extensión Plataforma 2:** `Columnas 4 a 17, Fila 5` (`SolidWall`).
- **Dimensiones Modal Fin de Recámara:** Ancho `200 px`, Alto `88 px`, Centrado en pantalla (`(320-200)/2 = 60`, `(240-88)/2 = 76`).
- **Teclas de Control Modal:** Tecla `1` / NumPad 1, Tecla `2` / NumPad 2, Tecla `3` / NumPad 3 / Escape.

---

## 3. Restricciones Técnicas (Zero Allocations)
- Cero asignaciones en memoria heap en `Update()` y `Draw()`.
- Texturas de la llave (`_keyTexture`) y avisos generados en la GPU durante la carga.
- Dibujado numérico y de textos mediante búferes `stackalloc char[16]` en [`PixelFont`](file:///c:/Users/rob_r/Sources/Mono/RetrosGames/RetroGamePiramid/src/Graphics/PixelFont.cs).

---

## 4. Criterios de Aceptación (QA)
1. **Configuración de Baldosas:**
   - `RoomGrid.GetTile(1, 4)` es `ExitDoor`.
   - `RoomGrid.GetTile(1, 5)` y `(2, 5)` son `SolidWall`.
   - `RoomGrid.GetTile(3, 5)` es `Empty`.
   - `RoomGrid.GetTile(x, 5)` para $x \in [4, 17]$ son `SolidWall` (excepto col 10 escalera).
2. **Llave Colgada y Salto Requerido:**
   - Caminar por debajo de la llave en la Plataforma 1 (fila 9) no recoge la llave.
   - Saltar hacia arriba debajo de la llave en Col 16 colisiona con ella, marcándola como recogida y otorgando 500 puntos.
3. **Bloqueo y Apertura de la Puerta:**
   - Tocar la puerta sin la llave no finaliza la recámara y despliega aviso de bloqueo.
   - Tocar la puerta con la llave activa `IsChamberCompleted = true`.
4. **Modal de Fin de Recámara y Opciones:**
   - Pulsar `1` pasa a otra recámara (Recámara 2).
   - Pulsar `2` re-inicia la misma recámara.
   - Pulsar `3` o Escape regresa al menú principal.
5. **Compilación y Tests:**
   - Compilación con `dotnet build -warnaserror` (0 errores, 0 advertencias).
   - 100% de las pruebas automatizadas superadas (`dotnet test`).
