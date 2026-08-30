# PLAN DE DESARROLLO — App AR de Componentes de PC

Proyecto universitario "Tópicos en Tecnologías Inmersivas".
Aplicación móvil Android con Realidad Aumentada (AR) para identificar componentes de PC
y guiar visualmente su ensamblaje/desensamblaje.

## Vision general

- **Motor:** Unity 6 LTS
- **Lenguaje:** C# (VS Code como editor de scripts)
- **AR:** AR Foundation + ARCore XR Plugin
- **Plataforma:** Android
- **Reconocimiento:** Mock primero (simulado), luego IA/Visión artificial
- **Componentes MVP:** RAM (primero), luego SSD, CPU, GPU
- **Modelos 3D:** Primitivos de Unity primero, Blender después (instalado en D:)

## Decisiones confirmadas (del usuario)

| Decision | Valor |
|---|---|
| Version de Unity | Unity 6 LTS mas reciente |
| Modelo 3D RAM inicial | Primitivos de Unity primero |
| Ubicacion Unity Editor | En disco D (`D:\Unity\Editor`) |
| Package name | `com.unt.pcar` |
| Ritmo de trabajo | Aprobar fase por fase |
| VS Code | Se queda en C: |
| Android SDK | Migrar a `D:\Android\Sdk` |
| JDK | Unity baja su propio en `D:\Unity\JDK` |

## Restriccion de espacio

El disco C: tiene solo ~4.6 GB libres. Toda instalacion pesada debe ir en disco D:
(192 GB libres). Las caches de build de Unity/Gradle, que por defecto van a C:,
deben redirigirse a D: para que el build de Android no agote C:.

## Estado del entorno (verificado)

| Herramienta | Estado | Ubicacion |
|---|---|---|
| Git | OK 2.51.2 | repo iniciado |
| Java (Adoptium 17) | OK funcional | `C:\Program Files\Eclipse Adoptium\jdk-17.0.17.10-hotspot` |
| JAVA_HOME | MAL | apunta a dir vacio en C: |
| Android SDK | OK ~6.8 GB | `C:\Users\user\AppData\Local\Android\Sdk` (en C, a migrar) |
| VS Code | OK | `C:\Users\user\AppData\Local\Programs\Microsoft VS Code` |
| Unity Hub | FALTA | instalar en `D:\Unity\Hub` |
| Unity 6 Editor | FALTA | instalar en `D:\Unity\Editor` |
| Blender | FALTA | instalar en `D:\Blender` (fase 12) |

## Mapa de instalaciones (todo en D: excepto VS Code)

| Componente | Ruta en D | Fase |
|---|---|---|
| Unity Hub | `D:\Unity\Hub` | 1 |
| Unity 6 Editor | `D:\Unity\Editor` | 1 |
| Android SDK | `D:\Android\Sdk` | 1 |
| JDK (Unity) | `D:\Unity\JDK` | 1 |
| Cache Gradle | `D:\Unity\GradleCache` | 1 |
| Cache paquetes Unity | `D:\Unity\UnityCache` | 1 |
| Blender | `D:\Blender` | 12 |
| Proyecto | `D:\Projects\pc-ar-assistant` | 2 |
| VS Code | C: (se queda) | 1 |

## FASES

### FASE 1 — Configuracion del entorno
1. Instalar Unity Hub en `D:\Unity\Hub`.
2. Instalar Unity 6 LTS en `D:\Unity\Editor` con modulos:
   - Android Build Support
   - Android SDK & NDK Tools
   - OpenJDK (bajara el propio a D:\Unity\JDK)
3. Migrar Android SDK de C: a `D:\Android\Sdk` (libera 6.8 GB en C:).
   - Actualizar `ANDROID_HOME` y `ANDROID_SDK_ROOT`.
4. Configurar JDK / corregir `JAVA_HOME`.
5. Redirigir caches de build a D:
   - `GRADLE_USER_HOME=D:\Unity\GradleCache`
   - Cache paquetes Unity -> `D:\Unity\UnityCache` (`--cache-path`)
6. Extensiones VS Code: C# , Unity Code Snippets, Debugger for Unity.
7. Verificar entorno.
**Criterio de completado:** Unity 6 LTS funcional en D:, SDK/JDK en D:,
caches redirigidas a D:, Java configurado.

### FASE 2 — Crear proyecto Unity 6
- Proyecto 3D (URP) en `D:\Projects\pc-ar-assistant`.
- Estructura de carpetas en Assets.

