using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class EngageAttackOrderResolverTests
{
    [Test]
    public void TryResolve_fire_single_returns_engage_with_salvo_one()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(4);
        var preview = EngagePreviewProjection.Project(in ctx, defaults.DlzPersonality);

        Assert.That(
            EngageAttackOrderResolver.TryResolve("fire-single", in ctx, preview, out var resolved, out _),
            Is.True);
        Assert.That(resolved.Kind, Is.EqualTo(ProjectAegis.Delegation.Core.OrderKind.Engage));
        Assert.That(resolved.SalvoSize, Is.EqualTo(1));
    }

    [Test]
    public void TryResolve_hold_fire_when_blocked_returns_false()
    {
        var ctx = ScenarioEngageDefaults.MvpFallback.ToEngageContext(0) with { HasFireControlTrack = false };
        var preview = EngagePreviewProjection.Project(in ctx, DlzPersonality.Normal);

        Assert.That(
            EngageAttackOrderResolver.TryResolve("fire-single", in ctx, preview, out _, out var reason),
            Is.False);
        Assert.That(reason, Is.Not.Null);
    }
}