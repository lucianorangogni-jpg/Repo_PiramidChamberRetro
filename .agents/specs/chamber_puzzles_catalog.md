# Catálogo y Enumeración Oficial de Puzles por Recámara

**Autor:** @architect (Game Designer Técnico / Analista Funcional)  
**Fecha:** 2026-10-03  
**Versión:** 1.0  
**Proyecto:** *RetroGamePiramid* (MonoGame / .NET 9)  
**Componentes Asociados:** `src/Systems/PuzzleManager.cs`, `src/Grid/RoomGrid.cs`, `src/Entities/`  

---

## 1. Visión General y Clasificación

Dentro de la pirámide ancestral, cada recámara presenta una serie de desafíos mecánicos, lógicos y de habilidad arcade basados en la cuadrícula de tiles (16x16 px). A continuación se establece la **enumeración oficial** de los puzles para cada recámara, asignando formalmente como **Puzle 1 de la Recámara 1** la mecánica de recarga secreta del tesoro del Nivel 0.

---

## 2. Recámara 1

### Puzle 1: Recarga Secreta del Tesoro del Faraón (Nivel 0) [US-012]
- **Ubicación:** Plataforma Nivel 0 (Fila 13, Suelo en Fila 14).
- **Entidades involucradas:** 
  - Losa rúnica conmutable: `(8, 13)`
  - Muro secreto conmutable: `(14, 13)`
  - Cofre del tesoro: `(17, 13)`
- **Condiciones previas (Prerrequisitos):**
  - Haber recogido los **2 tesoros existentes** (Tesoro 1 en `(17, 13)` y Tesoro 2 en `(8, 4)`).
  - Haber recogido la **llave sagrada** (en `(16, 6)`).
- **Mecánica de resolución:**
  - El jugador debe accionar la losa rúnica para **cerrar y abrir la puerta del nivel 0 tres (3) veces consecutivas** (3 ciclos completos de puerta).
- **Restricción de activación:**
  - **Activación única:** Solo se puede ejecutar una única vez por partida/recámara; una vez recargado el nuevo tesoro, la mecánica no vuelve a repetirse.
- **Pista visual en el escenario [US-013]:** En el fondo del Nivel 0 se encuentran grabados jeroglíficos egipcios en bajorrelieve con tonalidad tenue, indicando visualmente la fórmula: `[Cofre 1] + [Cofre 2] + [Llave] -> [3 Puertas] -> [Cofre Radiante Extra]`.
- **Efecto / Recompensa:**
  - El cofre del tesoro se recarga y reaparece cerrado en `(17, 13)` (`Treasure.Reset()`).
  - Otorga **+1000 puntos** adicionales al abrir de nuevo el muro y recolectarlo (alcanzando 3500 puntos totales en la recámara).
  - Mensaje en pantalla: `"!TESORO RECARGADO! +1000 PTS"` durante 180 frames.

---

### Puzle 2: Desbloqueo y Apertura de la Cámara del Tesoro (Nivel 0) [US-004]
- **Ubicación:** Plataforma Nivel 0 (Fila 13).
- **Entidades involucradas:**
  - Losa rúnica: `(8, 13)`
  - Dintel impenetrable: `(14, 10..12)`
  - Muro conmutable: `(14, 13)`
  - Cofre del faraón: `(17, 13)`
- **Mecánica de resolución:**
  - El paso hacia el cofre está cerrado por un muro de piedra sólido.
  - El jugador debe caminar sobre la losa rúnica en `(8, 13)`. Al pisarla por primera vez, el muro en `(14, 13)` se abre (se transforma en espacio transitable `Empty`).
  - Entrar en la cámara y saquear el cofre.
- **Efecto / Recompensa:**
  - Otorga **+1000 puntos**.
  - Mensaje en pantalla: `"!TESORO ENCONTRADO! +1000 PTS"`.

---

