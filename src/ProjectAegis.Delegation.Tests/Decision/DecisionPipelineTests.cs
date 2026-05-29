namespace ProjectAegis.Delegation.Tests.Decision;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class DecisionPipelineTests
{
    [Test]
    public void Pipeline_is_deterministic_for_same_seed_and_inputs()
    {
        var traits = PersonalityCatalog.All[0].Traits;
        var attention = new AttentionEvaluation(100, 5, new AttentionDegradation(false, false, false));
        var rng1 = new SeededRng(42, agentSalt: 7);
        var rng2 = new SeededRng(42, agentSalt: 7);

        var a = DecisionPipeline.Choose(
            StubPatrolPolicy.DefaultCandidates,
            traits,
            attention,
            rng1);
        var b = DecisionPipeline.Choose(
            StubPatrolPolicy.DefaultCandidates,
            traits,
            attention,
            rng2);

        Assert.That(a.Chosen.Kind, Is.EqualTo(b.Chosen.Kind));
        Assert.That(a.RngDraw, Is.EqualTo(b.RngDraw));
    }
}
