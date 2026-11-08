namespace ProjectAegis.Delegation.Tests.Attention;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Sim;
using NUnit.Framework;

[TestFixture]
public sealed class AttentionTests
{
    [Test]
    public void Overload_enables_all_degradation_flags_in_order()
    {
        var state = new ObservedState(
            SimTime: 10,
            ContactCount: 50,
            ActiveEngagementCount: 20,
            MemberAlive: new Dictionary<TargetId, bool>(),
            PrimaryHostileDestroyed: false);

        var result = AttentionCalculator.Evaluate(
            budget: 10,
            memberCount: 8,
            state);

        Assert.That(result.Load, Is.GreaterThan(result.Budget));
        Assert.That(result.Degradation.SlowerReactions, Is.True);
        Assert.That(result.Degradation.NarrowedFocus, Is.True);
        Assert.That(result.Degradation.SimplerDecisions, Is.True);
    }
}
