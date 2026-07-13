using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless player build entry point for game-ci / batchmode, plus a manual Build menu item.
///
/// CI usage:
///   Unity -batchmode -quit -projectPath . -executeMethod BuildPlayer.PerformBuild \
///     -buildTarget StandaloneLinux64 -outputPath Builds/Linux64/ProjectAegis \
///     -scriptingBackend IL2CPP -buildVersion 1.0.0
///
///   -buildTarget       StandaloneLinux64 | StandaloneWindows64 | StandaloneOSX  (default: active target)
///   -outputPath        output file path                                         (default: Builds/&lt;target&gt;/ProjectAegis)
///   -scriptingBackend  IL2CPP | Mono2x                                          (default: IL2CPP — release)
///   -buildVersion      bundle version string                                    (default: keep PlayerSettings)
///   -developmentBuild  flag → BuildOptions.Development
///
/// Exits non-zero on failure (batchmode only) so CI gates the build.
/// </summary>
public static class BuildPlayer
{
    private const string DefaultScene = "Assets/Scenes/DelegationSmoke.unity";

    [MenuItem("Build/Build Linux64 Player (Mono)")]
    public static void BuildLinux64Menu() =>
        Run(BuildTarget.StandaloneLinux64, "Builds/Linux64/ProjectAegis",
            ScriptingImplementation.Mono2x, version: null, BuildOptions.None);

    /// <summary>CI entry point — reads all parameters from the command line.</summary>
    public static void PerformBuild()
    {
        var args = Environment.GetCommandLineArgs();
        var target = ParseTarget(GetArg(args, "-buildTarget")) ?? EditorUserBuildSettings.activeBuildTarget;
        var backend = ParseBackend(GetArg(args, "-scriptingBackend")) ?? ScriptingImplementation.IL2CPP;
        var version = GetArg(args, "-buildVersion");
        var output = GetArg(args, "-outputPath") ?? DefaultOutput(target);
        var options = HasFlag(args, "-developmentBuild") ? BuildOptions.Development : BuildOptions.None;
        Run(target, output, backend, version, options);
    }

    private static void Run(BuildTarget target, string outputPath, ScriptingImplementation backend,
        string version, BuildOptions options)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var group = BuildPipeline.GetBuildTargetGroup(target);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(group), backend);
        if (!string.IsNullOrEmpty(version))
        {
            PlayerSettings.bundleVersion = version;
        }

        var scenes = ResolveScenes();
        Debug.Log($"[BuildPlayer] Building target={target} backend={backend} scenes={scenes.Length} out={outputPath}");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            targetGroup = group,
            options = options,
        });

        var summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildPlayer] Succeeded: {summary.totalSize} bytes ({summary.totalTime}).");
            Exit(0);
        }
        else
        {
            Debug.LogError($"[BuildPlayer] Failed: result={summary.result} errors={summary.totalErrors}.");
            Exit(1);
        }
    }

    /// <summary>Enabled scenes from Build Settings, falling back to the smoke scene for RC verification.</summary>
    private static string[] ResolveScenes()
    {
        var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        return enabled.Length > 0 ? enabled : new[] { DefaultScene };
    }

    private static string DefaultOutput(BuildTarget target)
    {
        var ext = target == BuildTarget.StandaloneWindows64 ? ".exe" : string.Empty;
        return $"Builds/{target}/ProjectAegis{ext}";
    }

    private static BuildTarget? ParseTarget(string value) => value switch
    {
        null or "" => null,
        "StandaloneLinux64" or "Linux64" => BuildTarget.StandaloneLinux64,
        "StandaloneWindows64" or "Win64" => BuildTarget.StandaloneWindows64,
        "StandaloneOSX" or "OSX" => BuildTarget.StandaloneOSX,
        _ => throw new ArgumentException($"Unsupported -buildTarget '{value}'"),
    };

    private static ScriptingImplementation? ParseBackend(string value) => value switch
    {
        null or "" => null,
        "IL2CPP" => ScriptingImplementation.IL2CPP,
        "Mono2x" or "Mono" => ScriptingImplementation.Mono2x,
        _ => throw new ArgumentException($"Unsupported -scriptingBackend '{value}'"),
    };

    private static string GetArg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static bool HasFlag(string[] args, string name) => Array.IndexOf(args, name) >= 0;

    private static void Exit(int code)
    {
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(code);
        }
    }
}
