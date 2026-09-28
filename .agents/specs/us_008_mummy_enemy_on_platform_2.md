# Especificación Técnica: [US-008] Entidad Enemiga Momia (Mummy) en Plataforma Nivel 2

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-26  
**Componentes:** `src/Entities/Mummy.cs`, `src/Graphics/MummyRenderer.cs`, `src/Entities/Player.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
1. **Nueva Entidad Momia (`Mummy`):**
   - Enemigo patrullero de temática egipcia clásica de pirámides arcade.
   - Aparece desde el inicio en la **Plataforma Nivel 2** (fila 4 transitable, Y = 64).
   - Patrulla horizontalmente y de forma autónoma por todo el piso del Nivel 2 (columnas 4 a 17, X = 64 a 274 px), solo hasta los espacios vacíos sin piso (columna 3 a la izquierda y columna 18 a la derecha).
   - Velocidad idéntica a la del arqueólogo (`Player.WALK_SPEED = 1.5f` px/frame).
   - Al llegar a los límites con los espacios sin piso, invierte el sentido de su marcha (rebote de patrulla izquierda/derecha sin caer al vacío).
2. **Colisión, Evasión y Salto:**
   - **Salto sobre la momia:** La altura de salto del arqueólogo se calibra a `28 px` y la distancia horizontal a `40 px` con margen de tolerancia retro de `2 px`, permitiendo saltar y esquivar limpiamente por encima de la momia sin ser alcanzado.
   - Si la momia entra en contacto físico directo (colisión AABB) con el arqueólogo, este queda inmediatamente **eliminado** y **pierde 1 vida**.
   - Si tras la colisión las vidas del jugador llegan a 0, se despliega la pantalla de `GAME OVER` (solo con opción de salir al menú). Si aún le quedan vidas, se muestra la pantalla de `ELIMINADO` con las opciones:
     - `1: SALIR AL MENU`
     - `2: CONTINUAR` (re-inicia la cámara conservando las vidas restantes).
3. **Reinicio y Persistencia:**
   - Al re-iniciar la recámara (pulsar `2` o inicio de partida), la momia vuelve a su posición y dirección inicial.

---

## 2. Parámetros y Constantes
- **Dimensiones Hitbox:** Ancho `14 px`, Alto `16 px` (idéntico al jugador).
- **Físicas de Salto del Jugador:** Altura `JUMP_HEIGHT_PIXELS = 28f` px, Distancia `JUMP_HORIZONTAL_DISTANCE = 40f` px.
- **Tolerancia de Colisión:** Margen de `2 px` para paso aéreo limpio.
- **Velocidad de patrulla:** `Mummy.SPEED = Player.WALK_SPEED = 1.5f` px/frame.
- **Rango de patrulla (Plataforma Nivel 2 - Toda la plataforma hasta espacios sin piso):**
  - `MinX = 4 * 16 = 64f` px (extremo izquierdo de la plataforma continua, justo en el borde del hueco sin piso de la columna 3).
  - `MaxX = 18 * 16 - 14 = 274f` px (extremo derecho de la plataforma continua, justo en el borde del hueco sin piso de la columna 18).
  - `Y = 4 * GameConstants.TILE_SIZE = 64f` px.
- **Spawn inicial:** Columna 13 (`X = 13 * 16 = 208f`, `Y = 64f`), dirección inicial hacia la derecha (`Direction.Right`).
- **Opciones Modal Eliminado (con vidas):** `1: SALIR AL MENU` y `2: CONTINUAR`.

---

## 3. Restricciones Técnicas (Zero Allocations)
- Cero asignaciones en memoria heap (`0 allocations`) durante `Update()` y `Draw()`.
- Generación procedural de texturas de la momia (2 frames de animación para marcha) en tiempo de carga (`LoadContent`).
- Estructura desacoplada de entidad (`Mummy.cs`) y renderizado gráfico (`MummyRenderer.cs`).

---

## 4. Criterios de Aceptación (QA)
1. **Patrulla Autónoma:** La momia se desplaza a velocidad 1.5 px/frame por la plataforma 2 y rebota en los extremos (X=64 y X=274) cubriendo todo el suelo sin caer en los espacios vacíos.
2. **Letalidad por Contacto:** Al colisionar con el jugador, el jugador entra en estado `Eliminated` y sus vidas disminuyen en 1.
3. **No Múltiples Descuentos:** Una vez eliminado, la permanencia del contacto no descuenta vidas adicionales en frames sucesivos.
4. **Reinicio en Posición Inicial:** Al re-iniciar la recámara, la momia se restablece en su posición de spawn.
5. **Compilación y Tests:** `dotnet build` compila con 0 errores y 0 advertencias, y `dotnet test` pasa el 100% de las pruebas automatizadas.
