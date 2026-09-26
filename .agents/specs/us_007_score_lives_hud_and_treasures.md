# Especificación Técnica: [US-007] HUD Superior Derecho (Puntaje y Vidas), Sistema de 3 Vidas y Tesoro en Nivel 2

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-26  
**Componentes:** `src/Entities/Player.cs`, `src/Systems/PuzzleManager.cs`, `src/Graphics/PuzzleRenderer.cs`, `src/Graphics/PixelFont.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
1. **HUD Superior Derecho (Puntuación y Vidas):** Incorporar un panel retro en el lateral superior derecho (`X = 212, Y = 18`, ancho `90 px`, alto `28 px`), simétrico al panel de menú de la izquierda, que exhiba:
   - Título/Encabezado: `ESTADO` (en color oro arena).
   - Puntuación acumulada: `PUNTOS: <score>`.
   - Vidas restantes: `VIDAS: <lives>`.
2. **Sistema de Vidas:**
   - El jugador inicia con 3 vidas (la activa + 2 de reserva).
   - Al sufrir una eliminación (por caída fatal de $\ge 2$ niveles), se descuenta 1 vida.
   - Si quedan vidas ($> 0$), al pulsar `2` re-inicia la cámara continuando la partida con las vidas restantes y conservando el puntaje acumulado.
   - Si las vidas llegan a 0, se entra en estado de `GAME OVER`: se despliega el mensaje de `GAME OVER` y **únicamente la opción `1: SALIR AL MENU`** (la opción de reinicio no está disponible).
3. **Segundo Tesoro en Plataforma Nivel 2:**
   - Se ubica un nuevo cofre de tesoro en la Plataforma Nivel 2 a la izquierda (`GridCoord(8, 4)`).
   - Al ser recolectado por el arqueólogo, suma 1000 puntos a la puntuación del jugador y despliega la notificación en pantalla.
   - Se mantiene el tesoro de la Plataforma 0 (`GridCoord(17, 13)`). Ambos cofres pueden ser recolectados independientemente.

---

## 2. Parámetros y Constantes
- **Vidas iniciales:** `INITIAL_LIVES = 3`.
- **Coordenadas Segundo Tesoro:** `Treasure2Coord = new GridCoord(8, 4)` (Plataforma 2, extremo izquierdo).
- **Valor de cada tesoro:** `1000 puntos`.
- **Posición Panel HUD Derecho:** `X = 212, Y = 18, Ancho = 90, Alto = 28`.

---

## 3. Restricciones Técnicas (Zero Allocations)
- Estrictamente 0 asignaciones en heap durante `Update()` y `Draw()`.
- Dibujo numérico mediante conversión directa de dígitos ASCII en `PixelFont` (`DrawMiniInt`) sin interpolación de strings (`$""`) por frame.
- Textos estáticos preasignados.

---

## 4. Criterios de Aceptación (QA)
1. **Visualización HUD Derecho:** En juego activo, se renderiza el panel en el lateral superior derecho con `ESTADO`, `PUNTOS: <pts>` y `VIDAS: <n>`.
2. **Vidas Iniciales:** Al comenzar la partida (o Nuevo Juego), el jugador cuenta con 3 vidas.
3. **Descuento de Vidas:** Al ser eliminado por caída fatal, las vidas disminuyen en 1.
4. **Segundo Tesoro Funcional:** En la Plataforma Nivel 2 (extremo izquierdo) existe un cofre que al entrar en contacto se abre, incrementa 1000 puntos y muestra el aviso de tesoro encontrado.
5. **Persistencia y Reinicio:** Ambos tesoros pueden recolectarse para un total de 2000 puntos.
6. **Compilación y Tests:** `dotnet build` compila con 0 errores y 0 advertencias, y `dotnet test` pasa todas las pruebas unitarias.
