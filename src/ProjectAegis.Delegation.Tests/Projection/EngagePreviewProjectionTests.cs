using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class EngagePreviewProjectionTests
{
    [Test]
    public void Preview_surfaces_dlz_out_when_approaching_under_normal_personality()
    {
        var engage = new ScenarioEngageDefaults(
            90_000,
            1_000,
            100_000,
            defaultMagazineRounds: 2,
            hasFireControlTrack: true,
            dlzPersonality: DlzPersonality.Normal);
        var preview = EngagePreviewProjection.Project(engage);
        Assert.That(preview.CanFire, Is.False);
        Assert.That(preview.AbortPreviewCode, Is.EqualTo(AbortReasonCatalog.Engage.DLZ_OUT));
    }
}