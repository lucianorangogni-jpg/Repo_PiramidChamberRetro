---
name: code-review
description: >-
  Audita código de C# y MonoGame para verificar cero allocations en el bucle de juego, arquitectura desacoplada y cumplimiento de reglas retro. Usar cuando se implementen nuevas funciones o se modifiquen entidades, sistemas o renderizado.
---

# Procedimiento de Revisión de Código (MonoGame Retro)

Este flujo de trabajo permite al revisor inspeccionar los cambios recientes contra las normas de [.agents/rules.md](../../rules.md).

## Pasos de Ejecución

1. **Identificar archivos modificados:**
   - Revisar diffs y archivos afectados mediante `git status` / `git diff` o inspección de archivos.

2. **Inspección de Rendimiento y Memoria (Zero-Allocation):**
   - Buscar asignaciones en `Update()` o `Draw()`:
     - Uso de palabras clave `new`.
     - Expresiones LINQ (`.Where(`, `.Select(`, `.Any(`, `.Count()`).
     - Interpolación de strings (`$"` dentro de llamadas repetitivas).
     - Boxing de enums o structs al pasar a métodos que aceptan `object`.

3. **Inspección de Fidelidad Retro y Arquitectura:**
   - Comprobar que la lógica no esté acoplada a llamadas directas de `SpriteBatch`.
   - Verificar uso de `SamplerState.PointClamp` y resolución virtual fija en el pipeline gráfico.
   - Confirmar que las entidades utilicen máquinas de estados y coordenadas discretas de rejilla.

4. **Emisión de Dictamen:**
   - Emitir veredicto formal (`APROBADO`, `APROBADO CON SUGERENCIAS` o `RECHAZADO`).
