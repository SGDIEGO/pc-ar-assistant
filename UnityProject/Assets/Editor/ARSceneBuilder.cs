using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

namespace PCAR.EditorTools
{
    /// <summary>
    /// FASE 6: construye la primera escena AR (AR Session + XR Origin + AR Camera)
    /// replica el flujo de "GameObject > XR > XR Origin (Mobile AR)" y "AR Session"
    /// para poder ejecutarlo en batchmode y generar el APK de prueba con AR.
    /// </summary>
    public static class ARSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ARScene.unity";

        public static void CreateARScene()
        {
            EnableBothInputSystems();
            Debug.Log("[PCAR][AR] Active Input Handling -> Both (requerido por TrackedPoseDriver de Input System)");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateARSessionObject();
            CreateXROriginObject();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[PCAR][AR] Escena AR creada: " + ScenePath);
        }

        // En Unity 6 PlayerSettings.activeInputHandler ya no existe como API publica;
        // se configura via la propiedad serializada "activeInputHandler" (0=Old, 1=New, 2=Both).
        private static void EnableBothInputSystems()
        {
            var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettings == null || playerSettings.Length == 0)
            {
                Debug.LogWarning("[PCAR][AR] No se encontro ProjectSettings.asset para configurar el input.");
                return;
            }

            var so = new SerializedObject(playerSettings[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null)
            {
                prop.intValue = 2;
                so.ApplyModifiedProperties();
            }
        }

        private static void CreateARSessionObject()
        {
            var arSession = ObjectFactory.CreateGameObject("AR Session",
                typeof(ARSession), typeof(ARInputManager));
            arSession.transform.SetSiblingIndex(0);
            Debug.Log("[PCAR][AR] AR Session + ARInputManager creados");
        }

        private static void CreateXROriginObject()
        {
            var originGo = ObjectFactory.CreateGameObject("XR Origin", typeof(XROrigin));

            var offsetGo = ObjectFactory.CreateGameObject("Camera Offset");
            offsetGo.transform.SetParent(originGo.transform, false);

            var arCamera = CreateARMainCamera();
            arCamera.transform.SetParent(offsetGo.transform, false);

            var origin = originGo.GetComponent<XROrigin>();
            origin.CameraFloorOffsetObject = offsetGo;
            origin.Camera = arCamera;

            var status = originGo.AddComponent<PCAR.AR.ARSessionStatus>();
            Debug.Log("[PCAR][AR] XR Origin + Camera Offset + AR Camera creado (status: " + status.GetType().Name + ")");
        }

        private static Camera CreateARMainCamera()
        {
            var cameraGo = ObjectFactory.CreateGameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener),
                typeof(ARCameraManager),
                typeof(ARCameraBackground),
                typeof(TrackedPoseDriver));

            var camera = cameraGo.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20f;

            var trackedPoseDriver = cameraGo.GetComponent<TrackedPoseDriver>();
            var positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
            var rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
            trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
            trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);
            return camera;
        }

        public static void BuildARApk()
        {
            CreateARScene();
            AndroidConfigurer.Configure();

            string output = "Builds/PCAR_AR.apk";
            System.IO.Directory.CreateDirectory("Builds");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = BuildTarget.Android,
                locationPathName = output,
                options = BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[PCAR][AR] BUILD OK -> " + output + " (" + report.summary.totalSize + " bytes)");
            }
            else
            {
                Debug.LogError("[PCAR][AR] BUILD FALLIDO: " + report.summary.result);
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error)
                        {
                            Debug.LogError("[PCAR][AR] " + msg.content);
                        }
                    }
                }
            }
        }
    }
}