### FASE 3 — Configurar Android
- Build Settings -> Android. Package name `com.unt.pcar`. Min SDK, orientacion.

### FASE 4 — VS Code + Unity
- Editor de scripts externo = VS Code. Script de prueba. Compilar y depurar.

### FASE 5 — AR Foundation + ARCore
- Paquetes via Package Manager:
  - `com.unity.xr.arfoundation`
  - `com.unity.xr.arcore`
- XR Plugin settings.

### FASE 6 — Primera escena AR
- XR Origin + AR Session. Probar en dispositivo Android.

### FASE 7 — Arquitectura de componentes
- Modelos de datos C# (ComponentInfo, AssemblyStep, ComponentRepository).
- Datos en JSON/ScriptableObjects. Estructura Scripts/[AR, Recognition, Components,
  Assembly, UI, Core].

### FASE 8 — Reconocimiento simulado
- Interfaz `IRecognitionService`.
- `MockRecognitionService` (boton "Simular reconocimiento RAM").
- `ComponentRepository` -> `RamComponent`.

### FASE 9 — Informacion de RAM
- Pantalla: nombre, tipo (DDR4), funcion, capacidad, velocidad, caracteristicas, advertencias.

### FASE 10 — Guia de ensamblaje
- `AssemblyGuideController` generico + pasos + navegacion Anterior/Siguiente.

### FASE 11 — Guia de desensamblaje
- Misma arquitectura, pasos distintos.

### FASE 12 — Modelo 3D RAM
- Primitivos de Unity -> Prefab reutilizable. Blender despues.

### FASE 13 — SSD, CPU, GPU
- Replicar el patron de RAM.

### FASE 14 — Integracion IA
- `AIRecognitionService` reemplaza al mock sin tocar el resto.

### FASE 15 — Optimizacion y APK final
- Splash, iconos, build release firmado.

## Arquitectura de carpetas (Assets)

```
Assets/
├── Scenes/
├── Scripts/
│   ├── AR/
│   ├── Recognition/
│   ├── Components/
│   ├── Assembly/
│   ├── UI/
│   └── Core/
├── Prefabs/
│   ├── AR/
│   ├── Components/
│   └── UI/
├── Models/
│   ├── RAM/
│   ├── CPU/
│   ├── SSD/
│   └── GPU/
├── Materials/
├── Animations/
├── Data/
└── Textures/
```

## PROGRESO

### FASE 1 — COMPLETADA (2026-08-29)

Instalado:
- Unity Hub 3.21 en `D:\Unity\Hub`
- Unity 6.5 LTS (6000.5.10f1, changeset 3bd4f66ad299) en `D:\Unity\Editor\6000.5.10f1\Editor`
- Android Build Support (AndroidPlayer) -> instalado manualmente via
  `TargetSupportInstaller/UnitySetup-Android-Support-for-Editor-6000.5.10f1.exe`
  (nota: ruta URL usa `TargetSupportInstaller`, no MacEditorTargetInstaller)
- Android SDK migrado de C: a `D:\Android\Sdk` (6.8 GB liberados en C:)
- JAVA_HOME corregido -> Adoptium 17 valido
- Variables persistentes: ANDROID_HOME/SDK_ROOT=D:\Android\Sdk,
  JAVA_HOME=Adoptium, GRADLE_USER_HOME=D:\Unity\GradleCache
- Caches de descarga del Hub redirigidas a D: via junction
  (`%APPDATA%\UnityHub\downloads` -> `D:\Unity\Downloads`)
- VS Code + extensiones: unity-code-snippets, unity-debug, csdevkit
- Licencia Unity Personal activa y valida (verificada en Editor.log)

Decisiones tecnicas importantes:
- El editor NO fue instalado por el Hub (se instalo manualmente con el instalador),
  por lo que el Hub NO puede agregar mas modulos con `install-modules`
  (error: "No modules found... only supported for editors installed with Unity Hub").
- Consecuencia: Unity usara el Android SDK externo de `D:\Android\Sdk` y el JDK
  Adoptium, que ya estan configurados. Se ajustara en Fase 3 (Preferences > External Tools).
- No se instalo el OpenJDK embebido de Unity (ni el SDK/NDK embebido) - no necesario.

Estado de discos al finalizar: C: ~8.7 GB libres (antes 4.6 GB), D: ~167 GB libres.

### FASE 3 — CONFIGURACION ANDROID + BUILD DE PRUEBA (2026-08-29)

