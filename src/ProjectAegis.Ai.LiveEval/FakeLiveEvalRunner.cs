using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Aar;
using ProjectAegis.Ai.Host.Evidence;

namespace ProjectAegis.Ai.LiveEval;

/// <summary>
/// Zero-spend LiveEval path: runs the host AAR pipeline with <see cref="AarScriptedChatClient"/>.
/// </summary>
public sealed class FakeLiveEvalRunner
{
    /// <summary>
    /// Evaluates a single evidence document with a grounded fake draft.
    /// </summary>
    public async Task<LiveEvalCaseResult> RunCaseAsync(
        string caseId,
        EvidenceDocument document,
        EvidenceQuantitativeSummary quantitative,
        IReadOnlyDictionary<string, string>? caseMetadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(quantitative);

        if (!TrackASourceGate.TryAllowPublicSource(caseMetadata, out var trackAReason))
        {
            return new LiveEvalCaseResult(
                CaseId: caseId,
                ProviderId: "fake",
                ModelId: "aar-scripted",
                PromptVersion: AarPromptVersion.Current,
                Status: GenerationStatus.ProviderUnavailable,
                CitationsResolved: false,
                NumbersConsistent: false,
                LatencyMilliseconds: 0,
                PromptTokens: null,
                CompletionTokens: null,
                EstimatedCostUsd: 0m,
                SkipReason: trackAReason);
        }

        PaidProviderGate.EnsureFakeOrThrow(LiveEvalMode.Fake);

        var ids = document.Header.EvidenceIds.ToArray();
        var summary = "Engagements " + quantitative.EngagementCount.ToString(CultureInfo.InvariantCulture)
            + "; decisions " + quantitative.DecisionCount.ToString(CultureInfo.InvariantCulture) + ".";
        var draftJson = JsonSerializer.Serialize(new
        {
            summary,
            outcomes = new[] { "fake live-eval outcome" },
            uncertainties = Array.Empty<string>(),
            citations = ids,
        });

        using var client = new AarScriptedChatClient();
        client.EnqueueAssistantText(draftJson);
        var pipeline = new AarAssistantPipeline(client);

        var sw = Stopwatch.StartNew();
        var result = await pipeline
            .RunAsync(
                new AarAssistantRequest(
                    Document: document,
                    Quantitative: quantitative,
                    AuthoritativeFacts: null,
                    BuildIdentity: document.Header.BuildIdentity,
                    ProviderId: "fake",
                    ModelId: "aar-scripted",
                    Enabled: true),
                cancellationToken)
            .ConfigureAwait(false);
        sw.Stop();

        var citationsOk = result.Status == GenerationStatus.Succeeded
            && result.ModelDraft is not null
            && AarCitationValidator.FindMissingCitation(
                result.ModelDraft.Citations,
                result.DeterministicReport.AllowedEvidenceIds) is null;

        var numbersOk = result.Status == GenerationStatus.Succeeded
            && result.ModelDraft is not null
            && AarNumberValidator.FindContradictoryNumber(
                ProseParts(result.ModelDraft),
                result.DeterministicReport.AllowedNumericTokens()) is null;

        return new LiveEvalCaseResult(
            CaseId: caseId,
            ProviderId: result.ProviderId ?? "fake",
            ModelId: result.ModelId ?? "aar-scripted",
            PromptVersion: result.PromptVersion,
            Status: result.Status,
            CitationsResolved: citationsOk,
            NumbersConsistent: numbersOk,
            LatencyMilliseconds: sw.ElapsedMilliseconds,
            PromptTokens: null,
            CompletionTokens: null,
            EstimatedCostUsd: 0m,
            SkipReason: result.SkipReason);
    }

    private static IEnumerable<string> ProseParts(AarModelDraft draft)
    {
        yield return draft.Summary;
        foreach (var o in draft.Outcomes)
        {
            yield return o;
        }

        foreach (var u in draft.Uncertainties)
        {
            yield return u;
        }
    }
}
