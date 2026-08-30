using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.Management;

namespace PCAR.EditorTools
{
    /// <summary>
    /// Configuracion de Android para PCAR.
    /// Se ejecuta con: Unity -batchmode -executeMethod PCAR.EditorTools.AndroidConfigurer.Configure
    /// </summary>
    public static class AndroidConfigurer
    {
        public static void Configure()
        {
            SwitchToAndroid();

            PlayerSettings.companyName = "UNT";
            PlayerSettings.productName = "PCAR Assistant";
            PlayerSettings.bundleVersion = "0.1.0";

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.unt.pcar");

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            SetExternalAndroidTools();

            Debug.Log("[PCAR] Configuracion Android aplicada: com.unt.pcar (SDK29+, target34)");
        }

        public static void GenerateProjectFiles()
        {
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
            AssetDatabase.Refresh();
            Debug.Log("[PCAR] Archivos de proyecto (.sln/.csproj) regenerados para VS Code");
        }

        public static void Verify()
        {
            Debug.Log("[PCAR][Verify] Active Build Target: " + EditorUserBuildSettings.activeBuildTarget);
            Debug.Log("[PCAR][Verify] Android SDK Root: " + EditorPrefs.GetString("AndroidSdkRoot"));
            Debug.Log("[PCAR][Verify] JDK Path: " + EditorPrefs.GetString("JdkPath"));
            Debug.Log("[PCAR][Verify] Product Name: " + PlayerSettings.productName);
            Debug.Log("[PCAR][Verify] Bundle ID (Android): " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Debug.Log("[PCAR][Verify] MinSdk: " + PlayerSettings.Android.minSdkVersion + " | TargetSdk: " + PlayerSettings.Android.targetSdkVersion);
        }

        private static void SwitchToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        private static void SetExternalAndroidTools()
        {
            string sdk = @"D:\Android\Sdk";
            string jdk = @"C:\Program Files\Eclipse Adoptium\jdk-17.0.17.10-hotspot";

            if (System.IO.Directory.Exists(sdk))
            {
                EditorPrefs.SetString("AndroidSdkRoot", sdk);
                Debug.Log("[PCAR] Android SDK externo -> " + sdk);
            }
            else
            {
                Debug.LogWarning("[PCAR] No existe el SDK en " + sdk);
            }

            if (System.IO.Directory.Exists(jdk))
            {
                EditorPrefs.SetString("JdkPath", jdk);
                Debug.Log("[PCAR] JDK externo -> " + jdk);
            }
            else
            {
                Debug.LogWarning("[PCAR] No existe el JDK en " + jdk);
            }
        }

        public static void ConfigureXR()
        {
            const string settingsFolder = "Assets/XR/Settings";
            if (!AssetDatabase.IsValidFolder(settingsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/XR"))
                    AssetDatabase.CreateFolder("Assets", "XR");
                AssetDatabase.CreateFolder("Assets/XR", "Settings");
            }

            var perBuildTarget = GetOrCreatePerBuildTarget();
            var androidSettings = perBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (androidSettings == null)
            {
                androidSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                androidSettings.name = BuildTargetGroup.Android + " Settings";
                AssetDatabase.AddObjectToAsset(androidSettings, perBuildTarget);
                perBuildTarget.SetSettingsForBuildTarget(BuildTargetGroup.Android, androidSettings);
            }

            var manager = androidSettings.Manager;
            if (manager == null)
            {
                manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = BuildTargetGroup.Android + " Providers";
                AssetDatabase.AddObjectToAsset(manager, perBuildTarget);
                androidSettings.Manager = manager;
            }

            manager.automaticLoading = true;
            manager.automaticRunning = false;

            bool arcoreSet = false;
            foreach (var loader in manager.activeLoaders)
            {
                if (loader != null && loader is ARCoreLoader)
                {
                    arcoreSet = true;
                    break;
                }
            }

            if (!arcoreSet)
            {
                var arcoreLoader = ScriptableObject.CreateInstance<ARCoreLoader>();
                arcoreLoader.name = "ARCore Loader";
                AssetDatabase.AddObjectToAsset(arcoreLoader, perBuildTarget);
                manager.TrySetLoaders(new System.Collections.Generic.List<XRLoader> { arcoreLoader });
                Debug.Log("[PCAR][XR] ARCore loader activado para Android");
            }

            EditorUtility.SetDirty(perBuildTarget);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PCAR][XR] XR Plug-in Management configurado (Android): " + AssetDatabase.GetAssetPath(perBuildTarget));
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreatePerBuildTarget()
        {
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget existing))
                return existing;

            var assets = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (assets.Length > 0)
            {
                var found = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(assets[0]));
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, found, true);
                return found;
            }

            var perBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            string path = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
            AssetDatabase.CreateAsset(perBuildTarget, path);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);
            return perBuildTarget;
        }

        public static void BuildTestApk()
        {
            Configure();

            string scenePath = "Assets/Scenes/MainMenu.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log("[PCAR] Escena base creada: " + scenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            string output = "Builds/PCAR_Test.apk";
            System.IO.Directory.CreateDirectory("Builds");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                target = BuildTarget.Android,
                locationPathName = output,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[PCAR] BUILD OK -> " + output + " (" + report.summary.totalSize + " bytes)");
            }
            else
            {
                Debug.LogError("[PCAR] BUILD FALLIDO: " + report.summary.result);
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error)
                        {
                            Debug.LogError("[PCAR] " + msg.content);
                        }
                    }
                }
            }
        }
    }
}
