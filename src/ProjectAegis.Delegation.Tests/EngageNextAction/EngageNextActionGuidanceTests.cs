using NUnit.Framework;
using ProjectAegis.Delegation.EngageNextAction;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.Tests.EngageNextAction;

[TestFixture]
public sealed class EngageNextActionGuidanceTests
{
    [TestCase("NO_FIRE_CONTROL_TRACK", "ACQUIRE_TRACK", "track")]
    [TestCase("NoFireControlTrack", "ACQUIRE_TRACK", "track")]
    [TestCase("CommsDenied", "RESTORE_COMMS", "communications")]
    [TestCase("AirNotReady", "WAIT_FOR_READINESS", "ready")]
    [TestCase("MountOffline", "REPAIR_MOUNT", "mount")]
    [TestCase("TRACK_STALE", "ACQUIRE_TRACK", "track")]
    [TestCase("EMCON_RADAR_PASSIVE", "REVIEW_EMISSIONS", "radar")]
    [TestCase("DLZ_OUT", "REPOSITION", "range")]
    [TestCase("WRA_RANGE_DENIAL", "REPOSITION", "range")]
    [TestCase("OUT_OF_ENVELOPE", "REPOSITION", "range")]
    [TestCase("COMMS_DENIED", "RESTORE_COMMS", "communications")]
    [TestCase("MOUNT_OFFLINE", "REPAIR_MOUNT", "mount")]
    [TestCase("AIR_NOT_READY", "WAIT_FOR_READINESS", "ready")]
    [TestCase("TRACK_SPOOFED", "VERIFY_CONTACT", "verify")]
    [TestCase("CONTACT_NOT_IDENTIFIED", "VERIFY_CONTACT", "verify")]
    [TestCase("WRA_SALVO", "REVIEW_SALVO", "salvo")]
    [TestCase("NO_AMMO", "RELOAD_REARM", "rearm")]
    [TestCase("ROE_HOLD_FIRE", "APPROVAL", "authorization")]
    public void Blocked_engagement_has_a_corrective_action_without_issuing_fire(string reason, string code, string expectedWord)
    {
        var row = EngageNextActionProjection.Project(new EngageNextActionInput("u1", "weapon", reason)).Rows.Single();
        var explain = EngageExplainProjection.Project(new EngagePreview("", false, reason));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(row.NextActionCode, Is.EqualTo(code));
            Assert.That(row.IsFireOrder, Is.False);
            Assert.That(explain.NextActionPlain, Does.Contain(expectedWord).IgnoreCase);
        }));
    }

    [Test]
    public void Clear_shot_and_missing_preview_do_not_suggest_a_correction()
    {
        Assert.That(EngageExplainProjection.Project(new EngagePreview("", true, null)).NextActionPlain, Is.Empty);
        Assert.That(EngageExplain.Empty.NextActionPlain, Is.Empty);
    }

    [Test]
    public void Unknown_reason_remains_advisory_without_inventing_a_command()
    {
        var row = EngageNextActionProjection.Project(new EngageNextActionInput("u1", "weapon", "NEW_GATE")).Rows.Single();
        Assert.That(row.NextActionCode, Is.Null);
        Assert.That(row.IsFireOrder, Is.False);
        Assert.That(EngageExplainProjection.Project(new EngagePreview("", false, "NEW_GATE")).NextActionPlain,
            Does.Contain("review").IgnoreCase);
    }
}
