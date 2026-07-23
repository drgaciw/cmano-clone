using System.Text;
using System.Text.Json;
using ProjectAegis.Data.Import;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogImportMarkdownCommandTests : IDisposable
{
    private const string PublicCorpusEnvName = "AEGIS_PUBLIC_CORPUS";
    private readonly string? _previousPublicCorpus;

    public CatalogImportMarkdownCommandTests()
    {
        // Baltic / mini-fixture expectations assume default import mode, not schema-only corpus.
        _previousPublicCorpus = Environment.GetEnvironmentVariable(PublicCorpusEnvName);
        Environment.SetEnvironmentVariable(PublicCorpusEnvName, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(PublicCorpusEnvName, _previousPublicCorpus);
    }

    [Fact]
    public void catalog_import_markdown_proposes_batches_for_mini_fixture()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-p2-{Guid.NewGuid():N}.db");
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();

        try
        {
            using var writer = new StringWriter();
            Assert.Equal(
                0,
                CatalogImportMarkdownCommand.Run(dbPath, markdown, maxRecords: 12, chunkSize: 500, writer));

            var json = writer.ToString();
            Assert.Contains("\"ok\": true", json);
            Assert.Contains("\"parsedCount\": 12", json);
            Assert.Contains("\"batchId\":", json);
            Assert.DoesNotContain("quarantineReport", json);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void catalog_import_markdown_includes_quarantine_report_when_rows_quarantined()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-p2-q-{Guid.NewGuid():N}.db");
        var markdownPath = WriteQuarantineFixture();

        try
        {
            using var writer = new StringWriter();
            Assert.Equal(
                0,
                CatalogImportMarkdownCommand.Run(dbPath, markdownPath, maxRecords: null, chunkSize: 500, writer));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.Equal(1, root.GetProperty("quarantinedCount").GetInt32());
            var report = root.GetProperty("quarantineReport");
            Assert.Equal(JsonValueKind.Array, report.ValueKind);
            Assert.Equal(1, report.GetArrayLength());
            Assert.Equal("confidence_below_minimum", report[0].GetProperty("reason").GetString());
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            if (File.Exists(markdownPath))
            {
                File.Delete(markdownPath);
            }
        }
    }

    [Fact]
    public void catalog_import_markdown_weapon_entity_proposes_fifty_weapons()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-s26-weapon-{Guid.NewGuid():N}.db");
        var markdown = CmoMarkdownImporter.ResolveWeaponSlice50FixturePath();

        try
        {
            using var writer = new StringWriter();
            Assert.Equal(
                0,
                CatalogImportMarkdownCommand.Run(
                    dbPath,
                    markdown,
                    maxRecords: null,
                    chunkSize: 500,
                    writer,
                    reportOutPath: null,
                    entity: CmoMarkdownImportEntity.Weapon));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.Equal("weapon", root.GetProperty("entity").GetString());
            Assert.Equal(50, root.GetProperty("parsedCount").GetInt32());
            Assert.Equal(1, root.GetProperty("batchCount").GetInt32());
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void catalog_import_markdown_platform_entity_proposes_platform_and_mount_batches()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-s26-platform-{Guid.NewGuid():N}.db");
        var markdown = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        // Use mini weapon fixture (not default weapon.md) so magazine batch + quarantine are deterministic.
        var weaponMarkdown = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();

        try
        {
            using var writer = new StringWriter();
            Assert.Equal(
                0,
                CatalogImportMarkdownCommand.Run(
                    dbPath,
                    markdown,
                    maxRecords: null,
                    chunkSize: 500,
                    writer,
                    reportOutPath: null,
                    entity: CmoMarkdownImportEntity.Platform,
                    mapBalticPlatformIds: true,
                    weaponMarkdownPath: weaponMarkdown));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.Equal("platform", root.GetProperty("entity").GetString());
            Assert.Equal(3, root.GetProperty("parsedCount").GetInt32());
            // platform + mount + loadout + magazine
            Assert.Equal(4, root.GetProperty("batchCount").GetInt32());
            // Generic ASuW Missile has no weapon-mini match.
            Assert.Equal(1, root.GetProperty("quarantinedCount").GetInt32());
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void catalog_import_markdown_platform_with_weapon_corpus_reduces_fitting_quarantine()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-weapon-md-{Guid.NewGuid():N}.db");
        var platformMd = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var weaponMd = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var missingWeapon = Path.Combine(Path.GetTempPath(), $"missing-weapon-{Guid.NewGuid():N}.md");

        try
        {
            using var writerNoWeapon = new StringWriter();
            Assert.Equal(
                0,
                CatalogImportMarkdownCommand.Run(
                    dbPath,
                    platformMd,
                    maxRecords: null,
                    chunkSize: 500,
                    writerNoWeapon,
                    weaponMarkdownPath: missingWeapon,
                    entity: CmoMarkdownImportEntity.Platform,
                    mapBalticPlatformIds: true));

            var withoutWeapon = JsonDocument.Parse(writerNoWeapon.ToString()).RootElement
                .GetProperty("quarantinedCount").GetInt32();

            var dbPath2 = Path.Combine(Path.GetTempPath(), $"aegis-cli-weapon-md2-{Guid.NewGuid():N}.db");
            using var writerWithWeapon = new StringWriter();
            Assert.Equal(
                0,
                CatalogImportMarkdownCommand.Run(
                    dbPath2,
                    platformMd,
                    maxRecords: null,
                    chunkSize: 500,
                    writerWithWeapon,
                    weaponMarkdownPath: weaponMd,
                    entity: CmoMarkdownImportEntity.Platform,
                    mapBalticPlatformIds: true));

            var withWeapon = JsonDocument.Parse(writerWithWeapon.ToString()).RootElement
                .GetProperty("quarantinedCount").GetInt32();

            Assert.True(withWeapon < withoutWeapon);
            if (File.Exists(dbPath2))
            {
                File.Delete(dbPath2);
            }
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void ResolveWeaponMarkdownPath_defaults_to_public_weapon_corpus_for_platform_entity()
    {
        var path = CatalogImportMarkdownCommand.ResolveWeaponMarkdownPath(
            CmoMarkdownImportEntity.Platform,
            weaponMarkdownPath: null);
        Assert.NotNull(path);
        Assert.EndsWith("weapon.md", path, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(path));
    }

    [Theory]
    [InlineData("aircraft", 100)]
    [InlineData("submarine", 100)]
    [InlineData("facility", 100)]
    public void catalog_import_markdown_entity_slice_proposes_platform_batches(string entity, int expectedCount)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-s30-{entity}-{Guid.NewGuid():N}.db");
        var markdown = entity switch
        {
            "aircraft" => CmoMarkdownImporter.ResolveAircraftSlice100FixturePath(),
            "submarine" => CmoMarkdownImporter.ResolveSubmarineSlice100FixturePath(),
            "facility" => CmoMarkdownImporter.ResolveFacilitySlice100FixturePath(),
            _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, null),
        };

        try
        {
            using var writer = new StringWriter();
            Assert.Equal(
                0,
                CatalogImportMarkdownCommand.Run(
                    dbPath,
                    markdown,
                    maxRecords: null,
                    chunkSize: 500,
                    writer,
                    reportOutPath: null,
                    entity: CatalogImportMarkdownCommand.ParseEntity(entity)));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.Equal(entity, root.GetProperty("entity").GetString());
            Assert.Equal(expectedCount, root.GetProperty("parsedCount").GetInt32());
            Assert.Equal(1, root.GetProperty("batchCount").GetInt32());
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void catalog_import_markdown_writes_report_out_file()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-p2-out-{Guid.NewGuid():N}.db");
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var reportPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-report-{Guid.NewGuid():N}.json");

        try
        {
            using var writer = new StringWriter();
            Assert.Equal(
                0,
                CatalogImportMarkdownCommand.Run(
                    dbPath,
                    markdown,
                    maxRecords: 5,
                    chunkSize: 500,
                    writer,
                    reportOutPath: reportPath));

            Assert.True(File.Exists(reportPath));
            var fileJson = File.ReadAllText(reportPath);
            Assert.Contains("\"batchCount\":", fileJson);
            Assert.Contains("\"approvedCount\":", fileJson);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            if (File.Exists(reportPath))
            {
                File.Delete(reportPath);
            }
        }
    }

    private static string WriteQuarantineFixture()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-cli-quarantine-{Guid.NewGuid():N}.md");
        var sb = new StringBuilder(
            """
            # Quarantine fixture

            ### Good Radar
            <sub>[/sensor/50001/](https://cmano-db.com/sensor/50001/)</sub>

            | Field | Value |
            |---|---|
            | Type | Radar |
            | Range Max | 80 nm |

            ### Bad Radar
            <sub>[/sensor/50002/](https://cmano-db.com/sensor/50002/)</sub>

            | Field | Value |
            |---|---|
            | Type | Radar |
            | Confidence | 0.1 |
            | Range Max | 80 nm |

            """);
        File.WriteAllText(path, sb.ToString());
        return path;
    }
}