Configuracion aplicada via `Assets/Editor/AndroidConfigurer.cs` (batchmode):
- companyName=UNT, productName "PCAR Assistant", bundle `com.unt.pcar`
- minSdk 26 / targetSdk 34, orientacion LandscapeLeft
- Build target Android, SDK/JDK externos configurados

**VALIDACION COMPLETA: primer APK compilado con exito** en
`UnityProject/Builds/PCAR_Test.apk` (imagen de ~19.7 MB, 138 MB sin comprimir).
Queda validado todo el toolchain Android (Gradle 9.1, SDK, NDK, JDK, IL2CPP).

Bugs conocido de Unity 6 en batchmode resuelto:
- Unity en batchmode ignora las EditorPrefs (`AndroidSdkRoot`, `JdkPath`, `AndroidNdkRoot`)
  y cae en las rutas EMBEBIDAS de `...\PlaybackEngines\AndroidPlayer\{SDK,NDK,OpenJDK}`.
  En un editor instalado manualmente esas carpetas no existen -> JDK/SDK/NDK "not found".
- SOLUCION ROBUSTA: crear **junctions** de las rutas embebidas hacia las herramientas reales:
  - `...\AndroidPlayer\OpenJDK` -> `C:\Program Files\Eclipse Adoptium\jdk-17.0.17.10-hotspot`
  - `...\AndroidPlayer\SDK` -> `D:\Android\Sdk`
  - `...\AndroidPlayer\NDK` -> `D:\Android\Sdk\ndk\27.2.12479018`
- JDK: ademas fijar `JAVA_HOME` en la sesion a Adoptium (la de la sesion
  apuntaba a una ruta vieja de OpenJDK que confundia la deteccion).
- NDK: el que ya estaba era **r28 (28.2.13676358)**, pero Unity 6.x solo soporta
  **r27c (27.2.12479018)** (docs oficiales) -> lo rechazaba. Se descargo e instalo
  r27c desde `https://dl.google.com/android/repository/android-ndk-r27c-windows.zip`
  (745 MB) en `D:\Android\Sdk\ndk\27.2.12479018`.

Cronologia de errores resueltos en el build de prueba:
1. `JDK not found` -> junction OpenJDK + JAVA_HOME correcto.
2. `Android SDK ... AndroidPlayer\SDK` (cae en embebida) -> junction SDK.
3. `Android NDK not found` (r28 no soportado) -> instalar r27c + junction NDK.
4. BUILD OK.

Nota: multiples pruebas generaron logs temporales `unity_*.log` en la raiz
(limpados). Los pits `Builds/` se generan en D:, no ocupan C:.

Estado: FASE 3 COMPLETADA. Siguiente -> FASE 4 (VS Code + Unity, depuracion) o
FASE 5 (AR Foundation + ARCore), segun plan.

### FASE 4 — VS CODE + UNITY (2026-08-29)

