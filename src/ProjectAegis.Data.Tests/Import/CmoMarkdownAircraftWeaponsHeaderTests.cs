using ProjectAegis.Data.Import;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

/// <summary>KILLCHAIN-05: aircraft markdown uses **Weapons / Loadouts**, not bare **Weapons**.</summary>
public sealed class CmoMarkdownAircraftWeaponsHeaderTests
{
    private const string AircraftWeaponsLoadoutsMarkdown =
        """
        # Aircraft weapons header fixture

        ### Super Tucano A-29B , Fixture
        <sub>[/aircraft/90001/](https://cmano-db.com/aircraft/90001/)</sub>

        | Field | Value |
        |---|---|
        | Type | Attack |
        | Nationality | NATO |

        **Weapons / Loadouts**

        - GBU-12 Paveway II — Bomb — Land Max: 15 km.
        - 20mm Gun Pod — Gun — Air Max: 2 km.

        """;

    [Theory]
    [InlineData("**Weapons**", true)]
    [InlineData("**Weapons / Loadouts**", true)]
    [InlineData("  **Weapons / Loadouts**  ", true)]
    [InlineData("**Sensors / EW**", false)]
    [InlineData("Weapons", false)]
    public void IsWeaponsSectionHeader_accepts_ship_and_aircraft_forms(string line, bool expected)
    {
        Assert.Equal(expected, CmoMarkdownImporter.IsWeaponsSectionHeader(line));
    }

    [Fact]
    public void ReadPlatformMountsFromText_parses_aircraft_Weapons_Loadouts_header()
    {
        var mounts = CmoMarkdownImporter.ReadPlatformMountsFromText(AircraftWeaponsLoadoutsMarkdown);
        Assert.True(mounts.Count >= 1, "expected at least one mount from **Weapons / Loadouts** section");
        Assert.Contains(mounts, m => m.PlatformId.Contains("super-tucano", StringComparison.OrdinalIgnoreCase)
            || m.PlatformId.Contains("a-29", StringComparison.OrdinalIgnoreCase)
            || m.MountId.Length > 0);
        Assert.All(mounts, m => Assert.False(string.IsNullOrWhiteSpace(m.MountId)));
    }

    [Fact]
    public void ReadPlatformLoadoutsFromText_parses_aircraft_Weapons_Loadouts_header()
    {
        var loadouts = CmoMarkdownImporter.ReadPlatformLoadoutsFromText(AircraftWeaponsLoadoutsMarkdown);
        Assert.True(loadouts.Count >= 1, "expected default loadout when **Weapons / Loadouts** section is present");
    }
}
