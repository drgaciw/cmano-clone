using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>One composable C2 element row for DRG-189 inspection chrome.</summary>
public sealed record C2NodeElementRowPresentation(
    string ElementId,
    string PlatformUnitId,
    string RoleLabel,
    string AvailabilityLabel,
    string PackageId,
    string PackageLabel,
    string MembershipKindLabel,
    string CapabilityScope,
    bool TaskOrgDetached,
    ulong LastSimTick,
    double LastSimTime,
    string CorrelationSequenceLabel,
    IReadOnlyList<string> SourceRefs,
    string DisplayLine);

/// <summary>Package roll-up row consuming every <see cref="MissionPackageMembership"/> field.</summary>
public sealed record MissionPackageRollupPresentation(
    string PackageId,
    string PackageLabel,
    IReadOnlyList<string> ElementIds,
    IReadOnlyList<string> UnitIds,
    string SummaryLine);

/// <summary>
/// Mission-package / composable C2-node inspection chrome. Presentation-only; never issues orders.
/// Build at tick/selection boundaries, not every render frame (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record MissionPackagePresentation(
    string HeaderLine,
    string AdvisoryBadge,
    string ActivePackageLine,
    string SummaryLine,
    string Fingerprint,
    string NextActionLine,
    IReadOnlyList<MissionPackageRollupPresentation> Packages,
    IReadOnlyList<C2NodeElementRowPresentation> Elements)
{
    /// <summary>Cleared presentation when no authored package snapshot is available.</summary>
    public static MissionPackagePresentation Empty { get; } = new(
        MissionPackagePresenter.HeaderLine,
        MissionPackagePresenter.AdvisoryBadge,
        "Active package: —",
        "Packages: 0 · Elements: 0",
        "pkg:empty",
        "Select a unit or load a scenario with authored mission packages to inspect C2 nodes.",
        Array.Empty<MissionPackageRollupPresentation>(),
        Array.Empty<C2NodeElementRowPresentation>());
}

/// <summary>Label bundle for mission-package panel hosts (DRG-189 bind path).</summary>
public sealed record MissionPackagePanelLabels(
    string HeaderLine,
    string AdvisoryBadge,
    string ActivePackageLine,
    string SummaryLine,
    string NextActionLine,
    IReadOnlyList<string> PackageLines,
    IReadOnlyList<string> ElementLines);

/// <summary>Maps presentation rows to panel label text without re-deriving package facts.</summary>
public static class MissionPackagePanelBinder
{
    /// <summary>Binds immutable presentation lines for UI Toolkit labels and list views.</summary>
    public static MissionPackagePanelLabels Bind(MissionPackagePresentation presentation) =>
        new(
            presentation.HeaderLine,
            presentation.AdvisoryBadge,
            presentation.ActivePackageLine,
            presentation.SummaryLine,
            presentation.NextActionLine,
            presentation.Packages.Select(static rollup => rollup.SummaryLine).ToArray(),
            presentation.Elements.Select(static element => element.DisplayLine).ToArray());
}

/// <summary>
/// Resolves replay-stable mission-package snapshots for presentation hosts.
/// Read-only; does not append to the order log.
/// </summary>
public static class MissionPackagePresentationSource
{
    /// <summary>Projects authored packages using the same bounded log semantics as coordination review.</summary>
    public static MissionPackageSnapshot Project(
        DelegationBridge? bridge,
        ISimWorldSnapshot? snapshot,
        IReadOnlyList<PackageDefinition>? packages = null)
    {
        if (bridge is null || snapshot is null)
        {
            return MissionPackageSnapshot.Empty;
        }

        packages ??= Array.Empty<PackageDefinition>();
        var boundedLog = BoundLogAt(bridge.Orchestrator.DecisionLog, snapshot.SimTime);
        return MissionPackageProjection.Project(
            packages,
            boundedLog,
            unitId => snapshot.IsMemberAlive(new TargetId(unitId)),
            currentSimTick: (ulong)Math.Max(0, (long)snapshot.SimTime),
            currentSimTime: snapshot.SimTime);
    }

