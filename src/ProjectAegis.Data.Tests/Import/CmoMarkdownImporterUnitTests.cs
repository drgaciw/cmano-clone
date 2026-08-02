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

    /// <summary>
    /// Wave 2: ship.md surface auxiliaries mention "submarine" in Type but are not subsurface.
    /// Representative platform_class strings from aegis_public_corpus.db domain=subsurface audit.
    /// </summary>
    [Theory]
    [InlineData("ASR - Submarine Rescue Ship", "surface")]
    [InlineData("AS - Submarine Tender", "surface")]
    [InlineData("PC - Submarine Chaser", "surface")]
    [InlineData("Anti-Submarine Warfare (ASW)", "air")]
    [InlineData("Aircraft - Helicopter ASW (NFH)", "air")]
    [InlineData("CVN - Nuclear Powered Aircraft Carrier", "surface")]
    [InlineData("SSN - Nuclear Powered Attack Submarine", "subsurface")]
    [InlineData("SSK - Hunter-Killer Submarine", "subsurface")]
    [InlineData("SS - Attack/Fleet Submarine", "subsurface")]
    [InlineData("SSBN - Nuclear Powered Ballistic Missile Submarine", "subsurface")]
    [InlineData("UUV - Unmanned Underwater Vehicle", "subsurface")]
    [InlineData("ROV - Remotely Operated Vehicle", "subsurface")]
    [InlineData("SDV - Swimmer Delivery Vehicle", "subsurface")]
    [InlineData("Underwater", "subsurface")]
    public void InferDomain_keeps_surface_auxiliaries_out_of_subsurface(
        string platformClass,
        string expectedDomain)
    {
        Assert.Equal(expectedDomain, CmoMarkdownImporter.InferDomain(platformClass));
        // Ship.md default is surface; submarine.md default is subsurface — auxiliaries must
        // stay surface even when the entity-family default would prefer subsurface.
        Assert.Equal(expectedDomain, CmoMarkdownImporter.InferDomain(platformClass, "subsurface"));
    }
}