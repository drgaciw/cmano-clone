using System.Text;
using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.TaskGroupCoord;

namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

/// <summary>Readiness status for one layered defense role (DRG-192).</summary>
public enum CoverageLayerStatus
{
    /// <summary>Availability or role assignment is unknown / last-known only.</summary>
    Unknown = 0,

    /// <summary>All elements in this defense layer are available.</summary>
    Intact = 1,

    /// <summary>At least one element in this layer is lost or detached while others remain available.</summary>
    Degraded = 2,

    /// <summary>All elements in this defense layer are lost or detached.</summary>
    Lost = 3,
}

/// <summary>Overall mission-package coverage posture severity for a task group.</summary>
public enum CoveragePostureSeverity
{
    /// <summary>No package assigned or coverage facts are unknown.</summary>
    Unknown = 0,

    /// <summary>Package C2 and authored coverage layers are intact.</summary>
    Nominal = 1,

    /// <summary>One or more elements/sectors are degraded, detached, or split.</summary>
    Degraded = 2,

    /// <summary>C2 is lost or all coverage collapsed to gap.</summary>
    Critical = 3,
}

/// <summary>Direction of posture change between a baseline and current or hypothetical snapshot.</summary>
public enum CoveragePostureDeltaKind
{
    /// <summary>Baseline or current snapshot is unavailable.</summary>
    Unknown = 0,

    /// <summary>Posture remained unchanged.</summary>
    Unchanged = 1,

    /// <summary>Coverage gaps were reduced or C2/split posture recovered.</summary>
    Improved = 2,

    /// <summary>New gaps or detached/lost elements degraded coverage posture.</summary>
    Degraded = 3,

    /// <summary>C2 was lost or all covered sectors collapsed to gap.</summary>
    Collapsed = 4,
}

/// <summary>Hypothetical element state change evaluated without mutating live simulation state.</summary>
public enum HypotheticalElementChangeKind
{
    /// <summary>Simulated destruction or loss of the unit.</summary>
    Loss = 0,

    /// <summary>Simulated detachment or reassignment of the unit to another group.</summary>
    DetachOrReassignment = 1,
}

/// <summary>Per-role layered defense accounting for one task group.</summary>
public sealed record CoverageLayerSummary(
    string RoleLabel,
    C2NodeRole? Role,
    CoverageLayerStatus Status,
    int TotalElements,
    int AvailableCount,
    int DetachedCount,
    int LostCount,
    int LastKnownCount,
    int CoveredCount,
    int GapCount,
    int UnknownCoverageCount,
    string Detail);

/// <summary>Explicit unprotected or gap area within a mission-package layer.</summary>
public sealed record CoverageGapEntry(
    string GroupId,
    string UnitId,
    string ElementId,
    C2NodeRole? Role,
    string CoverageId,
    string CoverageLabel,
    string ReasonCode,
    bool HasGeometry,
    string Detail);

/// <summary>Explicit authority and command-boundary disclosure for coverage posture views (ADR-010).</summary>
public sealed record CoverageAuthorityBoundary(
    bool IsOrder,
    bool IsWeaponsReleaseAuthorization,
    bool IsFireOrder,
    bool IsAutomaticEngagement,
    bool HasDetachedUnits,
    bool GroupCommandsBlockedByMissingMember,
    IReadOnlyList<string> ActiveConstraints,
    string Summary);

/// <summary>Projected layered defense and gap posture for one task group.</summary>
public sealed record CoverageGroupPosture(
    string GroupId,
    string PackageId,
    string PackageLabel,
    string GapCode,
    CoveragePostureSeverity Severity,
    int CoveredCount,
    int GapCount,
    int UnknownCount,
    int UsablePolygonCount,
    IReadOnlyList<CoverageLayerSummary> Layers,
    IReadOnlyList<CoverageGapEntry> UnprotectedGaps,
    IReadOnlyList<CoordinationGap> CoordinationGaps,
    CoverageAuthorityBoundary AuthorityBoundary,
    string SummaryLine);

