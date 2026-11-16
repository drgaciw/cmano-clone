using NUnit.Framework;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

[TestFixture]
public sealed class AdviceExplanationBinderTests
{
    [Test]
    public void Stale_transport_invocation_is_discarded_without_mutating_advice_authority()
    {
        var advice = AvailableAdvice();
        var transport = TransportFor(
            invocationKey: "c2.advice.datalink.assess:1:1:hostile-OLD",
            explanation: "stale prose");

        var bound = AdviceExplanationBinder.Bind(
            advice,
            expectedInvocationKey: "c2.advice.datalink.assess:1:2:hostile-1",
            transport);

        Assert.That(bound.Availability, Is.EqualTo(AdviceAvailability.Stale));
        Assert.That(bound.DiscardReason, Is.EqualTo("stale-invocation-key"));
        Assert.That(bound.Explanation, Is.Empty);
        Assert.That(bound.AuthorityPreserved, Is.True);
        Assert.That(advice.IsFireOrder, Is.False);
        Assert.That(advice.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(advice.HardConstraints, Does.Contain("advisory-only:no-authority-or-order"));
    }

    [Test]
    public void Unavailable_advice_preserves_availability_and_ignores_late_success_transport()
    {
        var advice = AdviceFrame.Unavailable(3, 3.0, AdviceAvailability.Stale, "refresh evidence", "hostile-1");
        var transport = TransportFor(
            invocationKey: "c2.advice.datalink.assess:3:3:hostile-1",
            explanation: "late success that must not override stale advice");

        var bound = AdviceExplanationBinder.Bind(
            advice,
            expectedInvocationKey: "c2.advice.datalink.assess:3:3:hostile-1",
            transport);

        Assert.That(bound.Availability, Is.EqualTo(AdviceAvailability.Stale));
        Assert.That(bound.DiscardReason, Is.EqualTo("advice-stale"));
        Assert.That(bound.Explanation, Is.Empty);
        Assert.That(advice.Availability, Is.EqualTo(AdviceAvailability.Stale));
    }

    [Test]
    public void Matching_transport_binds_explanation_and_keeps_authority_false()
    {
        var advice = AvailableAdvice();
        const string key = "c2.advice.resource.recommend:4:4.5:hostile-1";
        var transport = TransportFor(key, "Shooter ranking is advisory-only.");

        var bound = AdviceExplanationBinder.Bind(advice, key, transport);

        Assert.That(bound.Availability, Is.EqualTo(AdviceAvailability.Available));
        Assert.That(bound.Explanation, Is.EqualTo("Shooter ranking is advisory-only."));
        Assert.That(bound.AuthorityPreserved, Is.True);
        Assert.That(advice.IsFireOrder, Is.False);
        Assert.That(advice.IsWeaponsReleaseAuthorization, Is.False);
    }

    private static AdviceFrame AvailableAdvice() =>
        new(
            ContactId: "hostile-1",
            SimTick: 2,
            SimTime: 2.0,
            Availability: AdviceAvailability.Available,
            Confidence: 0.5,
            ContactConfidenceLabel: "PROBABLE",
            Evidence: [],
            Assumptions: ["a"],
            Rationale: "r",
            HardConstraints: ["advisory-only:no-authority-or-order"],
            PolicyConstraints: [],
            Alternatives: [],
            ResourceCommitments: [],
            MissionPackageFacts: [],
            Authority: null,
            Fallback: "manual",
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false);

    private static AiArtifactTransport TransportFor(string invocationKey, string explanation)
    {
        var content = new AdviceExplanationContent(
            InvocationKey: invocationKey,
            EvidenceFingerprint: "sha256:ev",
            ViewingSide: ViewingSide.Blue,
            Explanation: explanation,
            CitedFactKeys: ["rationale"],
            PromptVersion: "advice-explain/v1");
        var json = System.Text.Json.JsonSerializer.Serialize(content);
        var envelope = new ArtifactEnvelope(
            SchemaVersion: AiContractSchema.Version,
            BuildIdentity: "test",
            ScenarioId: "advice",
            CatalogRevision: "n/a",
            OrderLogFingerprint: "n/a",
            ProviderId: "fake",
            ModelId: "fake-advice",
            PromptVersion: "advice-explain/v1",
            EvidenceIds: ["sha256:ev"],
            ViewingSide: ViewingSide.Blue,
            DebriefMode: false,
            Status: GenerationStatus.Succeeded,
            ContentHash: "sha256:content");
        return new AiArtifactTransport(envelope, "application/json", json, Warnings: null);
    }
}
