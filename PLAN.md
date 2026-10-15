# PLAN — ESCULTURA1 → App AR "Componentes de Computadora"

Proyecto base: app de realidad aumentada (Vuforia) en Unity 6.
Tema actual: escultura + biografía de Máximo Ricardo Díaz Díaz.
Tema nuevo: componentes electrónicos de una computadora, mostrando su
información general e instrucciones de cómo ensamblar / desensamblar.

---

## 1. Diagnóstico del proyecto actual (verificado)

| Elemento | Dato |
|---|---|
| Motor | Unity **6000.0.62f1** (Unity 6) |
| AR | **Vuforia Engine 11.4.4** (empaquetado local, licencia incluida) |
| Escena principal | `Assets/Scenes/SampleScene.unity` |
| Estructura de la escena | ARCamera + ImageTarget("maxdiaz", dataset `Vuforia/Escultura.xml`) + escultura 3D + Canvas/world-text (biografía) + Luz direccional + EventSystem |
| Target/marcador actual | `maxdiaz` → imagen `maximodiaz.png` (marcador impreso, 1.0 × 2.217 m) |
| Modelo 3D | `escultura01-MAX.blend` (884 MB) |
| Dataset Vuforia | `Assets/StreamingAssets/Vuforia/Escultura.{dat,xml}` (1 ImageTarget) |
| Plataforma de build | Android (`com.MiProyecto.EsculturaAR`, minSdk 29) |
| **Render pipeline** | **Built-in RP** (confirmado, ver §5.1) |
| Scripts propios | `VuforiaLicense.cs` + `Scripts/` (ComponentInfo, ComponentInfoUI, ComponentSelector, OrbitTouch, SelectionOutline, InfoLabel3D, PCComponentsData) + `Editor/` (SceneBuilder, BuildScript) |
| Código de interoperación AR | Ninguno: el contenido (modelo + texto) son hijos del ImageTarget y aparecen/desaparecen con el tracking |

### Entorno de la máquina (verificado)
- Unity Hub instalado: `D:\Unity\Hub`
- Unity Editor usado: **6000.5.10f1** en `D:\Unity\Editor\6000.5.10f1` (con módulo Android)
  - ⚠️ No es la versión del proyecto (era 6000.0.62f1) y queda **fuera del rango soportado por Vuforia 11.4.4** (6000.0.38f1+ LTS). **DECISIÓN DEL USUARIO: usar 6000.5.10f1** (ya instalado). Proyecto actualizado a 6000.5.10f1; compila y ejecuta OK, Vuforia funciona en el móvil.
- Android SDK/NDK/JDK17 instalados: `D:\Android\Sdk`, Temurin 17 (JDK detectado por Unity: `C:\Program Files\Eclipse Adoptium\jdk-17.0.17.10-hotspot`)
- Teléfono: POCO X7 (`2412DPC0AG`) conectado + USB debugging OK
- Script de build automático añadido: `Assets/Editor/BuildScript.cs` (ejecuta `BuildScript.BuildAndroid`, genera `Builds/ESCULTURA1.apk`)

### Compatibilidad Vuforia (fuente: documentación oficial)
- Vuforia Engine 11.4.x soporta Unity 6 LTS **6000.0.38f1+**
- Android: ARCore mín. 1.45, NDK r26b+, Gradle 7.6.3+, Build Tools 30.0.3+, OpenGL ES 3.x / Vulkan
- El proyecto no usa ARFoundation (no requerido para Image Targets)

---

## 2. Fase A — Ejecutar el proyecto base ✅ HECHA

