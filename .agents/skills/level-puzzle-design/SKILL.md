---
name: level-puzzle-design
description: >-
  Diseña e implementa niveles, recámaras, puzles secuenciales y pistas visuales diegéticas (murales, símbolos o señales en el escenario) garantizando variedad entre niveles, ausencia de softlocks y soporte para modo sin spoilers (Blind Playtest). Usar cuando se creen o modifiquen niveles, acertijos, secuencias de activación o pistas visuales del entorno.
---

# Procedimiento de Diseño de Niveles, Puzles y Pistas Visuales

Este flujo de trabajo guía al Agente de Arquitectura y Diseño (`tech_architect`) y al Programador (`code_developer`) para crear o modificar niveles y acertijos reutilizando las mecánicas del juego de forma fresca, justa y legible para el jugador.

## Pasos de Ejecución

1. **Auditoría de Variedad y Progresión:**
   - Inspeccionar los niveles o recámaras anteriores antes de diseñar uno nuevo.
   - Garantizar que cada nuevo puzle cambie al menos uno de los ejes de diseño respecto a los niveles previos:
     - **Orden estricto vs. unión libre** (secuencia `->` frente a conjunto `+`).
     - **Combinación de entidades** (interruptores/puertas, llaves, cofres, trampas de suelo, saltos o caídas de enemigos).
     - **Contadores y repeticiones** (`I`, `II`, `III`, `IIII` o ciclos compuestos).
     - **Dependencias entre puzles** (puzles independientes vs. puzles que requieren completar desafíos previos del mismo nivel).

2. **Prevención de Bloqueos (*Anti-Softlock*):**
   - Verificar que ninguna condición de un puzle invalide permanentemente otro puzle del mismo nivel:
     - Si el jugador falla el orden o lanza un enemigo por una trampa antes de cumplir los prerrequisitos, la entidad o contador debe reiniciarse de forma segura para permitir reintentarlo sin perder la partida.
     - Verificar que el alcance de salto, patrulla de enemigos y ubicación de escaleras/plataformas permitan ejecutar la secuencia sin daño inevitable.

3. **Diseño de Pistas Visuales en el Entorno (*Diegetic Clues*):**
   - Todo puzle debe contar con una representación visual clara dentro del propio escenario (murales, jeroglíficos, iconos o señales de fondo).
   - **Gramática Visual Consistente:**
     - Mantener los mismos iconos para cada entidad o acción (llave, cofre, puerta, enemigo, arco de salto, flecha de caída, trampa, marcas numéricas `I..IIII`).
     - Diferenciar claramente entre **conector de unión (`+` / `☥`)** (cualquier orden) y **flecha de secuencia (`->`)** (orden estricto paso a paso).
   - **Verificación Espacial (Sin Solapamientos):**
     - Comprobar las coordenadas exactas de escaleras, puertas, plataformas, trampas e ítems del nivel para ubicar los glifos en zonas despejadas del muro o fondo.

4. **Modo *Blind Playtest* (Sin Spoilers para el Creador):**
   - Si el usuario solicita crear o cambiar puzles para **descifrarlos y probarlos por su cuenta** viendo las imágenes del juego:
     - Implementar la lógica completa en el sistema de puzles/entidades.
     - Implementar el renderizado de las pistas visuales correspondientes.
     - Escribir y ejecutar las pruebas unitarias que validen tanto la lógica como los píxeles/glifos del mural.
     - **Regla estricta de comunicación:** En la respuesta al usuario, confirmar que los puzles y sus pistas visuales están listos **sin revelar ni describir la solución paso a paso**.

5. **Validación Técnica y Cero Allocations:**
   - Asegurar que ni la máquina de estados del puzle ni el renderizado de las pistas visuales generen asignaciones en memoria heap (`0 allocations`) durante `Update()` y `Draw()` (pre-generar texturas en la carga o usar estructuras por valor).
   - Ejecutar la skill [`qa-testing`](../qa-testing/SKILL.md) (`dotnet test --warnaserror`) para confirmar que los niveles anteriores no sufrieron regresiones y que el nuevo nivel es 100% completable.