    private static Decision.DecisionLog BoundLogAt(
        Decision.DecisionLog source,
        double simTime)
    {
        var entries = source.ChronologicalEntries();
        if (!entries.Any(entry => entry.SimTime > simTime))
        {
            return source;
        }

        var bounded = new Decision.DecisionLog();
        foreach (var entry in entries)
        {
            if (entry.SimTime <= simTime)
            {
                bounded.Append(entry);
            }
        }

        return bounded;
    }
}

/// <summary>Formats read-only <see cref="MissionPackageSnapshot"/> rows for UI chrome.</summary>
public static class MissionPackagePresenter
{
    public const string HeaderLine = "MISSION PACKAGE / C2 NODES";
    public const string AdvisoryBadge = "INSPECT ONLY · IsOrder=false";

    /// <summary>
    /// Builds package inspection chrome. When <paramref name="selectedUnitId"/> is set, element rows
    /// are filtered to that platform while package roll-ups remain complete.
    /// </summary>
    public static MissionPackagePresentation Build(
        MissionPackageSnapshot? snapshot,
        string? selectedUnitId = null,
        string? selectedPackageId = null)
    {
        if (snapshot is null
            || (snapshot.Elements.Count == 0 && snapshot.Packages.Count == 0))
        {
            return MissionPackagePresentation.Empty;
        }

        var resolvedPackageId = !string.IsNullOrWhiteSpace(selectedPackageId)
            ? selectedPackageId
            : snapshot.ActivePackageId;
        var packageRollups = snapshot.Packages
            .OrderBy(static package => package.PackageId, StringComparer.Ordinal)
            .Select(FormatPackageRollup)
            .ToArray();
        var elements = snapshot.Elements
            .Where(element => MatchesSelection(element, selectedUnitId, resolvedPackageId))
            .OrderBy(static element => element.ElementId, StringComparer.Ordinal)
            .Select(FormatElementRow)
            .ToArray();
        var activePackage = snapshot.Packages.FirstOrDefault(package =>
            string.Equals(package.PackageId, resolvedPackageId, StringComparison.Ordinal));
        var unavailableCount = elements.Count(element =>
            string.Equals(element.AvailabilityLabel, "UNAVAILABLE", StringComparison.Ordinal));
        var lastKnownCount = elements.Count(element =>
            string.Equals(element.AvailabilityLabel, "LAST-KNOWN", StringComparison.Ordinal));

        return new MissionPackagePresentation(
            HeaderLine,
            AdvisoryBadge,
            FormatActivePackageLine(resolvedPackageId, activePackage),
            FormatSummaryLine(snapshot, elements.Length, unavailableCount, lastKnownCount, selectedUnitId),
            MissionPackageProjection.ComputeFingerprint(snapshot),
            NextAction(elements, unavailableCount, lastKnownCount, selectedUnitId),
            packageRollups,
            elements);
    }

    /// <summary>Compact single-line summary for drawer hosts and command review cross-links.</summary>
    public static string FormatSummaryLine(MissionPackagePresentation presentation)
    {
        if (ReferenceEquals(presentation, MissionPackagePresentation.Empty))
        {
            return "Mission package: UNKNOWN — no projection bound";
        }

        return $"{presentation.ActivePackageLine} · {presentation.SummaryLine} · {presentation.Fingerprint}";
    }

