using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Immutable selected-contact text; technical feasibility never grants release authority.</summary>
public sealed record SliceAContactPresentation(
    string PhaseLine,
    string ProvenanceLine,
    string FreshnessLine,
    string ChainLine,
    string AuthorityLine,
    string NextActionLine)
{
    /// <summary>Cleared presentation for an absent or unknown selection.</summary>
    public static SliceAContactPresentation Empty { get; } = new(
        "Phase: —", "Provenance: —", "Freshness: —", "Technical chain: UNKNOWN",
        "Release authority: UNKNOWN", "Select a contact to inspect its targeting chain.");
}

/// <summary>
/// Formats existing read-only projections, without issuing commands or deriving combat truth.
/// Build at tick/selection boundaries, not every render frame (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class SliceAContactPresenter
{
    /// <summary>Builds selected-contact text. Authority must be supplied for this contact/actor by the composition root.</summary>
    public static SliceAContactPresentation Build(
        string? contactId,
        KillChainContactSnapshot? killChain,
        ContactProvenanceSnapshot? provenance,
        SensorToShooterSnapshot? chains,
        C2AuthorityProjection? authority)
    {
        if (string.IsNullOrWhiteSpace(contactId)) return SliceAContactPresentation.Empty;
        var contact = killChain?.Contacts.FirstOrDefault(row => string.Equals(row.ContactId, contactId, StringComparison.Ordinal));
        var source = provenance?.Contacts.FirstOrDefault(row => string.Equals(row.ContactId, contactId, StringComparison.Ordinal));
        var chain = chains?.Chains.FirstOrDefault(row => string.Equals(row.ContactId, contactId, StringComparison.Ordinal));
        if (contact is null && source is null && chain is null) return SliceAContactPresentation.Empty;

        var phase = contact is null ? "Phase: UNKNOWN" : $"Phase: {contact.Phase} | Loss: {contact.Loss} | Technical targetability: {(contact.Targetable ? "YES" : "NO")}";
        var provenanceLine = source is null ? "Provenance: UNKNOWN — no active source record" :
            $"Source: {source.Source.ObserverId} | Confidence: {source.Confidence}\n{source.Source.SourceRef}\nClassification: {source.LastKnown.LifecycleState} | Quality: {source.QualityState}";
        var comms = source is null ? "UNKNOWN" : source.OutOfCommsUnknown ? "DENIED — current state unknown" :
            (source.QualityState & ContactProvenanceQualityState.SilentComms) != 0 ? "DEGRADED" : "no degradation reported";
        var freshness = source is null ? $"Freshness: UNKNOWN | Comms: {comms}" :
            $"Freshness: {source.Freshness.ToString().ToUpperInvariant()} | Age: {source.AgeTicks.ToString(CultureInfo.InvariantCulture)} ticks | Comms: {comms}";
        return new(phase, provenanceLine, freshness, FormatChain(chain), FormatAuthority(authority),
            NextAction(contact, source, chain, authority));
    }

    private static string FormatChain(SensorToShooterChain? chain)
    {
        if (chain is null) return "Technical chain: UNKNOWN — sensor → track → targetability → shooter facts unavailable";
        var text = new StringBuilder(chain.IsComplete ? "Technical chain: COMPLETE (not release authority)" : $"Technical chain: BROKEN — {chain.PrimaryCauseLabel}");
        foreach (var kind in new[] { SensorToShooterLinkKind.Sensor, SensorToShooterLinkKind.Track, SensorToShooterLinkKind.Targetability, SensorToShooterLinkKind.EligibleShooter })
        {
            var link = chain.Links.FirstOrDefault(item => item.Kind == kind);
            text.Append('\n').Append(kind == SensorToShooterLinkKind.EligibleShooter ? "Shooter" : kind.ToString()).Append(": ");
            if (link is null) { text.Append("UNKNOWN"); continue; }
            text.Append(link.IsLinked ? "LINKED" : "BROKEN");
            if (!string.IsNullOrEmpty(link.UnitId)) text.Append(" | ").Append(link.UnitId);
            if (!link.IsLinked) text.Append(" | ").Append(link.CauseLabel);
            if (!string.IsNullOrEmpty(link.Detail)) text.Append(" | ").Append(link.Detail);
        }
        return text.ToString();
    }

    private static string FormatAuthority(C2AuthorityProjection? authority)
    {
        if (authority is null) return "Release authority: UNKNOWN — no authority projection; not cleared to engage";
        var targeting = authority.Targeting;
        var label = targeting.Disposition switch
        {
            C2AuthorityDisposition.Permitted => "PERMITTED by authority projection (technical checks remain separate)",
            C2AuthorityDisposition.ApprovalRequired => "APPROVAL REQUIRED",
            _ => "WITHHELD",
        };
        return $"ROE: {authority.Roe.RoeLabel}\nRelease authority: {label}\nReason: {targeting.ReasonCode ?? "none reported"} | Approval: {targeting.PendingApproval?.ToString() ?? "none pending"}";
    }

    private static string NextAction(KillChainContactState? contact, ContactProvenanceState? source,
        SensorToShooterChain? chain, C2AuthorityProjection? authority)
    {
        if (contact?.Loss == KillChainLossKind.Lost || chain?.PrimaryBreakCause == SensorToShooterBreakCause.LostSensor)
            return "Reacquire the contact with a reporting sensor; last-known data is not a firing solution.";
        if (source is null) return "Obtain a current provenance report before relying on this contact.";
        if (source.OutOfCommsUnknown) return "Restore communications and confirm a current contact report.";
        if (source.Freshness == ContactProvenanceFreshness.Stale || contact?.Loss == KillChainLossKind.Stale ||
            (source.QualityState & ContactProvenanceQualityState.Stale) != 0 || chain?.PrimaryBreakCause == SensorToShooterBreakCause.StaleTrack)
            return "Refresh the sensor report and revalidate the track before targeting.";
        if ((source.QualityState & ContactProvenanceQualityState.CatalogMiss) != 0)
            return "Resolve the missing catalog identity before relying on technical eligibility.";
        if (chain is null) return "Obtain sensor-to-shooter facts; technical eligibility is unknown.";
        if (!chain.IsComplete)
            return chain.PrimaryBreakCause switch
            {
                SensorToShooterBreakCause.NoFireControl => "Acquire a fire-control-quality track from an eligible sensor.",
                SensorToShooterBreakCause.NoEligibleShooter => "Inspect shooter readiness, weapons, range and available ammunition.",
                SensorToShooterBreakCause.DegradedTrack => "Improve track quality and revalidate technical targetability.",
                _ => "Inspect and restore the broken chain links before targeting.",
            };
        if (authority is null) return "Obtain the selected actor's authority projection; technical feasibility is not permission to fire.";
        if (authority.Targeting.Disposition == C2AuthorityDisposition.ApprovalRequired)
            return "Request the required approval through the command workflow; do not treat this panel as clearance.";
        if (authority.Targeting.Disposition != C2AuthorityDisposition.Permitted)
            return $"Review the authority restriction ({authority.Targeting.ReasonCode ?? "unspecified"}) with command before engaging.";
        return "Review the projected chain and submit intent through the command workflow; execution revalidates eligibility and authority.";
    }
}
