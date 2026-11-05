using System.Diagnostics;
using NUnit.Framework;

namespace ProjectAegis.ExperimentRunner.Tests;

public sealed class ExperimentCliTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Test]
    public void Help_explains_metadata_and_ordering()
    {
        var result = Run("--help");
        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.Output, Does.Contain("metadata only").And.Contain("duplicates are retained"));
        });
    }

    [Test]
    public void Missing_arguments_return_usage_error()
    {
        var result = Run();
        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(2));
            Assert.That(result.Error, Does.Contain("Usage:"));
        });
    }

    [Test]
    public void Valid_definition_produces_real_stable_csv_and_metadata_notice()
    {
        WithFiles((input, output) =>
        {
            File.WriteAllText(input, "{\"scenarioId\":\"baltic-patrol\",\"seeds\":[8,7,8],\"ticks\":2,\"doctrineId\":\"d,\\\"label\\\"\",\"roeId\":\"r\"}");
            var first = Run(input, output);
            Assert.That(first.ExitCode, Is.Zero, first.Error);
            var csv = File.ReadAllText(output);
            var second = Run(input, output);
            var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Multiple(() =>
            {
                Assert.That(second.ExitCode, Is.Zero, second.Error);
                Assert.That(File.ReadAllText(output), Is.EqualTo(csv));
                Assert.That(rows, Has.Length.EqualTo(4));
                Assert.That(rows[0].TrimEnd('\r'), Is.EqualTo("scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint"));
                Assert.That(rows.Skip(1).Select(row => row.Split(',')[1]), Is.EqualTo(new[] { "8", "7", "8" }));
                Assert.That(rows[1], Is.EqualTo(rows[3]));
                Assert.That(rows.Skip(1).All(row => row.Split(',').Last().Trim().Length > 0), Is.True);
                Assert.That(first.Error, Does.Contain("metadata only").And.Contain("no runtime policy overrides"));
                Assert.That(first.Output, Does.Contain("Wrote 3 runs"));
            });
        });
    }

    [TestCase("{}", "scenarioId")]
    [TestCase("{", "JSON")]
    [TestCase("{\"scenarioId\":\"missing-scenario\",\"seeds\":[1],\"ticks\":1}", "missing-scenario")]
    public void Invalid_definitions_fail_without_writing_results(string json, string field)
    {
        WithFiles((input, output) =>
        {
            File.WriteAllText(input, json);
            var result = Run(input, output);
            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.EqualTo(1));
                Assert.That(result.Error, Does.Contain(field));
                Assert.That(File.Exists(output), Is.False);
            });
        });
    }

    [Test]
    public void Missing_input_reports_io_error()
    {
        WithFiles((input, output) =>
        {
            var result = Run(input, output);
            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.EqualTo(1));
                Assert.That(result.Error, Does.Contain("Experiment failed:"));
                Assert.That(File.Exists(output), Is.False);
            });
        });
    }

    [Test]
    public void Empty_input_path_returns_a_clear_error()
    {
        WithFiles((_, output) =>
        {
            var result = Run("", output);
            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.EqualTo(1));
                Assert.That(result.Error, Does.StartWith("Experiment failed:"));
                Assert.That(File.Exists(output), Is.False);
            });
        });
    }

    [Test]
    public void Empty_output_path_returns_a_clear_error()
    {
        WithFiles((input, _) =>
        {
            File.WriteAllText(input, "{\"scenarioId\":\"baltic-patrol\",\"seeds\":[7],\"ticks\":1}");
            var result = Run(input, "");
            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.EqualTo(1));
                Assert.That(result.Error, Does.Contain("Experiment failed:"));
                Assert.That(result.Error, Does.Not.Contain("Unhandled exception"));
            });
        });
    }

    private static (int ExitCode, string Output, string Error) Run(params string[] arguments)
    {
        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
            ?? Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? "", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var start = new ProcessStartInfo(host)
        {
            WorkingDirectory = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(Path.Combine(RepoRoot, "tools", "experiment-runner", "bin", configuration, "net8.0", "ProjectAegis.ExperimentRunner.dll"));
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60000))
        {
            process.Kill(entireProcessTree: true);
            throw new AssertionException("Experiment CLI did not exit within 60 seconds.");
        }
        return (process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }

    private static void WithFiles(Action<string, string> test)
    {
        var input = Path.Combine(Path.GetTempPath(), $"aegis-experiment-{Guid.NewGuid():N}.json");
        var output = Path.ChangeExtension(input, ".csv");
        try { test(input, output); }
        finally { File.Delete(input); File.Delete(output); }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ProjectAegis.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
