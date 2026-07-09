namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Req 20 P0: OOB ownership filters over <see cref="DelegationStateProjection"/> rows
/// (human-only / agent-only / all). Pure — no sim mutation.
/// </summary>
public enum DelegationOobFilterMode
{
    /// <summary>No ownership filter.</summary>
    All = 0,

    /// <summary>Human-controlled units (includes mixed C5 override).</summary>
    HumanOnly = 1,

    /// <summary>Agent-controlled units (includes mixed C5 override).</summary>
    AgentOnly = 2,
}

/// <summary>Filters projection / OOB rows by ownership badge.</summary>
public static class DelegationOobFilter
{
    /// <summary>True when <paramref name="owner"/> matches <paramref name="mode"/>.</summary>
    public static bool Matches(DelegationOwnerKind owner, DelegationOobFilterMode mode) =>
        mode switch
        {
            DelegationOobFilterMode.All => true,
            DelegationOobFilterMode.HumanOnly =>
                owner is DelegationOwnerKind.Human or DelegationOwnerKind.Mixed,
            DelegationOobFilterMode.AgentOnly =>
                owner is DelegationOwnerKind.Agent or DelegationOwnerKind.Mixed,
            _ => true,
        };

    /// <summary>Filter projection rows; preserves input order.</summary>
    public static IReadOnlyList<DelegationStateProjection> Filter(
        IReadOnlyList<DelegationStateProjection> projections,
        DelegationOobFilterMode mode)
    {
        if (projections is null)
        {
            throw new ArgumentNullException(nameof(projections));
        }

        if (mode == DelegationOobFilterMode.All)
        {
            return projections;
        }

        var filtered = new List<DelegationStateProjection>(projections.Count);
        foreach (var row in projections)
        {
            if (Matches(row.Owner, mode))
            {
                filtered.Add(row);
            }
        }

        return filtered;
    }

    /// <summary>
    /// Filter OOB tree rows by unit ownership map. Units missing from the map are dropped
    /// under HumanOnly/AgentOnly (treated as non-matching).
    /// </summary>
    public static IReadOnlyList<OobTreeEntry> FilterOob(
        IReadOnlyList<OobTreeEntry> rows,
        IReadOnlyDictionary<string, DelegationOwnerKind> ownershipByUnitId,
        DelegationOobFilterMode mode)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        if (ownershipByUnitId is null)
        {
            throw new ArgumentNullException(nameof(ownershipByUnitId));
        }

        if (mode == DelegationOobFilterMode.All)
        {
            return rows;
        }

        var filtered = new List<OobTreeEntry>(rows.Count);
        foreach (var row in rows)
        {
            if (ownershipByUnitId.TryGetValue(row.UnitId, out var owner) && Matches(owner, mode))
            {
                filtered.Add(row);
            }
        }

        return filtered;
    }
}
