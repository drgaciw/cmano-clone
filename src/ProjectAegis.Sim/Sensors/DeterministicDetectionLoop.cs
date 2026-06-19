namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Tick-4 MVP: sorted detection trials with SeededRng Detection domain.</summary>
public static class DeterministicDetectionLoop
{
    private sealed class TrialSortComparer : IComparer<ScenarioDetectionTrial>
    {
        public static readonly TrialSortComparer Instance = new();

        public int Compare(ScenarioDetectionTrial? x, ScenarioDetectionTrial? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var c = StringComparer.Ordinal.Compare(x.ObserverId, y.ObserverId);
            if (c != 0)
            {
                return c;
            }

            c = StringComparer.Ordinal.Compare(x.SensorId, y.SensorId);
            if (c != 0)
            {
                return c;
            }

            return StringComparer.Ordinal.Compare(x.TargetId, y.TargetId);
        }
    }

    /// <summary>Sort trials in-place: ObserverId → SensorId → TargetId (Ordinal).</summary>
    public static void SortTrials(ScenarioDetectionTrial[] trials)
    {
        if (trials.Length < 2)
        {
            return;
        }

        Array.Sort(trials, TrialSortComparer.Instance);
    }

    /// <summary>Return a sorted copy: ObserverId → SensorId → TargetId (Ordinal).</summary>
    public static ScenarioDetectionTrial[] SortTrialsCopy(IReadOnlyList<ScenarioDetectionTrial> trials)
    {
        if (trials.Count == 0)
        {
            return Array.Empty<ScenarioDetectionTrial>();
        }

        var copy = trials.ToArray();
        SortTrials(copy);
        return copy;
    }

    public static IReadOnlyList<DetectionRollResult> RollTick(
        SimSeed seed,
        ulong simTick,
        IReadOnlyList<ScenarioDetectionTrial> trials,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon,
        IReadOnlyCollection<string>? alreadyDetectedContactIds = null,
        IReadOnlyList<ScenarioJammer>? jammers = null,
        ICatalogReader? catalog = null,
        bool trialsPreSorted = false)
    {
        if (trials.Count == 0)
        {
            return Array.Empty<DetectionRollResult>();
        }

        var sorted = trialsPreSorted ? trials : SortTrialsCopy(trials);

        var results = new List<DetectionRollResult>(sorted.Count);
        var drawIndex = 0;
        foreach (var trial in sorted)
        {
            if (alreadyDetectedContactIds != null &&
                alreadyDetectedContactIds.Contains(trial.ContactId))
            {
                continue;
            }

            if (trial.RequiresActiveRadar &&
                ScenarioEmconResolver.ResolveRadar(trial.ObserverId, unitRadarEmcon, catalog) != EmconState.Active)
            {
                continue;
            }

            var jamStrength = trial.JamStrength;
            if (jammers is { Count: > 0 })
            {
                jamStrength = Math.Max(
                    jamStrength,
                    ScenarioJamResolver.ResolveJam(trial.ObserverId, trial.TargetId, simTick, jammers));
            }

            var pd = DetectionProbability.ComputePd(
                trial.BasePd,
                trial.EnvMask,
                trial.EccmFactor,
                jamStrength: jamStrength);
            var entityId = DetectionEntityId.FromTrial(trial.ObserverId, trial.SensorId, trial.TargetId);
            var draw = SeededRng.UnitFloat(seed, RngDomain.Detection, entityId, simTick, drawIndex++);
            var detected = draw < pd;
            results.Add(new DetectionRollResult(trial, pd, draw, detected));
        }

        return results;
    }
}