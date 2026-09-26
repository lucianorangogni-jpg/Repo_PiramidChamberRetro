---
name: qa-testing
description: >-
  Ejecuta compilaciones con dotnet build y pruebas unitarias/integración con dotnet test para validar la lógica matemática y funcional de la pirámide retro. Usar tras cualquier cambio de código o refactorización.
---

# Procedimiento de Pruebas y Validación (QA Testing)

Este flujo de trabajo guía al ingeniero de pruebas para verificar la integridad del código del juego.

## Pasos de Ejecución

1. **Verificación de Compilación:**
   - Ejecutar en terminal:
     `dotnet build`
   - Asegurarse de que el resultado sea 0 errores y 0 advertencias.

2. **Ejecución de Pruebas Automatizadas:**
   - Si existe suite de pruebas en el proyecto:
     `dotnet test`
   - Revisar que todos los tests pasen exitosamente (`Passed!`).

3. **Verificación de Casos Críticos:**
   - **Movimiento en Rejilla:** Salto entre celdas adyacentes válidas.
   - **Bordes y Caídas:** Salto hacia fuera de la pirámide sin disco activa estado de caída.
   - **Discos de Rescate:** Salto a disco activa transporte de retorno al ápice.
   - **Mecánica de Cubos:** El cambio de color de todos los cubos dispara el evento de victoria de nivel.

4. **Emisión de Reporte:**
   - Resumir el estado de compilación y las pruebas ejecutadas en un informe claro.