/// <summary>Aggregated mission-package coverage posture across all task groups.</summary>
public sealed record CoveragePostureSnapshot(
    IReadOnlyList<CoverageGroupPosture> Groups,
    int TotalCovered,
    int TotalGaps,
    int TotalUnknown,
    int TotalUsablePolygons,
    bool HasGaps,
    string CompactSummary)
{
    /// <summary>Empty posture snapshot when no task-group facts are present.</summary>
    public static CoveragePostureSnapshot Empty { get; } = new(
        Array.Empty<CoverageGroupPosture>(),
        0,
        0,
        0,
        0,
        HasGaps: false,
        CompactSummary: "POSTURE: no task-group coverage facts");
}

/// <summary>Single unit/coverage transition between baseline and current snapshots.</summary>
public sealed record CoverageTransitionItem(
    string UnitId,
    string ElementId,
    C2NodeRole? Role,
    string CoverageId,
    string CoverageLabel,
    CoverageStatus PreviousStatus,
    CoverageStatus CurrentStatus,
    CoordinationEffectState PreviousState,
    CoordinationEffectState CurrentState,
    string Summary);

/// <summary>Posture change explanation for a single task group.</summary>
public sealed record CoverageGroupPostureDiff(
    string GroupId,
    CoveragePostureDeltaKind DeltaKind,
    string PreviousGapCode,
    string CurrentGapCode,
    int PreviousCoveredCount,
    int CurrentCoveredCount,
    int PreviousGapCount,
    int CurrentGapCount,
    IReadOnlyList<C2NodeRole> DegradedOrLostRoles,
    IReadOnlyList<string> LostOrDetachedUnitIds,
    IReadOnlyList<string> RestoredUnitIds,
    IReadOnlyList<CoverageTransitionItem> CoverageTransitions,
    string Headline,
    IReadOnlyList<string> ExplanationLines);

/// <summary>Posture change comparison across task groups, either observed or hypothetical.</summary>
public sealed record CoveragePostureDiff(
    bool IsHypothetical,
    string? HypotheticalSubjectUnitId,
    HypotheticalElementChangeKind? HypotheticalChangeKind,
    string? ReassignmentTargetGroupId,
    CoveragePostureDeltaKind OverallDelta,
    IReadOnlyList<CoverageGroupPostureDiff> GroupDiffs,
    string Headline,
    IReadOnlyList<string> ExplanationLines)
{
    /// <summary>Empty unchanged posture diff.</summary>
    public static CoveragePostureDiff Empty { get; } = new(
        IsHypothetical: false,
        HypotheticalSubjectUnitId: null,
        HypotheticalChangeKind: null,
        ReassignmentTargetGroupId: null,
        OverallDelta: CoveragePostureDeltaKind.Unchanged,
        GroupDiffs: Array.Empty<CoverageGroupPostureDiff>(),
        Headline: "POSTURE UNCHANGED",
        ExplanationLines: Array.Empty<string>());
}

/// <summary>
/// Pure headless projection computing layered defense coverage, gap accounting, authority disclosure,
/// baseline-vs-current posture diffs, and non-mutating what-if reassignment/loss simulations (DRG-192).
/// </summary>
public static class CoveragePostureProjection
{
    private static readonly C2NodeRole?[] LayerOrder =
    {
        C2NodeRole.C2,
        C2NodeRole.Sensor,
        C2NodeRole.Shooter,
        C2NodeRole.Relay,
        null,
    };

    /// <summary>Projects layered defense posture from <paramref name="snapshot"/>.</summary>
    public static CoveragePostureSnapshot Project(CoordinationSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Groups.Count == 0)
        {
            return CoveragePostureSnapshot.Empty;
        }

        var groups = snapshot.Groups
            .Where(g => g is not null)
            .OrderBy(g => g.Coordination?.GroupId ?? string.Empty, StringComparer.Ordinal)
            .Select(ProjectGroup)
            .ToArray();

        if (groups.Length == 0)
        {
            return CoveragePostureSnapshot.Empty;
        }

