using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Glossary;

/// <summary>
/// Doctrine glossary consistency: every catalogued <see cref="FireAbortReason"/> that a policy
/// evaluator (req 13 / ADR-002 IPolicyEvaluator) can legally deny with must round-trip through
/// <c>MvpEngagementResolver.MapPolicyDenial</c> to its own correct, unambiguous
/// <see cref="EngagementAbortReason"/> / order-log code (req 12/14) -- never silently collapse
/// into an unrelated reason such as ROE_HOLD_FIRE.
/// </summary>
public sealed class DoctrineAbortReasonMappingTests
{
    [Test]
    public void WraRange_policy_denial_maps_to_out_of_envelope_not_hold_fire()
    {
        var world = new DictionaryEngageWorldQuery();
        var resolver = new MvpEngagementResolver(
            world,
            new MagazineLedger(),
            new AlwaysDenyPolicyEvaluator(FireAbortReason.WraRange));
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);

        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.OutOfEnvelope));
        Assert.That(EngagementAbortReasonCodes.ToLogCode(result.AbortReason), Is.EqualTo(AbortReasonCatalog.Engage.OUT_OF_ENVELOPE));
    }

    private sealed class AlwaysDenyPolicyEvaluator(FireAbortReason reason) : IPolicyEvaluator
    {
        public PolicyVerdict Evaluate(in PolicyContext ctx, in ActionRequest request) =>
            PolicyVerdict.Deny(reason);
    }
}
