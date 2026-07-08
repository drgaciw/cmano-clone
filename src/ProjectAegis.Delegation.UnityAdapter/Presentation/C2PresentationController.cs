using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Presentation-only selection state (doc 20). Does not mutate sim or order log.
/// S37-04: graph surfacing support (highlights/bind via read-only catalog projections per ADR-010, polish-boundary).
/// S39-03: residual C2 polish (filters/tooltips/density from S37/S38 carry). Per production/sprints/sprint-39-*.md + qa-plan + polish-scope-boundary-2026-06-19.md; maintain Graph* proxy 18/18+.
/// req20-rev2 Track T1 (TR-c2-005): multi-select ops (<see cref="ToggleFriendlyUnit"/>,
/// <see cref="SelectFriendlyUnits"/>, <see cref="CycleFriendlyUnit"/>) built on the shared
/// <see cref="SelectionSet"/> Phase 0 contract. Drag-box rect resolution
/// (<see cref="SelectionBoxResolver"/>), center-on-selection (<see cref="CenterOnSelectionResolver"/>)
/// and group-order fan-out (<see cref="GroupOrderPlan"/> / <see cref="GroupOrderFanOut"/>) are pure
/// sibling helpers in this namespace; the host wires pointer/keyboard input to them.
/// </summary>
public sealed class C2PresentationController
{
    /// <summary>Ordered multi-select set of friendly unit ids (req 20 §Selection, TR-c2-005).
    /// Single-select is a set of one; <see cref="SelectedUnitId"/> is the anchor unit.</summary>
    public SelectionSet Selection { get; } = new SelectionSet();

    /// <summary>Anchor (primary) selected friendly unit id, or null. Mirrors the pre-rev-2
    /// single-select contract by projecting <see cref="SelectionSet.PrimaryUnitId"/>.</summary>
    public string? SelectedUnitId => Selection.PrimaryUnitId;

    public string? SelectedContactId { get; private set; }
    public ContactSummaryEntry? SelectedContactSummary { get; private set; }

    // S37-04 / S37-13 / S39-03 residual: dependency graph highlights for selected unit (read-only; no bridge mutation)
    public IReadOnlyList<string> LastGraphHighlightIds { get; private set; } = Array.Empty<string>();
    public string? LastGraphLinkChainDisplay { get; private set; }

    public void SelectFriendlyUnit(string unitId)
    {
        if (!string.Equals(SelectedUnitId, unitId, StringComparison.Ordinal))
        {
            ClearGraphSurfacing();
        }

        Selection.ReplaceWith(unitId);
        SelectedContactId = null;
        SelectedContactSummary = null;
    }

    /// <summary>
    /// Shift/ctrl-click add-or-remove <paramref name="unitId"/> in the current multi-select (req 20
    /// §Selection, TR-c2-005). Unlike <see cref="SelectFriendlyUnit"/> this never replaces the rest
    /// of an existing multi-select. Clears any hostile-contact selection, matching single-select
    /// semantics.
    /// </summary>
    public void ToggleFriendlyUnit(string unitId)
    {
        ClearGraphSurfacing();
        Selection.Toggle(unitId);
        SelectedContactId = null;
        SelectedContactSummary = null;
    }

    /// <summary>
    /// Replace the whole selection with <paramref name="unitIds"/> in the given order (req 20
    /// §Selection, TR-c2-005; drag-box marquee result via <see cref="SelectionBoxResolver"/>).
    /// Null/empty clears the selection.
    /// </summary>
    public void SelectFriendlyUnits(IReadOnlyList<string>? unitIds)
    {
        ClearGraphSurfacing();
        Selection.Clear();
        if (unitIds != null)
        {
            foreach (var unitId in unitIds)
            {
                Selection.Add(unitId);
            }
        }

        SelectedContactId = null;
        SelectedContactSummary = null;
    }

    /// <summary>
    /// Union <paramref name="unitIds"/> into the current multi-select without disturbing units
    /// already selected (req 20 §Selection, TR-c2-005; shift+drag-box add). Ids already present are
    /// no-ops (never toggled off) — this is strictly additive, unlike <see cref="ToggleFriendlyUnit"/>.
    /// </summary>
    public void AddFriendlyUnits(IReadOnlyList<string>? unitIds)
    {
        if (unitIds == null || unitIds.Count == 0)
        {
            return;
        }

        ClearGraphSurfacing();
        foreach (var unitId in unitIds)
        {
            Selection.Add(unitId);
        }

        SelectedContactId = null;
        SelectedContactSummary = null;
    }

