using System.Globalization;
using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.EngageExplainContract;
using ProjectAegis.Delegation.EngageNextAction;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Selected engagement text derived only from read-only combat and contact projections.</summary>
public sealed record CombatDetailPresentation(
    string StatusLine, string WeaponLine, string HardConstraintsLine, string PolicyLine,
    string ConfidenceLine, string FiringSolutionLine, string NextActionLine, string BdaLine,
    string PostureLine, string CorrelationLine)
{
    /// <summary>No selected engagement evidence.</summary>
    public static CombatDetailPresentation Empty { get; } = new(
        "Engagement: UNKNOWN", "Weapon: UNKNOWN", "Hard constraints: UNKNOWN", "Policy: UNKNOWN",
        "Contact confidence: UNKNOWN", "Firing solution: UNKNOWN", "Select an engagement to inspect.",
        "BDA: UNKNOWN", "Posture: UNKNOWN", "Correlation: —");
}

/// <summary>Formats correlated evidence without deriving authority, targeting or damage from map state.</summary>
public static class CombatDetailPresenter
{
    /// <summary>Builds a detail card for an exact shooter/target/log-correlation tuple.</summary>
    public static CombatDetailPresentation Build(
        CombatEventSnapshot events, SliceAContactFrame contacts, BdaAssessSnapshot assessments,
        string shooterId, string targetId, ulong correlationId, string? contactId,
        IReadOnlyList<CombatEngagementExplanation>? explanations = null)
    {
        var leg = events.Events.Where(e => e.ShooterId == shooterId && e.TargetId == targetId
            && e.CorrelationId == correlationId).ToArray();
        if (leg.Length == 0) return CombatDetailPresentation.Empty;
        var latest = leg.OrderBy(e => e.SimTime).ThenBy(e => e.Phase).Last();
        var explanation = EngageExplainContractProjection.ProjectFromSnapshot(new EngageExplainCombatEventSnapshot(
            leg.Select(e => new EngageExplainCombatEventInput((EngageExplainCombatEventPhase)e.Phase,
                e.ShooterId, e.TargetId, e.WeaponFamilyId, e.Outcome, e.CorrelationId,
                e.SimTime, e.SimTick, e.ExplanationRef)).ToArray()));
        var refused = leg.LastOrDefault(e => e.Phase == CombatEventPhase.AuthorizationRefused);
        var reason = refused?.Outcome;
        var policyRefusal = refused?.ExplanationRef.StartsWith("policy:", StringComparison.Ordinal) == true;
        var next = reason == null ? null : EngageNextActionProjection.Project(
            new EngageNextActionInput(shooterId, latest.WeaponFamilyId, reason)).Rows.FirstOrDefault()?.NextActionCode;
        var contact = contacts.KillChain.Contacts.FirstOrDefault(c => c.ContactId == contactId && c.TargetId == targetId);
        var quality = contact == null ? null : contacts.Provenance.Contacts.FirstOrDefault(c => c.ContactId == contactId);
        var bda = contactId == null ? null : assessments.Contacts.FirstOrDefault(c => c.ContactId == contactId && c.TargetId == targetId);
        // A historical shot does not establish present fire control, and a kill outcome is not a BDA observation.
        var evidence = explanations?.FirstOrDefault(e => e.CorrelationId == correlationId
            && e.ShooterId == shooterId && e.TargetId == targetId);
        var firingSolution = evidence?.HasFireControlTrack switch
        {
            true => "Fire-control track present at execution",
            false => "No fire-control track at execution",
            _ => "UNKNOWN at execution",
        };
        var advice = next switch
        {
            EngageNextActionCodes.ReloadRearm => "Reload or rearm before requesting engagement again.",
            EngageNextActionCodes.Approval => "Request weapons-release approval; current refusal remains in force.",
            _ => reason == null ? "No corrective action reported." : EngageExplainProjection.ExplainCode(reason),
        };
        var constraint = refused == null ? "No refusal reported; individual constraints UNKNOWN"
            : policyRefusal ? "UNKNOWN — policy refusal reported" : EngageExplainProjection.ExplainCode(reason);
        return new CombatDetailPresentation(
            $"{shooterId} → {targetId}: {latest.Phase} / {latest.Outcome}",
            $"Weapon family: {latest.WeaponFamilyId}" + (evidence == null ? "" : $" | Salvo: {evidence.SalvoSize}"), $"Hard constraints: {constraint}",
            $"Policy: {(policyRefusal ? explanation.WhyWithheld : explanation.WhyPermitted ?? "UNKNOWN")}",
            $"Contact confidence: {(quality == null ? "UNKNOWN" : quality.Confidence.ToString())}",
            $"Firing solution: {firingSolution}", advice,
            bda == null ? "BDA: UNKNOWN — no contact-specific assessment" : $"BDA: {bda.State} | Source: {bda.Source}",
            "Posture: UNKNOWN — no posture fact supplied",
            $"Log correlation: {correlationId.ToString(CultureInfo.InvariantCulture)} | Sim time: {latest.SimTime.ToString("R", CultureInfo.InvariantCulture)}");
    }
}
