using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Presentation-only selection state (doc 20). Does not mutate sim or order log.
/// S37-04: graph surfacing support (highlights/bind via read-only catalog projections per ADR-010, polish-boundary).
/// </summary>
public sealed class C2PresentationController
{
    public string? SelectedUnitId { get; private set; }
    public string? SelectedContactId { get; private set; }
    public ContactSummaryEntry? SelectedContactSummary { get; private set; }

    // S37-04 / S37-13: dependency graph highlights for selected unit (read-only; no bridge mutation)
    public IReadOnlyList<string> LastGraphHighlightIds { get; private set; } = Array.Empty<string>();
    public string? LastGraphLinkChainDisplay { get; private set; }

    public void SelectFriendlyUnit(string unitId)
    {
        SelectedUnitId = unitId;
        SelectedContactId = null;
        SelectedContactSummary = null;
    }

    public void SelectHostileContact(string contactId, IReadOnlyList<ContactPictureEntry> contacts)
    {
        SelectedContactId = contactId;
        SelectedContactSummary = ContactSummaryProjection.Project(contactId, contacts);
        SelectedUnitId = null;
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
            SelectedUnitId = defaultUnit;
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