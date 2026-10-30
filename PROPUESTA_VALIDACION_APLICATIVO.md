# Propuesta de análisis de cumplimiento del aplicativo

## Alcance

La validación se centrará en los objetivos que pueden comprobarse en el aplicativo: la interfaz, el reconocimiento visual en tiempo real, la experiencia de realidad aumentada y el apoyo al ensamblaje básico.

Los objetivos de revisión de literatura científica y de descripción de Blender, Unity y Vuforia corresponden al documento académico del proyecto. No se pueden validar únicamente observando o probando el APK.

## Criterios de revisión

| Aspecto | Qué revisar | Evidencia que buscar |
|---|---|---|
| **Propósito educativo** | Si el contenido y los flujos ayudan a estudiantes de primaria a reconocer componentes y comprender un ensamblaje básico. | Textos comprensibles, pasos ordenados, identificación visual y navegación sencilla. |
| **Interfaz gráfica e interactiva** | Claridad de las secciones, controles, instrucciones y respuesta a las acciones del usuario. | Facilidad para entrar a Descripción, Ensamblaje y Desensamblaje; legibilidad y coherencia de los controles. |
| **Reconocimiento visual en tiempo real** | Si la cámara identifica los marcadores o componentes previstos y mantiene el reconocimiento mientras se mueve el dispositivo. | Componentes admitidos, tiempo y estabilidad del reconocimiento, comportamiento con distintas distancias, iluminación y ángulos. |
| **Superposición de realidad aumentada** | Si el contenido virtual aparece asociado al elemento físico correcto y conserva una posición y escala adecuadas. | Correspondencia entre marcador y modelo, alineación, estabilidad y comportamiento al mover o girar el teléfono. |
| **Apoyo al ensamblaje básico** | Si los pasos explican una secuencia viable y si la interacción permite avanzar y retroceder de forma intuitiva. | Orden de pasos, instrucciones suficientes, orientación coherente de los gestos y disponibilidad de controles alternativos. |
| **Compatibilidad y uso práctico** | Si la experiencia es utilizable en los teléfonos previstos y no depende de condiciones poco realistas. | Orientación de pantalla, permisos de cámara, rendimiento, errores visibles y recuperación cuando se pierde el reconocimiento. |

## Método propuesto

1. **Definir el alcance:** listar los componentes, marcadores, modelos y pasos que el aplicativo pretende cubrir.
2. **Inspeccionar el APK y el proyecto:** contrastar las pantallas, los recursos de AR, la navegación y el contenido con los objetivos.
3. **Preparar pruebas de uso:** probar cada componente en condiciones controladas y variar iluminación, distancia, ángulo y movimiento.
4. **Evaluar la experiencia con usuarios representativos:** observar si estudiantes de primaria pueden iniciar la actividad, reconocer lo que aparece y seguir los pasos sin ayuda excesiva.
5. **Registrar resultados:** usar una matriz con criterio, evidencia, resultado, observaciones y prioridad de mejora.

## Escala sugerida

- **Cumple:** existe evidencia clara y funciona de manera consistente.
- **Cumple parcialmente:** la función existe, pero tiene limitaciones de claridad, estabilidad o cobertura.
- **No cumple:** la función falta o no puede completarse de forma confiable.
- **No aplica al aplicativo:** corresponde al marco teórico o al documento académico.

## Entregable del análisis

El resultado de una futura validación podría ser una matriz de cumplimiento con evidencias observadas, riesgos para el uso educativo y recomendaciones priorizadas.

> Este archivo contiene una propuesta de validación. La inspección y ejecución del aplicativo todavía no se han realizado.
