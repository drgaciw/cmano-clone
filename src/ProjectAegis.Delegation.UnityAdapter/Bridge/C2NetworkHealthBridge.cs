namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using C2Network;
using Data.Catalog;
using Decision;
using Projection;

/// <summary>
/// Headless/Unity facade: C2 network-health snapshot from order log + friendly mesh + catalog links.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class C2NetworkHealthBridge
{
    /// <summary>
    /// Projects mesh health for side-aware friendly units alive in <paramref name="registry"/>.
    /// Catalog links may be empty when <paramref name="catalog"/> is null (comms-state-only fold).
    /// </summary>
    public static C2NetworkHealthSnapshot Build(
        DecisionLog log,
        TargetRegistry registry,
        ISimWorldSnapshot snapshot,
        ICatalogReader? catalog,
        ulong currentSimTick)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var friendlyUnitIds = FriendlyMeshUnitIdsProjection.Collect(registry, snapshot);
        var catalogLinks = catalog != null
            ? CatalogLinkListProjection.FromReader(catalog)
            : Array.Empty<CatalogLinkEntry>();

        return C2NetworkHealthProjector.Project(
            log,
            friendlyUnitIds,
            catalogLinks,
            currentSimTick);
    }
}
