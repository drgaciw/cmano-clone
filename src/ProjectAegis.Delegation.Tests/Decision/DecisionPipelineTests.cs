namespace ProjectAegis.Delegation.Tests.Decision;

using System.Linq;
using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
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

    [Test]
    public void Choose_never_selects_a_zero_scored_candidate_when_a_positive_scored_alternative_exists()
    {
        // Mirrors PatrolCandidateEngagePolicy's post-kill pre-filter (S57-03 AAR remediation:
        // "no re-engagement proposals once the primary target is confirmed destroyed" -- see
        // the comment on PatrolCandidateEngagePolicy.GenerateCandidates). That policy expresses
        // "don't propose this" by scoring Engage at 0.0 instead of removing it from the
        // candidate list. But DecisionPipeline.Choose turns scores into softmax-style weights
        // via Math.Exp(score / temperature), and Math.Exp(0) == 1, not 0 -- so the
        // "de-prioritized" zero-scored candidate still carries real, non-negligible selection
        // weight (comparable to Hold/Move) and can still be drawn, silently defeating the
        // pre-filter it was meant to enforce.
        var traits = PersonalityCatalog.All.Single(p => p.Name == "Aggressive").Traits;
        var attention = new AttentionEvaluation(100, 5, new AttentionDegradation(false, false, false));

        var destroyedCandidates = new PatrolCandidateEngagePolicy().GenerateCandidates(
            new PerceivedState(0, 5, 1, PrimaryHostileDestroyed: true),
            traits);

        for (var salt = 0; salt < 500; salt++)
        {
            var rng = new SeededRng(globalSeed: 12345, agentSalt: salt);
            var choice = DecisionPipeline.Choose(destroyedCandidates, traits, attention, rng);
            Assert.That(
                choice.Chosen.Kind,
                Is.Not.EqualTo(OrderKind.Engage),
                $"Engage should never be chosen once the primary target is destroyed " +
                $"(salt={salt}), but its zero score still receives softmax weight exp(0)=1.");
        }
    }
}
