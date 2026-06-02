namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;

public static class C2TopBarProjection
{
    public static C2TopBarState Project(
        double simTimeSeconds,
        SimulationPhase phase,
        string compressionLabel,
        string simulationModeLabel,
        DecisionLog log,
        int baseScore = 0)
    {
        var tally = LossesScoringProjection.Project(log, baseScore);
        var comms = CommsStateProjection.Project(log);
        return new C2TopBarState(
            FormatSimTime(simTimeSeconds),
            $"PHASE: {phase}",
            $"TIME: {compressionLabel}",
            $"MODE: {simulationModeLabel}",
            comms.TopBarLabel,
            $"SCORE: {tally.Score}  KILLS: {tally.HostileKills}  MSLS: {tally.MissilesFired}");
    }

    private static string FormatSimTime(double seconds)
    {
        var total = Math.Max(0, (int)seconds);
        var h = total / 3600;
        var m = (total % 3600) / 60;
        var s = total % 60;
        return $"SIM {h:D2}:{m:D2}:{s:D2}";
    }
}