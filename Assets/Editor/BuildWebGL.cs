using UnityEditor;
using UnityEngine;

public static class BuildWebGL
{
    [MenuItem("Build/WebGL")]
    public static void Build()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        var scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"WebGL build succeeded: {report.summary.outputPath}");
        }
        else
        {
            Debug.LogError($"WebGL build failed: {report.summary.result}");
        }
    }
}
