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
        int baseScore = 0,
        DateTimeOffset? scenarioEpochUtc = null,
        int localUtcOffsetHours = 0,
        double? scenarioDurationSeconds = null)
    {
        var comms = CommsStateProjection.Project(log);
        var scoreLabel = phase == SimulationPhase.Planning
            ? FormatFrozenScoreLine(baseScore)
            : FormatLiveScoreLine(log, baseScore);
        var (zuluLabel, localLabel) = FormatZuluAndLocal(
            simTimeSeconds,
            scenarioEpochUtc,
            localUtcOffsetHours);
        return new C2TopBarState(
            FormatSimTime(simTimeSeconds),
            $"PHASE: {phase}",
            $"TIME: {compressionLabel}",
            $"MODE: {simulationModeLabel}",
            comms.TopBarLabel,
            scoreLabel,
            zuluLabel,
            localLabel,
            FormatRemaining(simTimeSeconds, scenarioDurationSeconds));
    }

    private static string FormatFrozenScoreLine(int baseScore) =>
        $"SCORE: {baseScore}  KILLS: 0  MSLS: 0";

    private static string FormatLiveScoreLine(DecisionLog log, int baseScore)
    {
        var tally = LossesScoringProjection.Project(log, baseScore);
        return $"SCORE: {tally.Score}  KILLS: {tally.HostileKills}  MSLS: {tally.MissilesFired}";
    }

    private static string FormatSimTime(double seconds)
    {
        var total = Math.Max(0, (int)seconds);
        var h = total / 3600;
        var m = (total % 3600) / 60;
        var s = total % 60;
        return $"SIM {h:D2}:{m:D2}:{s:D2}";
    }

    /// <summary>
    /// CMD-22: Zulu from scenario epoch + sim time, or pure sim clock from midnight when epoch is null.
    /// Local = Zulu + fixed hour offset. Deterministic; no system clock.
    /// </summary>
    private static (string Zulu, string Local) FormatZuluAndLocal(
        double simTimeSeconds,
        DateTimeOffset? scenarioEpochUtc,
        int localUtcOffsetHours)
    {
        DateTimeOffset zulu;
        if (scenarioEpochUtc is { } epoch)
        {
            zulu = epoch.ToUniversalTime().AddSeconds(simTimeSeconds);
        }
        else
        {
            // Pure-from-sim: treat sim time as elapsed since 00:00:00 Zulu.
            var total = Math.Max(0, (int)simTimeSeconds);
            zulu = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)
                .AddSeconds(total);
        }

        var local = zulu.AddHours(localUtcOffsetHours);
        return (
            $"ZULU {zulu.Hour:D2}:{zulu.Minute:D2}:{zulu.Second:D2}",
            $"LOCAL {local.Hour:D2}:{local.Minute:D2}:{local.Second:D2}");
    }

    private static string FormatRemaining(double simTimeSeconds, double? scenarioDurationSeconds)
    {
        if (scenarioDurationSeconds is null)
        {
            return "REMAIN —";
        }

        var remaining = Math.Max(0, scenarioDurationSeconds.Value - Math.Max(0, simTimeSeconds));
        var total = (int)remaining;
        var h = total / 3600;
        var m = (total % 3600) / 60;
        var s = total % 60;
        return $"REMAIN {h:D2}:{m:D2}:{s:D2}";
    }
}
