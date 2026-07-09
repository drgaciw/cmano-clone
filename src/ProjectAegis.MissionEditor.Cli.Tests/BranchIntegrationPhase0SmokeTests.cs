namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Diagnostics;
using System.Text;
using Xunit;

/// <summary>
/// Encodes the Phase 0 verification block from
/// <c>production/agentic/scenario-editor-branch-integration-plan-2026-07-04.md</c>
/// as an executable gate (TDD wrapper around <c>tools/ci/smoke-scenario-editor-phase0.sh</c>).
/// </summary>
/// <remarks>
/// Does <b>not</b> nest a full <c>dotnet build</c>: CI already builds the solution before
/// <c>dotnet test</c>, and a nested build under the test host is prone to MSBuild node-reuse
/// deadlock and multiplies wall-clock beyond typical Buildkite budgets (~3 min agent kills).
/// </remarks>
public sealed class BranchIntegrationPhase0SmokeTests
{
    [Fact]
    public void Phase0_smoke_script_exists_and_passes_quick_mode()
    {
        var repoRoot = RequireRepoRoot();
        var script = Path.Combine(repoRoot, "tools", "ci", "smoke-scenario-editor-phase0.sh");
        Assert.True(File.Exists(script), $"Expected Phase 0 script at {script}");

        // Prefer the configuration the solution was built with (CI uses Release).
        var config = Directory.Exists(Path.Combine(repoRoot, "src", "ProjectAegis.Data", "bin", "Release"))
            ? "Release"
            : "Debug";

        var (exitCode, stdout, stderr) = RunProcess(
            repoRoot,
            "bash",
            [script, "--quick", "--skip-build"],
            timeoutMs: 180_000,
            extraEnv: new Dictionary<string, string>
            {
                ["MSBUILDDISABLENODEREUSE"] = "1",
                ["DOTNET_NOLOGO"] = "1",
                ["Configuration"] = config,
            });
        Assert.True(
            exitCode == 0,
            $"Phase 0 smoke failed (exit {exitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
    }

    private static string RequireRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 14 && dir != null; i++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException(
            $"Could not locate repo root (ProjectAegis.sln) walking up from {AppContext.BaseDirectory}");
    }

    /// <summary>
    /// Run a process with redirected streams drained asynchronously to avoid pipe-buffer deadlock.
    /// On timeout the process tree is killed; ExitCode is only read after the process has exited.
    /// </summary>
    private static (int ExitCode, string Stdout, string Stderr) RunProcess(
        string workingDirectory,
        string fileName,
        IReadOnlyList<string> args,
        int timeoutMs,
        IReadOnlyDictionary<string, string>? extraEnv = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        if (extraEnv != null)
        {
            foreach (var (key, value) in extraEnv)
            {
                psi.Environment[key] = value;
            }
        }

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {fileName}");

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdout.AppendLine(e.Data);
            }
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stderr.AppendLine(e.Data);
            }
        };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        if (!proc.WaitForExit(timeoutMs))
        {
            try
            {
                proc.Kill(entireProcessTree: true);
            }
            catch
            {
                // best-effort
            }

            proc.WaitForExit(10_000);
            throw new TimeoutException(
                $"{fileName} exceeded {timeoutMs}ms.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }

        proc.WaitForExit();
        return (proc.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
