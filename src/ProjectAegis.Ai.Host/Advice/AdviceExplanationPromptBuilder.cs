using System.Text;
using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Advice;

/// <summary>
/// Builds the offline prompt from viewing-side-visible facts only.
/// Evidence text is untrusted data (ADR-030).
/// </summary>
public sealed class AdviceExplanationPromptBuilder
{
    /// <summary>Filters side-tagged facts and builds chat messages.</summary>
    public IReadOnlyList<ChatMessage> Build(AdviceExplanationRequest request, out IReadOnlyList<string> citedFactKeys)
    {
        ArgumentNullException.ThrowIfNull(request);
        var visible = FilterVisibleFacts(request.Facts, request.ViewingSide);
        citedFactKeys = visible.Select(f => f.Key).ToArray();

        var sb = new StringBuilder();
        sb.AppendLine("Explain the following validated advisory facts in plain language.");
        sb.AppendLine("Do not invent numbers, sides, or authorities.");
        sb.AppendLine("DATA:");
        sb.AppendLine("contactId=" + request.Facts.ContactId);
        sb.AppendLine("availability=" + request.Facts.Availability);
        sb.AppendLine("confidenceLabel=" + request.Facts.ContactConfidenceLabel);
        if (request.Facts.Confidence is { } confidence)
        {
            sb.AppendLine("confidence=" + confidence.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        sb.AppendLine("rationale=" + request.Facts.Rationale);
        AppendList(sb, "assumptions", request.Facts.Assumptions);
        AppendList(sb, "hardConstraints", request.Facts.HardConstraints);
        AppendList(sb, "policyConstraints", request.Facts.PolicyConstraints);
        AppendList(sb, "alternatives", request.Facts.Alternatives);
        AppendList(sb, "missionPackageFacts", request.Facts.MissionPackageFacts);
        AppendList(sb, "resourceCommitments", request.Facts.ResourceCommitments);
        sb.AppendLine("fallback=" + request.Facts.Fallback);
        sb.AppendLine("isWeaponsReleaseAuthorization=" + request.Facts.IsWeaponsReleaseAuthorization);
        sb.AppendLine("isFireOrder=" + request.Facts.IsFireOrder);
        foreach (var fact in visible)
        {
            sb.AppendLine("sideFact." + fact.Key + "=" + fact.Text);
        }

        return
        [
            new ChatMessage(ChatRole.System, "You explain advisory facts. Output plain prose only."),
            new ChatMessage(ChatRole.User, sb.ToString()),
        ];
    }

    /// <summary>Returns side-tagged facts visible to <paramref name="viewingSide"/>.</summary>
    public static IReadOnlyList<AdviceSideTaggedFact> FilterVisibleFacts(
        AdviceExplanationFacts facts,
        ViewingSide viewingSide)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.SideTaggedFacts.Count == 0)
        {
            return Array.Empty<AdviceSideTaggedFact>();
        }

        if (viewingSide == ViewingSide.Debrief)
        {
            return facts.SideTaggedFacts;
        }

        return facts.SideTaggedFacts
            .Where(f => f.HiddenFrom.All(h => h != viewingSide))
            .ToArray();
    }

    private static void AppendList(StringBuilder sb, string key, IReadOnlyList<string> values)
    {
        foreach (var value in values)
        {
            sb.AppendLine(key + "=" + value);
        }
    }
}
