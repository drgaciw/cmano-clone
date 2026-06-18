using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogPlatformBrowseCommandTests
{
    [Fact]
    public void catalog_platform_browse_emits_json_with_row_count()
    {
        using var writer = new StringWriter();
        Assert.Equal(0, CatalogPlatformBrowseCommand.Run(dbPath: null, writer, maxRecords: 5));

        var json = writer.ToString();
        Assert.Contains("\"rowCount\":", json);
        Assert.Contains("\"rows\":", json);
    }
}