# Especificación Técnica: [US-016] Pista Visual Egipcia en el Fondo del Nivel 1 (Mural Jeroglífico del Puzle de la Momia)

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-10-04  
**Componentes:** `src/Graphics/HieroglyphRenderer.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/Graphics/HieroglyphRendererTests.cs`  
**Estado:** IMPLEMENTACIÓN Y PRUEBAS COMPLETADAS  

---

## 1. Descripción
En el fondo de la pantalla de la **Recámara 1**, sobre el muro de piedra del **Nivel 1** (entre la plataforma superior del Nivel 2 y la plataforma intermedia del Nivel 1, filas 6 a 8, $Y \in [96, 144]$), se incorpora un **segundo mural jeroglífico en bajo relieve egipcio antiguo**:
- **Tonalidad suave y sutil:** Grabado con tonos tenues y muy bajo contraste sobre el fondo oscuro de la tumba (`Color(18, 12, 22)`), de modo que apenas se distinga a simple vista, manteniendo total coherencia con el mural del Nivel 0.
- **Pista visual para el Puzle de la Momia (Puzle 2/5):** Representa mediante iconografía ancestral la secuencia completa para resolver el desafío:
  $$\text{[Momia} + \text{Salto 4x (IIII)]} \longrightarrow \text{[Momia Cayendo al Foso (⬇)]} \longrightarrow \text{[Nuevo Tesoro Sagrado Renacido (☥)]}$$

---

## 2. Composición Visual e Iconografía en Pixel-Art

El mural se ubica en el espacio vertical de la pared del Nivel 1 ($Y \in [96, 144]$, filas 6 a 8 de la cuadrícula, altura 48 px):

1. **Sello Sagrado Izquierdo ($X \in [32, 48]$):**
   - Escarabajo sagrado alado (Khepri) con disco solar y gema turquesa, símbolo egipcio de transformación y resurrección.

2. **Glifo 1: Momia y Salto de 4 Veces ($X \in [58, 102]$):**
   - **Momia Egipcia Erguida:** Figura vendada con ojos carmesí tenues y amuleto pectoral dorado.
   - **Parábola de Salto Curva:** Arco arqueado en relieve que sobrevuela de un lado a otro a la momia.
   - **Cuatro Marcas Sagradas ("IIII"):** Cuatro bastones rituales verticales dorados en la cúspide del salto, indicando de forma inequívoca los **4 saltos** requeridos.
   - **Silueta del Arqueólogo:** Figura saltadora estilizada en bajorrelieve en pleno vuelo sobre la momia.

3. **Conector Ritual 1 ($X \in [106, 114]$):**
   - Flecha ritual hacia la derecha (`->`) en pigmento ocre rojizo (`redOchre`).

4. **Glifo 2: Momia Precipitándose al Foso ($X \in [120, 164]$):**
   - **Plataforma y Abismo:** Dos secciones de suelo de piedra separadas por un hueco o foso sin suelo.
   - **Momia en Caída Vertical:** Silueta de la momia descendiendo con vendas ondeando hacia arriba.
   - **Flecha Ritual Descendente (⬇):** Flecha vertical en ocre rojizo apuntando directamente hacia el foso, indicando la necesidad de guiar a la momia a caer al vacío del piso inferior.

5. **Conector Ritual 2 ($X \in [168, 176]$):**
   - Flecha ritual hacia la derecha (`->`) en pigmento ocre rojizo (`redOchre`).

6. **Glifo 3: Nuevo Cofre Sagrado Renacido ($X \in [182, 218]$):**
   - **Cofre Sagrado Radiante:** Arca ceremonial renovada con rayos místicos dorados, tapa de oro celestial y gema turquesa central.
   - **Cruz Ankh (☥):** Símbolo sagrado de la vida y renacimiento a la derecha del cofre, confirmando que un nuevo tesoro renace en la cámara de Nivel 0.

---

## 3. Paleta Cromática y Restricciones Visuales (Ajuste de Contraste)
- Canal R: $\le 170$ (relieve de arenisca y oro ceremonial $\ge 75$)
- Canal G: $\le 135$
- Canal B: $\le 95$
- Los grabados se integran con elegancia en la pared de fondo con suficiente contraste para ser distinguidos claramente por el jugador, sin eclipsar las plataformas, momias, tesoros ni al arqueólogo (cuyo brillo alcanza 188-255).

---

## 4. Criterios de Aceptación (QA)
1. El mural de Nivel 1 solo se renderiza en la Recámara 1 (`chamberNumber == 1`).
2. La textura de Nivel 1 mide exactamente 320x48 píxeles y se dibuja en $Y = 96$.
3. Cero asignaciones en memoria heap en cada frame de renderizado (`0 allocations`).
4. 100% de pruebas unitarias pasando satisfactoriamente.
