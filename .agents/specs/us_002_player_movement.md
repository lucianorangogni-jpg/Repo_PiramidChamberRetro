# Especificación Técnica: [US-002] Movimiento del Arqueólogo, Escaleras y Salto Arcade

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-26  
**Componente:** `src/Entities/Player.cs`, `src/Input/InputManager.cs`, `src/Graphics/PlayerRenderer.cs`  
**Estado:** ESPECIFICACIÓN APROBADA  

---

## 1. Descripción
Implementar el arqueólogo protagonista con control por teclado mediante flechas de dirección (Izquierda, Derecha, Arriba, Abajo) y salto arcade determinista con la barra espaciadora, integrando la física con la cuadrícula de la cámara (`RoomGrid`).

---

## 2. Controles de Entrada
- **Flecha Izquierda (`Keys.Left`):** Desplazamiento horizontal hacia la izquierda. Voltea el sprite a la izquierda.
- **Flecha Derecha (`Keys.Right`):** Desplazamiento horizontal hacia la derecha. Voltea el sprite a la derecha.
- **Flecha Arriba (`Keys.Up`):** Subir en vertical cuando el jugador se encuentra sobre o en contacto con una baldosa `Ladder`.
- **Flecha Abajo (`Keys.Down`):** Descender en vertical por baldosas `Ladder`.
- **Barra Espaciadora (`Keys.Space`):** Disparo del salto arcade:
  - Salto en parábola direccional si se presiona Izquierda/Derecha (avance de hasta 2 baldosas).
  - Salto vertical en el sitio si no hay dirección horizontal pulsada.

---

## 3. Máquina de Estados Finita (`PlayerState`)
1. **`Idle`:** El jugador está posado sobre una superficie sólida o plataforma sin recibir input de movimiento.
2. **`Walking`:** Desplazamiento horizontal continuo a velocidad fija (`WALK_SPEED`). Detecta bordes para entrar en `Falling` o muros sólidos para detenerse.
3. **`Climbing`:** El jugador está alineado con una escalera (`Ladder`). Se desplaza en vertical con Arriba/Abajo. Si no hay pulsación, permanece estático en la escalera.
4. **`Jumping`:** Trayectoria parabólica matemática en el aire durante `JUMP_DURATION_FRAMES` (24 frames = 0.4s a 60 FPS). Altura de ápice: 24 píxeles (1.5 tiles).
5. **`Falling`:** Caída vertical acelerada por gravedad hasta tocar suelo sólido o agarrarse a una escalera.

---

## 4. Parámetros y Constantes de Balanceo
- `WALK_SPEED = 1.5f` píxeles por frame (~90 px/s a 60 FPS).
- `CLIMB_SPEED = 1.2f` píxeles por frame (~72 px/s a 60 FPS).
- `JUMP_DURATION_FRAMES = 24` frames.
- `JUMP_HEIGHT_PIXELS = 24f` píxeles.
- `JUMP_HORIZONTAL_DISTANCE = 32f` píxeles (2 tiles).
- `GRAVITY = 0.25f` píxeles/frame² (velocidad terminal: 4.0f px/frame).

---

## 5. Restricciones Técnicas (Zero Allocation)
- Cero asignaciones en memoria heap (`0 allocations`) en `Update()` y `Draw()`.
- Estructura `struct` o primitivos para posiciones, velocidades y temporizadores.
- Sprites procedurales 16x16 generados en `LoadContent()` o constructor de `PlayerRenderer`.

---

## 6. Criterios de Aceptación (QA)
1. El jugador se mueve horizontalmente con flechas izquierda/derecha colisionando con muros.
2. El jugador sube y baja correctamente por las escaleras situadas en columna 5 y columna 10 de la habitación por defecto.
3. Al presionar la barra espaciadora en el suelo, el jugador realiza una parábola de salto y aterriza limpiamente.
4. Si el jugador camina hacia el vacío, cae por gravedad hasta impactar con el suelo.
5. El proyecto compila con 0 advertencias y 0 errores (`dotnet build`).
6. Suite de pruebas unitarias cubre estados, colisiones y transiciones de salto (`dotnet test`).
