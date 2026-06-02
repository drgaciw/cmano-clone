namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;

/// <summary>Builds sorted OOB rows from registered member targets and per-tick alive state.</summary>
public static class OobTreeProjection
{
    public static IReadOnlyList<OobTreeEntry> Project(
        IReadOnlyList<TargetId> memberIds,
        Func<TargetId, bool> isAlive)
    {
        return memberIds
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .Select(id => new OobTreeEntry(id.Value, isAlive(id)))
            .ToArray();
    }
}