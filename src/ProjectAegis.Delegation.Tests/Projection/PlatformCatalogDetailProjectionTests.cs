using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class PlatformCatalogDetailProjectionTests
{
    [Test]
    public void Format_reflects_selected_browse_row_fields()
    {
        var row = new CatalogPlatformBrowseRow(
            "hostile-1",
            LatDeg: 58.5,
            LonDeg: 21.0,
            CombatRadiusNm: 200.0,
            MaxHp: 500,
            MaxSpeedKnots: 30);

        var detail = PlatformCatalogDetailProjection.Format(row);

        Assert.That(detail.LatLabel, Is.EqualTo("LAT: 58.5°"));
        Assert.That(detail.LonLabel, Is.EqualTo("LON: 21°"));
        Assert.That(detail.CombatRadiusLabel, Is.EqualTo("RADIUS: 200 nm"));
        Assert.That(detail.MaxHpLabel, Is.EqualTo("HP: 500"));
        Assert.That(detail.MaxSpeedLabel, Is.EqualTo("SPEED: 30 kt"));
    }

    [Test]
    public void Format_null_row_shows_empty_placeholders()
    {
        var detail = PlatformCatalogDetailProjection.Format(null);

        Assert.That(detail.LatLabel, Is.EqualTo("LAT: —"));
        Assert.That(detail.LonLabel, Is.EqualTo("LON: —"));
        Assert.That(detail.CombatRadiusLabel, Is.EqualTo("RADIUS: —"));
        Assert.That(detail.MaxHpLabel, Is.EqualTo("HP: —"));
        Assert.That(detail.MaxSpeedLabel, Is.EqualTo("SPEED: —"));
    }

    [Test]
    public void Format_missing_optional_fields_use_placeholders()
    {
        var row = new CatalogPlatformBrowseRow(
            "solo-ship",
            LatDeg: 1,
            LonDeg: 2,
            CombatRadiusNm: 10,
            MaxHp: null,
            MaxSpeedKnots: null);

        var detail = PlatformCatalogDetailProjection.Format(row);

        Assert.That(detail.MaxHpLabel, Is.EqualTo("HP: —"));
        Assert.That(detail.MaxSpeedLabel, Is.EqualTo("SPEED: —"));
    }
}