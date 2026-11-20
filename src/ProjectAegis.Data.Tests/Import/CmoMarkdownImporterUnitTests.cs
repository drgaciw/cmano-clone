using ProjectAegis.Data.Import;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Import;

public sealed class CmoMarkdownImporterUnitTests
{
    [TestCase(150, "nm", 0.75)]
    [TestCase(27.8, "km", 0.092666666666666672)]
    [TestCase(5000, "m", 0.05)]
    public void InferBasePd_maps_range_units_deterministically(double range, string unit, double expected)
    {
        var actual = CmoMarkdownImporter.InferBasePd(range, unit);
        Assert.That(actual, Is.EqualTo(expected).Within(0.000000000005));
    }
    [TestCase("Test Radar AN/SPY-1", "test-radar-an-spy-1")]
    [TestCase("Acoustic Intercept [MG-13 Svet] , Diesel Submarines", "acoustic-intercept-mg-13-svet")]
    public void SlugPlatformId_normalizes_titles(string title, string expected)
    {
        Assert.That(CmoMarkdownImporter.SlugPlatformId(title), Is.EqualTo(expected));
    }
    [TestCase("ASR - Submarine Rescue Ship", "surface")]
    [TestCase("AS - Submarine Tender", "surface")]
    [TestCase("PC - Submarine Chaser", "surface")]
    [TestCase("Anti-Submarine Warfare (ASW)", "air")]
    [TestCase("Aircraft - Helicopter ASW (NFH)", "air")]
    [TestCase("CVN - Nuclear Powered Aircraft Carrier", "surface")]
    [TestCase("SSN - Nuclear Powered Attack Submarine", "subsurface")]
    [TestCase("SSK - Hunter-Killer Submarine", "subsurface")]
    [TestCase("SS - Attack/Fleet Submarine", "subsurface")]
    [TestCase("SSBN - Nuclear Powered Ballistic Missile Submarine", "subsurface")]
    [TestCase("UUV - Unmanned Underwater Vehicle", "subsurface")]
    [TestCase("ROV - Remotely Operated Vehicle", "subsurface")]
    [TestCase("SDV - Swimmer Delivery Vehicle", "subsurface")]
    [TestCase("Underwater", "subsurface")]
    public void InferDomain_keeps_surface_auxiliaries_out_of_subsurface(
        string platformClass,
        string expectedDomain)
    {
        Assert.That(CmoMarkdownImporter.InferDomain(platformClass), Is.EqualTo(expectedDomain));
        // Ship.md default is surface; submarine.md default is subsurface — auxiliaries must
        // stay surface even when the entity-family default would prefer subsurface.
        Assert.That(CmoMarkdownImporter.InferDomain(platformClass, "subsurface"), Is.EqualTo(expectedDomain));
    }
}