1. ~~**Instalar la versión exacta de Unity** `6000.0.62f1`~~ → **USADO 6000.5.10f1 (decisión del usuario)**. Proyecto abierto y actualizado; licencia Unity Personal (ilimitada) activa, API Updater de Vuforia OK, sin errores de compilación, dataset `Escultura` y escultura `.blend` (884 MB) importados sin "Missing script".
2. Build APK vía `BuildScript.BuildAndroid` (batch): **`Builds/ESCULTURA1.apk` (223 MB)** → BUILD OK.
3. Instalado en POCO X7 vía `adb install -r` (requirió activar "Instalar vía USB" en el móvil). App lanzada: PID activo, Vuforia inicializando correctamente (`ConfigureVideoBackground`, `DefaultObserverEventHandler`, sin errores de runtime).
4. Queda pendiente la **prueba física del usuario**: apuntar la cámara al marcador `maximodiaz.png` impreso y ver la escultura 3D + biografía.

### Criterio de éxito Fase A
La app muestra la escultura 3D + biografía al apuntar la cámara al marcador impreso.

---

## 3. Fase B — Tema nuevo: componentes de computadora

Concepto: **un marcador por componente**. Al escanear la imagen con la cámara se
muestra el modelo 3D del componente + su información general + instrucciones de
ensamblaje/desensamblaje.

### 3.1 Componentes (DECIDIDO)
**CPU, RAM (DIMM), SSD (M.2)** — 3 componentes.
Concepto de interacción (DECIDIDO): **un solo marcador = imagen de una placa madre**.
Al escanearla con la cámara aparece el modelo 3D de la placa con los componentes y un panel interactivo (pestañas Información / Ensamblar / Desensamblar con Anterior/Siguiente; toque sobre el modelo selecciona el componente).

### 3.2 Marcadores (Image Targets)
1. ✅ Imagen prediseñada generada: `Assets/Markers/placa_madre_marker.png` (2480×1754 px, 300 DPI, imprimible). Marcador único con placa madre + CPU + RAM + SSD etiquetados.
2. ⛔ **BLOQUEADO**: generar el `.dat/.xml` del nuevo target requiere subir la imagen a **Vuforia Target Manager** (necesita cuenta gratuita en `developer.vuforia.com`). Vuforia 11.4.4 **no** extrae features localmente.
   - Alternativa sin cuenta: reutilizar el dataset actual (`Escultura.dat`, target `maxdiaz`) y mostrar el contenido nuevo sobre el marcador actual.
3. Sustituir `Assets/StreamingAssets/Vuforia/` con el nuevo dataset cuando exista.
4. La escultura de 884 MB se retira de la escena (se puede conservar el fichero).

### 3.3 Escena: prefab reutilizable por componente
Estructura de la escena objetivo (se construye vía `Assets/Editor/SceneBuilder.cs`):
- `ImageTarget` (placa madre) con hijos:
  - `ModelRoot` (contenedor de los modelos importados de Blender; escala 1 m)
  - `Canvas` (Screen Space Overlay) con panel inferior: botones de componente, título, texto, pestañas Información/Ensamblar/Desensamblar, contador de pasos, botones Anterior/Siguiente.

### 3.4 Scripts C# nuevos (✅ ESCRITOS E IMPORTADOS, compilan sin errores)
| Script | Función |
|---|---|
| `Assets/Scripts/ComponentInfo.cs` | Datos por componente (nombre, descripción, función, specs, pasos ensamblaje/desensamblaje) |
| `Assets/Scripts/ComponentInfoUI.cs` | Panel: pestañas Info/Ensamblar/Desensamblar, pasos con Anterior/Siguiente |
| `Assets/Scripts/ComponentSelector.cs` | Toque sobre el modelo 3D → selecciona componente (Input System) |
| `Assets/Scripts/OrbitTouch.cs` | Rotar + zoom con gestos táctiles (Input System) |
| `Assets/Editor/BuildScript.cs` | Build APK (ya existente) |

Convención de modelado (Blender) para el usuario:
- Un `.blend` por componente, unidades en metros (1 unidad = 1 m), origen en el centro.
- **Piezas como objetos separados y con nombres legibles** si se quiere despiece 3D.

### 3.5 Contenido (textos)
- Los redacta el agente en español (info real de componentes + pasos genéricos de
  montaje/desmontaje) y el usuario los revisa/ajusta.
- **PENDIENTE**: decidir si el despiece es 3D animado o solo pasos de texto.

