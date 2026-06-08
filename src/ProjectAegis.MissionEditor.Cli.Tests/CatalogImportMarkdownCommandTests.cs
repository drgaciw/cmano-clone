using System.Text;
using System.Text.Json;
using ProjectAegis.Data.Import;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogImportMarkdownCommandTests
{
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