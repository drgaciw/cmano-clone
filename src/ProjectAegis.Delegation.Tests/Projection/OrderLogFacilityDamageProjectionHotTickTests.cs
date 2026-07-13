using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class OrderLogFacilityDamageProjectionHotTickTests
{
    [Test]
    public void Facility_Domain_Damage_hp_ledger_hit_projects_damaged_capacity()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            10,
            1,
            new TargetId("runway-1"),
            100,
            75,
            PlatformDamageChangeReasonCodes.Hit));

        var picture = FacilityPictureProjection.ProjectWithDamage(log, facilities);

        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].CapacityState, Is.EqualTo(FacilityCapacityStates.Damaged));
    }

    [Test]
    public void Facility_Domain_Damage_hp_ledger_kill_projects_destroyed_capacity()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            10,
            1,
            new TargetId("runway-1"),
            75,
            0,
            PlatformDamageChangeReasonCodes.Kill));

        var picture = FacilityPictureProjection.ProjectWithDamage(log, facilities);

        Assert.That(picture[0].CapacityState, Is.EqualTo(FacilityCapacityStates.Destroyed));
    }

    [Test]
    public void Facility_Domain_Damage_hp_ledger_multiple_hits_skip_duplicate_damaged_transitions()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            10,
            1,
            new TargetId("runway-1"),
            100,
            75,
            PlatformDamageChangeReasonCodes.Hit));
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            20,
            2,
            new TargetId("runway-1"),
            75,
            50,
            PlatformDamageChangeReasonCodes.Hit));

        var changes = OrderLogFacilityDamageProjection.ProjectDamageChanges(log, facilities);

        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].NewState, Is.EqualTo(FacilityCapacityStates.Damaged));
        Assert.That(
            FacilityPictureProjection.ProjectWithDamage(log, facilities)[0].CapacityState,
            Is.EqualTo(FacilityCapacityStates.Damaged));
    }

    [Test]
    public void Facility_Domain_Damage_hp_ledger_preferred_over_outcome_stub_when_present()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendEngagementOutcome(Outcome(2, 1, "runway-1", EngagementOutcomeCodes.Hit, 10));
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            10,
            1,
            new TargetId("runway-1"),
            100,
            75,
            PlatformDamageChangeReasonCodes.Hit));

        var changes = OrderLogFacilityDamageProjection.ProjectDamageChanges(log, facilities);

        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].PreviousState, Is.EqualTo(FacilityCapacityStates.Operational));
        Assert.That(changes[0].NewState, Is.EqualTo(FacilityCapacityStates.Damaged));
    }

    [Test]
    public void Facility_Domain_Damage_non_facility_platform_damage_ignored()
    {
        const bool combatDomainsEnabled = true;
        Assert.That(combatDomainsEnabled, Is.True);

        var facilities = FacilitySeed("f1", "runway-1");
        var log = new DecisionLog();
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            10,
            1,
            new TargetId("u1"),
            100,
            75,
            PlatformDamageChangeReasonCodes.Hit));

        Assert.That(OrderLogFacilityDamageProjection.ProjectDamageChanges(log, facilities), Is.Empty);
        Assert.That(
            FacilityPictureProjection.ProjectWithDamage(log, facilities)[0].CapacityState,
            Is.EqualTo(FacilityCapacityStates.Operational));
    }

    private static Dictionary<string, FacilityPictureEntry> FacilitySeed(string facilityId, string targetId) =>
        new(StringComparer.Ordinal) { [targetId] = Facility(facilityId, targetId) };

    private static FacilityPictureEntry Facility(string facilityId, string targetId) =>
        new(facilityId, targetId, FacilityCapacityStates.Operational, 0, 0);

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