### Puzle 3: La Llave Colgada y la Trampa de Suelo Falso (Nivel 1) [US-010 / US-011]
- **Ubicación:** Plataforma Nivel 1 (Fila 9).
- **Entidades involucradas:**
  - Llave sagrada suspendida: `(16, 6)`
  - Trampa de suelo rúnica: `(16, 9)`
  - Muro de escape inferior: `(14, 13)`
- **Mecánica de resolución:**
  - La llave cuelga a 3 baldosas de altura sobre el suelo, fuera del alcance caminando.
  - Si el jugador camina por la columna 16, la trampa se dispara: el piso cede y el jugador cae en caída libre al Nivel 0 sin vidas perdidas, perdiendo la oportunidad de coger la llave en esa pasada. (El muro en `(14, 13)` se abre para permitirle salir y volver a subir por la Escalera 1).
  - **Solución:** Saltar anticipadamente desde la columna 15 hacia la 17. En la parábola del salto (frame 12, altura 28 px), el arqueólogo intercepta la llave en el aire y aterriza sano y salvo en la columna 17 sobrevolando la trampa.
- **Efecto / Recompensa:**
  - Otorga **+500 puntos** y posesión de la llave (`HasKey = true`).
  - Mensaje en pantalla: `"!LLAVE ENCONTRADA! +500 PTS"`.

---

### Puzle 4: Salto al Descansillo y Desbloqueo de la Puerta de Salida (Nivel 2) [US-010]
- **Ubicación:** Plataforma Nivel 2 (Fila 5).
- **Entidades involucradas:**
  - Plataforma principal: columnas 4 a 17 (Fila 5).
  - Foso de salto: columna 3 (espacio vacío).
  - Descansillo de salida: columnas 1 y 2 (Fila 5).
  - Puerta de salida de la recámara: `(1, 4)`.
- **Mecánica de resolución:**
  - Tras subir por la Escalera 2 (columna 10) y recorrer la plataforma superior, el jugador debe ejecutar un salto de precisión hacia la izquierda desde la columna 4 sobre el foso de la columna 3 para alcanzar el descansillo.
  - Si intenta tocar la puerta sin la llave, la puerta rechaza el acceso (`"!PUERTA CERRADA! NECESITAS LA LLAVE"`).
  - Al tocar la puerta portando la llave (`HasKey == true`), la recámara concluye con éxito (`IsChamberCompleted = true`), activando el modal de victoria y avance de recámara.
- **Efecto / Recompensa:**
  - Finalización de la Recámara 1 y acceso a la siguiente recámara.

---

### Desafío / Secreto Adicional: Cofre Ancestral de Plataforma 2
- **Ubicación:** `(8, 4)` en Plataforma Nivel 2.
- **Mecánica:** Cofre secundario visible sobre la plataforma superior.
- **Efecto / Recompensa:** **+1000 puntos**.

---

## 3. Recámara 2

### Puzle 1: Desbloqueo de la Cámara del Tesoro (Nivel 0)
- **Ubicación:** Plataforma Nivel 0 (Fila 13, Suelo en Fila 14).
- **Entidades involucradas:**
  - Losa rúnica: `(8, 13)`
  - Muro conmutable: `(14, 13)`
  - Cofre del tesoro: `(17, 13)`
- **Mecánica de resolución:**
  - Activación por pisada de la losa para franquear el muro de piedra y recolectar el cofre sagrado.
- **Efecto / Recompensa:**
  - **+1000 puntos**.

---

### Puzle 2: Llave Colgada y Trampa de Suelo en Plataforma Nivel 1 Elevada
- **Ubicación:** Plataforma Nivel 1 (Fila 10, Suelo en Fila 11).
- **Entidades involucradas:**
  - Llave colgada: `(16, 7)`
  - Trampa de suelo: `(16, 10)`
  - Suelo de plataforma: Columnas 3 a 17 en Fila 10.
