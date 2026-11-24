using Microsoft.Extensions.AI;
using NUnit.Framework;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Advice;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

[TestFixture]
public sealed class EvidenceProjectorProvenanceAndDegradedTests
{
    [Test]
    public void EvidenceProjector_emits_autonomyLevel_policySnapshotId_and_personality_without_altering_orderLogFingerprint()
    {
        var logWithProvenance = new DecisionLog();
        logWithProvenance.Append(OrderLogEntry.FromDecisionRecord(
            new DecisionRecord(
                3.0,
                new AgentId("agent-alpha"),
                new TargetId("u1"),
                AutonomyLevel.SemiAutonomous,
                OrderKind.Engage,
                [new ScoredIntent(OrderKind.Engage, 0.9, RiskLevel.High)],
                "lock",
                2.0,
                10.0,
                0.3,
                SimTick: 3,
                PolicySnapshotId: 105UL,
                Personality: "Bold"),
            simTick: 3));

        var logWithoutProvenance = new DecisionLog();
        logWithoutProvenance.Append(OrderLogEntry.FromDecisionRecord(
            new DecisionRecord(
                3.0,
                new AgentId("agent-alpha"),
                new TargetId("u1"),
                AutonomyLevel.SemiAutonomous,
                OrderKind.Engage,
                [new ScoredIntent(OrderKind.Engage, 0.9, RiskLevel.High)],
                "lock",
                2.0,
                10.0,
                0.3,
                SimTick: 3),
            simTick: 3));

        var projector = new EvidenceProjector();
        var req = new EvidenceProjectionRequest("b1", "scen", "rev", ViewingSide.Blue, DebriefMode: false, FingerprintPrefix: "fp");

        var withRes = projector.Project(logWithProvenance, req);
        var withoutRes = projector.Project(logWithoutProvenance, req);

        Assert.That(withRes.Document.Header.OrderLogFingerprint, Is.EqualTo(withoutRes.Document.Header.OrderLogFingerprint));
        var attrs = withRes.Document.Entries[0].Attributes!;
        Assert.That(attrs["autonomyLevel"], Is.EqualTo("SemiAutonomous"));
        Assert.That(attrs["policySnapshotId"], Is.EqualTo("105"));
        Assert.That(attrs["personality"], Is.EqualTo("Bold"));
    }

    [Test]
    public async Task AdviceExplanationPipeline_degraded_failures_never_invent_hidden_fallback_and_preserve_facts_fingerprint()
    {
        var facts = new AdviceExplanationFacts(
            ContactId: "hostile-1",
            Availability: "Available",
            Confidence: 0.72,
            ContactConfidenceLabel: "PROBABLE",
            Rationale: "Chain complete.",
            Assumptions: ["a1"],
            HardConstraints: ["advisory-only:no-authority-or-order"],
            PolicyConstraints: ["authority:Withheld:RoeHoldFire"],
            Alternatives: ["hold"],
            MissionPackageFacts: ["pkg"],
            ResourceCommitments: ["s2/w2: committed elsewhere"],
            Fallback: "manual review",
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false,
            SideTaggedFacts:
            [
                new AdviceSideTaggedFact("red-secret", "RED-HIDDEN", ViewingSide.Red, [ViewingSide.Blue]),
            ]);

        var fpBefore = AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts);

        // 1. ProviderUnavailable (exception)
        using var unavailableClient = new AdviceScriptedChatClient(); // empty queue throws InvalidOperationException
        var unavailableRes = await new AdviceExplanationPipeline(unavailableClient).RunAsync(
            new AdviceExplanationRequest("k1", fpBefore, ViewingSide.Blue, facts, "b1"));
        Assert.That(unavailableRes.Status, Is.EqualTo(GenerationStatus.ProviderUnavailable));
        Assert.That(unavailableRes.SkipReason, Is.EqualTo("provider-unavailable"));
        Assert.That(unavailableRes.Content, Is.Null);

        // 2. TimedOut
        using var timeoutClient = new ThrowingChatClient(new TimeoutException("slow"));
        var timeoutRes = await new AdviceExplanationPipeline(timeoutClient).RunAsync(
            new AdviceExplanationRequest("k2", fpBefore, ViewingSide.Blue, facts, "b1"));
        Assert.That(timeoutRes.Status, Is.EqualTo(GenerationStatus.TimedOut));
        Assert.That(timeoutRes.SkipReason, Is.EqualTo("timeout"));
        Assert.That(timeoutRes.Content, Is.Null);

        // 3. Cancelled
        using var cancelClient = new ThrowingChatClient(new OperationCanceledException("aborted"));
        var cancelRes = await new AdviceExplanationPipeline(cancelClient).RunAsync(
            new AdviceExplanationRequest("k3", fpBefore, ViewingSide.Blue, facts, "b1"));
        Assert.That(cancelRes.Status, Is.EqualTo(GenerationStatus.Cancelled));
        Assert.That(cancelRes.SkipReason, Is.EqualTo("cancelled"));
        Assert.That(cancelRes.Content, Is.Null);

        // 4. Malformed empty output
        using var emptyClient = new AdviceScriptedChatClient();
        emptyClient.EnqueueAssistantText("   ");
        var emptyRes = await new AdviceExplanationPipeline(emptyClient).RunAsync(
            new AdviceExplanationRequest("k4", fpBefore, ViewingSide.Blue, facts, "b1"));
        Assert.That(emptyRes.Status, Is.EqualTo(GenerationStatus.MalformedOutput));
        Assert.That(emptyRes.SkipReason, Is.EqualTo("empty-explanation"));
        Assert.That(emptyRes.Content, Is.Null);

        Assert.That(AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts), Is.EqualTo(fpBefore));
    }

    private sealed class ThrowingChatClient(Exception exception) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromException<ChatResponse>(exception);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw exception;

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
