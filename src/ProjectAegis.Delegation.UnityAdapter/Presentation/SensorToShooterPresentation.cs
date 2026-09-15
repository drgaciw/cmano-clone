using ProjectAegis.Delegation.SensorToShooter;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>One inspectable link row in the sensor-to-shooter chain (DRG-181).</summary>
public sealed record SensorToShooterLinkPresentation(
    string KindLabel,
    string StatusLabel,
    string UnitLine,
    string CauseLine,
    string DetailLine);

/// <summary>
/// Dedicated sensor → track → targetability → shooter chrome. Presentation-only; no fire orders.
/// Build at tick/selection boundaries, not every render frame (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record SensorToShooterPresentation(
    string ContactIdLine,
    string TargetIdLine,
    string ObserverIdLine,
    string StatusLine,
    string SensorLine,
    string TrackLine,
    string TargetabilityLine,
    string ShooterLine,
    string NextActionLine,
    string Fingerprint,
    IReadOnlyList<SensorToShooterLinkPresentation> Links)
{
    /// <summary>Cleared presentation for absent selection or unavailable chain facts.</summary>
    public static SensorToShooterPresentation Empty { get; } = new(
        "Contact: —",
        "Target: —",
        "Observer: —",
        "Chain: UNKNOWN",
        "Sensor: UNKNOWN",
        "Track: UNKNOWN",
        "Targetability: UNKNOWN",
        "Shooter: UNKNOWN",
        "Select a contact to inspect the sensor-to-shooter chain.",
        "sts:empty",
        Array.Empty<SensorToShooterLinkPresentation>());
}

/// <summary>Formats read-only <see cref="SensorToShooterSnapshot"/> chains for UI chrome.</summary>
public static class SensorToShooterPresenter
{
    private static readonly SensorToShooterLinkKind[] LinkOrder =
    {
        SensorToShooterLinkKind.Sensor,
        SensorToShooterLinkKind.Track,
        SensorToShooterLinkKind.Targetability,
        SensorToShooterLinkKind.EligibleShooter,
    };

    /// <summary>
    /// Builds chain inspection text for <paramref name="contactId"/>.
    /// When <paramref name="eligibilityAvailable"/> is false, shooter facts are withheld (fail closed).
    /// </summary>
    public static SensorToShooterPresentation Build(
        string? contactId,
        SensorToShooterSnapshot? snapshot,
        bool eligibilityAvailable)
    {
        if (string.IsNullOrWhiteSpace(contactId))
        {
            return SensorToShooterPresentation.Empty;
        }

        if (!eligibilityAvailable || snapshot is null || snapshot.Chains.Count == 0)
        {
            return Unknown(contactId, snapshot);
        }

        var chain = snapshot.Chains.FirstOrDefault(row =>
            string.Equals(row.ContactId, contactId, StringComparison.Ordinal));
        if (chain is null)
        {
            return Unknown(contactId, snapshot);
        }

        var fingerprint = SensorToShooterProjection.ComputeFingerprint(
            new SensorToShooterSnapshot(new[] { chain }));
        var links = BuildLinks(chain);
        return new SensorToShooterPresentation(
            $"Contact: {chain.ContactId}",
            $"Target: {chain.TargetId}",
            $"Observer: {chain.ObserverId}",
            chain.IsComplete
                ? "Chain: COMPLETE (technical eligibility only — not release authority)"
                : $"Chain: BROKEN — {chain.PrimaryCauseLabel}",
            FormatLinkLine(links[0]),
            FormatLinkLine(links[1]),
            FormatLinkLine(links[2]),
            FormatLinkLine(links[3]),
            NextAction(chain),
            fingerprint,
            links);
    }

    private static SensorToShooterPresentation Unknown(string contactId, SensorToShooterSnapshot? snapshot)
    {
        var fingerprint = snapshot is null || snapshot.Chains.Count == 0
            ? "sts:empty"
            : SensorToShooterProjection.ComputeFingerprint(snapshot);
        return new SensorToShooterPresentation(
            $"Contact: {contactId}",
            "Target: UNKNOWN",
            "Observer: UNKNOWN",
            "Chain: UNKNOWN — sensor-to-shooter facts unavailable",
            "Sensor: UNKNOWN",
            "Track: UNKNOWN",
            "Targetability: UNKNOWN",
            "Shooter: UNKNOWN",
            "Obtain current sensor-to-shooter facts before relying on technical eligibility.",
            fingerprint,
            Array.Empty<SensorToShooterLinkPresentation>());
    }

    private static IReadOnlyList<SensorToShooterLinkPresentation> BuildLinks(SensorToShooterChain chain)
    {
        var rows = new SensorToShooterLinkPresentation[LinkOrder.Length];
        for (var i = 0; i < LinkOrder.Length; i++)
        {
            var kind = LinkOrder[i];
            var link = chain.Links.FirstOrDefault(item => item.Kind == kind);
            rows[i] = link is null
                ? new SensorToShooterLinkPresentation(
                    KindLabel(kind),
                    "UNKNOWN",
                    string.Empty,
                    string.Empty,
                    string.Empty)
                : new SensorToShooterLinkPresentation(
                    KindLabel(kind),
                    link.IsLinked ? "LINKED" : "BROKEN",
                    string.IsNullOrEmpty(link.UnitId) ? string.Empty : link.UnitId,
                    link.IsLinked ? string.Empty : link.CauseLabel,
                    link.Detail ?? string.Empty);
        }

        return rows;
    }

    private static string FormatLinkLine(SensorToShooterLinkPresentation link)
    {
        var text = $"{link.KindLabel}: {link.StatusLabel}";
        if (!string.IsNullOrEmpty(link.UnitLine))
        {
            text += $" | {link.UnitLine}";
        }

        if (!string.IsNullOrEmpty(link.CauseLine))
        {
            text += $" | {link.CauseLine}";
        }

        if (!string.IsNullOrEmpty(link.DetailLine))
        {
            text += $" | {link.DetailLine}";
        }

        return text;
    }

    private static string KindLabel(SensorToShooterLinkKind kind) =>
        kind switch
        {
            SensorToShooterLinkKind.EligibleShooter => "Shooter",
            _ => kind.ToString(),
        };

    private static string NextAction(SensorToShooterChain chain)
    {
        if (chain.IsComplete)
        {
            return "Technical chain is complete. Review links and submit intent through the command workflow; this panel does not issue fire orders.";
        }

        return chain.PrimaryBreakCause switch
        {
            SensorToShooterBreakCause.LostSensor =>
                "Reacquire the contact with a reporting sensor; last-known data is not a firing solution.",
            SensorToShooterBreakCause.StaleTrack =>
                "Refresh the sensor report and revalidate track custody before targeting.",
            SensorToShooterBreakCause.NoFireControl =>
                "Acquire a fire-control-quality track from an eligible sensor.",
            SensorToShooterBreakCause.NoEligibleShooter =>
                "Inspect shooter readiness, weapons envelope, range, and available ammunition.",
            SensorToShooterBreakCause.DegradedTrack =>
                "Improve track quality and revalidate technical targetability.",
            _ => "Inspect and restore the broken chain links before targeting.",
        };
    }
}
