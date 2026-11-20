using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Advice;
using NUnit.Framework;

namespace ProjectAegis.Ai.Host.Tests.Advice;

[TestFixture]
public sealed class AdviceExplanationPipelineTests
{
    [Test]
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

        Assert.That(result.DiscardedAsStale, Is.True);
        Assert.That(result.Status, Is.EqualTo(GenerationStatus.Cancelled));
        Assert.That(result.SkipReason, Is.EqualTo("stale-discarded"));
        Assert.That(result.Content, Is.Null);
        Assert.That(result.Envelope, Is.Null);
        // Always read Transport/PromptVersion/ProviderId/ModelId so NotAccessedPositionalProperty stays clear.
        Assert.That(result.Transport, Is.Null);
        Assert.That(string.IsNullOrWhiteSpace(result.PromptVersion), Is.False);
        Assert.That(string.IsNullOrWhiteSpace(result.ProviderId), Is.False);
        Assert.That(string.IsNullOrWhiteSpace(result.ModelId), Is.False);
        scripted.Dispose();
    }

    [Test]
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

        Assert.That(result.DiscardedAsStale, Is.True);
        Assert.That(result.SkipReason, Is.EqualTo("stale-discarded"));
        scripted.Dispose();
    }

    [Test]
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

            Assert.That(result.Status, Is.EqualTo(GenerationStatus.Succeeded));
            Assert.That(result.Content, Is.Not.Null);
            Assert.That(string.Join('\n', captured), Does.Contain("blue-visible-track"));
            Assert.That(string.Join('\n', captured), Does.Not.Contain("RED-SECRET-INTENT-TOKEN"));
            Assert.That(result.Content!.Explanation, Does.Not.Contain("RED-SECRET-INTENT-TOKEN"));
            Assert.That(result.Content.CitedFactKeys, Does.Contain("blue-track"));
            Assert.That(result.Content.CitedFactKeys, Does.Not.Contain("red-intent"));
            Assert.That(facts.SideTaggedFacts.Any(f => f.OwningSide == ViewingSide.Red && f.Key == "red-intent"), Is.True);
            Assert.That(facts.SideTaggedFacts.Any(f => f.OwningSide == ViewingSide.Blue && f.Key == "blue-track"), Is.True);
            Assert.That(result.Transport, Is.Not.Null);
            Assert.That(result.Transport!.Envelope, Is.EqualTo(result.Envelope));
            Assert.That(result.PromptVersion, Is.EqualTo(AdvicePromptVersion.Current));
            Assert.That(result.ProviderId, Is.EqualTo("fake"));
            Assert.That(string.IsNullOrWhiteSpace(result.ModelId), Is.False);
        }
        finally
        {
            TryDeleteTree(root);
            scripted.Dispose();
        }
    }

    [Test]
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

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.RejectedCitation));
        Assert.That(result.SkipReason, Is.EqualTo("hidden-side-leak"));
        Assert.That(result.Content, Is.Null);
        scripted.Dispose();
    }

    [Test]
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

            Assert.That(onResult.Status, Is.EqualTo(GenerationStatus.Succeeded));
            Assert.That(onResult.Envelope, Is.Not.Null);
            Assert.That(store.Exists("advice-explain.envelope.json"), Is.True);

            var onFingerprint = AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts);
            Assert.That(onFingerprint, Is.EqualTo(offFingerprint));
            Assert.That(onResult.Content!.EvidenceFingerprint, Is.EqualTo(offFingerprint));
            // Order-log / authority fingerprint on the envelope is the scenario order log, not advice prose.
            Assert.That(onResult.Envelope!.OrderLogFingerprint, Is.EqualTo("n/a"));
        }
        finally
        {
            TryDeleteTree(root);
            scripted.Dispose();
        }
    }

    [Test]
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

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.ProviderUnavailable));
        Assert.That(result.SkipReason, Is.EqualTo("disabled"));
        Assert.That(AdviceExplanationPipeline.FingerprintAuthoritativeFacts(facts), Is.EqualTo(fingerprint));
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
                _captured.Add(message.Text);
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
