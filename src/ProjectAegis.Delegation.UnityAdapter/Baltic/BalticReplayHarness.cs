namespace ProjectAegis.Delegation.UnityAdapter.Baltic;

using Decision;
using Data.Catalog;
using Projection;
using Replay;
using ProjectAegis.Sim.Scenario;

/// <summary>Headless Baltic slice runner for CLI and replay-verify gate.</summary>
public static class BalticReplayHarness
{
    public sealed record Result(
        int Seed,
        string ScenarioPolicyId,
        int Ticks,
        string Fingerprint,
        string FingerprintSha256,
        int EngagementCount,
        ulong DetectionWorldHash,
        ulong WorldHash,
        IReadOnlyList<ReplayCheckpoint> Checkpoints,
        IReadOnlyList<MessageLogLine> Messages,
        SensorC2Snapshot SensorC2,
        string ScoringCsvRow,
        DecisionLog DecisionLog,
        IReadOnlyList<string> FireOrder);

    /// <summary>
    /// Resolves AC-2 <c>fire_order</c>: policy mission timeline when present, else chronological
    /// <see cref="OrderLogEntryKind.EventFired"/> event ids from the decision log.
    /// </summary>
    public static IReadOnlyList<string> ResolveFireOrder(
        ScenarioMissionTimeline? missionTimeline,
        DecisionLog decisionLog)
    {
        if (missionTimeline != null)
        {
            return missionTimeline.FireOrder;
        }

        var fired = new List<string>();
        foreach (var entry in decisionLog.ChronologicalEntries())
        {
            if (entry.Kind == OrderLogEntryKind.EventFired && entry.Payload is EventFiredRecord record)
            {
                fired.Add(record.EventId);
            }
        }

        return fired;
    }

    /// <summary>
    /// Polish helper (S36-05): reports first mismatch tick + sub-hash component for divergence diagnosis.
    /// Does not affect Run path, append order, or detection trial ordering. For test harness error paths only.
    /// </summary>
    public static string DiagnoseDivergence(Result a, Result b)
    {
        if (a.Fingerprint == b.Fingerprint && a.WorldHash == b.WorldHash && a.DetectionWorldHash == b.DetectionWorldHash)
            return "MATCH";
        // checkpoints provide tick-level localization (recorded at interval)
        for (int i = 0; i < Math.Min(a.Checkpoints.Count, b.Checkpoints.Count); i++)
        {
            var ca = a.Checkpoints[i];
            var cb = b.Checkpoints[i];
            if (ca.WorldHash != cb.WorldHash || ca.LogFingerprint != cb.LogFingerprint)
            {
                return $"First mismatch at tick {ca.SimTick}: worldHash {ca.WorldHash} vs {cb.WorldHash}; fingerprint differs; layer hint in subhash";
            }
        }
        return $"Final mismatch: WORLD={a.WorldHash}!={b.WorldHash} DET={a.DetectionWorldHash}!={b.DetectionWorldHash} FINGERPRINT differs";
    }

    public static Result Run(
        int seed,
        string scenarioPolicyId,
        int ticks,
        bool mvpEngagement = true,
        ICatalogReader? catalog = null,
        IReadOnlyDictionary<string, bool>? unitReadiness = null,
        IReadOnlyList<ScenarioNearFutureUnitRequest>? nearFutureUnits = null,
        int maxTechnologyLevel = 2)
    {
        if (ticks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks), "ticks must be >= 1");
        }

        // Scenario-scoped catalog sides (gauntlet ORBAT); always clear so runs don't leak.
        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            return RunCore(
                seed,
                scenarioPolicyId,
                ticks,
                mvpEngagement,
                catalog,
                unitReadiness,
                nearFutureUnits,
                maxTechnologyLevel);
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    private static Result RunCore(
        int seed,
        string scenarioPolicyId,
        int ticks,
        bool mvpEngagement,
        ICatalogReader? catalog,
        IReadOnlyDictionary<string, bool>? unitReadiness,
        IReadOnlyList<ScenarioNearFutureUnitRequest>? nearFutureUnits,
        int maxTechnologyLevel)
    {
        var stepper = BalticReplayStepper.Create(
            seed,
            scenarioPolicyId,
            mvpEngagement,
            catalog,
            unitReadiness,
            nearFutureUnits,
            maxTechnologyLevel);
        for (var t = 0; t < ticks; t++)
        {
            stepper.StepOneObservedTick();
        }

        return stepper.BuildResult(ticks);
    }
}