- **Mecánica de resolución:**
  - La trampa de suelo se sitúa en la fila 10 directamente bajo la llave colgada en fila 7.
  - Al pisar la columna 16 a pie, el suelo se abre y el jugador cae al Nivel 0.
  - Requiere ejecutar un salto de precisión desde la columna 15 hacia la 17 para atrapar la llave suspendida y aterrizar en la plataforma extendida.
- **Efecto / Recompensa:**
  - **+500 puntos** y obtención de la llave sagrada para la salida.

---

### Puzle 3: Patrulla de la Momia y Salto de Escape hacia la Puerta (Nivel 2) [US-008]
- **Ubicación:** Plataforma Nivel 2 (Fila 6).
- **Entidades involucradas:**
  - Momia ancestral patrullera (patrulla entre columna 4 y columna 18).
  - Escalera 2: Columna 12 (Filas 6 a 10).
  - Foso de salto: Columna 3.
  - Descansillo y Puerta de salida: `(1, 5)`.
  - Cofre secundario: `(8, 5)`.
- **Mecánica de resolución:**
  - La momia recorre continuamente la plataforma superior. El jugador debe calcular el momento exacto para ascender por la Escalera 2 en columna 12 sin ser interceptado por la momia.
  - El jugador puede esquivar o saltar a la momia para saquear el cofre en `(8, 5)` (+1000 PTS).
  - Finalmente, debe avanzar hacia la izquierda y saltar sobre el foso de la columna 3 para alcanzar el descansillo y cruzar la puerta de salida en `(1, 5)` con la llave recogida.
- **Efecto / Recompensa:**
  - Superación de la Recámara 2 y victoria de la pirámide.

---

## 4. Cuadro Resumen de Puzles y Puntuación

| Recámara | ID Puzle | Nombre del Puzle | Ubicación | Puntos | Condición de Éxito |
| :--- | :--- | :--- | :--- | :---: | :--- |
| **Recámara 1** | **Puzle 1** | **Recarga Secreta del Tesoro** | Nivel 0 `(8, 13)` / `(17, 13)` | +1000 | 2 tesoros y llave recogidos + 3 ciclos puerta (activación única) |
| **Recámara 1** | **Puzle 2** | **Apertura de Cámara del Tesoro** | Nivel 0 `(8, 13)` / `(14, 13)` | +1000 | Pisar losa para abrir muro y llegar al cofre |
| **Recámara 1** | **Puzle 3** | **Llave Colgada y Trampa de Suelo** | Nivel 1 `(16, 6)` / `(16, 9)` | +500 | Salto desde col 15 a 17 sobre trampa para tomar llave |
| **Recámara 1** | **Puzle 4** | **Salto al Descansillo y Puerta de Salida** | Nivel 2 `(1, 4)` / col 3 foso | Salida | Salto sobre foso col 3 y contacto con puerta teniendo llave |
| **Recámara 1** | Extra | **Cofre Superior de Plataforma 2** | Nivel 2 `(8, 4)` | +1000 | Recoger cofre en plataforma superior |
| **Recámara 2** | **Puzle 1** | **Apertura de Cámara del Tesoro** | Nivel 0 `(8, 13)` / `(14, 13)` | +1000 | Pisar losa para abrir muro y llegar al cofre |
| **Recámara 2** | **Puzle 2** | **Llave Colgada y Trampa en Nivel Elevado** | Nivel 1 `(16, 7)` / `(16, 10)` | +500 | Salto sobre trampa en fila 10 para recoger llave en fila 7 |
| **Recámara 2** | **Puzle 3** | **Patrulla de la Momia y Puerta de Salida** | Nivel 2 `(1, 5)` / momia col 4-18 | Salida | Esquivar momia, saltar foso col 3 y abrir puerta con llave |
| **Recámara 2** | Extra | **Cofre Custodiado por la Momia** | Nivel 2 `(8, 5)` | +1000 | Recoger cofre evadiendo la patrulla de la momia |