    private static bool MatchesSelection(
        C2NodeElement element,
        string? selectedUnitId,
        string? selectedPackageId)
    {
        if (!string.IsNullOrWhiteSpace(selectedUnitId)
            && !string.Equals(element.PlatformUnitId, selectedUnitId, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(selectedPackageId)
            && !string.Equals(element.Membership.PackageId, selectedPackageId, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static MissionPackageRollupPresentation FormatPackageRollup(MissionPackageMembership package) =>
        new(
            package.PackageId,
            package.PackageLabel,
            package.ElementIds,
            package.UnitIds,
            $"PKG {package.PackageId} · {package.PackageLabel} · elements {package.ElementIds.Count} · units {package.UnitIds.Count}");

    private static C2NodeElementRowPresentation FormatElementRow(C2NodeElement element)
    {
        var sourceRefs = element.SourceRefs
            .OrderBy(static reference => reference, StringComparer.Ordinal)
            .ToArray();
        var correlationLabel = element.CorrelationSequenceId is null
            ? "seq: —"
            : $"seq: {element.CorrelationSequenceId.Value.ToString(CultureInfo.InvariantCulture)}";
        var displayLine = new StringBuilder()
            .Append(element.ElementId)
            .Append(" · ")
            .Append(element.Role)
            .Append(" · ")
            .Append(element.PlatformUnitId)
            .Append(" · ")
            .Append(FormatAvailabilityLabel(element.Availability))
            .Append(" · ")
            .Append(element.Membership.PackageId)
            .Append(" · ")
            .Append(FormatMembershipKindLabel(element.Membership.Kind))
            .Append(" · ")
            .Append(element.CapabilityScope)
            .Append(element.TaskOrgDetached ? " · DETACHED" : string.Empty)
            .Append(" · tick ")
            .Append(element.LastSimTick.ToString(CultureInfo.InvariantCulture))
            .Append(" · ")
            .Append(element.LastSimTime.ToString("R", CultureInfo.InvariantCulture))
            .Append(" · ")
            .Append(correlationLabel)
            .Append(" · refs ")
            .Append(string.Join("+", sourceRefs))
            .ToString();

        return new C2NodeElementRowPresentation(
            element.ElementId,
            element.PlatformUnitId,
            element.Role.ToString().ToUpperInvariant(),
            FormatAvailabilityLabel(element.Availability),
            element.Membership.PackageId,
            element.Membership.PackageLabel,
            FormatMembershipKindLabel(element.Membership.Kind),
            element.CapabilityScope,
            element.TaskOrgDetached,
            element.LastSimTick,
            element.LastSimTime,
            correlationLabel,
            sourceRefs,
            displayLine);
    }

    private static string FormatActivePackageLine(
        string? activePackageId,
        MissionPackageMembership? activePackage) =>
        activePackage is null
            ? $"Active package: {activePackageId ?? "—"}"
            : $"Active package: {activePackage.PackageId} · {activePackage.PackageLabel}";

    private static string FormatSummaryLine(
        MissionPackageSnapshot snapshot,
        int visibleElementCount,
        int unavailableCount,
        int lastKnownCount,
        string? selectedUnitId)
    {
        var builder = new StringBuilder()
            .Append("Packages: ")
            .Append(snapshot.Packages.Count)
            .Append(" · Elements: ")
            .Append(visibleElementCount)
            .Append(" · unavailable ")
            .Append(unavailableCount)
            .Append(" · last-known ")
            .Append(lastKnownCount);
        if (!string.IsNullOrWhiteSpace(selectedUnitId))
        {
            builder.Append(" · unit ").Append(selectedUnitId);
        }

        return builder.ToString();
    }

    private static string FormatAvailabilityLabel(C2NodeAvailability availability) =>
        availability switch
        {
            C2NodeAvailability.Available => "AVAILABLE",
            C2NodeAvailability.Unavailable => "UNAVAILABLE",
            _ => "LAST-KNOWN",
        };

    private static string FormatMembershipKindLabel(C2NodeMembershipKind kind) =>
        kind switch
        {
            C2NodeMembershipKind.Organic => "ORGANIC",
            _ => "PACKAGE",
        };

    private static string NextAction(
        IReadOnlyList<C2NodeElementRowPresentation> elements,
        int unavailableCount,
        int lastKnownCount,
        string? selectedUnitId)
    {
        if (elements.Count == 0)
        {
            return string.IsNullOrWhiteSpace(selectedUnitId)
                ? "No composable C2 elements are visible for the current filter."
                : $"Unit {selectedUnitId} has no authored package elements in the active snapshot.";
        }

        if (unavailableCount > 0)
        {
            return "Review unavailable nodes before relying on package composition; this panel does not restore capability.";
        }

        if (lastKnownCount > 0)
        {
            return "Some nodes are last-known only — refresh comms and damage evidence before tasking.";
        }

        return "Package composition is available for inspection; submit intent through the command workflow.";
    }
}
