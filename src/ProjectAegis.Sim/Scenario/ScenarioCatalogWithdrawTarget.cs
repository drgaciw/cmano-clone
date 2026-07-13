namespace ProjectAegis.Sim.Scenario;

/// <summary>Withdraw/readiness trial identity without catalog damage; resolved via platform catalog.</summary>
public sealed record ScenarioCatalogWithdrawTarget(
    string PlatformId,
    double CurrentHpPct = 100.0);