---
name: technical-design
description: >-
  Aterriza requerimientos de gameplay retro en especificaciones mecánicas atómicas y arquitectura de componentes C# para MonoGame sin sobre-ingeniería. Usar antes de implementar cualquier nueva mecánica o sistema.
---

# Procedimiento de Análisis y Diseño Técnico (Game Design Técnico)

Este flujo de trabajo guía al Agente de Análisis y Diseño (`tech_architect`) para elaborar especificaciones técnicas previas a la programación.

## Pasos de Ejecución

1. **Aclaración y Alineación con el Usuario:**
   - Detectar puntos ambiguos en la mecánica deseada.
   - Formular preguntas directas sobre:
     - Tiempos de respuesta (frames de salto, cooldowns).
     - Condiciones de borde (caídas, colisiones, límites).
     - Estados posibles de la entidad o sistema.

2. **Modelado de Datos y Estados:**
   - Diseñar los structs de datos inmutables o ligeros para representar la información (evitar clases pesadas si un `readonly struct` es suficiente).
   - Definir los enums de la máquina de estados.
   - Establecer las firmas públicas de los métodos del sistema.

3. **Redacción de la Historia Técnica:**
   - Estructurar el ticket atómico con:
     1. **Descripción:** Qué hace la mecánica.
     2. **Parámetros:** Constantes y variables de balanceo.
     3. **Restricciones Técnicas:** Cumplimiento de cero allocations en `Update`/`Draw`.
     4. **Criterios de Aceptación:** Pasos exactos que el `qa_tester` validará.

4. **Traspaso al Programador (`code_developer`):**
   - Entregar la especificación para inicio de codificación.
