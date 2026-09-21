using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>One datalink row for DRG-190 COMMS / network-health chrome.</summary>
public sealed record C2NetworkHealthLinkRowPresentation(
    string EndpointLine,
    string HealthLabel,
    string CapabilityLabel,
    string StalenessLine,
    string AffectedLine);

/// <summary>One last-known contributor frozen by partition or denial.</summary>
public sealed record C2NetworkHealthContributorRowPresentation(
    string UnitLine,
    string ContactLine,
    string LifecycleLine,
    string LastKnownLine);

/// <summary>One modeled path that no longer carries live datalink updates.</summary>
public sealed record C2NetworkHealthLostPathRowPresentation(
    string PathLine,
    string LastKnownLine);

/// <summary>
/// Immutable C2 network-health / COMMS chrome. Presentation-only; never fabricates live capability.
/// Build at tick boundaries, not every render frame (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record C2NetworkHealthPresentation(
    string HeaderLine,
    string AdvisoryBadge,
    string NetworkHealthLine,
    string CommsStateLine,
    string CommsNodeLine,
    string SummaryLine,
    string LinkCountLine,
    string ContributorCountLine,
    string LostPathCountLine,
    string NextActionLine,
    string Fingerprint,
    string CssClass,
    IReadOnlyList<C2NetworkHealthLinkRowPresentation> LinkRows,
    IReadOnlyList<C2NetworkHealthContributorRowPresentation> ContributorRows,
    IReadOnlyList<C2NetworkHealthLostPathRowPresentation> LostPathRows)
{
    /// <summary>Cleared presentation when network-health snapshot is unavailable.</summary>
    public static C2NetworkHealthPresentation Empty { get; } = new(
        C2NetworkHealthPresenter.HeaderLine,
        C2NetworkHealthPresenter.AdvisoryBadge,
        "Network: UNKNOWN",
        "COMMS: —",
        "Node: —",
        "Network health unavailable — bind a C2NetworkHealthSnapshot at tick boundaries.",
        "Links: —",
        "Last-known contributors: —",
        "Lost paths: —",
        "Obtain current network-health facts before relying on mesh topology.",
        "c2net:empty",
        "c2-comms--unknown",
        Array.Empty<C2NetworkHealthLinkRowPresentation>(),
        Array.Empty<C2NetworkHealthContributorRowPresentation>(),
        Array.Empty<C2NetworkHealthLostPathRowPresentation>());
}

/// <summary>Label bundle for COMMS / network-health panel hosts (DRG-190 bind path).</summary>
public sealed record C2NetworkHealthPanelLabels(
    string HeaderLine,
    string AdvisoryBadge,
    string NetworkHealthLine,
    string CommsStateLine,
    string CommsNodeLine,
    string SummaryLine,
    string LinkCountLine,
    string ContributorCountLine,
    string LostPathCountLine,
    string NextActionLine,
    string CssClass,
    IReadOnlyList<string> LinkLines,
    IReadOnlyList<string> ContributorLines,
    IReadOnlyList<string> LostPathLines);

/// <summary>Maps presentation rows to panel label text without re-deriving network facts.</summary>
public static class C2NetworkHealthPanelBinder
{
    /// <summary>Binds immutable presentation lines for UI Toolkit labels.</summary>
    public static C2NetworkHealthPanelLabels Bind(C2NetworkHealthPresentation presentation) =>
        new(
            presentation.HeaderLine,
            presentation.AdvisoryBadge,
            presentation.NetworkHealthLine,
            presentation.CommsStateLine,
            presentation.CommsNodeLine,
            presentation.SummaryLine,
            presentation.LinkCountLine,
            presentation.ContributorCountLine,
            presentation.LostPathCountLine,
            presentation.NextActionLine,
            presentation.CssClass,
            presentation.LinkRows.Select(C2NetworkHealthPresenter.FormatLinkLine).ToArray(),
            presentation.ContributorRows.Select(C2NetworkHealthPresenter.FormatContributorLine).ToArray(),
            presentation.LostPathRows.Select(C2NetworkHealthPresenter.FormatLostPathLine).ToArray());
}

/// <summary>Formats read-only <see cref="C2NetworkHealthSnapshot"/> for COMMS chrome.</summary>
public static class C2NetworkHealthPresenter
{
    public const string HeaderLine = "C2 NETWORK HEALTH";
    public const string AdvisoryBadge = "READ-ONLY · live capability never fabricated";

