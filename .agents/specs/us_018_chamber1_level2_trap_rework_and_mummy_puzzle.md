# Especificación Técnica: [US-018] Rediseño de Trampa de Nivel 2: Trampilla con Temporizador, Caída de Momia y Nuevo Tesoro

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-10-04  
**Componentes:** `src/Systems/PuzzleManager.cs`, `src/Entities/Mummy.cs`, `src/Graphics/PuzzleRenderer.cs`, `Game1.cs`, `tests/RetroGamePiramid.Tests/`  
**Estado:** ESPECIFICACIÓN TÉCNICA APROBADA  

---

## 1. Descripción
En la **Recámara 1**, la trampa rúnica de suelo en **Nivel 2, columna 13** (`Trap2Coord = new GridCoord(13, 5)`), ubicada verticalmente sobre el segundo hueco del Nivel 1 (`(13, 9)`), se actualiza con una mecánica dinámica completa:

1. **Apertura por Paso Superior (Caminar o Saltar):**
   - Cuando el arqueólogo pasa por encima de la baldosa en columna 13 en el Nivel 2 ($Y \approx 64\text{ px}$), ya sea **caminando** o **saltando** en el aire sobre ella:
     - La trampilla se abre inmediatamente (`Trap2.Open()`, baldosa se convierte en `TileType.Empty`).
     - Se inicia un temporizador de apertura de **2 segundos (120 frames a 60 FPS)** (`Trap2OpenTimer = 120`).
2. **Evasión en Salto vs Caída Caminando:**
   - Si el jugador pasa **saltando** (`player.State == PlayerState.Jumping`), la parábola del salto continúa sin interrupciones y aterriza a salvo en el otro extremo (columna 14 o 12), dejando la trampilla abierta a su paso.
   - Si el jugador pisa **caminando o detenido**, cae al vacío inmediatamente (`player.ForceFall(grid)`).
3. **Cierre por Temporizador:**
   - La trampilla permanece abierta durante 120 frames. Al expirar el temporizador (`Trap2OpenTimer == 0`), se cierra automáticamente (`Trap2.Close()`, baldosa vuelve a ser `TileType.SolidWall`).
4. **Cualquier Personaje Puede Caer (Jugador o Momia):**
   - **Jugador:** Al caer al Nivel 0 por la trampa, sufre caída mortal ineludible, perdiendo 1 vida (`player.Eliminate()`) y mostrando `"!TRAMPA MORTAL! QUEDAS ATRAPADO"`.
   - **Momia de Nivel 2:** Mientras patrulla la plataforma de Nivel 2, si la trampilla está abierta al pisar la columna 13, la momia cae por gravedad a través del hueco hasta el Nivel 0.
5. **Regeneración y Destrucción de la Momia (Hasta 3 Veces):**
   - Cada vez que la momia de Nivel 2 cae en la trampa y llega al fondo:
     - **Caída 1 y 2:** La momia queda atrapada y se regenera en su lugar de origen en Nivel 2 (`spawnX: 15 * 16`, `spawnY: 4 * 16`), mostrando `"!MOMIA ATRAPADA! SE REGENERA (1/3)"` y `"!MOMIA ATRAPADA! SE REGENERA (2/3)"`.
     - **Caída 3:** La momia queda destruida y eliminada definitivamente de la recámara (`IsActive = false`), mostrando `"!MOMIA DESTRUIDA! NUEVO TESORO"`.
6. **Creación del Nuevo Tesoro en Nivel 0:**
   - Tras la 3.ª caída de la momia:
     - Si el tesoro de la cámara en Nivel 0 ya fue recogido, se crea de inmediato el nuevo tesoro ancestral en la cámara (`(17, 13)`), abriendo el muro secreto si estaba cerrado.
     - Si el tesoro de la cámara aún no ha sido recogido, queda habilitado en estado pendiente (`IsTreasure4Pending = true`), creándose automáticamente en cuanto el jugador recoja el actual.
     - Al recolectar este nuevo tesoro (`Treasure4`), otorga **+1000 puntos** y muestra `"!TESORO SAGRADO LIBERADO! +1000 PTS"`.

---

## 2. Parámetros y Constantes
- **Coordenada de Trampa 2:** `(13, 5)`.
- **Duración de Apertura:** 120 frames (2.0 segundos a 60 FPS).
- **Spawn de Momia Nivel 2 (Recámara 1):** `(15, 4)` (Columna 15, Fila 4 = 240, 64 px).
- **Caídas Máximas de Momia:** 3 caídas.
- **Coordenada de Tesoro 4:** `(17, 13)` (Cámara de Nivel 0).
- **Puntos por Tesoro 4:** +1000 PTS.

---

## 3. Máquina de Estados de la Trampa y Momia
```
[Cerrada] --(Jugador pasa caminando o saltando)--> [Abierta (Timer=120)]
                                                         |
                   +-------------------------------------+-------------------------------------+
                   |                                                                           |
         (Momia pisa col 13)                                                         (Timer expira)
                   |                                                                           |
            [Momia Cayendo]                                                               [Cerrada]
                   |
           (Llega a Nivel 0)
                   |
      +------------+------------+
      |                         |
(Caídas < 3)              (Caídas == 3)
      |                         |
[Momia Regenera]         [Momia Destruida]
      |                         |
      +----------------> [Genera/Habilita Tesoro 4]
```

---

## 4. Restricciones Técnicas (Zero Allocation)
- 0 asignaciones dinámicas (`new`) en `Update()` y `Draw()`.
- Cadenas constantes pre-internadas para notificaciones retro.
- Renderizado de cofres en `(17, 13)` sin colisiones visuales.

---

## 5. Criterios de Aceptación (QA)
1. Saltar sobre la columna 13 en Nivel 2 abre la trampa pero el jugador no cae y aterriza a salvo al otro lado.
2. Caminar sobre la columna 13 en Nivel 2 abre la trampa y hace caer al jugador, muriendo en Nivel 0 y perdiendo 1 vida.
3. La trampa abierta permanece abierta durante 120 frames y se cierra automáticamente al finalizar el tiempo.
4. La momia de Nivel 2 cae por la trampa si está abierta cuando pasa por la columna 13.
5. Al caer la momia, se regenera en su spawn en las caídas 1 y 2.
6. A la 3.ª caída, la momia se elimina definitivamente.
7. Al eliminarse la momia, si el tesoro de la cámara ya fue tomado, aparece el nuevo tesoro; si no fue tomado aún, aparece en cuanto se tome el actual.
8. Tomar el nuevo tesoro otorga +1000 puntos y muestra notificación retro.
9. `dotnet build -warnaserror` compila con 0 advertencias y 0 errores.
10. Todas las pruebas unitarias pasan (`dotnet test`).
