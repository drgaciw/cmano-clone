namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless/Unity facade: tactical map symbols from snapshot alive-state + order-log contacts.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001) — never mutates sim authority.
/// </summary>
public static class MapPictureBridge
{
    /// <summary>
    /// Builds an immutable map-symbol list for presentation bind (hosts / MapSymbolPool).
    /// Consumes <see cref="ISimWorldSnapshot"/> alive queries and a <see cref="DecisionLog"/>
    /// contact picture only — no live ECS / session write handles.
    /// </summary>
    /// <param name="snapshot">Read-only world snapshot (alive membership).</param>
    /// <param name="registry">Target registry for OOB member ids.</param>
    /// <param name="log">Decision / order log (contact picture projection source).</param>
    /// <param name="layoutSeed">Deterministic layout seed for placeholder placement.</param>
    /// <returns>Immutable <see cref="IReadOnlyList{T}"/> of <see cref="MapSymbolEntry"/> rows.</returns>
    /// <exception cref="ArgumentNullException">When snapshot, registry, or log is null.</exception>
    public static IReadOnlyList<MapSymbolEntry> Build(
        ISimWorldSnapshot snapshot,
        TargetRegistry registry,
        DecisionLog log,
        int layoutSeed)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(log);

        var oob = OobTreeProjection.Project(registry.CollectMemberIds(), snapshot.IsMemberAlive);
        var contacts = ContactPictureProjection.Project(log);
        return MapPictureProjection.Project(oob, contacts, layoutSeed);
    }
}
