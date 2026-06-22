using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

public static class BuildPlayer
{
    [MenuItem("Build/Build Linux64 Player")]
    public static void BuildLinux64()
    {
        string buildPath = "Builds/Linux64/ProjectAegis";
        Directory.CreateDirectory(Path.GetDirectoryName(buildPath));

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/DelegationSmoke.unity" }; // Use available smoke scene for RC1 verification; expand for full game scenes
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }
        else if (summary.result == BuildResult.Failed)
        {
            UnityEngine.Debug.LogError("Build failed");
        }
    }
}
