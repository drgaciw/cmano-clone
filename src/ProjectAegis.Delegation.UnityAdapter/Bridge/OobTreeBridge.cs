namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless/Unity facade: OOB tree from registry members + snapshot alive state.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001) — never mutates sim authority.
/// </summary>
public static class OobTreeBridge
{
    /// <summary>
    /// Builds an immutable OOB tree row list for presentation bind (C2 OOB panel hosts).
    /// Consumes registry member ids and <see cref="ISimWorldSnapshot"/> alive queries only —
    /// no live ECS / session write handles.
    /// </summary>
    /// <param name="snapshot">Read-only world snapshot (alive membership).</param>
    /// <param name="registry">Target registry for OOB member ids.</param>
    /// <returns>Immutable <see cref="IReadOnlyList{T}"/> of <see cref="OobTreeEntry"/> rows.</returns>
    /// <exception cref="ArgumentNullException">When snapshot or registry is null.</exception>
    public static IReadOnlyList<OobTreeEntry> Build(ISimWorldSnapshot snapshot, TargetRegistry registry)
    {
        // netstandard2.1 (Unity plugins): no ArgumentNullException.ThrowIfNull
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        return OobTreeProjection.Project(registry.CollectMemberIds(), snapshot.IsMemberAlive);
    }
}