- `m_ExternalScriptEditor` = Code.exe de VS Code (UnityProject/ProjectSettings/EditorSettings.asset).
- Extensiones VS Code relevantes ya instaladas:
  - `visualstudiotoolsforunity.vstuc` (debugger oficial VS Tools for Unity)
  - `kleber-swf.unity-code-snippets` (snippets de Unity)
  - `ms-dotnettools.csharp` + `csdevkit` + `vscode-dotnet-runtime` (C#/IntelliSense)
  - `ms-ceintl.vscode-language-pack-es` (idioma espanol)
- Creada config de depuracion VS Code en `UnityProject/.vscode/`:
  - `launch.json` (perfiles "Unity Editor" y "Jugador en dispositivo Android" via vstuc)
  - `tasks.json` (tarea `vstuc:openhack`)
- Script de prueba `Assets/Scripts/Core/Bootstrapper.cs` compila sin errores.
- Metodo editor `AndroidConfigurer.GenerateProjectFiles()` para regenerar .sln/.csproj.

Nota: en Unity 6 la generacion de archivos de proyecto (.sln/.csproj) para VS Code
la hace la extension `vstuc` al abrir el editor en modo GUI (no se genera en batchmode).
Abrir imports: primero abrir Unity en GUI una vez para que genere la solucion de
IntelliSense; despues editar desde VS Code.

Estado: FASE 4 COMPLETADA. Siguiente -> FASE 5 (AR Foundation + ARCore).

### FASE 5 — AR FOUNDATION + ARCORE (2026-08-29)

Paquetes instalados (version 6.5.0, compatibles con Unity 6000.5):
- `com.unity.xr.arfoundation` @ 6.5.0
- `com.unity.xr.arcore` @ 6.5.0
- `com.unity.xr.management` @ 4.4.0

Configuracion XR Plug-in Management aplicada via `AndroidConfigurer.ConfigureXR()`
(batchmode) y persistida en `Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset`:
- Creado el asset XRGeneralSettingsPerBuildTarget + sub-assets
  (Android Settings / Android Providers / ARCore Loader).
- Loader ARCore activado para Android (BuildTargetGroup.Android).
- `automaticLoading = true`, `automaticRunning = false`,
  `InitManagerOnStart = true` (config estandar de AR Foundation: el manager
  inicializa el loader, y ARSession controla start/stop).

Nota: la API interna `XRGeneralSettingsPerBuildTarget.GetOrCreate()` es `internal`
(e inaccesible) y la creacion in-memory sin persistir no hacia nada; se reimplemento
la creacion + persistencia del asset completa (CreateAsset + AddConfigObject +
sub-assets) para aplicar el loader ARCore de forma durable.

Estado: FASE 5 COMPLETADA. Siguiente -> FASE 6 (primera escena AR: XR Origin + AR Session).

### FASE 6 — PRIMERA ESCENA AR (2026-08-29)

Tool batchmode `Assets/Editor/ARSceneBuilder.cs`:
- Escena `Assets/Scenes/ARScene.unity` con:
  - `AR Session` (ARSession + ARInputManager)
  - `XR Origin` + `Camera Offset` + `Main Camera` (ARCameraManager + ARCameraBackground +
    TrackedPoseDriver con HandheldARInputDevice)
  - `ARSessionStatus` (onGUI mostrando estado del loader/tracking para validar en device)
- `activeInputHandler = Both` (requerido por TrackedPoseDriver de Input System).

**BUILD AR OK**: `Builds/PCAR_AR.apk` compilado con exito
(~53 MB, 566 MB sin comprimir) via `ARSceneBuilder.BuildARApk()`.
Queda la escena AR funcional lista para probar en dispositivo Android.

Estado: FASE 6 COMPLETADA. Siguiente -> FASE 7 (arquitectura de componentes:
ComponentInfo, AssemblyStep, ComponentRepository + datos JSON/SO para RAM).

### FASE 7 — ARQUITECTURA DE COMPONENTES (2026-08-29)

Modelos de datos C# (namespace `PCAR.Components`), datos independientes del codigo
en JSON cargados via store/resources:
- `Assets/Scripts/Components/AssemblyStep.cs` — paso (stepNumber, title, description,
  warning, arHint).
- `Assets/Scripts/Components/ComponentInfo.cs` — componente (id, name, description,
  function, specifications[], warnings[], assemblySteps[], disassemblySteps[]).
- `Assets/Scripts/Components/ComponentRepository.cs` — cache estatica que carga todos
  los JSON de `Assets/Resources/Data/Components/` y sirve por id (`Get`, `GetRam`,
  `GetAll`). Modular: agregar componente = agregar JSON.
- `Assets/Resources/Data/Components/ram.json` — datos RAM (DDR4, 8 GB, 3200 MHz) con
  5 pasos de ensamblaje y 5 de desensamblaje (del contexto del proyecto).

Nota: el JSON vive en `Assets/Resources/...` (no `Assets/Data/...`) para que
`Resources.LoadAll<TextAsset>` funcione en runtime sin wiring de editor.
Compilado verificado en batchmode (sin errores).

Estado: FASE 7 COMPLETADA. Siguiente -> FASE 8 (reconocimiento simulado:
IRecognitionService + MockRecognitionService + boton "Simular reconocimiento RAM").

### FASE 8 — RECONOCIMIENTO SIMULADO (2026-08-29)

- `Assets/Scripts/Recognition/IRecognitionService.cs` — contrato
  `RecognitionResult Recognize()` + struct `RecognitionResult`
  (success, componentId, confidence, message) con factories Found/NotFound.
  Modular: en Fase 14 se reemplaza el mock por IA sin tocar el resto.
- `Assets/Scripts/Recognition/MockRecognitionService.cs` — devuelve SIEMPRE
  `Found("ram", 0.94)` como si la IA hubiera identificado una RAM.
- `Assets/Scripts/Recognition/MockRecognitionButton.cs` — MonoBehaviour de prueba
  con boton OnGUI "Simular reconocimiento RAM": invoca el servicio, resuelve el id
  contra `ComponentRepository` y muestra el nombre/confianza. (UI real en Fase 9.)
- Compilado verificado en batchmode (sin errores).

Estado: FASE 8 COMPLETADA. Siguiente -> FASE 9 (pantalla de informacion de RAM:
nombre, tipo, funcion, capacidad, velocidad, caracteristicas, advertencias).

### FASE 9 — INFORMACION DE RAM (2026-08-29)

`Assets/Scripts/UI/RamInfoScreen.cs` (namespace `PCAR.UI`):
- Pantalla de informacion renderizada con GUI inmediata (OnGUI) para probarla en
  batchmode sin wiring de prefabs; sustituible por canvas uGUI en Fase 12.
- Muestra nombre, funcion, especificaciones (tipo/capacidad/velocidad, etc.) y
  advertencias del componente.
- Carga el componente desde `ComponentRepository.GetRam()` por defecto.
- Botones fijos inferiores "Ensamblar" / "Desensamblar" con eventos
  `OnAssembleRequested` / `OnDisassembleRequested` que enganchara la Fase 10/11.
- Compilado verificado en batchmode (sin errores).

Estado: FASE 9 COMPLETADA. Siguiente -> FASE 10 (guia de ensamblaje:
AssemblyGuideController generico + pasos + navegacion Anterior/Siguiente).

### FASE 10 — GUIA DE ENSAMBLAJE (2026-08-29)

- `Assets/Scripts/Assembly/AssemblyGuideController.cs` (namespace `PCAR.Assembly`):
  guia paso a paso generica y reutilizable. `Configure(List<AssemblyStep>)` recibe
  los pasos (sirve igual para ensamblaje y desensamblaje). Muestra numero, titulo,
  descripcion, hint AR y advertencia. Navegacion **Anterior / Siguiente** (Anterior
  desactivado en el paso 1; al llegar al ultimo, "Finalizar"). Eventos
  `OnCompleted` / `OnExitRequested`. Pantalla "Procedimiento completado".
- `Assets/Scripts/Core/PCARFlowController.cs` (namespace `PCAR.Core`): orquesta el
  flujo del MVP. InfoScreen <-> guia (ensamblaje con `assemblySteps`, desensamblaje
  con `disassemblySteps`). Reconoce via MockRecognitionService, carga componente del
  repository, y alterna escenas habilitando una a la vez.
- Compilado verificado en batchmode (sin errores).

Estado: FASE 10 COMPLETADA. Siguiente -> FASE 11 (guia de desensamblaje: misma
arquitectura, pasos distintos - ya cubierto por el controller generico + RAM json).

### FASE 11 — GUIA DE DESENSAMBLaje (2026-08-29)

Cubierta por la arquitectura generica de FASE 10: `AssemblyGuideController` recibe
cualquier lista de pasos y `PCARFlowController` enruta `disassemblySteps` (5 pasos
de desmontaje definidos en `ram.json`: apagar, descargar estatica, identificar RAM,
liberar seguros, retirar) al boton "Desensamblar". No hace falta nueva logica.

Estado: FASE 11 COMPLETADA (misma arquitectura, pasos distintos). Siguiente ->
FASE 12 (modelo 3D RAM: primitivos de Unity -> prefab reutilizable).

### FASE 12 — MODELO 3D RAM (2026-08-29)

- `Assets/Editor/RamModelBuilder.cs`: tool batchmode que construye un modelo de RAM
  con primitivos de Unity (Cubes): placa PCB verde, chips laterales negros
  (8 por lado), contactos dorados (40) y muesca de polaridad.
- Genera 3 materiales URP en `Assets/Materials/`:
  `PCB_DarkGreen.mat`, `Chip_Black.mat`, `Contact_Gold.mat`.
- **Prefab reutilizable:** `Assets/Prefabs/Components/RAM.prefab` (~188 KB).
  Se puede sustituir por un modelo de Blender (D:\Blender) mas adelante sin tocar codigo.
- Nota batchmode: `Shader.Find` devuelve null con `-nographics` -> el tool debe
  ejecutarse SIN `-nographics` para que la pipeline de shaders inicialice.
  Se agrego ResolveShader() con varios fallbacks.

Estado: FASE 12 COMPLETADA. Siguiente -> FASE 13 (SSD, CPU, GPU replicando el
patron de RAM).

### FASE 13 — SSD, CPU, GPU (2026-08-29)

Datos JSON (independientes del codigo, los carga `ComponentRepository`):
- `Assets/Resources/Data/Components/ssd.json` — SSD SATA 2.5" (240 GB).
- `Assets/Resources/Data/Components/cpu.json` — CPU socket AM4 (6 nucleos).
- `Assets/Resources/Data/Components/gpu.json` — GPU PCIe x16 (6 GB GDDR6).
Cada uno con function, specifications, warnings y assembly/disassembly steps.

Modelos 3D (primitivos) via `Assets/Editor/ComponentModelBuilder.cs`:
- `Assets/Prefabs/Components/SSD.prefab`, `CPU.prefab`, `GPU.prefab`
  (PCB/shroud/ventiladores/etc. + materiales URP).
- `Assets/Editor/DataVerifier.cs` — valida que los 4 JSON carguen (id, conteo de
  pasos). Verificado en batchmode: ram/ssd/cpu/gpu OK.

Nota: el reconocimiento (mock) sigue devolviendo solo RAM por MVP; estos datos
quedan listos para ampliarlo.

Estado: FASE 13 COMPLETADA. Siguiente -> FASE 14 (integracion IA:
AIRecognitionService reemplaza al mock sin tocar el resto).

### FASE 14 — INTEGRACION IA (skeleton) (2026-08-29)

- `Assets/Scripts/Recognition/IRecognitionModel.cs` (interfaz en
  `AIRecognitionService.cs`): punto de integracion de un modelo real de vision.
  Implementarla = reconocer el componente, sin tocar el resto de la app.
- `Assets/Scripts/Recognition/AIRecognitionService.cs`: reemplazo del mock.
  Sin modelo conectado (o confianza < umbral 0.6) devuelve el mensaje estandar
  "Componente no identificado. Intente acercarse o mejorar la iluminacion."
- `Assets/Scripts/Recognition/RecognitionServiceFactory.cs`: UNICO punto de switch
  `UseAI` (false por defecto -> mock). Activar IA no modifica el resto.
- `PCARFlowController` ahora crea el servicio via factory (no instancia directa).
- Compilado verificado en batchmode (sin errores).

Nota: la integracion de un modelo real de vision (TensorFlow Lite / ML Kit /
servicio externo) queda reservada para cuando se tenga el modelo; el contrato ya
esta listo.

Estado: FASE 14 COMPLETADA. Siguiente -> FASE 15 (optimizacion y APK final:
splash, iconos, build release firmado).

### FASE 15 — APK FINAL RELEASE (2026-08-29)

Escenas finales (`Assets/Editor/GameSceneBuilder.cs`, batchmode):
- `Scenes/MainMenu.unity` — menu principal con titulo + boton "Escanear" (y "Salir").
  Controller `Scripts/UI/MainMenuController.cs` carga la escena MainAR.
- `Scenes/MainAR.unity` — flujo completo en un solo GameObject "PCAR Flow":
  AR Session + XR Origin + AR Camera + Bootstrapper + RamInfoScreen +
  AssemblyGuideController + PCARFlowController (reconocimiento mock -> RAM).

BuildSettings: MainMenu (indice 0) + MainAR.

`AndroidConfigurer` extensiones:
- `ApplySigning()` — firma con keystore autofirmado `keystore/pcar.keystore`
  (alias `pcar`, credenciales en `keystore/CREDENCIALES.txt`, AMBOS gitignored).
- `ApplySplashAndIcon()` — splash con fondo oscuro; icono Android por defecto
  (sustituible por assets custom en Assets/Icon/).
- `BuildReleaseApk()` — compila release (IL2CPP, arm64) firmado.

**BUILD RELEASE OK**: `Builds/PCAR_Final.apk` (~29.9 MB), firmado (verificado con
`apksigner`: cert CN=PCAR/UNT sha256 2431ac1320...). JAR unsigned en jarsigner es
normal (Unity usa APK Signature v2/v3, no jar).

Nota batchmode (recurrente): si se reporta "JDK not found", fijar en la sesion
`JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-17.0.17.10-hotspot` (la variable
heredada apuntaba a la ruta vieja `...\Programs\OpenJDK\jdk-17`, invalida).

Pendiente de validacion fisica: instalar PCAR_Final.apk en dispositivo Android y
verificar ARCore + flujo completo con RAM.

Estado: FASE 15 COMPLETADA. MVP (1-15) cubierto de forma incremental.

## Principios

1. Que funcione.
2. Facil de probar.
3. Modular.
4. Facil de ampliar.

- No sobreingenieria.
- No backend, autenticacion ni BD remota inicial.
- Priorizar velocidad: solucion sencilla, funcional y mantenible.
