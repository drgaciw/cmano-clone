namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Cec;

/// <summary>
/// SWARM-31 / B6b (DRG-103): pure gate for engage-on-remote-data.
/// Organic FC remains the preferred path; remote is used only when organic is absent
/// and a mesh-quality composite track is available to a CEC-capable shooter.
/// Does not invent per-drone fire-control bodies (aggregate SoT unchanged).
/// </summary>
public static class CecRemoteEngageGate
{
    /// <summary>
    /// Evaluate whether the shot may proceed using remote CEC fire-control.
    /// Returns null when the organic path should be used / gate is N/A.
    /// Returns an abort reason when remote was required or requested but unavailable.
    /// </summary>
    public static EngagementAbortReason? Evaluate(
        bool hasOrganicFireControlTrack,
        bool usesRemoteCecTrack,
        bool shooterCecCapable,
        bool cecRemoteFireControlEligible)
    {
        // Organic FC present: remote path not required.
        if (hasOrganicFireControlTrack && !usesRemoteCecTrack)
        {
            return null;
        }

        // Explicit remote path or organic missing with remote eligibility claimed.
        if (usesRemoteCecTrack || (!hasOrganicFireControlTrack && cecRemoteFireControlEligible))
        {
            if (!shooterCecCapable)
            {
                return EngagementAbortReason.CecRemoteTrackUnavailable;
            }

            if (!cecRemoteFireControlEligible)
            {
                return EngagementAbortReason.CecRemoteTrackUnavailable;
            }

            // Remote eligible — caller treats as FC available.
            return null;
        }

        // No organic, no remote eligibility → classic no-track (resolver handles).
        return null;
    }

    /// <summary>
    /// Build eligibility from a live mesh controller for headless fixtures / world adapters.
    /// Shooter must be CEC-capable, currently InMesh, and there must be an FC-quality
    /// composite track for the target whose primary contributor is not the shooter.
    /// </summary>
    public static bool TryResolveRemoteEligibility(
        CecMeshController mesh,
        string sideId,
        string shooterUnitId,
        string targetId,
        out CecCompositeTrack? track)
    {
        track = null;
        if (mesh is null ||
            string.IsNullOrWhiteSpace(sideId) ||
            string.IsNullOrWhiteSpace(shooterUnitId) ||
            string.IsNullOrWhiteSpace(targetId))
        {
            return false;
        }

        var shooter = shooterUnitId.Trim();
        if (mesh.GetMeshState(shooter) != CecMeshState.InMesh)
        {
            return false;
        }

        foreach (var candidate in mesh.TryGetCompositeTracks(sideId.Trim()))
        {
            if (!string.Equals(candidate.TargetId, targetId.Trim(), StringComparison.Ordinal))
            {
                continue;
            }

            if (!candidate.FireControlQuality)
            {
                continue;
            }

            if (string.Equals(candidate.PrimaryContributorUnitId, shooter, StringComparison.Ordinal))
            {
                // Primary is self — not "remote" data (still may help organic; remote path denies).
                continue;
            }

            track = candidate;
            return true;
        }

        return false;
    }

    /// <summary>
    /// True when engagement may treat remote CEC as satisfying fire-control track.
    /// </summary>
    public static bool HasUsableFireControl(
        bool hasOrganicFireControlTrack,
        bool usesRemoteCecTrack,
        bool shooterCecCapable,
        bool cecRemoteFireControlEligible)
    {
        if (hasOrganicFireControlTrack)
        {
            return true;
        }

        if (!shooterCecCapable)
        {
            return false;
        }

        if (usesRemoteCecTrack || cecRemoteFireControlEligible)
        {
            return cecRemoteFireControlEligible;
        }

        return false;
    }
}
