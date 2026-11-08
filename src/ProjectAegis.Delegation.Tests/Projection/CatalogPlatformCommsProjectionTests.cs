using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class CatalogPlatformCommsProjectionTests
{
    [Test]
    public void ForPlatform_from_export_data_filters_and_sorts()
    {
        var data = new PlatformCatalogExportData(
            Platforms: [],
            Sensors: [],
            Mounts: [],
            Loadouts: [],
            Magazines: [],
            Comms:
            [
                new CatalogCommsBinding("u2", "LINK_B"),
                new CatalogCommsBinding("u1", "LINK_A"),
                new CatalogCommsBinding("u1", "LINK_C"),
            ]);

        var u1 = CatalogPlatformCommsProjection.ForPlatform(data, "u1");
        Assert.That(u1.Select(c => c.LinkId), Is.EqualTo(new[] { "LINK_A", "LINK_C" }));
    }
}