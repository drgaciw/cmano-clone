namespace ProjectAegis.Delegation.SensorToShooter;

/// <summary>
/// Headless apply path for <see cref="SensorToShooterSnapshot"/> (DRG-181).
/// Unity hosts map presentation fields onto labels without re-formatting.
/// </summary>
public static class SensorToShooterApplyState
{
    /// <summary>
    /// Maps the selected contact's sensor→shooter chain into presentation label strings.
    /// Empty when <paramref name="snapshot"/> is null, <paramref name="selectedContactId"/> is
    /// null/empty, or the contact is not in the snapshot.
    /// </summary>
    public static SensorToShooterPresentation Apply(
        SensorToShooterSnapshot? snapshot,
        string? selectedContactId)
    {
        if (snapshot is null || string.IsNullOrEmpty(selectedContactId))
        {
            return SensorToShooterPresentation.Empty;
        }

        SensorToShooterChain? chain = null;
        for (var i = 0; i < snapshot.Chains.Count; i++)
        {
            if (string.Equals(snapshot.Chains[i].ContactId, selectedContactId, StringComparison.Ordinal))
            {
                chain = snapshot.Chains[i];
                break;
            }
        }

        if (chain is null)
        {
            return SensorToShooterPresentation.Empty;
        }

        var sensor = FindLink(chain, SensorToShooterLinkKind.Sensor);
        var track = FindLink(chain, SensorToShooterLinkKind.Track);
        var targetability = FindLink(chain, SensorToShooterLinkKind.Targetability);
        var shooter = FindLink(chain, SensorToShooterLinkKind.EligibleShooter);

        return new SensorToShooterPresentation(
            ContactIdLine: $"CONTACT: {chain.ContactId}",
            CompleteLine: chain.IsComplete ? "CHAIN: COMPLETE" : "CHAIN: BROKEN",
            PrimaryCauseLine: FormatCause(chain),
            SensorLinkLine: FormatLink("SENSOR", sensor),
            TrackLinkLine: FormatLink("TRACK", track),
            TargetabilityLinkLine: FormatLink("TARGETABILITY", targetability),
            ShooterLinkLine: FormatLink("SHOOTER", shooter),
            ExplainLinkLine: $"EXPLAIN: engage/{chain.ContactId}");
    }

    private static SensorToShooterChainLink? FindLink(
        SensorToShooterChain chain,
        SensorToShooterLinkKind kind)
    {
        for (var i = 0; i < chain.Links.Count; i++)
        {
            if (chain.Links[i].Kind == kind)
            {
                return chain.Links[i];
            }
        }

        return null;
    }

    private static string FormatCause(SensorToShooterChain chain)
    {
        if (chain.IsComplete || chain.PrimaryBreakCause == SensorToShooterBreakCause.None)
        {
            return "CAUSE: —";
        }

        var label = chain.PrimaryCauseLabel;
        return string.IsNullOrEmpty(label) ? "CAUSE: —" : $"CAUSE: {label}";
    }

    private static string FormatLink(string kindLabel, SensorToShooterChainLink? link)
    {
        if (link is null)
        {
            return $"{kindLabel}: —";
        }

        if (link.IsLinked)
        {
            return string.IsNullOrEmpty(link.UnitId)
                ? $"{kindLabel}: LINKED"
                : $"{kindLabel}: LINKED {link.UnitId}";
        }

        var cause = link.CauseLabel;
        return string.IsNullOrEmpty(cause)
            ? $"{kindLabel}: BROKEN"
            : $"{kindLabel}: BROKEN {cause}";
    }
}

/// <summary>Applied sensor-to-shooter presentation fields (label text bags; no color).</summary>
public sealed record SensorToShooterPresentation(
    string ContactIdLine,
    string CompleteLine,
    string PrimaryCauseLine,
    string SensorLinkLine,
    string TrackLinkLine,
    string TargetabilityLinkLine,
    string ShooterLinkLine,
    string ExplainLinkLine)
{
    public static SensorToShooterPresentation Empty { get; } = new(
        "CONTACT: —",
        "CHAIN: —",
        "CAUSE: —",
        "SENSOR: —",
        "TRACK: —",
        "TARGETABILITY: —",
        "SHOOTER: —",
        "EXPLAIN: —");
}
