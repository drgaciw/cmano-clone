namespace ProjectAegis.Delegation.Projection;

public sealed record LossesScoringSnapshot(
    int Score,
    int HostileKills,
    int MissilesFired,
    int PolicyDenials);