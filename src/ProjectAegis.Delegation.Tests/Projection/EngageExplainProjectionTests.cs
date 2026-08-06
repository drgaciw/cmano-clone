using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class EngageExplainProjectionTests
{
    [Test]
    public void Null_preview_is_empty()
    {
        var e = EngageExplainProjection.Project(null);
        Assert.That(e, Is.EqualTo(EngageExplain.Empty));
        Assert.That(e.IsBlocked, Is.False);
    }

    [Test]
    public void CanFire_is_clear()
    {
        var preview = new EngagePreview("DLZ: In", true, null);
        var e = EngageExplainProjection.Project(preview);
        Assert.That(e.IsBlocked, Is.False);
        Assert.That(e.StatusLine, Is.EqualTo(EngageExplainProjection.CanFireLabel));
        Assert.That(e.ReasonPlain, Does.Contain("permitted").IgnoreCase);
    }

    [Test]
    public void Mount_offline_has_plain_reason()
    {
        var preview = new EngagePreview("DLZ: x", false, "MOUNT_OFFLINE");
        var e = EngageExplainProjection.Project(preview);
        Assert.That(e.IsBlocked, Is.True);
        Assert.That(e.ReasonCode, Is.EqualTo("MOUNT_OFFLINE"));
        Assert.That(e.ReasonPlain, Does.Contain("mount").IgnoreCase);
        Assert.That(e.StatusLine, Does.Contain("BLOCKED"));
    }

    [Test]
    public void Context_path_matches_preview()
    {
        var ctx = new EngageContext(
            RangeMeters: 50_000,
            Envelope: new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 2,
            HasFireControlTrack: false,
            MountOnline: true,
            RadarEmconActive: true);
        var e = EngageExplainProjection.ProjectFromContext(in ctx, DlzPersonality.Normal);
        Assert.That(e.IsBlocked, Is.True);
        Assert.That(e.ReasonCode, Is.Not.Null);
    }
}
