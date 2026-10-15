using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildScript
{
    // Pipeline completo: configura el marcador a A4, reconstruye el modelo con la escala y los
    // materiales correctos, y solo despues compila. Si se compila sin este paso el APK lleva la
    // escena vieja (modelo de 0.21 m, rotacion editada a mano y tamano de marcador 0.21x0.174).
    // Se invoca con:
    //   Unity.exe -batchmode -quit -projectPath <proj> -executeMethod BuildScript.BuildFull
    public static void BuildFull()
    {
        EditorSceneManager.OpenScene(SceneBuilder.ScenePath, OpenSceneMode.Single);

        SceneBuilder.ConfigureTarget();
        SceneBuilder.Diagnose();
        SceneBuilder.RebuildScene();

        Debug.Log("[BuildScript] Escena lista. Estado del ImageTarget:");
        GameObject target = GameObject.Find(SceneBuilder.ImageTargetName);
        if (target != null)
        {
            foreach (MonoBehaviour mb in target.GetComponents<MonoBehaviour>())
                if (mb != null) Debug.Log("[BuildScript]   " + mb.GetType().Name + " en " + target.name);
        }
        else
        {
            Debug.LogError("[BuildScript] No se encontro '" + SceneBuilder.ImageTargetName + "' tras reconstruir.");
        }

        BuildAndroid();
    }

    // Solo el sondeo de semantica de mWidth/mHeight, sin compilar APK. Para deducir que relacion
    // impone el ImageTargetBehaviour de Vuforia antes de tocar ConfigureTarget.
    public static void ProbeOnly()
    {
        SceneBuilder.ProbeTargetSize();
        EditorApplication.Exit(0);
    }

    public static void BuildAndroid()
    {
        string[] scenes = { "Assets/Scenes/SampleScene.unity" };
        string outputDir = Path.Combine(Application.dataPath, "..", "Builds");
        Directory.CreateDirectory(outputDir);
        string apk = Path.Combine(outputDir, "ESCULTURA1.apk");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apk,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        bool ok = report.summary.result == BuildResult.Succeeded;
        if (!ok)
        {
            Debug.LogError("BUILD FAILED");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("BUILD OK: " + apk);
        EditorApplication.Exit(0);
    }
}