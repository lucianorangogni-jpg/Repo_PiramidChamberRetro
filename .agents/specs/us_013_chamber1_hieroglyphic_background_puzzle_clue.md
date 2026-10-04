# Especificación Técnica: [US-013] Pista Visual Egipcia en el Fondo del Nivel 0 (Mural Jeroglífico del Puzle 1)

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-10-03  
**Componentes:** `src/Graphics/HieroglyphRenderer.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
En el fondo de la pantalla de la **Recámara 1**, sobre el muro de piedra del **Nivel 0** (entre la base del suelo y la plataforma intermedia), se incorpora un **mural con estilo egipcio antiguo en bajo relieve**:
- **Tonalidad suave y sutil:** Grabado con tonos tenues y bajo contraste sobre el fondo oscuro de la tumba (`Color(18, 12, 22)`), de modo que apenas se distinga a simple vista, requiriendo que un arqueólogo observador inspeccione el fondo.
- **Pista visual para el Puzle 1:** El mural representa en iconografía y jeroglíficos la fórmula necesaria para desbloquear la recarga secreta del tesoro del Faraón:
  $$\text{[Cofre 1]} + \text{[Cofre 2]} + \text{[Llave]} \longrightarrow \text{[Puerta 1]} \, \text{[Puerta 2]} \, \text{[Puerta 3]} \longrightarrow \text{[Cofre Radiante Extra]}$$

---

## 2. Composición Visual e Iconografía en Pixel-Art

El mural se ubica en el espacio vertical de la pared del Nivel 0 ($Y \in [160, 208]$, filas 10 a 12 de la cuadrícula):

1. **Sección Central (Corredor de la Losa y la Puerta, Columnas 6 a 13, $X \in [96, 220]$):**
   - **Cenefa decorativa superior e inferior:** Líneas horizontales de relieve arquitectónico egipcio con motivos triangulares escalonados (estilo piramidal/loto).
   - **Jeroglíficos del Puzle (de izquierda a derecha):**
     1. `Cofre 1`: Silueta de arca ceremonial del faraón con tapa curva y patas.
     2. `Cruz Ankh (+)`: Símbolo de unión egipcio con asa ovalada y brazos.
     3. `Cofre 2`: Segunda silueta de arca ceremonial.
     4. `Cruz Ankh (+)`: Símbolo de unión.
     5. `Llave Sagrada`: Silueta de la llave egipcia alada con aro superior y dientes.
     6. `Flecha Ritual (->)`: Símbolo jeroglífico indicando transición hacia el ritual.
     7. `Tres Puertas Egipcias (III)`: Tres pilonos de entrada ceremonial consecutivos, coronados por tres marcas numéricas jeroglíficas `| | |` y símbolos de conmutación (indicando abrir y cerrar 3 veces).
     8. `Flecha Ritual (->)`: Símbolo de consecuencia o revelación.
     9. `Cofre Radiante Extra`: Arca sagrada coronada por un sol alado / rayos de resplandor sutiles (+1), revelando la recarga del tesoro secreto.

2. **Sección Izquierda (Columnas 1 a 4, $X \in [18, 78]$):**
   - Grabados egipcios complementarios de fondo:
     - Ojo de Horus (*Udjat* 𓂀) protector.
     - Silueta ceremonial de Anubis / deidades de las tumbas orando hacia el santuario.

---

## 3. Paleta Cromática y Sutileza Visual
Para cumplir el requerimiento de *"poco visibles con una tonada muy suave, que apenas se puedan distinguir"*:
- Fondo de la cámara: `RGB(18, 12, 22)`.
- Ranuras talladas en bajo relieve (sombras de cantería): `RGB(24, 16, 26)`.
- Superficie de piedra tallada (relieve base): `RGB(38, 28, 30)`.
- Pigmento ocre/arenisca envejecido (glifos): `RGB(52, 40, 32)`.
- Toques tenues de oro ceremonial desvanecido: `RGB(68, 52, 34)`.
- Toques tenues de turquesa egipcio apagado: `RGB(28, 44, 46)`.

El contraste resultante respecto al fondo es de apenas 15 a 35 unidades por canal, creando la atmósfera de incisiones en piedra desgastadas por milenios.

---

## 4. Arquitectura y Restricciones Técnicas (Zero Allocation)
1. **Clase `HieroglyphRenderer`:**
   - Ubicada en `src/Graphics/HieroglyphRenderer.cs`.
   - Implementa `IDisposable`.
   - Genera proceduralmente la textura del mural (`_muralTexture`, $320 \times 48\text{ px}$ o región de nivel 0) en su constructor durante la carga inicial.
   - En `Draw(SpriteBatch spriteBatch, int currentChamber)`:
     - Dibuja el mural en el fondo exclusivamente si `currentChamber == 1`.
     - Utiliza un `Rectangle` preasignado (`_destRect`).
     - **0 asignaciones en memoria heap** en cada frame.
2. **Orden de Renderizado en `Game1.cs`:**
   - Se dibuja inmediatamente después de `_virtualViewport.Begin(_spriteBatch)` y antes de `_tileRenderer.Draw(...)`.
   - Los bloques de muros, escaleras y suelo sólido se superponen de forma natural, ocultando los bordes del mural y mostrando únicamente las incisiones en las áreas visibles transitables.
   - El arqueólogo (`Player`) camina por delante del mural sin que los jeroglíficos interfieran con su colisión o visibilidad.

---

## 5. Criterios de Aceptación (QA)
1. En la **Recámara 1**, el mural jeroglífico se visualiza suavemente en el fondo del Nivel 0.
2. Los glifos representan con claridad la secuencia: 2 cofres + 1 llave $\rightarrow$ 3 puertas $\rightarrow$ cofre radiante extra.
3. La tonalidad es suave y de bajo contraste, integrada armónicamente en el ambiente retro de la tumba sin distraer la jugabilidad principal.
4. En la **Recámara 2**, el mural del Puzle 1 de la Recámara 1 no se renderiza.
5. El bucle de `Draw()` no produce allocations de memoria heap (`GC Allocations = 0`).
6. El proyecto compila con 0 advertencias y 0 errores (`dotnet build -warnaserror`).
7. Todas las pruebas unitarias pasan exitosamente (`dotnet test`).
