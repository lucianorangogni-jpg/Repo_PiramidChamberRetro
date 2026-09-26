# Documento de Diseño Técnico y Arquitectura: "RetroGamePiramid"
**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Versión:** 1.0  
**Fecha:** 2026-09-26  
**Stack:** C# (.NET 9), MonoGame DesktopGL

---

## 1. Visión y Pilares del Juego

- **Género:** Plataformas retro 2D con puzles en cámara única (Homenaje a *Montezuma's Revenge*, *Sokoban* y *Lode Runner*).
- **Ambientación:** Un arqueólogo con sombrero salacot explorando cámaras secretas dentro de una pirámide egipcia.
- **Bucle Central de Juego (Core Gameplay Loop):**
  1. **Explorar y Recolectar:** Recoger todas las gemas de colores (joyas sagradas) distribuidas por la cámara.
  2. **Resolver el Puzle Sokoban:** Empujar bloques de piedra ancestrales sobre interruptores/placas de presión del suelo para hacer aparecer la Llave Sagrada.
  3. **Gestionar Amenazas (Momias):** Esquivar momias patrulleras o aturdirlas temporalmente con el látigo/antorcha para franquear pasajes estrechos.
  4. **Escapar:** Tomar la llave y alcanzar la Puerta de Salida para descender a la siguiente cámara de la pirámide.

---

## 2. Especificación de Mecánicas

### 2.1. Entorno y Cuadrícula (Tile Grid)
- **Resolución Virtual Nativa:** `320 x 240` píxeles (aspect ratio 4:3 clásico retro).
- **Dimensiones de Tile:** `16 x 16` píxeles.
- **Tamaño de Pantalla / Cámara:** Rejilla de `20 x 15` celdas (pantalla fija sin scroll, diseño arcade estricto).
- **Tipos de Celdas:**
  - `Empty`: Aire / espacio transitable.
  - `SolidWall`: Bloque de piedra impenetrable e inamovible.
  - `Ladder`: Escalera de cuerda/madera que permite trepar arriba/abajo.
  - `PressurePlate`: Placa de presión en el suelo (detecta peso de bloques).
  - `ExitDoor`: Puerta cerrada que requiere la llave para abrirse.

### 2.2. Movimiento del Arqueólogo (Tile-Based Arcade)
- **Desplazamiento Horizontal:** Pasos discretos alineados a la cuadrícula (movimiento por celdas con interpolación de desplazamiento fluido a 60 FPS).
- **Salto:** Parábola de salto fija de 2 tiles de longitud horizontal y 1.5 tiles de altura máxima. No hay variación por presión del botón (estilo arcade determinista).
- **Trepar:** Permite subir y bajar por tiles de tipo `Ladder`.
- **Ataque de Aturdimiento (Látigo / Antorcha):**
  - Acción frontal que cubre 1 celda de distancia en la dirección que mira el jugador.
  - Tiene una duración de animación fija de 10 frames (~160 ms).
  - Bloquea el movimiento durante la ejecución.

### 2.3. Bloques de Piedra (Puzle Sokoban)
- Bloques de piedra de `16 x 16` que descansan sobre superficies sólidas.
- **Empuje:** El jugador puede empujarlos en horizontal si:
  1. Camina en dirección al bloque.
  2. La celda inmediatamente posterior al bloque está vacía (`Empty`).
- **Gravedad de Bloques:** Si un bloque queda suspendido en el aire (sin suelo debajo), cae verticalmente celda por celda hasta tocar suelo o una placa de presión.
- **Activación de Interruptor:** Cuando un bloque ocupa la misma posición que un `PressurePlate`, el interruptor pasa a estado `Active`.
- **Condición de Aparición de la Llave:** Cuando todas las placas de presión requeridas están activas, la `Key` se materializa en una celda predefinida con un efecto sonoro de revelación.

### 2.4. Momias Patrulleras
- **Patrulla:** Se mueven en horizontal de un extremo a otro de una plataforma o piso. Al chocar contra una pared o al borde de una caída, cambian de sentido.
- **Colisión Letal:** Si entran en contacto con el arqueólogo en estado `Normal`, este pierde una vida y se reinicia la cámara.
- **Estado Aturdido (`Stunned`):**
  - Al ser impactadas por el látigo/antorcha, quedan paralizadas durante `STUN_DURATION_SECONDS = 3.5f` segundos.
  - Durante este estado: parpadean visualmente, no patrullan y son **inofensivas** (el arqueólogo puede atravesarlas caminando o saltándolas).

---

## 3. Arquitectura de Software y Componentes (MonoGame / C#)

```
RetroGamePiramid/
└── src/
    ├── Core/
    │   ├── GameConstants.cs           # Dimensiones de tile, resoluciones, timers
    │   └── Direction.cs               # Enum: Left, Right, Up, Down, None
    ├── Grid/
    │   ├── TileType.cs                # Enum: Empty, Wall, Ladder, PressurePlate, ExitDoor
    │   ├── RoomGrid.cs                # Array 2D plano TileType[20, 15] de cero allocations
    │   └── GridCoord.cs               # readonly struct (int X, int Y)
    ├── Entities/
    │   ├── Player.cs                  # Máquina de estados: Idle, Walking, Climbing, Jumping, Whipping, Dead
    │   ├── PushBlock.cs               # Bloque empujable con gravedad y posición en rejilla
    │   ├── Mummy.cs                   # Momia patrullera con estados: Patrol, Stunned
    │   ├── Gem.cs                     # Joya coleccionable (pos, color, valor, recolectada)
    │   ├── Key.cs                     # Llave sagrada (activa/inactiva, recogida)
    │   └── ExitDoor.cs                # Estado de puerta: Locked, Open
    ├── Graphics/
    │   ├── VirtualViewport.cs         # Manejo de RenderTarget2D (320x240) y PointClamp
    │   └── RetroRenderer.cs           # Dibujado de tiles, sprites y HUD retro
    ├── Input/
    │   └── InputManager.cs            # Mapeo de teclas (flechas/WASD, espacio para saltar, Z/Ctrl para látigo)
    └── Scenes/
        ├── GameplayScene.cs           # Gestor de la cámara actual, ciclo de juego y victoria
        └── TitleScene.cs              # Pantalla de inicio
```

---

## 4. Plan de Implementación en Tareas Atómicas (User Stories para @coder)

- **[US-001] Sistema de Rejilla y RenderTarget Virtual:**
  Configurar la resolución virtual de 320x240 en `RenderTarget2D`, renderizado con `SamplerState.PointClamp` y la estructura `RoomGrid` con tiles básicos (Muros, Suelo, Escaleras).
- **[US-002] Arqueólogo: Movimiento Tile-Locked, Escaleras y Salto Arcade:**
  Implementar al jugador con movimiento horizontal discreto por celdas, trepado vertical por escaleras y salto parabólico arcade fijo.
- **[US-003] Bloques Empujables (Sokoban) y Placas de Presión:**
  Implementar la entidad `PushBlock` con física de empuje horizontal y caída por gravedad, interactuando con `PressurePlate` para revelar la llave.
- **[US-004] Coleccionables y Salida (Gemas, Llave y Puerta):**
  Lógica de recolección de gemas, recogida de la llave y desbloqueo de la `ExitDoor` para ganar el nivel.
- **[US-005] Momias Patrulleras y Mecánica de Aturdimiento (Látigo):**
  IA de patrulla horizontal para las momias, acción del látigo del arqueólogo y estado `Stunned` con temporizador.