        var totalCovered = groups.Sum(g => g.CoveredCount);
        var totalGaps = groups.Sum(g => g.GapCount);
        var totalUnknown = groups.Sum(g => g.UnknownCount);
        var totalPolygons = groups.Sum(g => g.UsablePolygonCount);
        var hasGaps = totalGaps > 0 || groups.Any(g => g.UnprotectedGaps.Count > 0 || g.CoordinationGaps.Count > 0);
        var compact = $"POSTURE: {groups.Length} group(s) | {totalCovered} covered | {totalGaps} gap(s) | {totalUnknown} unknown";

        return new CoveragePostureSnapshot(
            groups,
            totalCovered,
            totalGaps,
            totalUnknown,
            totalPolygons,
            hasGaps,
            compact);
    }

    /// <summary>Explains posture changes between <paramref name="baseline"/> and <paramref name="current"/>.</summary>
    public static CoveragePostureDiff Compare(
        CoordinationSnapshot? baseline,
        CoordinationSnapshot? current)
    {
        if (baseline is null || current is null)
        {
            return CoveragePostureDiff.Empty with { OverallDelta = CoveragePostureDeltaKind.Unknown, Headline = "POSTURE DIFF UNAVAILABLE" };
        }

        var baselineByGroup = IndexGroupsById(baseline.Groups);
        var currentByGroup = IndexGroupsById(current.Groups);

        var allGroupIds = baselineByGroup.Keys
            .Union(currentByGroup.Keys, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        var groupDiffs = new List<CoverageGroupPostureDiff>(allGroupIds.Length);
        var allLines = new List<string>();
        foreach (var groupId in allGroupIds)
        {
            baselineByGroup.TryGetValue(groupId, out var prev);
            currentByGroup.TryGetValue(groupId, out var curr);
            var diff = CompareGroup(groupId, prev, curr);
            groupDiffs.Add(diff);
            allLines.AddRange(diff.ExplanationLines);
        }

        var overall = ResolveOverallDelta(groupDiffs);
        var headline = groupDiffs.FirstOrDefault(d => d.DeltaKind != CoveragePostureDeltaKind.Unchanged)?.Headline
            ?? "POSTURE UNCHANGED";

        return new CoveragePostureDiff(
            IsHypothetical: false,
            HypotheticalSubjectUnitId: null,
            HypotheticalChangeKind: null,
            ReassignmentTargetGroupId: null,
            OverallDelta: overall,
            GroupDiffs: groupDiffs,
            Headline: headline,
            ExplanationLines: allLines);
    }

    /// <summary>Non-mutatively simulates unit loss or reassignment/detachment against <paramref name="baseline"/>.</summary>
    public static CoveragePostureDiff SimulateElementChange(
        CoordinationSnapshot? baseline,
        string groupId,
        string unitId,
        HypotheticalElementChangeKind changeKind,
        string? targetGroupId = null)
    {
        if (baseline is null || string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(unitId))
        {
            return CoveragePostureDiff.Empty with { IsHypothetical = true, OverallDelta = CoveragePostureDeltaKind.Unknown };
        }

        var foundGroup = false;
        var unitExistsInGroup = false;
        var updatedGroups = new List<CoordinationGroupSnapshot>(baseline.Groups.Count);
        foreach (var group in baseline.Groups)
        {
            if (group is null)
            {
                continue;
            }

            if (group.Coordination is null || !string.Equals(group.Coordination.GroupId, groupId, StringComparison.Ordinal))
            {
                updatedGroups.Add(group);
                continue;
            }

            foundGroup = true;
            unitExistsInGroup = ContainsUnit(group, unitId);
            updatedGroups.Add(unitExistsInGroup
                ? BuildHypotheticalGroup(group, unitId, changeKind)
                : group);
        }

        var hypothetical = new CoordinationSnapshot(updatedGroups);
        var diff = Compare(baseline, hypothetical);
        var lines = diff.ExplanationLines.ToList();
        if (!foundGroup)
        {
            lines.Add($"No posture change: group '{groupId}' was not found in baseline (non-mutating projection; IsOrder=false).");
        }
        else if (!unitExistsInGroup)
        {
            lines.Add($"No posture change: unit '{unitId}' is not present in group '{groupId}' (non-mutating projection; IsOrder=false).");
        }
        else if (!string.IsNullOrWhiteSpace(targetGroupId))
        {
            lines.Add($"Hypothetical reassignment of {unitId} to {targetGroupId} (non-mutating projection; IsOrder=false).");
        }

        return diff with
        {
            IsHypothetical = true,
            HypotheticalSubjectUnitId = unitId,
            HypotheticalChangeKind = changeKind,
            ReassignmentTargetGroupId = targetGroupId,
            ExplanationLines = lines,
        };
    }

    private static CoverageGroupPosture ProjectGroup(CoordinationGroupSnapshot group)
    {
        var coord = group.Coordination;
        var intent = group.Intent;
        var effects = group.Effects ?? Array.Empty<CoordinationEffect>();
        var coordGaps = group.Gaps ?? Array.Empty<CoordinationGap>();

        var groupId = coord?.GroupId ?? string.Empty;
        var packageId = coord?.AssignedPackageId ?? string.Empty;
        var packageLabel = coord?.AssignedPackageLabel ?? string.Empty;
        var gapCode = coord?.GapCode ?? "UNKNOWN";

        var layers = new List<CoverageLayerSummary>();
        foreach (var role in LayerOrder)
        {
            var matching = effects.Where(e => e.Role == role).ToArray();
            if (matching.Length == 0)
            {
                continue;
            }

            var available = matching.Count(e => e.State == CoordinationEffectState.Available);
            var detached = matching.Count(e => e.State == CoordinationEffectState.Detached);
            var lost = matching.Count(e => e.State == CoordinationEffectState.Lost);
            var lastKnown = matching.Count(e => e.State == CoordinationEffectState.UnknownAvailability);
            var covered = matching.Count(e => e.Coverage.Status == CoverageStatus.Covered);
            var gaps = matching.Count(e => e.Coverage.Status == CoverageStatus.Gap);
            var unknownCov = matching.Count(e => e.Coverage.Status == CoverageStatus.Unknown);

            var status = available == matching.Length
                ? CoverageLayerStatus.Intact
                : available == 0 && (lost + detached) == matching.Length
                    ? CoverageLayerStatus.Lost
                    : available == 0 && lastKnown > 0
                        ? CoverageLayerStatus.Unknown
                        : CoverageLayerStatus.Degraded;

            var roleLabel = role?.ToString() ?? "Unassigned";
            var detail = $"{roleLabel}: {available}/{matching.Length} available ({covered} covered, {gaps} gap, {detached} detached, {lost} lost)";
            layers.Add(new CoverageLayerSummary(
                roleLabel,
                role,
                status,
                matching.Length,
                available,
                detached,
                lost,
                lastKnown,
                covered,
                gaps,
                unknownCov,
                detail));
        }

        var unprotected = new List<CoverageGapEntry>();
        foreach (var effect in effects.OrderBy(e => e.UnitId, StringComparer.Ordinal).ThenBy(e => e.ElementId, StringComparer.Ordinal))
        {
            var isGap = effect.Coverage.Status == CoverageStatus.Gap
                || (effect.State is CoordinationEffectState.Lost or CoordinationEffectState.Detached
                    && effect.Coverage.Status != CoverageStatus.Covered);
            if (!isGap)
            {
                continue;
            }

            var reason = effect.Coverage.ReasonCode
                ?? (effect.State == CoordinationEffectState.Detached ? "CONTRIBUTOR_DETACHED" : "CONTRIBUTOR_UNAVAILABLE");
            var covId = string.IsNullOrEmpty(effect.Coverage.CoverageId) ? effect.ElementId : effect.Coverage.CoverageId;
            var covLabel = string.IsNullOrEmpty(effect.Coverage.Label) ? $"{effect.Role?.ToString() ?? "Unit"} sector" : effect.Coverage.Label;
            unprotected.Add(new CoverageGapEntry(
                groupId,
                effect.UnitId,
                effect.ElementId,
                effect.Role,
                covId,
                covLabel,
                reason,
                IsUsableGeometry(effect.Coverage.Geometry),
                $"{covLabel} ({covId}) GAP: {effect.UnitId} [{effect.Role?.ToString() ?? "Unassigned"}] {effect.State} ({reason})"));
        }

        var coveredCount = effects.Count(e => e.Coverage.Status == CoverageStatus.Covered);
        var gapCount = effects.Count(e => e.Coverage.Status == CoverageStatus.Gap);
        var unknownCount = effects.Count(e => e.Coverage.Status == CoverageStatus.Unknown);
        var usablePolygons = effects.Count(e => e.Coverage.Status != CoverageStatus.Unknown && IsUsableGeometry(e.Coverage.Geometry));

        var c2Layer = layers.FirstOrDefault(l => l.Role == C2NodeRole.C2);
        var allUnknown = effects.Count == 0
            || effects.All(e =>
                e.State is CoordinationEffectState.UnknownRole or CoordinationEffectState.UnknownAvailability
                && e.Coverage.Status == CoverageStatus.Unknown);
        var severity = gapCode == "NO_C2" || c2Layer?.Status == CoverageLayerStatus.Lost || (coveredCount == 0 && gapCount > 0)
            ? CoveragePostureSeverity.Critical
            : allUnknown || string.IsNullOrWhiteSpace(packageId)
                ? CoveragePostureSeverity.Unknown
                : gapCode is "SPLIT" or "UNASSIGNED" || gapCount > 0 || coordGaps.Count > 0
                    ? CoveragePostureSeverity.Degraded
                    : CoveragePostureSeverity.Nominal;

        var hasDetached = effects.Any(e => e.State == CoordinationEffectState.Detached);
        var hasLost = effects.Any(e => e.State == CoordinationEffectState.Lost);
        var constraints = intent?.Constraints?.ToArray() ?? Array.Empty<string>();
        var boundary = new CoverageAuthorityBoundary(
            IsOrder: intent?.IsOrder ?? false,
            IsWeaponsReleaseAuthorization: coord?.IsWeaponsReleaseAuthorization ?? false,
            IsFireOrder: coord?.IsFireOrder ?? false,
            IsAutomaticEngagement: coord?.IsAutomaticEngagement ?? false,
            HasDetachedUnits: hasDetached,
            GroupCommandsBlockedByMissingMember: hasLost,
            ActiveConstraints: constraints,
            Summary: $"Advisory only (IsOrder={(intent?.IsOrder ?? false ? "true" : "false")}, WRA=false, Fire=false)"
                + (hasDetached ? " · detached units excluded from group command scope" : string.Empty)
                + (hasLost ? " · group commands blocked (MISSING_OR_LOST_MEMBER)" : string.Empty));

        var summaryLine = $"{groupId}: {severity} ({gapCode}) · {coveredCount} covered / {gapCount} gap / {unknownCount} unknown";

        return new CoverageGroupPosture(
            groupId,
            packageId,
            packageLabel,
            gapCode,
            severity,
            coveredCount,
            gapCount,
            unknownCount,
            usablePolygons,
            layers,
            unprotected,
            coordGaps,
            boundary,
            summaryLine);
    }

    private static Dictionary<string, CoordinationGroupSnapshot> IndexGroupsById(
        IReadOnlyList<CoordinationGroupSnapshot>? groups)
    {
        var map = new Dictionary<string, CoordinationGroupSnapshot>(StringComparer.Ordinal);
        if (groups is null)
        {
            return map;
        }

        foreach (var group in groups)
        {
            if (group?.Coordination is null)
            {
                continue;
            }

            var key = group.Coordination.GroupId ?? string.Empty;
            map[key] = group;
        }

        return map;
    }

    private static bool ContainsUnit(CoordinationGroupSnapshot group, string unitId)
    {
        var effects = group.Effects;
        if (effects is not null)
        {
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect is not null && string.Equals(effect.UnitId, unitId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        var members = group.Coordination?.Members;
        if (members is not null)
        {
            for (var i = 0; i < members.Count; i++)
            {
                if (string.Equals(members[i], unitId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static CoverageGroupPostureDiff CompareGroup(
        string groupId,
        CoordinationGroupSnapshot? previous,
        CoordinationGroupSnapshot? current)
    {
        var prevGapCode = previous?.Coordination?.GapCode ?? "NONE";
        var currGapCode = current is null
            ? "GROUP_MISSING"
            : current.Coordination?.GapCode ?? "NONE";
        var prevEffects = previous?.Effects ?? Array.Empty<CoordinationEffect>();
        var currEffects = current?.Effects ?? Array.Empty<CoordinationEffect>();

        var prevCovered = prevEffects.Count(e => e is not null && e.Coverage.Status == CoverageStatus.Covered);
        var currCovered = currEffects.Count(e => e is not null && e.Coverage.Status == CoverageStatus.Covered);
        var prevGaps = prevEffects.Count(e => e is not null && e.Coverage.Status == CoverageStatus.Gap);
        var currGaps = currEffects.Count(e => e is not null && e.Coverage.Status == CoverageStatus.Gap);

        var transitions = new List<CoverageTransitionItem>();
        var degradedRoles = new HashSet<C2NodeRole>();
        var lostOrDetachedUnits = new HashSet<string>(StringComparer.Ordinal);
        var restoredUnits = new HashSet<string>(StringComparer.Ordinal);

        var prevByKey = new Dictionary<string, CoordinationEffect>(StringComparer.Ordinal);
        foreach (var prev in prevEffects)
        {
            if (prev is null)
            {
                continue;
            }

            prevByKey[$"{prev.UnitId}|{prev.ElementId}"] = prev;
        }

        if (current is null)
        {
            foreach (var prev in prevEffects)
            {
                if (prev is null)
                {
                    continue;
                }

                var covId = string.IsNullOrEmpty(prev.Coverage.CoverageId) ? prev.ElementId : prev.Coverage.CoverageId;
                var covLabel = string.IsNullOrEmpty(prev.Coverage.Label) ? covId : prev.Coverage.Label;
                transitions.Add(new CoverageTransitionItem(
                    prev.UnitId,
                    prev.ElementId,
                    prev.Role,
                    covId,
                    covLabel,
                    prev.Coverage.Status,
                    CoverageStatus.Gap,
                    prev.State,
                    CoordinationEffectState.Lost,
                    $"{prev.UnitId} ({prev.Role?.ToString() ?? "Unassigned"}): {prev.State}->Lost (group removed), {covId} {prev.Coverage.Status}->Gap"));

                lostOrDetachedUnits.Add(prev.UnitId);
                if (prev.Role is { } role)
                {
                    degradedRoles.Add(role);
                }
            }
        }
        else
        {
            foreach (var curr in currEffects)
            {
                if (curr is null)
                {
                    continue;
                }

                var key = $"{curr.UnitId}|{curr.ElementId}";
                prevByKey.TryGetValue(key, out var prev);
                var prevStatus = prev?.Coverage.Status ?? CoverageStatus.Unknown;
                var prevState = prev?.State ?? CoordinationEffectState.UnknownAvailability;
                var currStatus = curr.Coverage.Status;
                var currState = curr.State;

                if (prevStatus != currStatus || prevState != currState)
                {
                    var covId = string.IsNullOrEmpty(curr.Coverage.CoverageId) ? curr.ElementId : curr.Coverage.CoverageId;
                    var covLabel = string.IsNullOrEmpty(curr.Coverage.Label) ? covId : curr.Coverage.Label;
                    transitions.Add(new CoverageTransitionItem(
                        curr.UnitId,
                        curr.ElementId,
                        curr.Role,
                        covId,
                        covLabel,
                        prevStatus,
                        currStatus,
                        prevState,
                        currState,
                        $"{curr.UnitId} ({curr.Role?.ToString() ?? "Unassigned"}): {prevState}->{currState}, {covId} {prevStatus}->{currStatus}"));

                    if (currState is CoordinationEffectState.Lost or CoordinationEffectState.Detached
                        || (prevStatus == CoverageStatus.Covered && currStatus == CoverageStatus.Gap))
                    {
                        lostOrDetachedUnits.Add(curr.UnitId);
                        if (curr.Role is { } role)
                        {
                            degradedRoles.Add(role);
                        }
                    }
                    else if (prevState is CoordinationEffectState.Lost or CoordinationEffectState.Detached
                             && currState == CoordinationEffectState.Available)
                    {
                        restoredUnits.Add(curr.UnitId);
                    }
                }
            }
        }

        var c2LostOrNoC2 = (currGapCode == "NO_C2" && prevGapCode != "NO_C2")
            || (current is null && (prevCovered > 0 || degradedRoles.Contains(C2NodeRole.C2)))
            || (prevCovered > 0 && currCovered == 0 && currGaps > 0)
            || degradedRoles.Contains(C2NodeRole.C2);

        var deltaKind = c2LostOrNoC2
            ? CoveragePostureDeltaKind.Collapsed
            : current is null
                ? CoveragePostureDeltaKind.Degraded
                : currGaps > prevGaps || lostOrDetachedUnits.Count > 0 || (prevGapCode == "NONE" && currGapCode != "NONE")
                    ? CoveragePostureDeltaKind.Degraded
                    : currGaps < prevGaps || restoredUnits.Count > 0 || (prevGapCode != "NONE" && currGapCode == "NONE")
                        ? CoveragePostureDeltaKind.Improved
                        : CoveragePostureDeltaKind.Unchanged;

        var lines = new List<string>();
        var isOrder = current?.Intent?.IsOrder ?? false;
        var hasLost = current is null || currEffects.Any(e => e is not null && e.State == CoordinationEffectState.Lost);
        var hasDetached = currEffects.Any(e => e is not null && e.State == CoordinationEffectState.Detached);

        var detailSb = new StringBuilder();
        detailSb.Append("Posture ").Append(deltaKind.ToString().ToUpperInvariant())
            .Append(" (").Append(prevGapCode).Append(" -> ").Append(currGapCode).Append("): ");
        if (transitions.Count > 0)
        {
            detailSb.Append(string.Join("; ", transitions.Select(t => t.Summary)));
        }
        else if (current is null)
        {
            detailSb.Append("group removed from current snapshot");
        }
        else
        {
            detailSb.Append("no layer/coverage transitions");
        }

        detailSb.Append(". ");
        if (hasDetached)
        {
            detailSb.Append("Detached unit(s) operate outside group scope; ");
        }

        if (hasLost)
        {
            detailSb.Append("Group commands blocked (MISSING_OR_LOST_MEMBER); ");
        }

        detailSb.Append("advisory only (IsOrder=").Append(isOrder ? "true" : "false").Append(").");
        var headline = detailSb.ToString();
        lines.Add(headline);

        return new CoverageGroupPostureDiff(
            groupId,
            deltaKind,
            prevGapCode,
            currGapCode,
            prevCovered,
            currCovered,
            prevGaps,
            currGaps,
            degradedRoles.OrderBy(r => (int)r).ToArray(),
            lostOrDetachedUnits.OrderBy(u => u, StringComparer.Ordinal).ToArray(),
            restoredUnits.OrderBy(u => u, StringComparer.Ordinal).ToArray(),
            transitions,
            headline,
            lines);
    }

    private static CoordinationGroupSnapshot BuildHypotheticalGroup(
        CoordinationGroupSnapshot baseline,
        string unitId,
        HypotheticalElementChangeKind changeKind)
    {
        var newState = changeKind == HypotheticalElementChangeKind.Loss
            ? CoordinationEffectState.Lost
            : CoordinationEffectState.Detached;
        var reasonCode = changeKind == HypotheticalElementChangeKind.Loss
            ? "SIMULATED_UNIT_LOSS"
            : "SIMULATED_REASSIGNMENT";

        var updatedEffects = new List<CoordinationEffect>(baseline.Effects.Count);
        foreach (var effect in baseline.Effects)
        {
            if (!string.Equals(effect.UnitId, unitId, StringComparison.Ordinal))
            {
                updatedEffects.Add(effect);
                continue;
            }

            var updatedCoverage = effect.Coverage.Status == CoverageStatus.Unknown && effect.Coverage.Geometry is null
                ? effect.Coverage
                : effect.Coverage with { Status = CoverageStatus.Gap, ReasonCode = reasonCode };
            updatedEffects.Add(effect with { State = newState, Coverage = updatedCoverage });
        }

        var remainingMembers = changeKind == HypotheticalElementChangeKind.DetachOrReassignment
            ? baseline.Coordination.Members.Where(m => !string.Equals(m, unitId, StringComparison.Ordinal)).ToArray()
            : baseline.Coordination.Members;
        var hasC2 = updatedEffects.Any(e => e.Role == C2NodeRole.C2 && e.State == CoordinationEffectState.Available);
        var isSplit = string.Equals(baseline.Coordination.GapCode, TaskGroupCoordGapCode.Split, StringComparison.Ordinal)
            || changeKind == HypotheticalElementChangeKind.DetachOrReassignment
            || updatedEffects.Any(e => e.State == CoordinationEffectState.Detached);

        var updatedCoordination = string.IsNullOrWhiteSpace(baseline.Coordination.AssignedPackageId)
            ? baseline.Coordination with { Members = remainingMembers }
            : TaskGroupCoordProjection.Project(new TaskGroupCoordInput(
                baseline.Coordination.GroupId,
                remainingMembers,
                baseline.Coordination.AssignedPackageId,
                baseline.Coordination.AssignedPackageLabel,
                HasC2: hasC2,
                C2NodeId: updatedEffects.FirstOrDefault(e => e.Role == C2NodeRole.C2)?.ElementId ?? string.Empty,
                IsSplit: isSplit));

        var updatedGaps = new List<CoordinationGap>();
        foreach (var effect in updatedEffects)
        {
            if (effect.Role is null)
            {
                updatedGaps.Add(new CoordinationGap("UNKNOWN_ROLE", effect.UnitId, null, "No authored package role."));
                if (effect.State == CoordinationEffectState.Lost)
                {
                    updatedGaps.Add(new CoordinationGap("LOST_MEMBER", effect.UnitId, null, "Member is not alive in the snapshot."));
                }
                continue;
            }

            if (effect.State is CoordinationEffectState.Lost or CoordinationEffectState.Detached)
            {
                var prefix = effect.State == CoordinationEffectState.Detached ? "DETACHED" : "LOST";
                updatedGaps.Add(new CoordinationGap(
                    $"{prefix}_{effect.Role.Value.ToString().ToUpperInvariant()}",
                    effect.UnitId,
                    effect.Role,
                    $"{effect.Role} responsibility contributor is {effect.State.ToString().ToLowerInvariant()}."));
            }
        }

        return baseline with
        {
            Coordination = updatedCoordination,
            Effects = updatedEffects,
            Gaps = updatedGaps,
        };
    }

    private static CoveragePostureDeltaKind ResolveOverallDelta(IReadOnlyList<CoverageGroupPostureDiff> diffs)
    {
        if (diffs.Count == 0)
        {
            return CoveragePostureDeltaKind.Unchanged;
        }

        if (diffs.Any(d => d.DeltaKind == CoveragePostureDeltaKind.Collapsed))
        {
            return CoveragePostureDeltaKind.Collapsed;
        }

        if (diffs.Any(d => d.DeltaKind == CoveragePostureDeltaKind.Degraded))
        {
            return CoveragePostureDeltaKind.Degraded;
        }

        if (diffs.Any(d => d.DeltaKind == CoveragePostureDeltaKind.Improved))
        {
            return CoveragePostureDeltaKind.Improved;
        }

        return CoveragePostureDeltaKind.Unchanged;
    }

    private static bool IsUsableGeometry(CoverageAreaGeometry? geometry) =>
        geometry is not null
        && !string.IsNullOrWhiteSpace(geometry.SourceRef)
        && geometry.Boundary is { Count: >= 3 }
        && geometry.Boundary.All(p =>
            double.IsFinite(p.NormalizedX)
            && double.IsFinite(p.NormalizedY)
            && p.NormalizedX is >= 0 and <= 1
            && p.NormalizedY is >= 0 and <= 1);
}
