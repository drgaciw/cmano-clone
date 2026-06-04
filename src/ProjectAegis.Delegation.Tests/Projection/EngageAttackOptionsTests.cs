using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class EngageAttackOptionsTests
{
    [Test]
    public void Build_lists_single_salvo_and_hold_with_abort_reason_when_blocked()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 1,
            HasFireControlTrack: true,
            SalvoSize: 2);
        var preview = EngagePreviewProjection.Project(ctx, DlzPersonality.Normal);

        var options = EngageAttackOptions.Build(ctx, preview);

        Assert.That(options, Has.Count.EqualTo(3));
        Assert.That(options[0].Id, Is.EqualTo("fire-single"));
        Assert.That(options[0].Enabled, Is.True);
        Assert.That(options[1].Enabled, Is.False);
        Assert.That(options[1].DisabledReason, Is.EqualTo("NO_AMMO"));
        Assert.That(options[2].Id, Is.EqualTo("hold-fire"));
    }
}