### 3.6 Flujo UX (a confirmar)
`Escanear imagen → aparece modelo 3D + panel con su información → pestañas
"Información / Ensamblar / Desensamblar" con pasos y navegación Anterior/Siguiente`.

---

## 4. Preguntas pendientes (bloquean el cierre de Fase B)

1. ✅ ~~Unity~~ resuelto → usar 6000.5.10f1.
2. ✅ ~~Lista definitiva~~ → **CPU, RAM, SSD + un solo marcador de placa madre**.
3. ⛔ **Marcadores — bloqueante**: sin cuenta en `developer.vuforia.com`, no se puede generar el dataset del nuevo marcador. Alternativa: reutilizar el marcador actual.
4. ⏳ **Modelos**: a modelar con Blender + MCP. **Esperando que el usuario abra Blender con el addon MCP activo.**
5. ✅ ~~Flujo UX~~ → confirmado: escanear → modelo + panel con pestañas Información / Ensamblar / Desensamblar y Anterior/Siguiente; toque para seleccionar componente.

---

## 5. Riesgos / notas

### 5.1 Render pipeline: **Built-in RP, NO URP** (verificado 2026-10-04)

Aunque el paquete URP 17.5.0 esta instalado, **ningun Render Pipeline Asset esta asignado**:

| Fichero | Valor |
|---|---|
| `ProjectSettings/GraphicsSettings.asset` | `m_CustomRenderPipeline: {fileID: 0}` |
| `ProjectSettings/QualitySettings.asset` | `customRenderPipeline: {fileID: 0}` en los **6** niveles de calidad |
| `Assets/Settings/Mobile_RPAsset.asset`, `PC_RPAsset.asset` | existen pero **huerfanos** (nadie los referencia) |

=> Unity corre con **Built-in Render Pipeline**. Los shaders de Vuforia y de TextMeshPro del
proyecto lo confirman: `Assets/Resources/VuforiaConfiguration.asset` apunta a
`Vuforia/Shaders/Built-In/{VideoBackground,DepthMask,DepthContour}.shader` y todos los
`Assets/TextMesh Pro/Shaders/*.shader` son `CGPROGRAM` + `UnityCG.cginc` (solo Built-in).

**NO activar URP.** El paquete de Vuforia 11.4.4 solo trae 3 shaders URP
(`CameraDiffuse_URP`, `DepthContourLine_URP`, `DepthContourMask_URP`) y **no existe
`VideoBackground_URP.shader`**. Al activar URP se perderia el fondo de camara (pantalla morada)
y todo el texto de TMP se volveria morado. Los materiales `Assets/Materials/PC_*.mat` usan el
shader built-in `Standard` y son correctos tal cual; si alguna vez se migra a URP hay que
pasarlos a `Universal Render Pipeline/Lit` con `_BaseColor` / `_Metallic` / `_Smoothness`.

### 5.2 Bugs corregidos (2026-10-04)

| Sintoma | Causa | Correccion |
|---|---|---|
| Pieza morada al tocarla | `SelectionOutline` resolvia `"Universal Render Pipeline/Unlit"`, shader de SRP que bajo Built-in RP se dibuja en magenta. La malla del contorno es la pieza inflada con `Cull Off`, asi que la cubria entera | Material asset `Assets/Resources/SelectionOutline.mat` (shader `Unlit/Color`) cargado desde `Resources`, sin `Shader.Find` en runtime. Ademas `GetComponentsInChildren<MeshFilter>()` para abarcar todas las piezas de la ensamblaje |
| A veces no renderiza nada | `Shader.Find` en un player solo encuentra shaders incluidos; al fallar ambos intentos se hacia `new Material(null)`, dejando el `MeshRenderer` sin material | Si no hay material, log de error y `return`. Nunca `new Material(null)` |
| El modelo salta de tamano al hacer pinching | `OrbitTouch` clampeaba la escala **absoluta** a `[0.4, 2.5]` cuando la escala real del modelo es ~0.06: el primer frame saltaba a 6.6x y ya no podia encogerse | `minScaleFactor` / `maxScaleFactor` relativos a la escala base capturada en `Awake` |
| El modelo sale pequeno / descolocado | `ConfigureTarget` declaraba `0.21 x 0.174158` (ratio 1.2058, el del JPG con el que se entreno el target) pero se imprime el PNG `placa_madre_marker.png` (ratio 1.4139, hecho a medida para A4) | Marcador y modelo a **A4 apaisado: 0.297 x 0.21 m**. `FitScale` ajustado por los dos ejes con `min()` en vez de `max(size.x, size.y)`, para que la escala no dependa de la inclinacion |

