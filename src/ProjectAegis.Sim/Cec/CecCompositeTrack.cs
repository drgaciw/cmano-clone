namespace ProjectAegis.Sim.Cec;

/// <summary>
/// SWARM-31 / B6a: fused composite track picture built from ≥2 mesh-connected CEC contributors.
/// </summary>
public sealed record CecCompositeTrack(
    string TrackId,
    string TargetId,
    string SideId,
    string PrimaryContributorUnitId,
    int ContributorCount,
    bool FireControlQuality,
    double Quality);
