namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Decision;

/// <summary>Rebuilds facility capacity picture from seed entries plus order-log damage projection.</summary>
public static class FacilityPictureProjection
{
    public static IReadOnlyList<FacilityPictureEntry> ProjectWithDamage(
        DecisionLog log,
        IReadOnlyDictionary<string, FacilityPictureEntry> facilityByTargetId)
    {
        if (facilityByTargetId.Count == 0)
        {
            return Array.Empty<FacilityPictureEntry>();
        }

        var damageChanges = OrderLogFacilityDamageProjection.ProjectDamageChanges(log, facilityByTargetId);
        if (damageChanges.Count == 0)
        {
            return facilityByTargetId.Values
                .OrderBy(f => f.FacilityId, StringComparer.Ordinal)
                .ToArray();
        }

        var tracks = facilityByTargetId.Values.ToDictionary(f => f.FacilityId, StringComparer.Ordinal);
        foreach (var change in damageChanges)
        {
            if (!tracks.TryGetValue(change.FacilityId, out var existing))
            {
                continue;
            }

            tracks[change.FacilityId] = existing with
            {
                CapacityState = change.NewState,
                LastSimTick = change.SimTick,
                LastSimTime = change.SimTime,
            };
        }

        return tracks.Values
            .OrderBy(f => f.FacilityId, StringComparer.Ordinal)
            .ToArray();
    }
}