Otros arreglos: guarda contra `Shader.Find("Standard")` nulo; material por defecto
`PC_default` para renderers cuyo material del FBX no esta en la paleta; `AssignMaterials`
idempotente (quita el prefijo `PC_` antes de buscar en la paleta, si no la segunda
ejecucion perdia todos los colores); `OrbitTouch` resetea el estado del pinching cuando
se sueltan los dedos.

### 5.3 Pendiente / limitaciones

- **Licencia Vuforia** tipo Development: sirve para pruebas/clase; no para distribucion pública.
- Vuforia Engine 11.4.4 no incluye ARFoundation; el plan no lo usa.
- **El target se entreno con una imagen de otro ratio.** `Componentes.dat` se genero desde
  `placa_madre_referencia_CCBYSA.jpg` (ratio 1.2058) y se imprime
  `placa_madre_marker.png` (ratio 1.4139): un 17% de discrepancia que degrada la robustez
  del tracking. Declarar `0.297 x 0.21` corrige la escala metrica pero no elimina esa
  discrepancia. La solucion limpia es subir el **PNG** al Target Manager, sigue bloqueado
  por la cuenta (ver §4.3).
- **Imprimir el marcador a tamano real, sin escalar.** Si la impresora aplica "ajustar al area
  imprimible" recorta ~5 mm por lado y el tamano declarado deja de cuadrar.
- MinSdk 29 OK para el POCO X7 (Android 15).
- Los marcadores deben imprimirse planos y con buen contraste/features.
- ~~RESALTADO DE SELECCION NO FUNCIONA~~ → **corregido**, ver §5.2.

### 5.4 Siguiente paso

Ejecutar `Tools/AR/2. Reconstruir escena AR` en Unity 6000.5.10f1 para que la escena vuelva a
ser coherente con el script: la escena estaba editada a mano (rotacion +38° en X frente a los
-30° que escribia el codigo, y `m_LocalEulerAnglesHint` obsoleto en `{0,0,0}`), de modo que la
escala guardada (0.0607129) se habia calculado para una orientacion que ya no era la vigente.

---

## 6. Fechas / estado de ejecución

| Fase | Estado |
|---|---|
| A1 Instalar Unity 6000.0.62f1 (+ Android) | ✅ SUSTITUIDA → usado 6000.5.10f1 (decisión del usuario) |
| A2 Abrir proyecto y validar | ✅ Hecho (sin errores, licencia OK, Vuforia OK) |
| A4 Build APK | ✅ Hecho (`Builds/ESCULTURA1.apk`, 223 MB, BUILD OK) |
| A5 Instalar + probar en POCO | ✅ App instalada y lanzada (Vuforia OK). Pendiente: prueba física con marcador impreso |
| B1 Definir componentes | ✅ CPU, RAM, SSD + marcador único de placa madre |
| B2 Crear marcadores + dataset Vuforia | 🔶 Marcador generado; **dataset bloqueado por cuenta Target Manager** |
| B3 Reconstruir escena (prefabs) | Pendiente (depende de modelos + SceneBuilder) |
| B4 Scripts C# | ✅ Escritos e importados (ComponentInfo, ComponentInfoUI, ComponentSelector, OrbitTouch) |
| B5 Contenido | Pendiente |
| B6 Build APK final + pruebas | Pendiente |