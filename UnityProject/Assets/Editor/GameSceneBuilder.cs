using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

namespace PCAR.EditorTools
{
    /// <summary>
    /// FASE 15: crea las escenas finales de la app.
    /// - MainMenu.launch: menu principal con "Escanear".
    /// - MainAR.unity: flujo completo (AR Session + XR Origin + Info + Guia + Flow).
    /// Reutiliza el enfoque de ARSceneBuilder para construir la parte AR.
    /// </summary>
    public static class GameSceneBuilder
    {
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/Scenes/MainAR.unity";

        public static void BuildAll()
        {
            BuildMenuScene();
            BuildGameScene();
        }

        public static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMainCamera();
            var menu = new GameObject("MainMenuController");
            menu.AddComponent<PCAR.UI.MainMenuController>();

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            Debug.Log("[PCAR][Scene] Menu creado: " + MenuScenePath);
        }

        public static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateARSessionObject();
            CreateXROriginObject();
            CreateFlowObject();

            EditorSceneManager.SaveScene(scene, GameScenePath);
            Debug.Log("[PCAR][Scene] Escena de juego creada: " + GameScenePath);
        }

        private static void CreateFlowObject()
        {
            var flow = new GameObject("PCAR Flow");
            flow.AddComponent<PCAR.Core.Bootstrapper>();
            var info = flow.AddComponent<PCAR.UI.RamInfoScreen>();
            var guide = flow.AddComponent<PCAR.Assembly.AssemblyGuideController>();
            var controller = flow.AddComponent<PCAR.Core.PCARFlowController>();

            // Wiring de referencias cruzadas (mismo GameObject, igual que FlowController espera)
            controller.infoScreen = info;
            controller.guide = guide;

            Debug.Log("[PCAR][Scene] Flujo MVP montado: Info + Guide + FlowController");
        }

        private static void CreateMainCamera()
        {
            var cameraGo = ObjectFactory.CreateGameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraGo.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.10f);
        }

        private static void CreateARSessionObject()
        {
            ObjectFactory.CreateGameObject("AR Session", typeof(ARSession), typeof(ARInputManager));
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

            originGo.AddComponent<PCAR.AR.ARSessionStatus>();
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
    }
}
