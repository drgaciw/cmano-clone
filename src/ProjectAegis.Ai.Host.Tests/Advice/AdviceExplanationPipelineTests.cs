using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Advice;
using Xunit;

namespace ProjectAegis.Ai.Host.Tests.Advice;

public sealed class AdviceExplanationPipelineTests
{
    [Fact]
    public async Task Late_response_is_discarded_when_invocation_key_changes()
    {
        var facts = SampleFacts();
        var fingerprint = AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts);
        var currentKey = "c2.advice.datalink.assess:1:1:hostile-1";
        var scripted = new AdviceScriptedChatClient();
        scripted.EnqueueAssistantText("Contact hostile-1 remains advisory-only under current evidence.");

        var pipeline = new AdviceExplanationPipeline(scripted);
        var request = new AdviceExplanationRequest(
            InvocationKey: currentKey,
            EvidenceFingerprint: fingerprint,
            ViewingSide: ViewingSide.Blue,
            Facts: facts,
            BuildIdentity: "test-build",
            GetCurrentKeys: () =>
            {
                // Selection changed while the model was in flight.
                return new AdviceStaleCheck(
                    InvocationKey: "c2.advice.datalink.assess:1:2:hostile-2",
                    EvidenceFingerprint: fingerprint);
            });

        var result = await pipeline.RunAsync(request);

