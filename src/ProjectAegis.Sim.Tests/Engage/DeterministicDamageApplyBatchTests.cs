using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class DeterministicDamageApplyBatchTests
{
    [Test]
    public void Empty_batch_returns_empty_sorted_list()
    {
        var sorted = DeterministicDamageApplyBatch.Sort(Array.Empty<EngagementDamageOutcome>());
        Assert.That(sorted, Is.Empty);
    }

    [Test]
    public void Single_outcome_returns_unchanged()
    {
        var input = new[] { new EngagementDamageOutcome(7, 3, EngagementOutcomeCodes.Kill) };
        var sorted = DeterministicDamageApplyBatch.Sort(input);
        AssertEx.Single(sorted);
        Assert.That(sorted[0], Is.EqualTo(input[0]));
    }

    [Test]
    public void Outcomes_sorted_by_engagement_id_ascending()
    {
        var input = new[]
        {
            new EngagementDamageOutcome(30, 1, EngagementOutcomeCodes.Miss),
            new EngagementDamageOutcome(10, 2, EngagementOutcomeCodes.Kill),
            new EngagementDamageOutcome(20, 3, EngagementOutcomeCodes.Hit),
        };

        var sorted = DeterministicDamageApplyBatch.Sort(input);
        Assert.That(sorted.Select(o => o.EngagementId).ToArray(), Is.EqualTo(new ulong[] { 10, 20, 30 }));
    }

    [Test]
    public void Same_engagement_id_tie_breaks_on_sequence_id()
    {
        var input = new[]
        {
            new EngagementDamageOutcome(5, 9, EngagementOutcomeCodes.Miss),
            new EngagementDamageOutcome(5, 1, EngagementOutcomeCodes.Kill),
            new EngagementDamageOutcome(5, 4, EngagementOutcomeCodes.Hit),
        };

        var sorted = DeterministicDamageApplyBatch.Sort(input);
        Assert.That(sorted.Select(o => o.SequenceId).ToArray(), Is.EqualTo(new ulong[] { 1, 4, 9 }));
    }

    [Test]
    public void HotTick_sorted_outcomes_drive_catalog_damage_apply_order()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            damage: [new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 0)]);
        var ledger = PlatformHpLedger.SeedFromWithdrawTargets(
            [new ScenarioCatalogWithdrawTarget("u1", 100)]);
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 5, 9, EngagementOutcomeCodes.Miss),
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 5, 1, EngagementOutcomeCodes.Hit),
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 5, 4, EngagementOutcomeCodes.Hit),
        };

        var changes = CatalogDamageHotTickApplier.ApplySortedOutcomes(ledger, catalog, outcomes);

        Assert.That(changes.Count, Is.EqualTo(2));
        Assert.That(changes[0].ReasonCode, Is.EqualTo(EngagementOutcomeCodes.Hit));
        Assert.That(changes[1].ReasonCode, Is.EqualTo(EngagementOutcomeCodes.Hit));
        Assert.That(
            Math.Round(ledger.TryGetHpPct("u1", out var hp) ? hp : -1, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(50.0, 6, MidpointRounding.AwayFromZero)));
    }
}