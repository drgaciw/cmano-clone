using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class GlobeIonGateProjectionTests
{
    [Test]
    public void Project_no_token_package_present_is_inactive_no_ion_token()
    {
        var gate = GlobeIonGateProjection.Project(hasToken: false, packageAvailable: true);

        Assert.That(gate.HasTokenConfigured, Is.False);
        Assert.That(gate.PackageAvailable, Is.True);
        Assert.That(gate.StreamingActive, Is.False);
        Assert.That(gate.StatusLine, Is.EqualTo(GlobeIonGateProjection.StatusNoIonToken));
        Assert.That(gate.StatusLine, Is.EqualTo("GLOBE TILES · INACTIVE · NO_ION_TOKEN"));
    }

    [Test]
    public void Project_package_missing_is_inactive_package_missing()
    {
        var gate = GlobeIonGateProjection.Project(hasToken: false, packageAvailable: false);

        Assert.That(gate.PackageAvailable, Is.False);
        Assert.That(gate.StreamingActive, Is.False);
        Assert.That(gate.StatusLine, Is.EqualTo(GlobeIonGateProjection.StatusPackageMissing));
        Assert.That(gate.StatusLine, Is.EqualTo("GLOBE TILES · INACTIVE · PACKAGE_MISSING"));
    }

    [Test]
    public void Project_token_present_but_package_missing_still_inactive()
    {
        var gate = GlobeIonGateProjection.Project(hasToken: true, packageAvailable: false);

        Assert.That(gate.HasTokenConfigured, Is.True);
        Assert.That(gate.PackageAvailable, Is.False);
        Assert.That(gate.StreamingActive, Is.False);
        Assert.That(gate.StatusLine, Is.EqualTo(GlobeIonGateProjection.StatusPackageMissing));
    }

    [Test]
    public void Project_token_and_package_is_active()
    {
        var config = GlobeTileStreamingConfig.Default;
        var gate = GlobeIonGateProjection.Project(hasToken: true, packageAvailable: true, config);

        Assert.That(gate.HasTokenConfigured, Is.True);
        Assert.That(gate.PackageAvailable, Is.True);
        Assert.That(gate.StreamingActive, Is.True);
        Assert.That(gate.StatusLine, Is.EqualTo(GlobeIonGateProjection.StatusActive));
        Assert.That(gate.StatusLine, Is.EqualTo("GLOBE TILES · ACTIVE"));
    }

    [Test]
    public void Project_never_stores_token_values_only_presence()
    {
        // Gate record has no token field — only booleans + status line.
        var gate = GlobeIonGateProjection.Project(hasToken: true, packageAvailable: true);
        var props = typeof(GlobeIonGateState).GetProperties();

        Assert.That(props.Select(p => p.Name), Does.Not.Contain("Token"));
        Assert.That(props.Select(p => p.Name), Does.Not.Contain("IonAccessToken"));
        Assert.That(gate.HasTokenConfigured, Is.True);
    }

    [Test]
    public void Apply_formats_status_and_config_for_ui()
    {
        var presentation = GlobeTileStreamingApplyState.ProjectAndApply(
            hasToken: false,
            packageAvailable: true,
            GlobeTileStreamingConfig.Default);

        Assert.That(presentation.StatusLine, Is.EqualTo("GLOBE TILES · INACTIVE · NO_ION_TOKEN"));
        Assert.That(presentation.StreamingActive, Is.False);
        Assert.That(presentation.IonAssetId, Is.EqualTo(GlobeTileStreamingConfig.CesiumWorldTerrainAssetId));
        Assert.That(presentation.ConfigSummary, Does.Contain("asset=1"));
        Assert.That(presentation.ConfigSummary, Does.Contain("sse=16"));
    }

    [Test]
    public void Apply_active_gate_propagates_streaming_true()
    {
        var presentation = GlobeTileStreamingApplyState.ProjectAndApply(
            hasToken: true,
            packageAvailable: true);

        Assert.That(presentation.StatusLine, Is.EqualTo("GLOBE TILES · ACTIVE"));
        Assert.That(presentation.StreamingActive, Is.True);
        Assert.That(presentation.HasTokenConfigured, Is.True);
        Assert.That(presentation.PackageAvailable, Is.True);
    }
}
