using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class OrderLogFacilityDamageProjectionTests
{
    [Test]
    public void Hit_outcome_marks_facility_damaged()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendEngagementOutcome(Outcome(2, 1, "runway-1", EngagementOutcomeCodes.Hit, 10));

        var picture = FacilityPictureProjection.ProjectWithDamage(log, facilities);
        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].CapacityState, Is.EqualTo(FacilityCapacityStates.Damaged));
    }

    [Test]
    public void Kill_outcome_marks_facility_destroyed()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendEngagementOutcome(Kill(2, 1, "runway-1", 10));

        var picture = FacilityPictureProjection.ProjectWithDamage(log, facilities);
        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].CapacityState, Is.EqualTo(FacilityCapacityStates.Destroyed));
    }

    [Test]
    public void Miss_outcome_leaves_facility_operational()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendEngagementOutcome(Outcome(2, 1, "runway-1", EngagementOutcomeCodes.Miss, 10));

        var picture = FacilityPictureProjection.ProjectWithDamage(log, facilities);
        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].CapacityState, Is.EqualTo(FacilityCapacityStates.Operational));
    }

    [Test]
    public void Zero_facilities_yields_no_damage_changes()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var log = new DecisionLog();
        log.AppendEngagementOutcome(Kill(2, 1, "runway-1", 10));

        var changes = OrderLogFacilityDamageProjection.ProjectDamageChanges(
            log,
            new Dictionary<string, FacilityPictureEntry>(StringComparer.Ordinal));

        Assert.That(changes, Is.Empty);
        Assert.That(
            FacilityPictureProjection.ProjectWithDamage(log, new Dictionary<string, FacilityPictureEntry>(StringComparer.Ordinal)),
            Is.Empty);
    }

    [Test]
    public void Multiple_facility_hits_apply_in_stable_engagement_then_sequence_order()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = new Dictionary<string, FacilityPictureEntry>(StringComparer.Ordinal)
        {
            ["runway-1"] = Facility("f1", "runway-1"),
            ["runway-2"] = Facility("f2", "runway-2"),
        };

        var log = new DecisionLog();
        log.AppendEngagementOutcome(Outcome(2, 2, "runway-2", EngagementOutcomeCodes.Hit, 20));
        log.AppendEngagementOutcome(Outcome(2, 1, "runway-1", EngagementOutcomeCodes.Hit, 10));

        var changes = OrderLogFacilityDamageProjection.ProjectDamageChanges(log, facilities);
        Assert.That(changes, Has.Count.EqualTo(2));
        Assert.That(changes[0].TargetId, Is.EqualTo("runway-1"));
        Assert.That(changes[1].TargetId, Is.EqualTo("runway-2"));

        var picture = FacilityPictureProjection.ProjectWithDamage(log, facilities);
        Assert.That(picture.Select(f => f.CapacityState), Is.EqualTo(new[]
        {
            FacilityCapacityStates.Damaged,
            FacilityCapacityStates.Damaged,
        }));
    }

    [Test]
    public void Kill_after_hit_promotes_facility_to_destroyed()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendEngagementOutcome(Outcome(2, 1, "runway-1", EngagementOutcomeCodes.Hit, 10));
        log.AppendEngagementOutcome(Kill(3, 2, "runway-1", 10));

        var changes = OrderLogFacilityDamageProjection.ProjectDamageChanges(log, facilities);
        Assert.That(changes, Has.Count.EqualTo(2));
        Assert.That(changes[0].NewState, Is.EqualTo(FacilityCapacityStates.Damaged));
        Assert.That(changes[1].NewState, Is.EqualTo(FacilityCapacityStates.Destroyed));

        var picture = FacilityPictureProjection.ProjectWithDamage(log, facilities);
        Assert.That(picture[0].CapacityState, Is.EqualTo(FacilityCapacityStates.Destroyed));
    }

    [Test]
    public void Non_facility_target_ignored()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendEngagementOutcome(Kill(2, 1, "ship-1", 10));

        Assert.That(OrderLogFacilityDamageProjection.ProjectDamageChanges(log, facilities), Is.Empty);
        Assert.That(
            FacilityPictureProjection.ProjectWithDamage(log, facilities)[0].CapacityState,
            Is.EqualTo(FacilityCapacityStates.Operational));
    }

    private static Dictionary<string, FacilityPictureEntry> FacilitySeed(string facilityId, string targetId) =>
        new(StringComparer.Ordinal) { [targetId] = Facility(facilityId, targetId) };

    private static FacilityPictureEntry Facility(string facilityId, string targetId) =>
        new(facilityId, targetId, FacilityCapacityStates.Operational, 0, 0);

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