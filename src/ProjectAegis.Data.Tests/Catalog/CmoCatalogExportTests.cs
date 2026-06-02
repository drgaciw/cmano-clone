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
            RunNodeExport(fixture, outJson);
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

    private static void RunNodeExport(string rawFixture, string outJson)
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
        using var proc = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start node");
        proc.WaitForExit(60_000);
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"export-catalog-sensors failed: {proc.StandardError.ReadToEnd()}");
        }
    }
}