# Especificación Técnica: [US-019] Pista Visual Egipcia en el Fondo del Nivel 2 (Mural Jeroglífico del Puzle de la Trampa y Momia)

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-10-09  
**Componentes:** `src/Graphics/HieroglyphRenderer.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/Graphics/HieroglyphRendererTests.cs`, `chamber_puzzles_catalog.md`  
**Estado:** EN IMPLEMENTACIÓN  

---

## 1. Descripción
En el fondo de la pantalla de la **Recámara 1**, sobre la pared de piedra del **Nivel 2** (filas 1 a 4 de la cuadrícula, $Y \in [16, 80]$, entre el techo y la plataforma superior), se incorpora un **tercer mural jeroglífico en bajorrelieve egipcio antiguo**:
- **Ubicación en Nivel 2:** En la pared superior visible, con los glifos principales situados en $X \in [120, 286]$ para no quedar tapados por la caja retro de opciones del HUD (situada en $X \in [18, 108]$) ni por los personajes apoyados en la plataforma ($Y \ge 64$).
- **Contraste nítido:** Utiliza la paleta de contraste mejorado calibrada para bajorrelieves de pared, permitiendo que el jugador distinga claramente la pista sobre el fondo oscuro de la tumba (`Color(18, 12, 22)`).
- **Pista visual para el Puzle 6 (US-018):** Representa mediante iconografía y jeroglíficos la fórmula completa del desafío:
  $$\text{[Momia} + \text{Trampa Abierta con 3 Marcas (III)]} \longrightarrow \text{[Momia Cayendo por la Trampa 3 Veces (3x ⬇)]} \longrightarrow \text{[Nuevo Tesoro Sagrado Renacido en Cámara (☥)]}$$

---

## 2. Composición Visual e Iconografía en Pixel-Art

El mural mide $320 \times 64\text{ px}$ y se renderiza en $Y = 16$ (cubriendo las filas 1 a 4):

1. **Cenefa Decorativa de Arenisca ($X \in [116, 290]$, $Y = 4..6$ y $Y = 44..46$):**
   - Moldura ceremonial tallada con hendiduras de cincelado (`groove`), arenisca (`stone`) y acentos en ocre (`ochre`).

2. **Glifo 1: Momia y Trampa de Suelo con 3 Marcas Sagradas ("III") ($X \in [124, 168]$):**
   - **Tres Marcas Sagradas ("III"):** Tres bastones verticales dorados (`fadedGold` / `radiantGold`) en lo alto ($Y = 8..12$), indicando las 3 caídas requeridas.
   - **Momia Guardiana:** Silueta erguida de la momia con vendas en arenisca y ocre (`stone`, `ochre`), ojos carmesí (`redOchre`) y pectoral ceremonial.
   - **Trampa con Compuerta:** Trampilla en el suelo con bisagras y líneas de advertencia talladas.

3. **Conector Ritual 1 ($X \in [172, 180]$):**
   - Flecha ritual hacia la derecha (`->`) en pigmento ocre rojizo (`redOchre` / `ochre`).

4. **Glifo 2: Momia Precipitándose por la Trampa Abierta ($X \in [184, 228]$):**
   - **Trampa Abierta con Foso:** Plataforma con vano central abierto hacia el abismo inferior.
   - **Flecha Ritual Descendente (⬇):** Flecha vertical en pigmento rojo terracota apuntando directamente al hueco de la trampa.
   - **Momia en Caída Libre:** Silueta de la momia precipitándose verticalmente en el foso.
   - **Indicador Ritual de 3 Caídas:** Tres marcas doradas `| | |` (`radiantGold`) junto a la flecha descendente.

5. **Conector Ritual 2 ($X \in [232, 240]$):**
   - Flecha ritual hacia la derecha (`->`) en pigmento ocre rojizo (`redOchre` / `ochre`).

6. **Glifo 3: Momia Derrotada y Nuevo Tesoro en la Cámara ($X \in [244, 286]$):**
   - **Cofre Sagrado Radiante Renacido:** Arca ceremonial renovada (`Treasure4`) con tapa de oro celestial (`radiantGold`), resplandor místico y gema turquesa central (`turquoise`).
   - **Cruz Ankh (☥):** Símbolo sagrado de resurrección / vida a la derecha del cofre, confirmando que un nuevo tesoro se engendra en la cámara de Nivel 0.

---

## 3. Paleta Cromática (Contraste Mejorado de Bajorrelieve)
- Fondo de la cámara: `RGB(18, 12, 22)`.
- Hendiduras / sombras de cincelado (`groove`): `RGB(38, 28, 34)`.
- Superficie de piedra tallada (`stone`): `RGB(78, 58, 48)`.
- Pigmento ocre egipcio (`ochre`): `RGB(105, 78, 50)`.
- Oro ceremonial antiguo (`fadedGold`): `RGB(136, 102, 52)`.
- Oro radiante místico (`radiantGold`): `RGB(165, 126, 62)`.
- Turquesa / malaquita egipcia (`turquoise`): `RGB(45, 96, 90)`.
- Rojo terracota ritual (`redOchre`): `RGB(115, 48, 38)`.

Restricciones de brillo: $R \le 170$, $G \le 135$, $B \le 95$. Permite distinguir nítidamente la iconografía sin competir con los elementos en primer plano ($188-255$).

---

## 4. Arquitectura Técnica y Rendimiento (Zero Allocation)
1. **Dimensiones y Constantes en `HieroglyphRenderer`:**
   - `MURAL_LEVEL2_WIDTH = 320`
   - `MURAL_LEVEL2_HEIGHT = 64`
   - `MURAL_LEVEL2_Y = 16`
2. **Métodos:**
   - `GenerateMuralLevel2Pixels(int width, int height)`: Procedural, desacoplado de `GraphicsDevice` para pruebas unitarias.
   - `GenerateMuralLevel2Texture(GraphicsDevice graphicsDevice)`: Instancia `Texture2D` y carga píxeles en inicialización.
   - `Dispose()`: Libera `_muralLevel2Texture`.
   - `Draw(SpriteBatch spriteBatch, int chamberNumber)`: Renderiza `_muralLevel2Texture` en `_destRectLevel2` si `chamberNumber == 1`. Cero allocations en el bucle continuo.

---

## 5. Criterios de Aceptación (QA)
1. El mural de Nivel 2 solo se dibuja en la Recámara 1 (`chamberNumber == 1`).
2. La textura mide exactamente $320 \times 64\text{ px}$ y se renderiza en $Y = 16$.
3. Contiene la secuencia completa: Momia + Trampa con marcas III -> Flecha -> Momia cayendo con flecha ⬇ -> Flecha -> Cofre renovado con Ankh.
4. Totalmente libre de memory allocations en cada frame de `Draw()` (`0 allocations`).
5. Todas las pruebas unitarias pasan al 100% (`dotnet test`).

