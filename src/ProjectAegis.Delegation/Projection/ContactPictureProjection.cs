namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Decision;

/// <summary>Rebuilds the active contact picture from <see cref="OrderLogEntryKind.ContactChange"/> rows (sensor GDD / C2 UI).</summary>
public static class ContactPictureProjection
{
    public static IReadOnlyList<ContactPictureEntry> Project(DecisionLog log)
    {
        var sorted = log.ContactChanges
            .OrderBy(c => c.SimTick)
            .ThenBy(c => c.SequenceId)
            .Select(c => OrderLogEntryFactories.FromContactChange(c))
            .ToArray();
        return Project(sorted);
    }

    public static IReadOnlyList<ContactPictureEntry> Project(IReadOnlyList<OrderLogEntry> entries)
    {
        var tracks = new Dictionary<string, ContactPictureEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry.Kind != OrderLogEntryKind.ContactChange ||
                entry.Payload is not ContactChangeRecord change)
            {
                continue;
            }

            if (string.Equals(change.NewState, "Lost", StringComparison.Ordinal))
            {
                tracks.Remove(change.ContactId);
                continue;
            }

            tracks[change.ContactId] = new ContactPictureEntry(
                change.ContactId,
                change.TargetId,
                change.ObserverId,
                change.NewState,
                change.SimTick,
                change.SimTime);
        }

        return tracks.Values
            .OrderBy(c => c.ContactId, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Merges sensor contact changes with order-log BDA Lost rows (projection-only).</summary>
    public static IReadOnlyList<ContactPictureEntry> ProjectWithBda(DecisionLog log)
    {
        var contactEntries = log.ContactChanges
            .OrderBy(c => c.SimTick)
            .ThenBy(c => c.SequenceId)
            .Select(c => OrderLogEntryFactories.FromContactChange(c))
            .ToArray();

        var basePicture = Project(contactEntries);
        var contactByTargetId = basePicture.ToDictionary(c => c.TargetId, StringComparer.Ordinal);
        var bdaLost = OrderLogBdaProjection.ProjectLostContacts(log, contactByTargetId);
        if (bdaLost.Count == 0)
        {
            return basePicture;
        }

        var mergedEntries = contactEntries
            .Concat(bdaLost.Select(c => OrderLogEntryFactories.FromContactChange(c)))
            .OrderBy(e => e.SimTime)
            .ThenBy(e => e.SequenceId)
            .ToArray();

        return Project(mergedEntries);
    }
}