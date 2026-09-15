using System.Globalization;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-180: structured contact provenance rows for live C2 surfaces.
/// Binds read-only frame facts only — hosts must not re-derive sim truth (ADR-010 §2–3).
/// </summary>
public sealed record SliceAContactLiveSurfaceState(
    string SourceLine,
    string ConfidenceLine,
    string AgeLine,
    string LastKnownLine,
    string CommsLine,
    string DeclutterToken,
    string SourceCueClass,
    string ConfidenceCueClass,
    string AgeCueClass,
    string LastKnownCueClass,
    string CommsCueClass,
    bool ContactExplanationAvailable,
    bool EngagementExplanationAvailable,
    string? EngagementInspectionKey)
{
    /// <summary>Cleared live-surface state for absent selection or unavailable evidence.</summary>
    public static SliceAContactLiveSurfaceState Empty { get; } = new(
        "SRC: —",
        "CONF: —",
        "AGE: —",
        "LAST-KNOWN: —",
        "COMMS: —",
        string.Empty,
        ContactProvenanceCueClasses.Unknown,
        ContactProvenanceCueClasses.Unknown,
        ContactProvenanceCueClasses.Unknown,
        ContactProvenanceCueClasses.Unknown,
        ContactProvenanceCueClasses.Unknown,
        false,
        false,
        null);
}

/// <summary>Non-color USS cue tokens for contact provenance rows (text + border class).</summary>
public static class ContactProvenanceCueClasses
{
    public const string Unknown = "contact-provenance-cue--unknown";
    public const string Nominal = "contact-provenance-cue--nominal";
    public const string Degraded = "contact-provenance-cue--degraded";
    public const string Denied = "contact-provenance-cue--denied";
}

/// <summary>Headless text declutter tokens paired with cue classes — never color-only state.</summary>
public static class ContactProvenanceDeclutterTokens
{
    public const string Unknown = "[EVIDENCE:UNKNOWN]";
    public const string Stale = "[EVIDENCE:STALE]";
    public const string CommsDenied = "[EVIDENCE:COMMS-DENIED]";
    public const string CatalogMiss = "[EVIDENCE:CATALOG-MISS]";
    public const string Fresh = "[EVIDENCE:FRESH]";
}

/// <summary>Maps Slice A frame rows into per-field live-surface labels and deep-link targets.</summary>
public static class SliceAContactLiveSurfaceBinder
{
    /// <summary>Builds fail-closed provenance rows for the selected contact.</summary>
    public static SliceAContactLiveSurfaceState Bind(
        string? contactId,
        SliceAContactFrame frame,
        CombatPresentationFrame? combatFrame = null)
    {
        if (string.IsNullOrWhiteSpace(contactId))
        {
            return SliceAContactLiveSurfaceState.Empty;
        }

        var killChain = frame.KillChain.Contacts.FirstOrDefault(row =>
            string.Equals(row.ContactId, contactId, StringComparison.Ordinal));
        var provenance = frame.Provenance.Contacts.FirstOrDefault(row =>
            string.Equals(row.ContactId, contactId, StringComparison.Ordinal));
        if (killChain is null && provenance is null)
        {
            return SliceAContactLiveSurfaceState.Empty;
        }

        var declutter = ResolveDeclutterToken(provenance);
        var engagementKey = ResolveEngagementInspectionKey(contactId, frame, combatFrame);
        var hasContactExplanation = killChain is not null || provenance is not null;
        return new SliceAContactLiveSurfaceState(
            FormatSource(provenance),
            FormatConfidence(provenance),
            FormatAge(provenance),
            FormatLastKnown(provenance, killChain),
            FormatComms(provenance),
            declutter,
            ResolveSourceCue(provenance),
            ResolveConfidenceCue(provenance),
            ResolveAgeCue(provenance),
            ResolveLastKnownCue(provenance, killChain),
            ResolveCommsCue(provenance),
            hasContactExplanation,
            engagementKey is not null,
            engagementKey);
    }

    private static string FormatSource(ContactProvenanceState? provenance) =>
        provenance is null
            ? "SRC: UNKNOWN — no active source record"
            : $"SRC: {provenance.Source.ObserverId} | ref {provenance.Source.SourceRef}";

    private static string FormatConfidence(ContactProvenanceState? provenance) =>
        provenance is null
            ? "CONF: UNKNOWN"
            : $"CONF: {provenance.Confidence.ToString().ToUpperInvariant()}";

    private static string FormatAge(ContactProvenanceState? provenance) =>
        provenance is null
            ? "AGE: UNKNOWN"
            : $"AGE: {provenance.AgeTicks.ToString(CultureInfo.InvariantCulture)} ticks | {provenance.Freshness.ToString().ToUpperInvariant()}";

