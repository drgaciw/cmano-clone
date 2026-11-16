using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;

namespace ProjectAegis.Ai.Host.Aar;

/// <summary>
/// Builds delimited AAR prompts. Evidence text is data, never concatenated into instructions.
/// </summary>
public sealed class AarPromptBuilder
{
    private static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false,
    };

    /// <summary>Builds the chat messages for one AAR explain invocation.</summary>
    public IReadOnlyList<ChatMessage> Build(
        EvidenceDocument document,
        AarDeterministicReport report)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(report);

        var system = new StringBuilder();
        system.AppendLine("You are the Project Aegis AAR assistant.");
        system.AppendLine("Return JSON only with keys: summary (string), outcomes (string[]), uncertainties (string[]), citations (string[]).");
        system.AppendLine("Cite only evidence IDs listed in EVIDENCE_IDS. Do not invent numbers; the host copies quantitative values.");
        system.Append("PromptVersion=").Append(AarPromptVersion.Current);

        var data = new StringBuilder();
        data.AppendLine("<<<EVIDENCE_DATA_BEGIN>>>");
        data.Append("VIEWING_SIDE=").AppendLine(document.Header.ViewingSide.ToString());
        data.Append("DEBRIEF_MODE=").AppendLine(document.Header.DebriefMode ? "true" : "false");
        data.Append("MISSION_OUTCOME=").AppendLine(report.MissionOutcome);
        data.Append("DECISION_COUNT=").AppendLine(report.DecisionCount.ToString(CultureInfo.InvariantCulture));
        data.Append("POLICY_DENIAL_COUNT=").AppendLine(report.PolicyDenialCount.ToString(CultureInfo.InvariantCulture));
        data.Append("ENGAGEMENT_COUNT=").AppendLine(report.EngagementCount.ToString(CultureInfo.InvariantCulture));
        data.Append("CONTROLLER_CHANGE_COUNT=").AppendLine(
            report.ControllerChangeCount.ToString(CultureInfo.InvariantCulture));
        if (report.ObjectivesRatio is { } ratio)
        {
            data.Append("OBJECTIVES_RATIO=").AppendLine(ratio.ToString(CultureInfo.InvariantCulture));
        }

        data.AppendLine("EVIDENCE_IDS=");
        foreach (var id in document.Header.EvidenceIds)
        {
            data.AppendLine(id);
        }

        data.AppendLine("ENTRIES_JSON=");
        foreach (var entry in document.Entries)
        {
            // Evidence fields are untrusted data; serialize as JSON blob inside delimiters.
            data.AppendLine(JsonSerializer.Serialize(new
            {
                entry.EvidenceId,
                entry.SequenceId,
                Kind = entry.Kind.ToString(),
                entry.SimTime,
                entry.UnitId,
                entry.Summary,
                entry.Attributes,
            }, Compact));
        }

        data.Append("<<<EVIDENCE_DATA_END>>>");

        return
        [
            new ChatMessage(ChatRole.System, system.ToString()),
            new ChatMessage(ChatRole.User, data.ToString()),
        ];
    }
}
