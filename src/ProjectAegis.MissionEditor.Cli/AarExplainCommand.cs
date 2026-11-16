using System.Globalization;
using System.Text.Json;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host;
using ProjectAegis.Ai.Host.Aar;
using ProjectAegis.Ai.Host.Evidence;

namespace ProjectAegis.MissionEditor.Cli;

/// <summary>
/// DRG-744 / CMA-15: opt-in <c>aar_explain</c> verb. Delegates to <see cref="AarAssistantPipeline"/>.
/// Fake clients only — never opens a paid provider.
/// </summary>
/// <remarks>
/// Exit codes: <c>0</c> structured result written (including non-Succeeded generation statuses);
/// <c>1</c> usage / missing evidence / IO failure.
/// Off by default (<c>--enabled</c> required to invoke the fake client).
/// </remarks>
public static class AarExplainCommand
{
    /// <summary>
    /// Runs AAR explain against an exported JSONL evidence file.
    /// </summary>
    public static async Task<int> RunAsync(
        string? evidencePath,
        bool enabled,
        string? artifactRoot,
        string? scriptPath,
        TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(evidencePath))
        {
            return McpToolResult.WriteError(
                output,
                "MISSING_EVIDENCE",
                "aar_explain requires --evidence <evidence.jsonl>");
        }

        if (!File.Exists(evidencePath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", "Evidence file not found: " + evidencePath);
        }

        EvidenceDocument document;
        try
        {
            document = await JsonlEvidenceWriter.ReadRoundTripAsync(evidencePath).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or JsonException)
        {
            return McpToolResult.WriteError(output, "EVIDENCE_PARSE", ex.Message);
        }

        var quantitative = Summarize(document);
        AtomicArtifactStore? store = null;
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            Directory.CreateDirectory(artifactRoot);
            store = new AtomicArtifactStore(artifactRoot, new AiHostOptions());
        }

        IChatClientOwner clientOwner;
        try
        {
            clientOwner = CreateClient(enabled, scriptPath, document, quantitative);
        }
        catch (FileNotFoundException ex)
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", ex.Message);
        }

        using (clientOwner)
        {
            var pipeline = new AarAssistantPipeline(clientOwner.Client, artifactStore: store);
            var result = await pipeline
                .RunAsync(
                    new AarAssistantRequest(
                        Document: document,
                        Quantitative: quantitative,
                        AuthoritativeFacts: null,
                        BuildIdentity: document.Header.BuildIdentity,
                        ProviderId: "fake",
                        ModelId: clientOwner.ModelId,
                        Enabled: enabled,
                        ArtifactFileName: "aar-explain.envelope.json"))
                .ConfigureAwait(false);

            var payload = new Dictionary<string, object?>
            {
                ["ok"] = true,
                ["verb"] = "aar_explain",
                ["enabled"] = enabled,
                ["status"] = result.Status.ToString(),
                ["promptVersion"] = result.PromptVersion,
                ["providerId"] = result.ProviderId,
                ["modelId"] = result.ModelId,
                ["skipReason"] = result.SkipReason,
                ["deterministicReport"] = new
                {
                    result.DeterministicReport.MissionOutcome,
                    result.DeterministicReport.DecisionCount,
                    result.DeterministicReport.PolicyDenialCount,
                    result.DeterministicReport.EngagementCount,
                    result.DeterministicReport.ControllerChangeCount,
                    result.DeterministicReport.ObjectivesRatio,
                    allowedEvidenceIds = result.DeterministicReport.AllowedEvidenceIds,
                },
                ["modelDraft"] = result.ModelDraft is null
                    ? null
                    : new
                    {
                        result.ModelDraft.Summary,
                        result.ModelDraft.Outcomes,
                        result.ModelDraft.Uncertainties,
                        result.ModelDraft.Citations,
                    },
                ["envelopePublished"] = result.Envelope is not null,
                ["exitCodeNotes"] =
                    "0 = structured result (any GenerationStatus); 1 = usage/missing evidence/parse failure.",
            };

            return McpToolResult.WriteOk(output, payload);
        }
    }

    private static EvidenceQuantitativeSummary Summarize(EvidenceDocument document)
    {
        var decisions = 0;
        var denials = 0;
        var engagements = 0;
        var controller = 0;
        foreach (var entry in document.Entries)
        {
            switch (entry.Kind)
            {
                case EvidenceEntryKind.AgentDecision:
                    decisions++;
                    break;
                case EvidenceEntryKind.PolicyDenial:
                    denials++;
                    break;
                case EvidenceEntryKind.Engagement:
                case EvidenceEntryKind.EngagementOutcome:
                    engagements++;
                    break;
                case EvidenceEntryKind.ControllerChange:
                    controller++;
                    break;
            }
        }

        return new EvidenceQuantitativeSummary(decisions, denials, engagements, controller, ObjectivesRatio: null);
    }

    private static IChatClientOwner CreateClient(
        bool enabled,
        string? scriptPath,
        EvidenceDocument document,
        EvidenceQuantitativeSummary quantitative)
    {
        if (!enabled)
        {
            // Pipeline short-circuits before GetResponseAsync when disabled.
            return new FakeOwner(new FakeChatClient(), "fake-aar");
        }

        if (!string.IsNullOrWhiteSpace(scriptPath))
        {
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("AAR script not found: " + scriptPath, scriptPath);
            }

            var text = File.ReadAllText(scriptPath);
            var scripted = new AarScriptedChatClient();
            scripted.EnqueueAssistantText(text);
            return new ScriptedOwner(scripted, "aar-scripted");
        }

        // Default enabled path: grounded fake draft citing exported evidence IDs only.
        var ids = document.Header.EvidenceIds.ToArray();
        var summary = "Engagements " + quantitative.EngagementCount.ToString(CultureInfo.InvariantCulture)
            + "; decisions " + quantitative.DecisionCount.ToString(CultureInfo.InvariantCulture)
            + "; denials " + quantitative.PolicyDenialCount.ToString(CultureInfo.InvariantCulture) + ".";
        var draftJson = JsonSerializer.Serialize(new
        {
            summary,
            outcomes = new[] { "deterministic fake outcome" },
            uncertainties = Array.Empty<string>(),
            citations = ids,
        });
        var client = new AarScriptedChatClient();
        client.EnqueueAssistantText(draftJson);
        return new ScriptedOwner(client, "aar-scripted");
    }

    private interface IChatClientOwner : IDisposable
    {
        Microsoft.Extensions.AI.IChatClient Client { get; }

        string ModelId { get; }
    }

    private sealed class FakeOwner(FakeChatClient client, string modelId) : IChatClientOwner
    {
        public Microsoft.Extensions.AI.IChatClient Client { get; } = client;

        public string ModelId { get; } = modelId;

        public void Dispose() => client.Dispose();
    }

    private sealed class ScriptedOwner(AarScriptedChatClient client, string modelId) : IChatClientOwner
    {
        public Microsoft.Extensions.AI.IChatClient Client { get; } = client;

        public string ModelId { get; } = modelId;

        public void Dispose() => client.Dispose();
    }
}