    private static string FormatLastKnown(ContactProvenanceState? provenance, KillChainContactState? killChain)
    {
        if (provenance is null)
        {
            if (killChain?.Loss == KillChainLossKind.Lost)
            {
                return $"LAST-KNOWN: LOST @ T {killChain.LastSimTime.ToString("R", CultureInfo.InvariantCulture)}";
            }

            return "LAST-KNOWN: UNKNOWN";
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "LAST-KNOWN: {0} | target {1} | T {2} | tick {3}",
            provenance.LastKnown.LifecycleState,
            provenance.LastKnown.TargetId,
            provenance.LastKnown.LastSimTime.ToString("R", CultureInfo.InvariantCulture),
            provenance.LastKnown.LastSimTick);
    }

    private static string FormatComms(ContactProvenanceState? provenance) =>
        provenance is null
            ? "COMMS: UNKNOWN — reporting link state unavailable"
            : provenance.OutOfCommsUnknown
                ? "COMMS: DENIED — current state unknown"
                : (provenance.QualityState & ContactProvenanceQualityState.SilentComms) != 0
                    ? "COMMS: DEGRADED — silent comms reported"
                    : "COMMS: no degradation reported";

    private static string ResolveDeclutterToken(ContactProvenanceState? provenance)
    {
        if (provenance is null)
        {
            return ContactProvenanceDeclutterTokens.Unknown;
        }

        if (provenance.OutOfCommsUnknown)
        {
            return ContactProvenanceDeclutterTokens.CommsDenied;
        }

        if ((provenance.QualityState & ContactProvenanceQualityState.CatalogMiss) != 0)
        {
            return ContactProvenanceDeclutterTokens.CatalogMiss;
        }

        if (provenance.Freshness == ContactProvenanceFreshness.Stale
            || (provenance.QualityState & ContactProvenanceQualityState.Stale) != 0)
        {
            return ContactProvenanceDeclutterTokens.Stale;
        }

        return ContactProvenanceDeclutterTokens.Fresh;
    }

    private static string ResolveSourceCue(ContactProvenanceState? provenance) =>
        provenance is null ? ContactProvenanceCueClasses.Unknown : ContactProvenanceCueClasses.Nominal;

    private static string ResolveConfidenceCue(ContactProvenanceState? provenance) =>
        provenance?.Confidence switch
        {
            null => ContactProvenanceCueClasses.Unknown,
            ContactProvenanceConfidence.Unknown or ContactProvenanceConfidence.Low => ContactProvenanceCueClasses.Degraded,
            _ => ContactProvenanceCueClasses.Nominal,
        };

    private static string ResolveAgeCue(ContactProvenanceState? provenance)
    {
        if (provenance is null)
        {
            return ContactProvenanceCueClasses.Unknown;
        }

        return provenance.Freshness == ContactProvenanceFreshness.Stale
            || (provenance.QualityState & ContactProvenanceQualityState.Stale) != 0
            ? ContactProvenanceCueClasses.Degraded
            : ContactProvenanceCueClasses.Nominal;
    }

    private static string ResolveLastKnownCue(ContactProvenanceState? provenance, KillChainContactState? killChain)
    {
        if (killChain?.Loss == KillChainLossKind.Lost)
        {
            return ContactProvenanceCueClasses.Denied;
        }

        if (provenance is null)
        {
            return ContactProvenanceCueClasses.Unknown;
        }

        return provenance.Freshness == ContactProvenanceFreshness.Stale
            ? ContactProvenanceCueClasses.Degraded
            : ContactProvenanceCueClasses.Nominal;
    }

    private static string ResolveCommsCue(ContactProvenanceState? provenance)
    {
        if (provenance is null)
        {
            return ContactProvenanceCueClasses.Unknown;
        }

        if (provenance.OutOfCommsUnknown)
        {
            return ContactProvenanceCueClasses.Denied;
        }

        return (provenance.QualityState & ContactProvenanceQualityState.SilentComms) != 0
            ? ContactProvenanceCueClasses.Degraded
            : ContactProvenanceCueClasses.Nominal;
    }

    private static string? ResolveEngagementInspectionKey(
        string contactId,
        SliceAContactFrame frame,
        CombatPresentationFrame? combatFrame)
    {
        if (combatFrame is null || combatFrame.Events.Events.Count == 0)
        {
            return null;
        }

        var targetId = frame.KillChain.Contacts.FirstOrDefault(row =>
            string.Equals(row.ContactId, contactId, StringComparison.Ordinal))?.TargetId
            ?? frame.Provenance.Contacts.FirstOrDefault(row =>
                string.Equals(row.ContactId, contactId, StringComparison.Ordinal))?.Source.TargetId;
        if (string.IsNullOrEmpty(targetId))
        {
            return null;
        }

        CombatEvent? chosen = null;
        for (var i = combatFrame.Events.Events.Count - 1; i >= 0; i--)
        {
            var candidate = combatFrame.Events.Events[i];
            if (!string.Equals(candidate.TargetId, targetId, StringComparison.Ordinal))
            {
                continue;
            }

            chosen = candidate;
            break;
        }

        return chosen is null ? null : CombatMapPresenter.KeyFor(chosen);
    }
}
