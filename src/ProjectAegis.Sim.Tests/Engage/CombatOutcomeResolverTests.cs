using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class CombatOutcomeResolverTests
{
    [Test]
    public void Pk_one_always_hits_pk_zero_always_misses()
    {
        var seed = SimSeed.FromScenario(99);
        var request = new EngageRequest(1, 2, 0, 5);
        var launch = EngageResult.Launch(1);

        var hit = CombatOutcomeResolver.Apply(seed, request, launch, 1.0);
        var miss = CombatOutcomeResolver.Apply(seed, request, launch, 0.0);

        Assert.That(hit.OutcomeCode, Is.EqualTo(EngagementOutcomeCodes.Hit));
        Assert.That(miss.OutcomeCode, Is.EqualTo(EngagementOutcomeCodes.Miss));
    }

    [Test]
    public void Same_seed_produces_identical_outcome()
    {
        var seed = SimSeed.FromScenario(42);
        var request = new EngageRequest(1, 2, 0, 3);
        var launch = EngageResult.Launch(7);

        var a = CombatOutcomeResolver.Apply(seed, request, launch, 0.5);
        var b = CombatOutcomeResolver.Apply(seed, request, launch, 0.5);

        Assert.That(b.OutcomeCode, Is.EqualTo(a.OutcomeCode));
        Assert.That(b.PkDraw, Is.EqualTo(a.PkDraw));
    }

    [Test]
    public void Pk_kill_one_promotes_hit_to_kill()
    {
        var seed = SimSeed.FromScenario(7);
        var request = new EngageRequest(1, 2, 0, 4);
        var hit = new EngageResult(true, 3, EngagementAbortReason.None, EngagementOutcomeCodes.Hit, 0.1);

        var kill = CombatOutcomeResolver.ApplyKillOnHit(seed, request, hit, 1.0);
        Assert.That(kill.OutcomeCode, Is.EqualTo(EngagementOutcomeCodes.Kill));
    }

    [Test]
    public void Pk_intercept_one_blocks_kill_promotion()
    {
        var seed = SimSeed.FromScenario(11);
        var request = new EngageRequest(1, 2, 0, 4);
        var hit = new EngageResult(true, 3, EngagementAbortReason.None, EngagementOutcomeCodes.Hit, 0.1);

        var intercepted = CombatOutcomeResolver.ApplyInterceptOnHit(seed, request, hit, 1.0);
        var afterKill = CombatOutcomeResolver.ApplyKillOnHit(seed, request, intercepted, 1.0);

        Assert.That(afterKill.OutcomeCode, Is.EqualTo(EngagementOutcomeCodes.Intercept));
    }
}