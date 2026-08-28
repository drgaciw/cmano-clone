namespace ProjectAegis.Delegation.C2Network;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>
/// Folds <see cref="CommsStateProjection"/>, <see cref="DatalinkPictureProjection"/>, and
/// <see cref="ContactPictureProjection"/> into a replay-stable network-health snapshot.
/// </summary>
public static class C2NetworkHealthProjector
{
    /// <summary>
    /// Optional per-link status override keyed by sorted endpoint pair.
    /// Values follow <see cref="DatalinkPictureProjection"/> status tokens (Up/Degraded/Down).
    /// </summary>
    public sealed record LinkStatusOverride(string FromUnitId, string ToUnitId, string Status);

    /// <summary>
    /// Projects link health for a friendly mesh at <paramref name="currentSimTick"/>.
    /// Global comms transitions come from <paramref name="log"/>; optional overrides model
    /// single-link partition without mutating the order log.
    /// </summary>
    public static C2NetworkHealthSnapshot Project(
        DecisionLog log,
        IReadOnlyList<string> friendlyUnitIds,
        IReadOnlyList<CatalogLinkEntry> catalogLinks,
        ulong currentSimTick,
        IReadOnlyList<LinkStatusOverride>? linkStatusOverrides = null)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (friendlyUnitIds is null)
        {
            throw new ArgumentNullException(nameof(friendlyUnitIds));
        }

        if (catalogLinks is null)
        {
            throw new ArgumentNullException(nameof(catalogLinks));
        }

        var commsSnapshot = CommsStateProjection.Project(log);
        var contacts = ContactPictureProjection.Project(log);
        var overrideMap = BuildOverrideMap(linkStatusOverrides);

        var mesh = DatalinkUnitPairFeed.BuildMesh(friendlyUnitIds, catalogLinks);
        if (mesh.Count == 0)
        {
            return EmptySnapshot(commsSnapshot);
        }

        var edges = DatalinkPictureProjection.Project(
            mesh,
            catalogLinks,
            status: DatalinkUnitPairFeed.ResolveEdgeStatus(commsSnapshot));

        var sortedUnits = SortUnits(friendlyUnitIds);
        var partitionedUnits = ComputePartitionedUnits(sortedUnits, edges, overrideMap, commsSnapshot.State);
        var linkRows = BuildLinkRows(edges, overrideMap, commsSnapshot.State, partitionedUnits, contacts, currentSimTick);
        var contributors = BuildLastKnownContributors(contacts, partitionedUnits, commsSnapshot.State, linkRows);
        var lostPaths = BuildLostPaths(linkRows, contacts, currentSimTick);
        var networkHealth = ResolveNetworkHealth(linkRows, commsSnapshot.State);

