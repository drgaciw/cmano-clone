namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using C2Network;
using Data.Catalog;
using Decision;
using Projection;

/// <summary>
/// Headless/Unity facade: C2 network-health snapshot from order log + OOB + catalog links.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class C2NetworkHealthBridge
{
    /// <summary>
    /// Projects mesh health for friendly units alive in <paramref name="oobTree"/>.
    /// Catalog links may be empty when <paramref name="catalog"/> is null (comms-state-only fold).
    /// </summary>
    public static C2NetworkHealthSnapshot Build(
        DecisionLog log,
        IReadOnlyList<OobTreeEntry> oobTree,
        ICatalogReader? catalog,
        ulong currentSimTick)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (oobTree is null)
        {
            throw new ArgumentNullException(nameof(oobTree));
        }

        var friendlyUnitIds = oobTree
            .Where(entry => entry.IsAlive && !string.IsNullOrWhiteSpace(entry.UnitId))
            .Select(entry => entry.UnitId)
            .ToArray();
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
