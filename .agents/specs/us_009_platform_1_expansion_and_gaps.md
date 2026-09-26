# Especificación Técnica: [US-009] Expansión de Suelo y Huecos en Plataforma Nivel 1

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-09-26  
**Componentes:** `src/Grid/RoomGrid.cs`, `tests/RetroGamePiramid.Tests/Grid/DefaultRoomLayoutTests.cs`, `tests/RetroGamePiramid.Tests/Entities/PlayerTests.cs`  
**Estado:** ESPECIFICACIÓN APROBADA E IMPLEMENTADA  

---

## 1. Descripción
1. **Expansión de la Plataforma Nivel 1 (Fila 9):**
   - Se amplía el suelo de la Plataforma 1 para cubrir el sector superior de la cámara sobre el área del tesoro del Nivel 0.
   - La nueva sección se extiende desde la vertical de la puerta secreta / muro conmutable (`Columna 14`) hasta casi el final de la pantalla (`Columna 17`).
   - Se mantiene un espacio vacío en el extremo derecho (`Columna 18`), exactamente idéntico al espacio vacío preexistente en el extremo izquierdo (`Columna 1`).
2. **Mecánica de Saltos y Huecos Intermedios ("Espacios Vacíos"):**
   - Entre la sección original (Columnas 2 a 10) y la nueva sección (Columnas 14 a 17) se disponen huecos y un descansillo sólido intermedio:
     - `Columna 11`: Vacío (`Empty`, 16 px).
     - `Columna 12`: Descansillo sólido (`SolidWall`, 16 px).
     - `Columna 13`: Vacío (`Empty`, 16 px).
   - Esta configuración permite al arqueólogo cruzar saltando en ambas direcciones mediante la distancia arcade calibrada de 40 px:
     - Ida: Salto desde Col 9/10 sobre Col 11 $\rightarrow$ Aterriza en Col 12 $\rightarrow$ Salto sobre Col 13 $\rightarrow$ Aterriza en Col 14/15.
     - Retorno: Salto desde Col 14 sobre Col 13 $\rightarrow$ Aterriza en Col 12 $\rightarrow$ Salto sobre Col 11 $\rightarrow$ Aterriza en Col 9/10.
3. **Mecánica de Caídas y Regla de 2 Niveles:**
   - Si el jugador cae por los huecos de la Plataforma 1 (Col 11, Col 13) o camina hacia el vacío del extremo derecho (Col 18):
     - Cae desde la Plataforma 1 (fila 9) hacia el suelo de la Plataforma 0 (fila 14).
     - La diferencia de altura es de $1 - 0 = 1$ nivel.
     - Según la regla de letalidad retro (requiere mínimo 2 niveles de desnivel para eliminar), la caída es **segura**: el jugador aterriza sobre sus pies en estado `Idle` y no pierde vidas.
   - En el caso de Col 18, el jugador aterriza limpiamente dentro del recinto del tesoro del Nivel 0.

---

## 2. Mapa de Distribución en Fila 9 (Plataforma Nivel 1)

| Columna | Tipo de Baldosa | Función / Elemento |
| :---: | :---: | :--- |
| **0** | `SolidWall` | Muro perimetral exterior izquierdo |
| **1** | `Empty` | Espacio vacío izquierdo (1 celda = 16 px) |
| **2..4** | `SolidWall` | Sección 1: Suelo inicial de Plataforma 1 |
| **5** | `Ladder` | Escalera vertical conectando Nivel 0 con Nivel 1 |
| **6..9** | `SolidWall` | Sección 1: Suelo firme de Plataforma 1 |
| **10** | `Ladder` | Escalera vertical conectando Nivel 1 con Nivel 2 |
| **11** | `Empty` | **Hueco 1:** Vacío para salto (16 px) |
| **12** | `SolidWall` | **Descansillo Intermedio:** Bloque sólido de apoyo para salto |
| **13** | `Empty` | **Hueco 2:** Vacío para salto (16 px) |
| **14** | `SolidWall` | Sección 2: Suelo sobre el dintel y muro secreto del Nivel 0 |
| **15..17** | `SolidWall` | Sección 2: Suelo continuo sobre la sala del tesoro del Nivel 0 |
| **18** | `Empty` | **Espacio vacío derecho:** Simétrico al de Col 1 (16 px) |
| **19** | `SolidWall` | Muro perimetral exterior derecho |

---

## 3. Restricciones Técnicas (Zero Allocations)
- Cero asignaciones en memoria heap (`0 allocations`) durante el bucle principal (`Update` y `Draw`).
- `PlatformInfo` para Plataforma 1 actualizado: `new PlatformInfo(1, 9, 2, 17, "Plataforma Nivel 1")`.
- El método `GetPlatformLevel(posY)` mantiene la correspondencia exacta por fila (`Row == 9`), reconociendo todas las secciones y descansillos de la Plataforma 1.

---

## 4. Criterios de Aceptación y Pruebas Unitarias
1. **Configuración de Baldosas:**
   - `RoomGrid.GetTile(12, 9)` es `SolidWall`.
   - `RoomGrid.GetTile(x, 9)` para $x \in [14, 17]$ son `SolidWall`.
   - `RoomGrid.GetTile(11, 9)`, `(13, 9)` y `(18, 9)` son `Empty`.
2. **Navegabilidad Mediante Salto:**
   - El arqueólogo puede saltar desde Col 9 hacia Col 12 y de Col 12 hacia Col 14/15 con éxito y sin caer.
   - El arqueólogo puede saltar de retorno desde Col 14 hacia Col 12 y de Col 12 hacia Col 9.
3. **Caídas Seguras:**
   - Caer por los huecos (Col 11, Col 13) o por el extremo derecho (Col 18) hace aterrizar al jugador en el Nivel 0 (Y = 208) con estado `Idle` y sin perder vidas (`IsEliminated == false`).
4. **Validación Automática:**
   - 100% de las pruebas (`dotnet test`) pasando satisfactoriamente.
   - Compilación con cero advertencias y cero errores (`dotnet build -warnaserror`).