        return new C2NetworkHealthSnapshot(
            networkHealth,
            commsSnapshot.State,
            commsSnapshot.NodeId,
            linkRows,
            contributors,
            lostPaths);
    }

    private static C2NetworkHealthSnapshot EmptySnapshot(CommsStateSnapshot commsSnapshot) =>
        new(
            MapCommsToNetworkHealth(commsSnapshot.State),
            commsSnapshot.State,
            commsSnapshot.NodeId,
            Array.Empty<C2NetworkLinkHealthEntry>(),
            Array.Empty<C2NetworkContributor>(),
            Array.Empty<C2NetworkLostPath>());

    private static Dictionary<(string From, string To), string> BuildOverrideMap(
        IReadOnlyList<LinkStatusOverride>? overrides)
    {
        var map = new Dictionary<(string, string), string>();
        if (overrides is null)
        {
            return map;
        }

        foreach (var entry in overrides)
        {
            if (entry is null
                || string.IsNullOrWhiteSpace(entry.FromUnitId)
                || string.IsNullOrWhiteSpace(entry.ToUnitId))
            {
                continue;
            }

            var key = NormalizePair(entry.FromUnitId, entry.ToUnitId);
            map[key] = NormalizeStatus(entry.Status);
        }

        return map;
    }

    private static string[] SortUnits(IReadOnlyList<string> friendlyUnitIds) =>
        friendlyUnitIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

    private static HashSet<string> ComputePartitionedUnits(
        string[] sortedUnits,
        IReadOnlyList<DatalinkEdgeEntry> edges,
        IReadOnlyDictionary<(string From, string To), string> overrideMap,
        CommsState commsState)
    {
        if (sortedUnits.Length == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        if (commsState == CommsState.Denied)
        {
            return sortedUnits.Skip(1).ToHashSet(StringComparer.Ordinal);
        }

        var reachable = new HashSet<string>(StringComparer.Ordinal) { sortedUnits[0] };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var edge in edges)
            {
                if (!IsTraversable(edge, overrideMap, commsState))
                {
                    continue;
                }

                if (reachable.Contains(edge.FromUnitId) && reachable.Add(edge.ToUnitId))
                {
                    changed = true;
                }

                if (reachable.Contains(edge.ToUnitId) && reachable.Add(edge.FromUnitId))
                {
                    changed = true;
                }
            }
        }

        var partitioned = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unitId in sortedUnits)
        {
            if (!reachable.Contains(unitId))
            {
                partitioned.Add(unitId);
            }
        }

        return partitioned;
    }

    private static bool IsTraversable(
        DatalinkEdgeEntry edge,
        IReadOnlyDictionary<(string From, string To), string> overrideMap,
        CommsState commsState)
    {
        var status = ResolveEdgeStatus(edge, overrideMap, commsState);
        return status is DatalinkPictureProjection.StatusUp or DatalinkPictureProjection.StatusDegraded;
    }

    private static string ResolveEdgeStatus(
        DatalinkEdgeEntry edge,
        IReadOnlyDictionary<(string From, string To), string> overrideMap,
        CommsState commsState)
    {
        var key = NormalizePair(edge.FromUnitId, edge.ToUnitId);
        if (overrideMap.TryGetValue(key, out var overrideStatus))
        {
            return overrideStatus;
        }

        if (commsState == CommsState.Denied)
        {
            return DatalinkPictureProjection.StatusDown;
        }

        if (commsState == CommsState.Degraded)
        {
            return edge.Status == DatalinkPictureProjection.StatusUp
                ? DatalinkPictureProjection.StatusDegraded
                : edge.Status;
        }

        return edge.Status;
    }

    private static IReadOnlyList<C2NetworkLinkHealthEntry> BuildLinkRows(
        IReadOnlyList<DatalinkEdgeEntry> edges,
        IReadOnlyDictionary<(string From, string To), string> overrideMap,
        CommsState commsState,
        HashSet<string> partitionedUnits,
        IReadOnlyList<ContactPictureEntry> contacts,
        ulong currentSimTick)
    {
        var rows = new List<C2NetworkLinkHealthEntry>(edges.Count);
        foreach (var edge in edges.OrderBy(e => e.FromUnitId, StringComparer.Ordinal)
                     .ThenBy(e => e.ToUnitId, StringComparer.Ordinal)
                     .ThenBy(e => e.LinkType, StringComparer.Ordinal))
        {
            var status = ResolveEdgeStatus(edge, overrideMap, commsState);
            var health = MapStatusToLinkHealth(status);
            var isLive = health == C2LinkHealth.Healthy;
            var affected = CollectAffectedContributors(edge, health, partitionedUnits);
            var staleness = ComputeLinkStaleness(edge, health, affected, contacts, currentSimTick);

            rows.Add(new C2NetworkLinkHealthEntry(
                edge.FromUnitId,
                edge.ToUnitId,
                edge.LinkType,
                health,
                staleness,
                isLive,
                affected));
        }

        return rows;
    }

    private static IReadOnlyList<string> CollectAffectedContributors(
        DatalinkEdgeEntry edge,
        C2LinkHealth health,
        HashSet<string> partitionedUnits)
    {
        if (health == C2LinkHealth.Healthy)
        {
            return Array.Empty<string>();
        }

        var affected = new List<string>();
        if (partitionedUnits.Contains(edge.ToUnitId))
        {
            affected.Add(edge.ToUnitId);
        }

        if (partitionedUnits.Contains(edge.FromUnitId))
        {
            affected.Add(edge.FromUnitId);
        }

        return affected
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    private static ulong ComputeLinkStaleness(
        DatalinkEdgeEntry edge,
        C2LinkHealth health,
        IReadOnlyList<string> affectedContributorUnitIds,
        IReadOnlyList<ContactPictureEntry> contacts,
        ulong currentSimTick)
    {
        if (health == C2LinkHealth.Healthy)
        {
            return 0;
        }

        ulong maxTick = 0;
        foreach (var contact in contacts)
        {
            if (!IsDatalinkSharedContact(contact))
            {
                continue;
            }

            if (affectedContributorUnitIds.Count > 0)
            {
                if (!affectedContributorUnitIds.Contains(contact.ObserverId))
                {
                    continue;
                }
            }
            else if (contact.ObserverId != edge.FromUnitId
                     && contact.ObserverId != edge.ToUnitId)
            {
                continue;
            }

            if (contact.LastSimTick > maxTick)
            {
                maxTick = contact.LastSimTick;
            }
        }

        if (maxTick == 0 || currentSimTick <= maxTick)
        {
            return health == C2LinkHealth.Partitioned ? 1UL : 0UL;
        }

        return currentSimTick - maxTick;
    }

    private static IReadOnlyList<C2NetworkContributor> BuildLastKnownContributors(
        IReadOnlyList<ContactPictureEntry> contacts,
        HashSet<string> partitionedUnits,
        CommsState commsState,
        IReadOnlyList<C2NetworkLinkHealthEntry> links)
    {
        var hasNonLiveLink = commsState == CommsState.Denied
            || links.Any(l => l.Health != C2LinkHealth.Healthy);

        if (!hasNonLiveLink)
        {
            return Array.Empty<C2NetworkContributor>();
        }

        var rows = new List<C2NetworkContributor>();
        foreach (var contact in contacts.OrderBy(c => c.ObserverId, StringComparer.Ordinal)
                     .ThenBy(c => c.ContactId, StringComparer.Ordinal))
        {
            if (!IsDatalinkSharedContact(contact))
            {
                continue;
            }

            var isLive = commsState == CommsState.Nominal
                && !partitionedUnits.Contains(contact.ObserverId)
                && links.All(l => l.Health == C2LinkHealth.Healthy
                    || !l.AffectedContributorUnitIds.Contains(contact.ObserverId));

            if (isLive)
            {
                continue;
            }

            rows.Add(new C2NetworkContributor(
                contact.ObserverId,
                contact.ContactId,
                contact.TargetId,
                contact.LifecycleState,
                contact.LastSimTick,
                IsLiveCapability: false));
        }

        return rows;
    }

    private static IReadOnlyList<C2NetworkLostPath> BuildLostPaths(
        IReadOnlyList<C2NetworkLinkHealthEntry> links,
        IReadOnlyList<ContactPictureEntry> contacts,
        ulong currentSimTick)
    {
        var lost = new List<C2NetworkLostPath>();
        foreach (var link in links)
        {
            if (link.Health != C2LinkHealth.Partitioned || link.IsLiveCapability)
            {
                continue;
            }

            var lastKnown = link.StalenessTicks >= currentSimTick
                ? 0UL
                : currentSimTick - link.StalenessTicks;

            if (link.AffectedContributorUnitIds.Count > 0)
            {
                foreach (var contact in contacts)
                {
                    if (link.AffectedContributorUnitIds.Contains(contact.ObserverId)
                        && contact.LastSimTick > lastKnown)
                    {
                        lastKnown = contact.LastSimTick;
                    }
                }
            }

            lost.Add(new C2NetworkLostPath(
                link.FromUnitId,
                link.ToUnitId,
                link.LinkType,
                lastKnown));
        }

        return lost
            .OrderBy(p => p.FromUnitId, StringComparer.Ordinal)
            .ThenBy(p => p.ToUnitId, StringComparer.Ordinal)
            .ThenBy(p => p.LinkType, StringComparer.Ordinal)
            .ToArray();
    }

    private static C2NetworkHealthLevel ResolveNetworkHealth(
        IReadOnlyList<C2NetworkLinkHealthEntry> links,
        CommsState commsState)
    {
        if (commsState == CommsState.Denied || links.Any(l => l.Health == C2LinkHealth.Partitioned))
        {
            return C2NetworkHealthLevel.Partitioned;
        }

        if (commsState == CommsState.Degraded || links.Any(l => l.Health == C2LinkHealth.Degraded))
        {
            return C2NetworkHealthLevel.Degraded;
        }

        return C2NetworkHealthLevel.Healthy;
    }

    private static C2NetworkHealthLevel MapCommsToNetworkHealth(CommsState state) =>
        state switch
        {
            CommsState.Denied => C2NetworkHealthLevel.Partitioned,
            CommsState.Degraded => C2NetworkHealthLevel.Degraded,
            _ => C2NetworkHealthLevel.Healthy,
        };

    private static C2LinkHealth MapStatusToLinkHealth(string status) =>
        status switch
        {
            DatalinkPictureProjection.StatusDegraded => C2LinkHealth.Degraded,
            DatalinkPictureProjection.StatusDown => C2LinkHealth.Partitioned,
            _ => C2LinkHealth.Healthy,
        };

    private static (string From, string To) NormalizePair(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return DatalinkPictureProjection.StatusUp;
        }

        var trimmed = status.Trim();
        return trimmed switch
        {
            DatalinkPictureProjection.StatusDegraded => DatalinkPictureProjection.StatusDegraded,
            DatalinkPictureProjection.StatusDown => DatalinkPictureProjection.StatusDown,
            DatalinkPictureProjection.StatusUp => DatalinkPictureProjection.StatusUp,
            _ => trimmed,
        };
    }

    private static bool IsDatalinkSharedContact(ContactPictureEntry contact) =>
        contact.ContactId.StartsWith("dl-", StringComparison.Ordinal);
}