        Assert.True(result.DiscardedAsStale);
        Assert.Equal(GenerationStatus.Cancelled, result.Status);
        Assert.Equal("stale-discarded", result.SkipReason);
        Assert.Null(result.Content);
        Assert.Null(result.Envelope);
        scripted.Dispose();
    }

    [Fact]
    public async Task Late_response_is_discarded_when_evidence_fingerprint_changes()
    {
        var facts = SampleFacts();
        var fingerprint = AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts);
        var scripted = new AdviceScriptedChatClient();
        scripted.EnqueueAssistantText("ok");

        var pipeline = new AdviceExplanationPipeline(scripted);
        var result = await pipeline.RunAsync(new AdviceExplanationRequest(
            InvocationKey: "c2.advice.resource.recommend:2:3:hostile-1",
            EvidenceFingerprint: fingerprint,
            ViewingSide: ViewingSide.Blue,
            Facts: facts,
            BuildIdentity: "test-build",
            GetCurrentKeys: () => new AdviceStaleCheck(
                InvocationKey: "c2.advice.resource.recommend:2:3:hostile-1",
                EvidenceFingerprint: "sha256:changed")));

        Assert.True(result.DiscardedAsStale);
        Assert.Equal("stale-discarded", result.SkipReason);
        scripted.Dispose();
    }

    [Fact]
    public async Task Hidden_side_facts_never_enter_prompt_or_published_explanation()
    {
        var facts = SampleFacts(sideTagged:
        [
            new AdviceSideTaggedFact(
                Key: "red-intent",
                Text: "RED-SECRET-INTENT-TOKEN",
                OwningSide: ViewingSide.Red,
                HiddenFrom: [ViewingSide.Blue]),
            new AdviceSideTaggedFact(
                Key: "blue-track",
                Text: "blue-visible-track",
                OwningSide: ViewingSide.Blue,
                HiddenFrom: Array.Empty<ViewingSide>()),
        ]);

        var captured = new List<string>();
        var scripted = new CapturingAdviceChatClient(captured, "Blue track is advisory-only.");
        var root = CreateTempRoot();
        try
        {
            var store = new AtomicArtifactStore(root, new AiHostOptions());
            var pipeline = new AdviceExplanationPipeline(scripted, artifactStore: store);
            var fingerprint = AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts);
            var result = await pipeline.RunAsync(new AdviceExplanationRequest(
                InvocationKey: "c2.advice.datalink.assess:1:1:hostile-1",
                EvidenceFingerprint: fingerprint,
                ViewingSide: ViewingSide.Blue,
                Facts: facts,
                BuildIdentity: "test-build"));

            Assert.Equal(GenerationStatus.Succeeded, result.Status);
            Assert.NotNull(result.Content);
            Assert.Contains("blue-visible-track", string.Join('\n', captured), StringComparison.Ordinal);
            Assert.DoesNotContain("RED-SECRET-INTENT-TOKEN", string.Join('\n', captured), StringComparison.Ordinal);
            Assert.DoesNotContain("RED-SECRET-INTENT-TOKEN", result.Content!.Explanation, StringComparison.Ordinal);
            Assert.Contains("blue-track", result.Content.CitedFactKeys);
            Assert.DoesNotContain("red-intent", result.Content.CitedFactKeys);
        }
        finally
        {
            TryDeleteTree(root);
            scripted.Dispose();
        }
    }

    [Fact]
    public async Task Hidden_side_leak_in_model_prose_is_rejected()
    {
        var facts = SampleFacts(sideTagged:
        [
            new AdviceSideTaggedFact(
                Key: "red-intent",
                Text: "RED-SECRET-INTENT-TOKEN",
                OwningSide: ViewingSide.Red,
                HiddenFrom: [ViewingSide.Blue]),
        ]);
        var scripted = new AdviceScriptedChatClient();
        scripted.EnqueueAssistantText("Leaked RED-SECRET-INTENT-TOKEN into player advice.");
        var pipeline = new AdviceExplanationPipeline(scripted);
        var result = await pipeline.RunAsync(new AdviceExplanationRequest(
            InvocationKey: "c2.advice.datalink.assess:1:1:hostile-1",
            EvidenceFingerprint: AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts),
            ViewingSide: ViewingSide.Blue,
            Facts: facts,
            BuildIdentity: "test-build"));

        Assert.Equal(GenerationStatus.RejectedCitation, result.Status);
        Assert.Equal("hidden-side-leak", result.SkipReason);
        Assert.Null(result.Content);
        scripted.Dispose();
    }

    [Fact]
    public async Task Authoritative_fingerprint_matches_with_explanations_on_and_off()
    {
        var facts = SampleFacts();
        var offFingerprint = AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts);

        var scripted = new AdviceScriptedChatClient();
        scripted.EnqueueAssistantText("Advisory-only explanation of current facts.");
        var root = CreateTempRoot();
        try
        {
            var store = new AtomicArtifactStore(root, new AiHostOptions());
            var pipeline = new AdviceExplanationPipeline(scripted, artifactStore: store);
            var onResult = await pipeline.RunAsync(new AdviceExplanationRequest(
                InvocationKey: "c2.advice.mission-package.explain:5:9.5:hostile-1",
                EvidenceFingerprint: offFingerprint,
                ViewingSide: ViewingSide.Blue,
                Facts: facts,
                BuildIdentity: "test-build"));

            Assert.Equal(GenerationStatus.Succeeded, onResult.Status);
            Assert.NotNull(onResult.Envelope);
            Assert.True(store.Exists("advice-explain.envelope.json"));

            var onFingerprint = AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts);
            Assert.Equal(offFingerprint, onFingerprint);
            Assert.Equal(offFingerprint, onResult.Content!.EvidenceFingerprint);
            // Order-log / authority fingerprint on the envelope is the scenario order log, not advice prose.
            Assert.Equal("n/a", onResult.Envelope!.OrderLogFingerprint);
        }
        finally
        {
            TryDeleteTree(root);
            scripted.Dispose();
        }
    }

    [Fact]
    public async Task Disabled_path_preserves_authoritative_fingerprint_without_publish()
    {
        var facts = SampleFacts();
        var fingerprint = AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts);
        var scripted = new AdviceScriptedChatClient();
        var pipeline = new AdviceExplanationPipeline(scripted);
        var result = await pipeline.RunAsync(new AdviceExplanationRequest(
            InvocationKey: "c2.advice.datalink.assess:1:1:hostile-1",
            EvidenceFingerprint: fingerprint,
            ViewingSide: ViewingSide.Blue,
            Facts: facts,
            BuildIdentity: "test-build",
            Enabled: false));

        Assert.Equal(GenerationStatus.ProviderUnavailable, result.Status);
        Assert.Equal("disabled", result.SkipReason);
        Assert.Equal(fingerprint, AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts));
        scripted.Dispose();
    }

    private static AdviceExplanationFacts SampleFacts(
        IReadOnlyList<AdviceSideTaggedFact>? sideTagged = null) =>
        new(
            ContactId: "hostile-1",
            Availability: "Available",
            Confidence: 0.72,
            ContactConfidenceLabel: "PROBABLE",
            Rationale: "Current technical chain is complete; authority remains separate.",
            Assumptions: ["No weapon-specific assessment supplied."],
            HardConstraints: ["advisory-only:no-authority-or-order"],
            PolicyConstraints: ["authority:unknown:no implicit permission"],
            Alternatives: ["restore-chain:link"],
            MissionPackageFacts: ["package:recon"],
            ResourceCommitments: Array.Empty<string>(),
            Fallback: "manual review of cited current facts",
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false,
            SideTaggedFacts: sideTagged ?? Array.Empty<AdviceSideTaggedFact>());

    private static string CreateTempRoot() =>
        Path.Combine(Path.GetTempPath(), "aegis-advice-explain-" + Guid.NewGuid().ToString("N"));

    private static void TryDeleteTree(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private sealed class CapturingAdviceChatClient : IChatClient
    {
        private readonly List<string> _captured;
        private readonly string _response;
        private int _disposed;

        public CapturingAdviceChatClient(List<string> captured, string response)
        {
            _captured = captured;
            _response = response;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            foreach (var message in chatMessages)
            {
                _captured.Add(message.Text ?? string.Empty);
            }

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _response))
            {
                ModelId = "advice-scripted",
                FinishReason = ChatFinishReason.Stop,
            });
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
            foreach (var update in response.ToChatResponseUpdates())
            {
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
    }
}
