using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Aar;

/// <summary>
/// DRG-739: host AAR pipeline. Quantitative values are host-copied; model supplies prose + IDs.
/// </summary>
public sealed class AarAssistantPipeline : IAarAssistantPipeline
{
    private readonly IChatClient _chatClient;
    private readonly AarPromptBuilder _promptBuilder;
    private readonly AtomicArtifactStore? _artifactStore;
    private readonly AiHostTelemetry? _telemetry;

    /// <summary>Creates a pipeline over an offline or real <see cref="IChatClient"/>.</summary>
    public AarAssistantPipeline(
        IChatClient chatClient,
        AarPromptBuilder? promptBuilder = null,
        AtomicArtifactStore? artifactStore = null,
        AiHostTelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        _chatClient = chatClient;
        _promptBuilder = promptBuilder ?? new AarPromptBuilder();
        _artifactStore = artifactStore;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task<AarAssistantResult> RunAsync(
        AarAssistantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Document);

        var report = AarDeterministicReport.FromDocument(
            request.Document,
            request.Quantitative,
            request.AuthoritativeFacts);

        if (!request.Enabled)
        {
            return new AarAssistantResult(
                Status: GenerationStatus.ProviderUnavailable,
                DeterministicReport: report,
                ModelDraft: null,
                PromptVersion: AarPromptVersion.Current,
                ProviderId: request.ProviderId,
                ModelId: request.ModelId,
                SkipReason: "disabled",
                Envelope: null);
        }

        ChatResponse? response = null;
        try
        {
            var messages = _promptBuilder.Build(request.Document, report);
            response = await _chatClient
                .GetResponseAsync(messages, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return await FinishAsync(
                    request,
                    report,
                    GenerationStatus.Cancelled,
                    draft: null,
                    skipReason: "cancelled",
                    modelId: request.ModelId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return await FinishAsync(
                    request,
                    report,
                    GenerationStatus.TimedOut,
                    draft: null,
                    skipReason: "timeout",
                    modelId: request.ModelId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return await FinishAsync(
                    request,
                    report,
                    GenerationStatus.ProviderUnavailable,
                    draft: null,
                    skipReason: "provider-unavailable",
                    modelId: request.ModelId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var assistantText = response.Text;
        var modelId = response.ModelId ?? request.ModelId;
        if (!AarModelDraftParser.TryParse(assistantText, out var draft) || draft is null)
        {
            return await FinishAsync(
                    request,
                    report,
                    GenerationStatus.MalformedOutput,
                    draft: null,
                    skipReason: "malformed-output",
                    modelId: modelId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var missing = AarCitationValidator.FindMissingCitation(draft.Citations, report.AllowedEvidenceIds);
        if (missing is not null)
        {
            return await FinishAsync(
                    request,
                    report,
                    GenerationStatus.RejectedCitation,
                    draft,
                    skipReason: "missing-citation:" + missing,
                    modelId: modelId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var prose = new List<string> { draft.Summary };
        prose.AddRange(draft.Outcomes);
        prose.AddRange(draft.Uncertainties);
        var badNumber = AarNumberValidator.FindContradictoryNumber(prose, report.AllowedNumericTokens());
        if (badNumber is not null)
        {
            return await FinishAsync(
                    request,
                    report,
                    GenerationStatus.RejectedNumber,
                    draft,
                    skipReason: "contradictory-number:" + badNumber,
                    modelId: modelId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return await FinishAsync(
                request,
                report,
                GenerationStatus.Succeeded,
                draft,
                skipReason: null,
                modelId: modelId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<AarAssistantResult> FinishAsync(
        AarAssistantRequest request,
        AarDeterministicReport report,
        GenerationStatus status,
        AarModelDraft? draft,
        string? skipReason,
        string modelId,
        CancellationToken cancellationToken)
    {
        _telemetry?.RecordGeneration(request.ProviderId, modelId, status);

        ArtifactEnvelope? envelope = null;
        if (_artifactStore is not null)
        {
            var contentHash = HashContent(status, report, draft);
            envelope = new ArtifactEnvelope(
                SchemaVersion: AiContractSchema.Version,
                BuildIdentity: request.BuildIdentity,
                ScenarioId: request.Document.Header.ScenarioId,
                CatalogRevision: request.Document.Header.CatalogRevision,
                OrderLogFingerprint: request.Document.Header.OrderLogFingerprint,
                ProviderId: request.ProviderId,
                ModelId: modelId,
                PromptVersion: AarPromptVersion.Current,
                EvidenceIds: draft?.Citations.ToArray() ?? Array.Empty<string>(),
                ViewingSide: request.Document.Header.ViewingSide,
                DebriefMode: request.Document.Header.DebriefMode,
                Status: status,
                ContentHash: contentHash);

            await _artifactStore
                .PublishAsync(request.ArtifactFileName, envelope, cancellationToken)
                .ConfigureAwait(false);
            _telemetry?.RecordPublish(request.ArtifactFileName, status);
        }

        return new AarAssistantResult(
            Status: status,
            DeterministicReport: report,
            ModelDraft: draft,
            PromptVersion: AarPromptVersion.Current,
            ProviderId: request.ProviderId,
            ModelId: modelId,
            SkipReason: skipReason,
            Envelope: envelope);
    }

    private static string HashContent(
        GenerationStatus status,
        AarDeterministicReport report,
        AarModelDraft? draft)
    {
        var payload = JsonSerializer.Serialize(new
        {
            status = status.ToString(),
            promptVersion = AarPromptVersion.Current,
            report.MissionOutcome,
            report.DecisionCount,
            report.PolicyDenialCount,
            report.EngagementCount,
            report.ControllerChangeCount,
            report.ObjectivesRatio,
            draft,
        });
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