    /// <summary>
    /// N/P cycle to the next/previous alive friendly unit within the friendly OOB order (req 20
    /// §Keyboard; <see cref="ProjectAegis.Delegation.Input.C2InputActions.CycleUnit"/>). Replaces the
    /// selection with a single unit — the new anchor — matching single-select semantics. Returns
    /// false (no-op) when no alive friendly unit is available to cycle to.
    /// </summary>
    public bool CycleFriendlyUnit(IReadOnlyList<OobTreeEntry> oob, bool forward)
    {
        var next = C2SelectionResolver.CycleUnit(oob, SelectedUnitId, forward);
        if (next == null)
        {
            return false;
        }

        SelectFriendlyUnit(next);
        return true;
    }

    public void SelectHostileContact(string contactId, IReadOnlyList<ContactPictureEntry> contacts)
    {
        // BUG (qa-loop-08): contacts carry no platform graph of their own, but the previously
        // selected friendly unit's graph highlights/link-chain must not be left dangling on the
        // controller once selection moves away from that unit — otherwise a bound C2 graph panel
        // keeps showing stale highlights for a unit that is no longer selected (C2 view desync).
        ClearGraphSurfacing();

        SelectedContactId = contactId;
        SelectedContactSummary = ContactSummaryProjection.Project(contactId, contacts);
        Selection.Clear();
    }

    public void ApplyDefaultSelection(IReadOnlyList<OobTreeEntry> oob)
    {
        if (!string.IsNullOrEmpty(SelectedUnitId) || !string.IsNullOrEmpty(SelectedContactId))
        {
            return;
        }

        var defaultUnit = C2SelectionResolver.ResolveDefaultFriendlyUnit(oob);
        if (defaultUnit != null)
        {
            Selection.ReplaceWith(defaultUnit);
        }
    }

    public UnitDetailEntry? ResolveUnitDetail(
        ISimWorldSnapshot snapshot,
        TargetRegistry registry,
        DelegationBridge bridge)
    {
        if (string.IsNullOrEmpty(SelectedUnitId))
        {
            return UnitDetailBridge.BuildPrimary(snapshot, bridge);
        }

        return UnitDetailBridge.BuildSelected(
            new TargetId(SelectedUnitId),
            snapshot,
            bridge);
    }

    public string? ResolveContactLine() =>
        SelectedContactSummary?.DisplayLine;

    /// <summary>
    /// S37-04: Compute read-only graph highlights + link-chain summary for selected unit.
    /// Uses catalog projections only (ADR-010 headless-first, no DelegationBridge).
    /// Viewer/panel/highlights/bind consumers call after selection.
    /// </summary>
    public void ApplyGraphSurfacing(ICatalogReader? catalog)
    {
        LastGraphHighlightIds = Array.Empty<string>();
        LastGraphLinkChainDisplay = null;

        if (string.IsNullOrEmpty(SelectedUnitId) || catalog == null)
        {
            return;
        }

        var unitId = SelectedUnitId!;
        var edges = catalog.GetSortedDependencyEdges();
        var related = edges
            .Where(e => string.Equals(e.PlatformId, unitId, StringComparison.Ordinal))
            .ToList();

        var highlights = new List<string> { unitId };
        var chainParts = new List<string>();

        foreach (var e in related)
        {
            if (!string.IsNullOrEmpty(e.SensorId)) { highlights.Add(e.SensorId); chainParts.Add($"sensor:{e.SensorId}"); }
            if (!string.IsNullOrEmpty(e.MountId)) { highlights.Add(e.MountId); }
            if (!string.IsNullOrEmpty(e.WeaponId)) { highlights.Add(e.WeaponId); chainParts.Add($"weapon:{e.WeaponId}"); }
            if (!string.IsNullOrEmpty(e.LinkId)) { highlights.Add(e.LinkId); chainParts.Add($"link:{e.LinkId}:{e.CommsFittingId}"); }
        }

        LastGraphHighlightIds = highlights.Distinct().ToArray();
        LastGraphLinkChainDisplay = chainParts.Count > 0 ? string.Join(" | ", chainParts) : "(no chains)";
    }

    public void ClearGraphSurfacing()
    {
        LastGraphHighlightIds = Array.Empty<string>();
        LastGraphLinkChainDisplay = null;
    }
}