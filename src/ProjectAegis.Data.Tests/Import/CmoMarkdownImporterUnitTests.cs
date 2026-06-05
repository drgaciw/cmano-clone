using ProjectAegis.Data.Import;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

public sealed class CmoMarkdownImporterUnitTests
{
    [Theory]
    [InlineData(150, "nm", 0.75)]
    [InlineData(27.8, "km", 0.092666666666666672)]
    [InlineData(5000, "m", 0.05)]
    public void InferBasePd_maps_range_units_deterministically(double range, string unit, double expected)
    {
        var actual = CmoMarkdownImporter.InferBasePd(range, unit);
        Assert.Equal(expected, actual, precision: 12);
    }

    [Theory]
    [InlineData("Test Radar AN/SPY-1", "test-radar-an-spy-1")]
    [InlineData("Acoustic Intercept [MG-13 Svet] , Diesel Submarines", "acoustic-intercept-mg-13-svet")]
    public void SlugPlatformId_normalizes_titles(string title, string expected)
    {
        Assert.Equal(expected, CmoMarkdownImporter.SlugPlatformId(title));
    }
}