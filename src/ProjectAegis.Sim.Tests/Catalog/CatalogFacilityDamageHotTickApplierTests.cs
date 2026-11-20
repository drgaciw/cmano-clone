using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class CatalogFacilityDamageHotTickApplierTests
{
    private static InMemoryCatalogReader FacilityDamageFixture(double resilience = 1.0) =>
        new(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            "facility-hot-tick-damage-fixture",
            damage: [new CatalogPlatformDamage("runway-1", MaxHp: 100, WithdrawThresholdPct: 0, Resilience: resilience)]);

    private static PlatformHpLedger FacilityLedger(double hpPct = 100) =>
        PlatformHpLedger.SeedFromWithdrawTargets(
            [new ScenarioCatalogWithdrawTarget("runway-1", hpPct)]);

    [Test]
    public void Facility_Domain_HotTick_hit_applies_sorted_hp_delta_to_facility_ledger()
    {
        var ledger = FacilityLedger(hpPct: 100);
        var catalog = FacilityDamageFixture();
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply("runway-1", 30, 1, EngagementOutcomeCodes.Miss),
            new CatalogDamageHotTickApplier.OutcomeApply("runway-1", 10, 2, EngagementOutcomeCodes.Hit),
            new CatalogDamageHotTickApplier.OutcomeApply("runway-1", 20, 3, EngagementOutcomeCodes.Hit),
        };
        var facilityTargets = new HashSet<string>(StringComparer.Ordinal) { "runway-1" };

        var changes = CatalogDamageHotTickApplier.ApplySortedFacilityOutcomes(
            ledger,
            catalog,
            outcomes,
            facilityTargets);

        Assert.That(changes.Count, Is.EqualTo(2));
        Assert.That(changes[0].ReasonCode, Is.EqualTo(EngagementOutcomeCodes.Hit));
        Assert.That(changes[1].ReasonCode, Is.EqualTo(EngagementOutcomeCodes.Hit));
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("runway-1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(50.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Facility_Domain_Damage_hot_tick_hit_damage_level_bounded_0_to_3()
    {
        var ledger = FacilityLedger(hpPct: 100);
        var catalog = FacilityDamageFixture(resilience: 2.0);
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply(
                "runway-1",
                10,
                1,
                EngagementOutcomeCodes.Hit,
                HitSeverity: 1.0),
        };
        var facilityTargets = new HashSet<string>(StringComparer.Ordinal) { "runway-1" };

        var changes = CatalogDamageHotTickApplier.ApplySortedFacilityOutcomes(
            ledger,
            catalog,
            outcomes,
            facilityTargets);

        AssertEx.Single(changes);
        Assert.That(changes[0].DamageLevel, Is.EqualTo(2));
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("runway-1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(50.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Facility_Domain_Damage_zero_resilience_hit_produces_no_hp_change()
    {
        var ledger = FacilityLedger(hpPct: 80);
        var catalog = FacilityDamageFixture(resilience: 0.0);
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply("runway-1", 10, 1, EngagementOutcomeCodes.Hit),
        };
        var facilityTargets = new HashSet<string>(StringComparer.Ordinal) { "runway-1" };

        var changes = CatalogDamageHotTickApplier.ApplySortedFacilityOutcomes(
            ledger,
            catalog,
            outcomes,
            facilityTargets);

        Assert.That(changes, Is.Empty);
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("runway-1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(80.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Facility_Domain_Damage_kill_outcome_zeroes_facility_hp_after_prior_hit()
    {
        var ledger = FacilityLedger(hpPct: 80);
        var catalog = FacilityDamageFixture();
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply("runway-1", 10, 1, EngagementOutcomeCodes.Hit),
            new CatalogDamageHotTickApplier.OutcomeApply("runway-1", 20, 2, EngagementOutcomeCodes.Kill),
        };
        var facilityTargets = new HashSet<string>(StringComparer.Ordinal) { "runway-1" };

        var changes = CatalogDamageHotTickApplier.ApplySortedFacilityOutcomes(
            ledger,
            catalog,
            outcomes,
            facilityTargets);

        Assert.That(changes.Count, Is.EqualTo(2));
        Assert.That(changes[1].ReasonCode, Is.EqualTo(EngagementOutcomeCodes.Kill));
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("runway-1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(0.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Facility_Domain_Damage_missing_damage_row_skips_facility_hit_apply()
    {
        var ledger = FacilityLedger(hpPct: 80);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply("runway-1", 10, 1, EngagementOutcomeCodes.Hit),
        };
        var facilityTargets = new HashSet<string>(StringComparer.Ordinal) { "runway-1" };

        var changes = CatalogDamageHotTickApplier.ApplySortedFacilityOutcomes(
            ledger,
            catalog,
            outcomes,
            facilityTargets);

        Assert.That(changes, Is.Empty);
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("runway-1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(80.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Facility_Domain_Damage_hp_capacity_maps_operational_damaged_destroyed()
    {
        Assert.That(FacilityHpCapacity.MapHpPctToCapacityState(100), Is.EqualTo(FacilityHpCapacity.Operational));
        Assert.That(FacilityHpCapacity.MapHpPctToCapacityState(75), Is.EqualTo(FacilityHpCapacity.Damaged));
        Assert.That(FacilityHpCapacity.MapHpPctToCapacityState(0), Is.EqualTo(FacilityHpCapacity.Destroyed));
    }
}