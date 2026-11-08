using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class PlatformCommsListProjectionTests
{
    [Test]
    public void FormatRow_includes_link_role_and_satcom_flag()
    {
        var line = PlatformCommsListProjection.FormatRow(
            new CatalogCommsBinding("u1", "NATO_TADIL_J", Role: "txrx", SatcomCapable: true));

        Assert.That(line, Is.EqualTo("NATO_TADIL_J role=txrx satcom=true"));
    }

    [Test]
    public void FormatRows_sorts_by_link_id()
    {
        var lines = PlatformCommsListProjection.FormatRows(
        [
            new CatalogCommsBinding("u1", "Z_LINK"),
            new CatalogCommsBinding("u1", "A_LINK"),
        ]);

        Assert.That(lines, Is.EqualTo(new[] { "A_LINK role=txrx satcom=false", "Z_LINK role=txrx satcom=false" }));
    }
}