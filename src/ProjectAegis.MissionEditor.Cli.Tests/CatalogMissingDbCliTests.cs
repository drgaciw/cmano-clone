using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>UAT CLI-12: read-only catalog verbs given an explicit but missing --db return NOT_FOUND and create nothing.</summary>
[TestFixture]
public sealed class CatalogMissingDbCliTests
{
    private static readonly Dictionary<string, Func<string, TextWriter, int>> Verbs = new()
    {
        ["catalog_intelligence_run"] = (db, w) => CatalogIntelligenceRunCommand.Run(db, w),
        ["catalog_platform_browse"] = (db, w) => CatalogPlatformBrowseCommand.Run(db, w),
        ["catalog_dependency_graph"] = (db, w) => CatalogDependencyGraphCommand.Run(db, w),
        ["catalog_link_report"] = (db, w) => CatalogLinkReportCommand.Run(db, w),
        ["catalog_mount_loadout_quarantine_triage"] = (db, w) =>
            MountLoadoutQuarantineTriageCommand.Run(db, dryRun: true, null, null, w),
    };

    [TestCase("catalog_intelligence_run")]
    [TestCase("catalog_platform_browse")]
    [TestCase("catalog_dependency_graph")]
    [TestCase("catalog_link_report")]
    [TestCase("catalog_mount_loadout_quarantine_triage")]
    public void explicit_missing_db_returns_not_found_and_creates_no_file(string verb)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"aegis-missing-db-{Guid.NewGuid():N}");
        var missing = Path.Combine(dir, "missing.db");
        try
        {
            using var writer = new StringWriter();
            var code = Verbs[verb](missing, writer);

            Assert.That(code, Is.EqualTo(1));
            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.False);
            Assert.That(doc.RootElement.GetProperty("code").GetString(), Is.EqualTo("NOT_FOUND"));
            Assert.That(File.Exists(missing), Is.False, "a missing --db must not be created");
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
