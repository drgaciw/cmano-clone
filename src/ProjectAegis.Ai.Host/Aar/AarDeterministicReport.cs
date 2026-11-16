using System.Globalization;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;

namespace ProjectAegis.Ai.Host.Aar;

/// <summary>
/// Host-authored quantitative report copied from evidence / projection (never from a model).
/// Always returned alongside any <see cref="GenerationStatus"/>.
/// </summary>
public sealed record AarDeterministicReport(
    string MissionOutcome,
    int DecisionCount,
    int PolicyDenialCount,
    int EngagementCount,
    int ControllerChangeCount,
    double? ObjectivesRatio,
    IReadOnlyDictionary<string, string> AuthoritativeFacts,
    IReadOnlyList<string> AllowedEvidenceIds)
{
    /// <summary>Builds a deterministic report from a projection result.</summary>
    public static AarDeterministicReport FromProjection(EvidenceProjectionResult projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        var q = projection.Quantitative;
        var facts = projection.AuthoritativeFacts is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(projection.AuthoritativeFacts, StringComparer.Ordinal);

        facts["decisionCount"] = q.DecisionCount.ToString(CultureInfo.InvariantCulture);
        facts["policyDenialCount"] = q.PolicyDenialCount.ToString(CultureInfo.InvariantCulture);
        facts["engagementCount"] = q.EngagementCount.ToString(CultureInfo.InvariantCulture);
        facts["controllerChangeCount"] = q.ControllerChangeCount.ToString(CultureInfo.InvariantCulture);
        if (q.ObjectivesRatio is { } ratio)
        {
            facts["objectivesRatio"] = ratio.ToString(CultureInfo.InvariantCulture);
        }

        var outcome = facts.TryGetValue("missionOutcome", out var mo) && !string.IsNullOrWhiteSpace(mo)
            ? mo
            : "Unknown";

        return new AarDeterministicReport(
            MissionOutcome: outcome,
            DecisionCount: q.DecisionCount,
            PolicyDenialCount: q.PolicyDenialCount,
            EngagementCount: q.EngagementCount,
            ControllerChangeCount: q.ControllerChangeCount,
            ObjectivesRatio: q.ObjectivesRatio,
            AuthoritativeFacts: facts,
            AllowedEvidenceIds: projection.Document.Header.EvidenceIds.ToArray());
    }

    /// <summary>Builds a deterministic report from an already-exported evidence document.</summary>
    public static AarDeterministicReport FromDocument(
        EvidenceDocument document,
        EvidenceQuantitativeSummary quantitative,
        IReadOnlyDictionary<string, string>? authoritativeFacts = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(quantitative);
        return FromProjection(new EvidenceProjectionResult(
            document,
            quantitative,
            AarSection: null,
            AuthoritativeFacts: authoritativeFacts));
    }

    /// <summary>Numeric tokens the model prose may echo (InvariantCulture).</summary>
    public IReadOnlySet<string> AllowedNumericTokens()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in AuthoritativeFacts.Values)
        {
            foreach (var token in AarNumberValidator.ExtractNumericTokens(value))
            {
                set.Add(token);
            }
        }

        set.Add(DecisionCount.ToString(CultureInfo.InvariantCulture));
        set.Add(PolicyDenialCount.ToString(CultureInfo.InvariantCulture));
        set.Add(EngagementCount.ToString(CultureInfo.InvariantCulture));
        set.Add(ControllerChangeCount.ToString(CultureInfo.InvariantCulture));
        if (ObjectivesRatio is { } ratio)
        {
            set.Add(ratio.ToString(CultureInfo.InvariantCulture));
        }

        return set;
    }
}
