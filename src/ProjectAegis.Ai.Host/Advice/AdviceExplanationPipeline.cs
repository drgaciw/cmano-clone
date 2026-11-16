using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Advice;

/// <summary>
/// DRG-749: host AdviceFrame explanation. Generated prose is a host artifact only.
/// Never writes into SkillEnvelope or ReplayProvenance.
/// </summary>
public sealed class AdviceExplanationPipeline : IAdviceExplanationPipeline
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IChatClient _chatClient;
    private readonly AdviceExplanationPromptBuilder _promptBuilder;
    private readonly AtomicArtifactStore? _artifactStore;
    private readonly AiHostTelemetry? _telemetry;

    /// <summary>Creates a pipeline over an offline or real <see cref="IChatClient"/>.</summary>
    public AdviceExplanationPipeline(
        IChatClient chatClient,
        AdviceExplanationPromptBuilder? promptBuilder = null,
        AtomicArtifactStore? artifactStore = null,
        AiHostTelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        _chatClient = chatClient;
        _promptBuilder = promptBuilder ?? new AdviceExplanationPromptBuilder();
        _artifactStore = artifactStore;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task<AdviceExplanationResult> RunAsync(
        AdviceExplanationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Facts);

        if (!request.Enabled)
        {
            return Finish(
                request,
                GenerationStatus.ProviderUnavailable,
                content: null,
                skipReason: "disabled",
                modelId: request.ModelId,
                discarded: false);
        }

        ChatResponse response;
        IReadOnlyList<string> citedKeys;
        try
        {
            var messages = _promptBuilder.Build(request, out citedKeys);
            response = await _chatClient
                .GetResponseAsync(messages, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Finish(
                request,
                GenerationStatus.Cancelled,
                content: null,
                skipReason: "cancelled",
                modelId: request.ModelId,
                discarded: false);
        }
        catch (TimeoutException)
        {
            return Finish(
                request,
                GenerationStatus.TimedOut,
                content: null,
                skipReason: "timeout",
                modelId: request.ModelId,
                discarded: false);
        }
        catch (InvalidOperationException)
        {
            return Finish(
                request,
                GenerationStatus.ProviderUnavailable,
                content: null,
                skipReason: "provider-unavailable",
                modelId: request.ModelId,
                discarded: false);
        }

        // Stale discard: selection or evidence changed while the model was in flight.
        if (request.GetCurrentKeys is not null)
        {
            var current = request.GetCurrentKeys();
            if (!string.Equals(current.InvocationKey, request.InvocationKey, StringComparison.Ordinal)
                || !string.Equals(current.EvidenceFingerprint, request.EvidenceFingerprint, StringComparison.Ordinal))
            {
                return Finish(
                    request,
                    GenerationStatus.Cancelled,
                    content: null,
                    skipReason: "stale-discarded",
                    modelId: response.ModelId ?? request.ModelId,
                    discarded: true);
            }
        }

        var explanation = response.Text.Trim();
        if (explanation.Length == 0)
        {
            return Finish(
                request,
                GenerationStatus.MalformedOutput,
                content: null,
                skipReason: "empty-explanation",
                modelId: response.ModelId ?? request.ModelId,
                discarded: false);
        }

        // Reject opposing-side leakage that slipped into model prose.
        var hiddenTexts = request.Facts.SideTaggedFacts
            .Where(f => f.HiddenFrom.Contains(request.ViewingSide))
            .Select(f => f.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();
        foreach (var hidden in hiddenTexts)
        {
            if (explanation.Contains(hidden, StringComparison.Ordinal))
            {
                return Finish(
                    request,
                    GenerationStatus.RejectedCitation,
                    content: null,
                    skipReason: "hidden-side-leak",
                    modelId: response.ModelId ?? request.ModelId,
                    discarded: false);
            }
        }

        var content = new AdviceExplanationContent(
            InvocationKey: request.InvocationKey,
            EvidenceFingerprint: request.EvidenceFingerprint,
            ViewingSide: request.ViewingSide,
            Explanation: explanation,
            CitedFactKeys: citedKeys,
            PromptVersion: AdvicePromptVersion.Current);

        return await PublishAsync(
                request,
                GenerationStatus.Succeeded,
                content,
                skipReason: null,
                modelId: response.ModelId ?? request.ModelId,
                discarded: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<AdviceExplanationResult> PublishAsync(
        AdviceExplanationRequest request,
        GenerationStatus status,
        AdviceExplanationContent? content,
        string? skipReason,
        string modelId,
        bool discarded,
        CancellationToken cancellationToken)
    {
        _telemetry?.RecordGeneration(request.ProviderId, modelId, status);

        ArtifactEnvelope? envelope = null;
        AiArtifactTransport? transport = null;
        if (_artifactStore is not null && content is not null && !discarded)
        {
            var contentJson = JsonSerializer.Serialize(content, JsonOptions);
            var contentHash = "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contentJson)))
                .ToLowerInvariant();
            envelope = new ArtifactEnvelope(
                SchemaVersion: AiContractSchema.Version,
                BuildIdentity: request.BuildIdentity,
                ScenarioId: request.ScenarioId,
                CatalogRevision: request.CatalogRevision,
                OrderLogFingerprint: request.OrderLogFingerprint,
                ProviderId: request.ProviderId,
                ModelId: modelId,
                PromptVersion: AdvicePromptVersion.Current,
                EvidenceIds: new[] { request.EvidenceFingerprint },
                ViewingSide: request.ViewingSide,
                DebriefMode: request.ViewingSide == ViewingSide.Debrief,
                Status: status,
                ContentHash: contentHash);

            await _artifactStore
                .PublishAsync(request.ArtifactFileName, envelope, cancellationToken)
                .ConfigureAwait(false);
            _telemetry?.RecordPublish(request.ArtifactFileName, status);

            transport = new AiArtifactTransport(
                Envelope: envelope,
                ContentMediaType: "application/json",
                ContentText: contentJson,
                Warnings: null);
        }

        return new AdviceExplanationResult(
            Status: status,
            Content: content,
            Envelope: envelope,
            Transport: transport,
            PromptVersion: AdvicePromptVersion.Current,
            ProviderId: request.ProviderId,
            ModelId: modelId,
            SkipReason: skipReason,
            DiscardedAsStale: discarded);
    }

    private AdviceExplanationResult Finish(
        AdviceExplanationRequest request,
        GenerationStatus status,
        AdviceExplanationContent? content,
        string? skipReason,
        string modelId,
        bool discarded)
    {
        _telemetry?.RecordGeneration(request.ProviderId, modelId, status);
        return new AdviceExplanationResult(
            Status: status,
            Content: content,
            Envelope: null,
            Transport: null,
            PromptVersion: AdvicePromptVersion.Current,
            ProviderId: request.ProviderId,
            ModelId: modelId,
            SkipReason: skipReason,
            DiscardedAsStale: discarded);
    }

    /// <summary>
    /// Deterministic fingerprint of authoritative AdviceFrame fields used for stale checks
    /// and on/off equality tests. Does not include generated explanation text.
    /// </summary>
    public static string FingerprintAuthoritativeFacts(AdviceExplanationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var payload = JsonSerializer.Serialize(new
        {
            facts.ContactId,
            facts.Availability,
            facts.Confidence,
            facts.ContactConfidenceLabel,
            facts.Rationale,
            facts.Assumptions,
            facts.HardConstraints,
            facts.PolicyConstraints,
            facts.Alternatives,
            facts.MissionPackageFacts,
            facts.ResourceCommitments,
            facts.Fallback,
            facts.IsWeaponsReleaseAuthorization,
            facts.IsFireOrder,
        }, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