    /// <summary>Builds network-health chrome from a headless snapshot.</summary>
    public static C2NetworkHealthPresentation Build(C2NetworkHealthSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return C2NetworkHealthPresentation.Empty;
        }

        var linkRows = snapshot.Links.Select(FormatLinkRow).ToArray();
        var contributorRows = snapshot.LastKnownContributors.Select(FormatContributorRow).ToArray();
        var lostPathRows = snapshot.LostPaths.Select(FormatLostPathRow).ToArray();
        var fingerprint = C2NetworkHealthFingerprint.Compute(snapshot);

        return new C2NetworkHealthPresentation(
            HeaderLine,
            AdvisoryBadge,
            FormatNetworkHealthLine(snapshot.NetworkHealth),
            CommsStateProjection.FormatTopBar(snapshot.CommsState),
            FormatCommsNodeLine(snapshot.CommsNodeId),
            FormatSummaryLine(snapshot),
            FormatLinkCountLine(linkRows),
            FormatContributorCountLine(contributorRows),
            FormatLostPathCountLine(lostPathRows),
            NextAction(snapshot),
            fingerprint,
            ResolveCssClass(snapshot.NetworkHealth),
            linkRows,
            contributorRows,
            lostPathRows);
    }

    /// <summary>Compact single-line summary for top-bar / SensorC2 hosts.</summary>
    public static string FormatSummaryLine(C2NetworkHealthPresentation presentation)
    {
        if (ReferenceEquals(presentation, C2NetworkHealthPresentation.Empty))
        {
            return "Network: UNKNOWN — no snapshot bound";
        }

        return presentation.SummaryLine;
    }

    /// <summary>Single link row label for list binders.</summary>
    public static string FormatLinkLine(C2NetworkHealthLinkRowPresentation row)
    {
        var text = new StringBuilder()
            .Append(row.EndpointLine)
            .Append(" · ")
            .Append(row.HealthLabel)
            .Append(" · ")
            .Append(row.CapabilityLabel);
        if (!string.IsNullOrEmpty(row.StalenessLine))
        {
            text.Append(" · ").Append(row.StalenessLine);
        }

        if (!string.IsNullOrEmpty(row.AffectedLine))
        {
            text.Append(" · ").Append(row.AffectedLine);
        }

        return text.ToString();
    }

    /// <summary>Single contributor row label for list binders.</summary>
    public static string FormatContributorLine(C2NetworkHealthContributorRowPresentation row) =>
        $"{row.UnitLine} · {row.ContactLine} · {row.LifecycleLine} · {row.LastKnownLine}";

    /// <summary>Single lost-path row label for list binders.</summary>
    public static string FormatLostPathLine(C2NetworkHealthLostPathRowPresentation row) =>
        $"{row.PathLine} · {row.LastKnownLine}";

    private static string FormatSummaryLine(C2NetworkHealthSnapshot snapshot)
    {
        var partitioned = snapshot.Links.Count(l => l.Health == C2LinkHealth.Partitioned);
        var degraded = snapshot.Links.Count(l => l.Health == C2LinkHealth.Degraded);
        var lastKnown = snapshot.LastKnownContributors.Count;
        var text = new StringBuilder()
            .Append(FormatNetworkHealthLine(snapshot.NetworkHealth))
            .Append(" · ")
            .Append(CommsStateProjection.FormatTopBar(snapshot.CommsState));
        if (partitioned > 0)
        {
            text.Append(" · ").Append(partitioned.ToString(CultureInfo.InvariantCulture)).Append(" link(s) partitioned");
        }

        if (degraded > 0)
        {
            text.Append(" · ").Append(degraded.ToString(CultureInfo.InvariantCulture)).Append(" link(s) degraded");
        }

        if (lastKnown > 0)
        {
            text.Append(" · ").Append(lastKnown.ToString(CultureInfo.InvariantCulture)).Append(" last-known contributor(s)");
        }

        return text.ToString();
    }

    private static C2NetworkHealthLinkRowPresentation FormatLinkRow(C2NetworkLinkHealthEntry link)
    {
        var affected = link.AffectedContributorUnitIds.Count == 0
            ? string.Empty
            : $"affected: {string.Join(", ", link.AffectedContributorUnitIds)}";
        var staleness = link.StalenessTicks == 0
            ? string.Empty
            : $"staleness: {link.StalenessTicks} tick(s)";
        return new C2NetworkHealthLinkRowPresentation(
            $"{link.FromUnitId} ↔ {link.ToUnitId} ({link.LinkType})",
            FormatLinkHealthLabel(link.Health),
            link.IsLiveCapability ? "LIVE" : "LAST-KNOWN ONLY",
            staleness,
            affected);
    }

    private static C2NetworkHealthContributorRowPresentation FormatContributorRow(C2NetworkContributor contributor) =>
        new(
            $"Unit: {contributor.UnitId}",
            $"Contact: {contributor.ContactId} → {contributor.TargetId}",
            $"Lifecycle: {contributor.LifecycleState}",
            $"Last known @ tick {contributor.LastKnownSimTick}");

    private static C2NetworkHealthLostPathRowPresentation FormatLostPathRow(C2NetworkLostPath lostPath) =>
        new(
            $"Path: {lostPath.FromUnitId} ↔ {lostPath.ToUnitId} ({lostPath.LinkType})",
            $"Last known @ tick {lostPath.LastKnownSimTick}");

    private static string FormatNetworkHealthLine(C2NetworkHealthLevel level) =>
        $"Network: {FormatNetworkHealthLabel(level)}";

    private static string FormatCommsNodeLine(string nodeId) =>
        string.IsNullOrWhiteSpace(nodeId) ? "Node: —" : $"Node: {nodeId}";

    private static string FormatLinkCountLine(IReadOnlyList<C2NetworkHealthLinkRowPresentation> rows) =>
        rows.Count == 0 ? "Links: none" : $"Links: {rows.Count}";

    private static string FormatContributorCountLine(IReadOnlyList<C2NetworkHealthContributorRowPresentation> rows) =>
        rows.Count == 0
            ? "Last-known contributors: none (mesh live)"
            : $"Last-known contributors: {rows.Count}";

    private static string FormatLostPathCountLine(IReadOnlyList<C2NetworkHealthLostPathRowPresentation> rows) =>
        rows.Count == 0 ? "Lost paths: none" : $"Lost paths: {rows.Count}";

    private static string FormatNetworkHealthLabel(C2NetworkHealthLevel level) =>
        level switch
        {
            C2NetworkHealthLevel.Healthy => "HEALTHY",
            C2NetworkHealthLevel.Degraded => "DEGRADED",
            C2NetworkHealthLevel.Partitioned => "PARTITIONED",
            _ => "UNKNOWN",
        };

    private static string FormatLinkHealthLabel(C2LinkHealth health) =>
        health switch
        {
            C2LinkHealth.Healthy => "HEALTHY",
            C2LinkHealth.Degraded => "DEGRADED",
            C2LinkHealth.Partitioned => "PARTITIONED",
            _ => "UNKNOWN",
        };

    private static string ResolveCssClass(C2NetworkHealthLevel level) =>
        level switch
        {
            C2NetworkHealthLevel.Partitioned => "c2-comms--partitioned",
            C2NetworkHealthLevel.Degraded => "c2-comms--degraded",
            C2NetworkHealthLevel.Healthy => "c2-comms--healthy",
            _ => "c2-comms--unknown",
        };

    private static string NextAction(C2NetworkHealthSnapshot snapshot)
    {
        if (snapshot.CommsState == CommsState.Denied)
        {
            return "Global comms denied — rely on organic sensors only; shared tracks are last-known, not live.";
        }

        if (snapshot.NetworkHealth == C2NetworkHealthLevel.Partitioned)
        {
            var cutLinks = snapshot.Links.Count(l => l.Health == C2LinkHealth.Partitioned);
            return cutLinks > 0
                ? $"Inspect {cutLinks} partitioned link(s) and last-known contributors before treating shared tracks as current."
                : "Mesh is partitioned — verify which units are reachable before issuing coordinated orders.";
        }

        if (snapshot.NetworkHealth == C2NetworkHealthLevel.Degraded)
        {
            return "Mesh is degraded — peer shares may be stale; confirm track freshness before engaging.";
        }

        if (snapshot.LastKnownContributors.Count > 0)
        {
            return "Some contributors are frozen as last-known — do not treat their shares as live updates.";
        }

        return "Network mesh is healthy; datalink shares remain live across modeled links.";
    }
}
