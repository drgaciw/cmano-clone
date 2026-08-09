using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class GlobeTileStreamingConfigTests
{
    [Test]
    public void Default_uses_cesium_world_terrain_asset_id_and_sse_16()
    {
        var config = GlobeTileStreamingConfig.Default;

        Assert.That(config.IonAssetId, Is.EqualTo(GlobeTileStreamingConfig.CesiumWorldTerrainAssetId));
        Assert.That(config.IonAssetId, Is.EqualTo(1));
        Assert.That(config.MaximumScreenSpaceError, Is.EqualTo(16.0).Within(1e-9));
        Assert.That(config.MaximumScreenSpaceError, Is.EqualTo(GlobeTileStreamingConfig.DefaultMaximumScreenSpaceError));
    }

    [Test]
    public void Default_enables_preload_and_frustum_culling()
    {
        var config = GlobeTileStreamingConfig.Default;

        Assert.That(config.PreloadAncestors, Is.True);
        Assert.That(config.PreloadSiblings, Is.True);
        Assert.That(config.EnableFrustumCulling, Is.True);
    }

    [Test]
    public void Default_includes_optional_baltic_bbox_theater_clip()
    {
        var config = GlobeTileStreamingConfig.Default;

        Assert.That(config.BalticBbox, Is.Not.Null);
        Assert.That(config.BalticBbox!.MinLat, Is.EqualTo(59.5).Within(1e-9));
        Assert.That(config.BalticBbox.MaxLat, Is.EqualTo(60.5).Within(1e-9));
        Assert.That(config.BalticBbox.MinLon, Is.EqualTo(24.0).Within(1e-9));
        Assert.That(config.BalticBbox.MaxLon, Is.EqualTo(25.5).Within(1e-9));
        Assert.That(config.BalticBbox, Is.EqualTo(GlobeTileStreamingConfig.DefaultBalticBbox));
    }

    [Test]
    public void Custom_config_can_omit_baltic_bbox()
    {
        var config = new GlobeTileStreamingConfig(
            IonAssetId: 2,
            MaximumScreenSpaceError: 8.0,
            PreloadAncestors: false,
            PreloadSiblings: false,
            EnableFrustumCulling: false,
            BalticBbox: null);

        Assert.That(config.IonAssetId, Is.EqualTo(2));
        Assert.That(config.MaximumScreenSpaceError, Is.EqualTo(8.0).Within(1e-9));
        Assert.That(config.PreloadAncestors, Is.False);
        Assert.That(config.BalticBbox, Is.Null);
    }

    [Test]
    public void CesiumWorldTerrainAssetId_is_well_known_public_catalog_constant()
    {
        // Document: this is a public Cesium ion asset id, not a secret.
        Assert.That(GlobeTileStreamingConfig.CesiumWorldTerrainAssetId, Is.EqualTo(1));
        Assert.That(GlobeTileStreamingConfig.CesiumWorldTerrainAssetId, Is.GreaterThan(0));
    }
}
