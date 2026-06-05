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
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}