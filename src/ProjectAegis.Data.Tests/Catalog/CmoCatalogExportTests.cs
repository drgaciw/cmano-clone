using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

[Collection("CatalogSqlite")]
public sealed class CmoCatalogExportTests
{
    [Fact]
    public void Cmo_export_fixture_imports_deterministically()
    {
        var fixture = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "sensor-mini.json"));
        var outDir = Path.Combine(Path.GetTempPath(), "aegis-cmo-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        var outJson = Path.Combine(outDir, "cmo_export.json");
        var dbPath = Path.Combine(outDir, "catalog.db");

        try
        {
            ResolveExportJson(fixture, outJson);
            var bindings = CatalogJsonImporter.ReadSensorBindings(outJson);
            Assert.True(bindings.Count >= 2);
            CatalogJsonImporter.WriteSqlite(dbPath, bindings);
            using var reader = new SqliteCatalogReader(dbPath, "cmo-export-test");
            Assert.True(reader.TryGetBasePd("test-radar-an-spy-1", "cmo-sensor-1001", out var pd1));
            Assert.True(pd1 > 0);
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Prefer live node export when the toolchain is on PATH; otherwise use the checked-in golden
    /// so headless CI agents without node/nvm still exercise the import path.
    /// </summary>
    private static void ResolveExportJson(string rawFixture, string outJson)
    {
        if (TryRunNodeExport(rawFixture, outJson))
        {
            return;
        }

        var golden = ResolveGoldenExportPath();
        if (!File.Exists(golden))
        {
            throw new FileNotFoundException($"CMO export golden fixture not found: {golden}");
        }

        File.Copy(golden, outJson, overwrite: true);
    }

    private static string ResolveGoldenExportPath()
    {
        var copied = Path.Combine(AppContext.BaseDirectory, "fixtures", "sensor-mini-export.golden.json");
        if (File.Exists(copied))
        {
            return copied;
        }

        return CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "sensor-mini-export.golden.json"));
    }

    private static bool TryRunNodeExport(string rawFixture, string outJson)
    {
        try
        {
            var script = CatalogJsonImporter.ResolveRepoRelative(
                Path.Combine("tools", "cmano-db-crawler", "export-catalog-sensors.mjs"));
            var workDir = CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "node",
                ArgumentList = { script, rawFixture, outJson },
                WorkingDirectory = workDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null)
            {
                return false;
            }

            proc.WaitForExit(60_000);
            return proc.ExitCode == 0 && File.Exists(outJson);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
