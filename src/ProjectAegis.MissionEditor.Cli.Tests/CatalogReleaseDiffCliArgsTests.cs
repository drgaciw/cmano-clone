namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

/// <summary>
/// <c>Program.cs</c>'s <c>catalog_release_diff</c> positional-invocation fallback
/// (<c>catalog_release_diff --db &lt;catalog.db&gt; &lt;fromReleaseVersion&gt; &lt;toReleaseVersion&gt;</c>,
/// documented by <see cref="CatalogReleaseDiffCommand.PrintHelp"/>) only exists inside Program.cs's
/// top-level-statement local function -- it is never exercised by <see cref="CatalogReleaseDiffCommand.Run"/>
/// unit tests, which all call the command class directly with already-parsed <c>from</c>/<c>to</c> strings.
/// This test spawns the actual built CLI so the real argument-resolution bug in Program.cs is reproduced.
/// </summary>
public sealed class CatalogReleaseDiffCliArgsTests
{
    [Fact]
    public void catalog_release_diff_positional_invocation_resolves_same_diff_as_flag_invocation()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-release-diff-positional-{Guid.NewGuid():N}.db");
        const string fromVersion = "unified-corpus-TL-0-cli-positional-from";
        const string toVersion = "unified-corpus-TL-0-cli-positional-to";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-cli-positional-from", batchSuffix: "sensor-cli-positional-from");
            SeedDomainRelease(dbPath, "nightly-platform-s32-07-cli-positional-from", batchSuffix: "platform-cli-positional-from");
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-cli-positional-to", batchSuffix: "sensor-cli-positional-to", maxRecords: 12);
            SeedDomainRelease(dbPath, "nightly-weapon-s32-07-cli-positional-to", batchSuffix: "weapon-cli-positional-to");

            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordUnifiedRelease(
                    fromVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-07-cli-positional-from", "nightly-platform-s32-07-cli-positional-from"],
                    createdUtcTicks: 9630);

                store.RecordUnifiedRelease(
                    toVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-07-cli-positional-to", "nightly-weapon-s32-07-cli-positional-to"],
                    createdUtcTicks: 9640);
            }

            // Exact positional invocation form documented by CatalogReleaseDiffCommand.PrintHelp:
            //   catalog_release_diff --db <catalog.db> <fromReleaseVersion> <toReleaseVersion>
            // No --from/--to flags given, so Program.cs must fall back to positional resolution.
            var (exitCode, stdout, stderr) = RunCli("catalog_release_diff", "--db", dbPath, fromVersion, toVersion);
            Assert.Equal(0, exitCode);
            Assert.True(string.IsNullOrEmpty(stderr), $"Unexpected stderr: {stderr}");

            using var doc = JsonDocument.Parse(stdout);
            var root = doc.RootElement;
            Assert.True(root.GetProperty("ok").GetBoolean());

            // The positional form must resolve the SAME from/to release versions as the flag form
            // (CatalogImport_catalog_release_diff_emits_sorted_delta_rows in CatalogReleaseDiffCommandTests
            // proves --from/--to flags with this exact fixture shape yield diffCount=3, Added/Changed/Removed).
            Assert.Equal(fromVersion, root.GetProperty("fromReleaseVersion").GetString());
            Assert.Equal(toVersion, root.GetProperty("toReleaseVersion").GetString());
            Assert.False(root.GetProperty("isEmpty").GetBoolean());
            Assert.Equal(3, root.GetProperty("diffCount").GetInt32());
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var cliDllPath = Path.Combine(AppContext.BaseDirectory, "ProjectAegis.MissionEditor.Cli.dll");
        Assert.True(File.Exists(cliDllPath), $"Expected built CLI at {cliDllPath}");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(cliDllPath);
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(60_000);
        return (proc.ExitCode, stdout, stderr);
    }

    private static void SeedDomainRelease(
        string dbPath,
        string releaseVersion,
        string batchSuffix,
        int maxRecords = 6)
    {
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var propose = CmoMarkdownImportProposer.ProposeFromMarkdown(
            dbPath,
            markdown,
            maxRecords: maxRecords,
            chunkSize: 500,
            clock: new FixedCatalogClock(8000));

        using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(8001));
        Assert.True(gate.ApproveBatch(propose.Batches[0].BatchId, "human", "release-diff-cli-positional-test").Committed);
        CatalogSnapshotBinder.BindAfterApprove(
            dbPath,
            propose.Batches[0].BatchId,
            new FixedCatalogClock(8002),
            releaseVersion: releaseVersion);
    }

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}
