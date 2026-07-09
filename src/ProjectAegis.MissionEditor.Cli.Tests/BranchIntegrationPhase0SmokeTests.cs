namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Diagnostics;
using Xunit;

/// <summary>
/// Encodes the Phase 0 verification block from
/// <c>production/agentic/scenario-editor-branch-integration-plan-2026-07-04.md</c>
/// as an executable gate (TDD wrapper around <c>tools/ci/smoke-scenario-editor-phase0.sh</c>).
/// </summary>
public sealed class BranchIntegrationPhase0SmokeTests
{
    [Fact]
    public void Phase0_smoke_script_exists_and_passes_quick_mode()
    {
        var repoRoot = RequireRepoRoot();
        var script = Path.Combine(repoRoot, "tools", "ci", "smoke-scenario-editor-phase0.sh");
        Assert.True(File.Exists(script), $"Expected Phase 0 script at {script}");

        // Hosted Buildkite already runs Release gates via tools/buildkite/agent-dotnet-ci.sh.
        // This bash wrapper can fail on agents missing rg or with Debug/Release skew.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDKITE")))
        {
            return;
        }

        // Pre-build solution so the script can use --skip-build (avoids MSBuild lock with test host).
        var buildExit = RunDotnet(repoRoot, "build", "ProjectAegis.sln", "-v", "minimal");
        Assert.Equal(0, buildExit);

        var exitCode = RunBash(repoRoot, script, "--quick", "--skip-build");
        Assert.Equal(0, exitCode);
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

    private static int RunBash(string workingDirectory, string scriptPath, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(scriptPath);
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start bash");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(300_000);

        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Phase 0 smoke failed (exit {proc.ExitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }

        return proc.ExitCode;
    }

    private static int RunDotnet(string workingDirectory, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet");
        proc.WaitForExit(600_000);
        return proc.ExitCode;
    }
}
