using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class MineTransitHazardHotTickApplierTests
{
    private static InMemoryCatalogReader TransitFixture() =>
        new(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            "mine-transit-hazard-fixture",
            damage: [new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 25)]);

    private static ScenarioMineHazardSettings SampleHazard() =>
        new(
            zoneMinRangeMeters: 45_000,
            zoneMaxRangeMeters: 70_000,
            triggerRadiusMeters: 8_000,
            hazardSeverity: 1.0,
            mines:
            [
                new ScenarioMinePlacement("mine-a", 52_000, 1.0),
                new ScenarioMinePlacement("mine-b", 61_000, 1.0),
            ],
            transit:
            [
                new ScenarioMineTransitSchedule(
                    "u1",
                    [40_000, 52_000, 61_000, 75_000]),
            ]);

    [Test]
    public void Mine_transit_hazard_disabled_when_combat_domains_flag_off()
    {
        Assert.That(
            MineTransitHazardHotTickApplier.IsEnabled(
                    combatDomainsEnabled: false,
                    SampleHazard()),
            Is.False);
    }

    [Test]
    public void Mine_transit_hazard_policy_json_round_trips_zone_mines_and_transit()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "mine-transit-json",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
            MineHazard = new ScenarioMineHazardJsonDto
            {
                ZoneMinRangeMeters = 45_000,
                ZoneMaxRangeMeters = 70_000,
                TriggerRadiusMeters = 8_000,
                HazardSeverity = 1.0,
                Mines =
                [
                    new ScenarioMinePlacementJsonDto { MineId = "mine-a", RangeMeters = 52_000, Lethality = 0.85 },
                ],
                Transit =
                [
                    new ScenarioMineTransitJsonDto
                    {
                        PlatformId = "u1",
                        RangesMeters = [40_000, 52_000],
                    },
                ],
            },
        });

        Assert.That(profile.MineHazard, Is.Not.Null);
        Assert.That(profile.MineHazard!.ZoneMinRangeMeters, Is.EqualTo(45_000));
        AssertEx.Single(profile.MineHazard.Mines);
        Assert.That(profile.MineHazard.Mines[0].MineId, Is.EqualTo("mine-a"));
        AssertEx.Single(profile.MineHazard.Transit);
        Assert.That(profile.MineHazard.Transit[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(profile.MineHazard.Transit[0].RangesMeters.Count, Is.EqualTo(2));
    }

    [Test]
    public void Mine_transit_hazard_skirts_zone_boundary_without_hp_change()
    {
        var ledger = PlatformHpLedger.SeedFromWithdrawTargets(
            [new ScenarioCatalogWithdrawTarget("u1", 100)]);
        var catalog = TransitFixture();
        var hazard = SampleHazard();
        var seed = SimSeed.FromScenario(42);

        var outsideZone = MineTransitHazardHotTickApplier.ApplyTransitHazardTick(
            seed,
            simTick: 1,
            ledger,
            catalog,
            hazard);

        Assert.That(outsideZone, Is.Empty);
        Assert.That(ledger.TryGetHpPct("u1", out var hp) ? hp : -1, Is.EqualTo(100));
    }

    [Test]
    public void Mine_transit_hazard_applies_deterministic_hp_delta_inside_zone()
    {
        var ledger = PlatformHpLedger.SeedFromWithdrawTargets(
            [new ScenarioCatalogWithdrawTarget("u1", 100)]);
        var catalog = TransitFixture();
        var hazard = SampleHazard();
        var seed = SimSeed.FromScenario(42);

        var tickTwo = MineTransitHazardHotTickApplier.ApplyTransitHazardTick(
            seed,
            simTick: 2,
            ledger,
            catalog,
            hazard);

        Assert.That(tickTwo, Is.Not.Empty);
        AssertEx.All(tickTwo, c => Assert.That(c.ReasonCode, Is.EqualTo(PlatformDamageChangeReasonCodes.MineTransitHazard)));
        Assert.That(ledger.TryGetHpPct("u1", out var hp), Is.True);
        Assert.That(hp < 100, Is.True);
    }

    [Test]
    public void Mine_transit_hazard_same_seed_produces_identical_outcome_set()
    {
        var catalog = TransitFixture();
        var hazard = SampleHazard();
        var seed = SimSeed.FromScenario(42);

        var ledgerA = PlatformHpLedger.SeedFromWithdrawTargets(
            [new ScenarioCatalogWithdrawTarget("u1", 100)]);
        var ledgerB = PlatformHpLedger.SeedFromWithdrawTargets(
            [new ScenarioCatalogWithdrawTarget("u1", 100)]);

        var changesA = MineTransitHazardHotTickApplier.ApplyTransitHazardTick(seed, 2, ledgerA, catalog, hazard);
        var changesB = MineTransitHazardHotTickApplier.ApplyTransitHazardTick(seed, 2, ledgerB, catalog, hazard);

        Assert.That(changesB.Count, Is.EqualTo(changesA.Count));
        for (var i = 0; i < changesA.Count; i++)
        {
            Assert.That(changesB[i].PlatformId, Is.EqualTo(changesA[i].PlatformId));
            Assert.That(changesB[i].PreviousHpPct, Is.EqualTo(changesA[i].PreviousHpPct));
            Assert.That(changesB[i].NewHpPct, Is.EqualTo(changesA[i].NewHpPct));
            Assert.That(changesB[i].ReasonCode, Is.EqualTo(changesA[i].ReasonCode));
            Assert.That(changesB[i].DamageLevel, Is.EqualTo(changesA[i].DamageLevel));
        }
    }

    [Test]
    public void Mine_transit_hazard_empty_zone_produces_no_changes()
    {
        var ledger = PlatformHpLedger.SeedFromWithdrawTargets(
            [new ScenarioCatalogWithdrawTarget("u1", 100)]);
        var catalog = TransitFixture();
        var hazard = new ScenarioMineHazardSettings(
            zoneMinRangeMeters: 80_000,
            zoneMaxRangeMeters: 90_000,
            triggerRadiusMeters: 5_000,
            hazardSeverity: 1.0,
            mines: [new ScenarioMinePlacement("mine-a", 85_000, 1.0)],
            transit: [new ScenarioMineTransitSchedule("u1", [52_000, 61_000])]);
        var seed = SimSeed.FromScenario(42);

        var changes = MineTransitHazardHotTickApplier.ApplyTransitHazardTick(seed, 2, ledger, catalog, hazard);

        Assert.That(changes, Is.Empty);
        Assert.That(ledger.TryGetHpPct("u1", out var hp) ? hp : -1, Is.EqualTo(100));
    }

    [Test]
    public void Mine_transit_hazard_multiple_platforms_evaluated_in_stable_platform_order()
    {
        var ledger = PlatformHpLedger.SeedFromWithdrawTargets(
        [
            new ScenarioCatalogWithdrawTarget("u2", 100),
            new ScenarioCatalogWithdrawTarget("u1", 100),
        ]);
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            "mine-transit-multi-platform",
            damage:
            [
                new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 25),
                new CatalogPlatformDamage("u2", MaxHp: 100, WithdrawThresholdPct: 25),
            ]);
        var hazard = new ScenarioMineHazardSettings(
            zoneMinRangeMeters: 45_000,
            zoneMaxRangeMeters: 70_000,
            triggerRadiusMeters: 8_000,
            hazardSeverity: 1.0,
            mines: [new ScenarioMinePlacement("mine-a", 52_000, 1.0)],
            transit:
            [
                new ScenarioMineTransitSchedule("u2", [52_000]),
                new ScenarioMineTransitSchedule("u1", [52_000]),
            ]);
        var seed = SimSeed.FromScenario(17);

        var changes = MineTransitHazardHotTickApplier.ApplyTransitHazardTick(seed, 1, ledger, catalog, hazard);

        Assert.That(changes.Count, Is.EqualTo(2));
        Assert.That(changes[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(changes[1].PlatformId, Is.EqualTo("u2"));
    }
}