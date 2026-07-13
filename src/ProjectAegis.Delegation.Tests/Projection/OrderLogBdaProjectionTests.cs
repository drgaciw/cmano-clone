using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class OrderLogBdaProjectionTests
{
    [Test]
    public void Killed_contact_drops_from_contact_picture()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendEngagementOutcome(Kill(2, 1, "hostile-1", 10));

        var picture = ContactPictureProjection.ProjectWithBda(log);
        Assert.That(picture, Is.Empty);
    }

    [Test]
    public void Miss_outcome_leaves_contact_picture_unchanged()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendEngagementOutcome(Outcome(2, 1, "hostile-1", EngagementOutcomeCodes.Miss, 10));

        var picture = ContactPictureProjection.ProjectWithBda(log);
        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].ContactId, Is.EqualTo("c1"));
        Assert.That(picture[0].LifecycleState, Is.EqualTo("Identified"));
    }

    [Test]
    public void Hit_damage_level_1_projects_degraded_contact_status()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 75, PlatformDamageChangeReasonCodes.Hit, 1));

        var picture = ContactPictureProjection.ProjectWithBda(log);
        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].LifecycleState, Is.EqualTo(BdaContactDamageStates.DegradedL1));
    }

    [Test]
    public void Hit_damage_level_2_projects_higher_degraded_contact_status()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 50, PlatformDamageChangeReasonCodes.Hit, 2));

        var picture = ContactPictureProjection.ProjectWithBda(log);
        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].LifecycleState, Is.EqualTo(BdaContactDamageStates.DegradedL2));
    }

    [Test]
    public void Hit_damage_level_3_drops_contact_from_picture()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 25, PlatformDamageChangeReasonCodes.Hit, 3));

        var picture = ContactPictureProjection.ProjectWithBda(log);
        Assert.That(picture, Is.Empty);
    }

    [Test]
    public void Hit_damage_level_zero_leaves_contact_unchanged()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 100, PlatformDamageChangeReasonCodes.Hit, 0));

        var picture = ContactPictureProjection.ProjectWithBda(log);
        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].LifecycleState, Is.EqualTo("Identified"));
    }

    [Test]
    public void Multiple_hits_apply_in_stable_tick_then_sequence_order()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 50, PlatformDamageChangeReasonCodes.Hit, 2));
        log.AppendPlatformDamageChange(DamageChange(2, 2, "hostile-2", 100, 75, PlatformDamageChangeReasonCodes.Hit, 1));

        var changes = OrderLogBdaProjection.ProjectBdaContactChanges(
            log,
            ContactPictureProjection.Project(log).ToDictionary(c => c.TargetId));

        Assert.That(changes, Has.Count.EqualTo(2));
        Assert.That(changes[0].TargetId, Is.EqualTo("hostile-1"));
        Assert.That(changes[0].NewState, Is.EqualTo(BdaContactDamageStates.DegradedL2));
        Assert.That(changes[1].TargetId, Is.EqualTo("hostile-2"));
        Assert.That(changes[1].NewState, Is.EqualTo(BdaContactDamageStates.DegradedL1));
    }

    [Test]
    public void Escalating_hits_promote_contact_to_lost()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 75, PlatformDamageChangeReasonCodes.Hit, 1));
        log.AppendPlatformDamageChange(DamageChange(3, 2, "hostile-1", 75, 0, PlatformDamageChangeReasonCodes.Hit, 3));

        var changes = OrderLogBdaProjection.ProjectBdaContactChanges(
            log,
            ContactPictureProjection.Project(log).ToDictionary(c => c.TargetId));

        Assert.That(changes, Has.Count.EqualTo(2));
        Assert.That(changes[0].NewState, Is.EqualTo(BdaContactDamageStates.DegradedL1));
        Assert.That(changes[1].NewState, Is.EqualTo(BdaContactDamageStates.Lost));
        Assert.That(ContactPictureProjection.ProjectWithBda(log), Is.Empty);
    }

    [Test]
    public void Multiple_kills_apply_in_stable_engagement_then_sequence_order()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Identified"));
        log.AppendEngagementOutcome(Kill(2, 2, "hostile-2", 20));
        log.AppendEngagementOutcome(Kill(2, 1, "hostile-1", 10));

        var lost = OrderLogBdaProjection.ProjectLostContacts(
            log,
            ContactPictureProjection.Project(log).ToDictionary(c => c.TargetId));

        Assert.That(lost, Has.Count.EqualTo(2));
        Assert.That(lost[0].TargetId, Is.EqualTo("hostile-1"));
        Assert.That(lost[1].TargetId, Is.EqualTo("hostile-2"));
        Assert.That(ContactPictureProjection.ProjectWithBda(log), Is.Empty);
    }

    [Test]
    public void Ambient_tick_platform_damage_does_not_change_contact_picture()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(
            DamageChange(2, 1, "hostile-1", 100, 99, PlatformDamageChangeReasonCodes.AmbientTick, 0));

        var picture = ContactPictureProjection.ProjectWithBda(log);
        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].LifecycleState, Is.EqualTo("Identified"));
    }

    private static ContactChangeRecord Change(
        ulong tick,
        string contactId,
        string targetId,
        string previous,
        string next) =>
        new(0, tick, tick, "u1", contactId, targetId, previous, next);

    private static PlatformDamageChangeRecord DamageChange(
        ulong tick,
        ulong sequenceId,
        string targetId,
        double previousHp,
        double newHp,
        string reasonCode,
        int damageLevel) =>
        new(sequenceId, tick, tick, new TargetId(targetId), previousHp, newHp, reasonCode, damageLevel);

    private static EngagementOutcomeRecord Kill(
        ulong tick,
        ulong sequenceId,
        string victimTargetId,
        ulong engagementId) =>
        Outcome(tick, sequenceId, victimTargetId, EngagementOutcomeCodes.Kill, engagementId);

    private static EngagementOutcomeRecord Outcome(
        ulong tick,
        ulong sequenceId,
        string victimTargetId,
        string outcomeCode,
        ulong engagementId) =>
        new(
            sequenceId,
            tick,
            tick,
            new TargetId("shooter-1"),
            new TargetId(victimTargetId),
            engagementId,
            outcomeCode,
            0.42);
}