# Especificación Técnica: [US-003] Menú Principal con Selección de Recámara

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-28  
**Componentes:** `src/Scenes/TitleMenu.cs`, `src/Scenes/MenuOption.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
1. **Opciones del Menú Principal:**
   - `NUEVO`: Inicia una nueva expedición restableciendo vidas a 3 y puntaje a 0, cargando la recámara seleccionada en el menú (`SelectedChamber`).
   - `CONTINUAR`: Reanuda la expedición activa conservando el progreso si la partida está en curso.
   - `RECAMARA: < 1 >` / `RECAMARA: < 2 >`: Opción interactiva para alternar la recámara a jugar.
   - `SALIR`: Cierra la aplicación.
2. **Navegación y Control:**
   - **Flechas Arriba / Abajo o W / S:** Desplazan el cursor cíclicamente entre las 4 opciones (`NUEVO` <-> `CONTINUAR` <-> `RECAMARA` <-> `SALIR`).
   - **Flechas Izquierda / Derecha o A / D:** Cuando el cursor se encuentra en `RECAMARA`, alternan entre `Recámara 1` y `Recámara 2`.
   - **Enter o Espacio:**
     - En `RECAMARA`: alterna entre `Recámara 1` y `Recámara 2` sin abandonar el menú.
     - En `NUEVO`: inicia la partida en la recámara configurada.
     - En `CONTINUAR`: reanuda la partida.
     - En `SALIR`: cierra el juego.
   - **Atajos directos de teclado 1 y 2:** Pulsar las teclas `1` o `2` en el menú principal selecciona directamente la Recámara 1 o Recámara 2 de forma instantánea.

---

## 2. Parámetros y Constantes
- **Recámaras Disponibles:** 1 (Recámara 1, por defecto) y 2 (Recámara 2).
- **Dimensiones y Centrado:** Escala retro 2x, centrado horizontal automático.
- **Rendimiento:** Cero allocations en el bucle de renderizado y actualización.
- **Recámara 2 (Geometría y Puzle):**
  - Plataforma Nivel 1: fila 10 ($Y = 160$, transitable $Y = 144$).
  - Trampa bajo la llave: baldosa `(16, 10)` en el piso de la Plataforma 1.
  - Llave colgada: baldosa `(16, 7)` ($Y \in [112, 128)$), alcanzable en el ápice del salto ($Y = 116$).
  - Plataforma Nivel 2: fila 6 ($Y = 96$), puerta en `(1, 5)`.

---

## 3. Criterios de Aceptación (QA)
1. **Navegación Cíclica:** Se recorren las 4 opciones con arriba/abajo sin desbordamientos.
2. **Alternancia de Recámara:** Se puede cambiar de recámara con izquierda/derecha, confirmación en la opción o pulsando 1/2.
3. **Inicio Correcto:** Elegir `RECAMARA: < 2 >` y dar `NUEVO` carga la Recámara 2 con la momia en Plataforma 2 y la trampa en fila 10.
4. **Pruebas:** 100% de las pruebas unitarias pasan exitosamente.

