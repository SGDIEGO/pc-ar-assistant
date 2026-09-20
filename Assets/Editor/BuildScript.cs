using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
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