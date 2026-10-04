# Especificación Técnica: [US-014] Momia Guardiana en Plataforma 1 de Recámara 1 (Despertar al Recoger la Llave)

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-10-04  
**Componentes:** `src/Entities/Mummy.cs`, `src/Graphics/MummyRenderer.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
En la **Recámara 1**, se añade una segunda momia guardiana en la **Plataforma Nivel 1** (fila 9 de la cuadrícula, superficie de tránsito fila 8):
1. **Posición Inicial:** Situada en el extremo izquierdo de la Plataforma 1 (columna 2, $X = 32\text{ px}$, $Y = 128\text{ px}$), orientada hacia la derecha (`Direction.Right`).
2. **Estado Inicial (Inmóvil / Dormida):**
   - Comienza en estado estático (`IsAwake = false`).
   - No realiza patrulla ni desplazamiento mientras esté dormida.
   - En la presentación gráfica, permanece estática en su pose de guardia sin alternar animación de pasos.
   - Si el arqueólogo colisiona con ella mientras duerme, es letal (pierde 1 vida).
3. **Mecánica de Activación (Despertar con la Llave):**
   - Al recoger el jugador la llave sagrada colgada (`puzzleManager.HasKey == true`), la momia guardiana despierta de inmediato (`IsAwake = true`).
   - Al despertar, comienza a patrullar a velocidad arcade ($1.5\text{ px/frame}$) entre la columna 2 ($X = 32$) y la columna 10 ($X = 162$), cubriendo el acceso hacia la Escalera 2 y la Escalera 1.
4. **Comportamiento por Recámara:**
   - La momia de Plataforma 1 solo está activa en la **Recámara 1** (`IsActive = true`).
   - En la **Recámara 2**, esta momia permanece desactivada (`IsActive = false`) y no se actualiza ni se renderiza.

---

## 2. Parámetros y Constantes
- **Entidad:** `Mummy _mummyPlatform1`
- **Ubicación de Spawn (Recámara 1):**
  - $X = 32\text{ px}$ (columna 2)
  - $Y = 128\text{ px}$ (fila 8, sobre suelo de fila 9)
  - $\text{MinX} = 32\text{ px}$ (borde izquierdo de Plataforma 1)
  - $\text{MaxX} = 162\text{ px}$ (borde derecho de sección 1 de Plataforma 1, antes del hueco de columna 11)
  - Orientación inicial: `Direction.Right`
  - Estado inicial: `IsAwake = false`
- **Velocidad de Patrulla:** $1.5\text{ px/frame}$ (igual a la velocidad del jugador)
- **Hitbox:** $14 \times 16\text{ px}$ con margen retro de tolerancia de 2 px.

---

## 3. Máquina de Estados de la Momia (`Mummy`)
Se amplía la clase `Mummy` con las siguientes propiedades:
- `public bool IsActive { get; set; } = true;`
- `public bool IsAwake { get; set; } = true;`
- Método `public void WakeUp()`: activa `IsAwake = true`.
- En `Update(Player player)`:
  - Si `!IsActive`, retorna inmediatamente sin procesar.
  - Si `!IsAwake`, no desplaza la posición $X$, pero sí comprueba colisión AABB letal con el jugador.
  - Si `IsAwake`, avanza según su orientación e invierte dirección en `MinX` y `MaxX`, comprobando colisión con el jugador.

---

## 4. Restricciones Técnicas (Zero Allocation)
- Cero asignaciones en memoria heap (`0 allocations`) en `Update()` y `Draw()`.
- Reutilización de la instancia de `MummyRenderer` para dibujar ambas momias.
- Cuando `IsAwake == false`, `MummyRenderer` utiliza el frame fijo de reposo (`_walk1Texture`).

---

## 5. Criterios de Aceptación (QA)
1. Al iniciar la Recámara 1, la momia de Plataforma 1 aparece en $X = 32$, $Y = 128$ y permanece totalmente inmóvil.
2. Mientras la llave no haya sido recogida (`HasKey == false`), la momia no se mueve de $X = 32$.
3. Al recoger la llave (`HasKey == true`), la momia despierta y comienza a patrullar horizontalmente entre $X = 32$ y $X = 162$.
4. Si el jugador colisiona con la momia (esté dormida o despierta), pierde una vida (`player.Eliminate()`).
5. Al reiniciar la recámara (`RestartCurrentRoom` / `LoadChamber(1)`), la momia vuelve a su posición inicial en $X = 32$ y a su estado inmóvil (`IsAwake = false`).
6. En la Recámara 2, la momia de Plataforma 1 no está activa (`IsActive = false`) y no interfiere con la jugabilidad.
7. El proyecto compila con 0 advertencias y 0 errores (`dotnet build -warnaserror`).
8. 100% de las pruebas unitarias pasan exitosamente (`dotnet test`).
