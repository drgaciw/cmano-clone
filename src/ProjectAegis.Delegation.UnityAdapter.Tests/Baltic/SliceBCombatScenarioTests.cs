namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class SliceBCombatScenarioTests
{
    [Test]
    public void Run_is_deterministic_for_same_seed()
    {
        var first = SliceBCombatScenario.Run(7);
        var second = SliceBCombatScenario.Run(7);

        Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
        Assert.That(second.Log.ComputeFingerprint(), Is.EqualTo(first.Log.ComputeFingerprint()));
    }

    [TestCase("Missile")]
    [TestCase("Gun")]
    [TestCase("Laser")]
    public void Run_proves_permitted_and_refused_actual_resolver_legs(string family)
    {
        var result = SliceBCombatScenario.Run(7);
        var familyEvents = result.Events.Events.Where(e => e.WeaponFamilyId == family).ToArray();
        var correlations = familyEvents.GroupBy(e => e.CorrelationId).ToArray();

        Assert.That(correlations, Has.Length.EqualTo(2));
        Assert.That(correlations.Any(g => g.Any(e => e.Phase == CombatEventPhase.Firing)
            && g.Any(e => e.Phase == CombatEventPhase.TerminalOutcome)), Is.True);
        Assert.That(correlations.Any(g => g.Any(e => e.Phase == CombatEventPhase.AuthorizationRefused)
            && g.All(e => e.Phase != CombatEventPhase.Firing)), Is.True);
    }

    [Test]
    public void Run_symbols_cover_every_unique_shooter_and_target()
    {
        var result = SliceBCombatScenario.Run();

        Assert.That(result.Symbols, Has.Count.EqualTo(12));
        Assert.That(result.Symbols.Select(s => s.SymbolId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(12));
    }
}
