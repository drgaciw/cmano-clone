namespace ProjectAegis.Delegation.Projection;

using Core;

/// <summary>Builds sorted OOB rows from registered member targets and per-tick alive state.</summary>
public static class OobTreeProjection
{
    public static IReadOnlyList<OobTreeEntry> Project(
        IReadOnlyList<TargetId> memberIds,
        Func<TargetId, bool> isAlive) => Project(memberIds, isAlive, _ => null);

    /// <summary>Projects each unit's alive state and configured player command ownership.</summary>
    public static IReadOnlyList<OobTreeEntry> Project(
        IReadOnlyList<TargetId> memberIds,
        Func<TargetId, bool> isAlive,
        Func<TargetId, bool?> isPlayerCommandTarget)
    {
        return memberIds
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .Select(id => new OobTreeEntry(id.Value, isAlive(id))
            {
                IsPlayerCommandTarget = isPlayerCommandTarget(id),
            })
            .ToArray();
    }
}