namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Tick-4 MVP: sorted detection trials with SeededRng Detection domain.</summary>
public static class DeterministicDetectionLoop
{
    public static IReadOnlyList<DetectionRollResult> RollTick(
        SimSeed seed,
        ulong simTick,
        IReadOnlyList<ScenarioDetectionTrial> trials,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon,
        IReadOnlyCollection<string>? alreadyDetectedContactIds = null,
        IReadOnlyList<ScenarioJammer>? jammers = null)
    {
        if (trials.Count == 0)
        {
            return Array.Empty<DetectionRollResult>();
        }

        var sorted = trials
            .OrderBy(t => t.ObserverId, StringComparer.Ordinal)
            .ThenBy(t => t.SensorId, StringComparer.Ordinal)
            .ThenBy(t => t.TargetId, StringComparer.Ordinal)
            .ToArray();

        var results = new List<DetectionRollResult>(sorted.Length);
        var drawIndex = 0;
        foreach (var trial in sorted)
        {
            if (alreadyDetectedContactIds != null &&
                alreadyDetectedContactIds.Contains(trial.ContactId))
            {
                continue;
            }

            if (trial.RequiresActiveRadar &&
                ScenarioEmconResolver.ResolveRadar(trial.ObserverId, unitRadarEmcon) != EmconState.Active)
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

            var pd = DetectionProbability.ComputePd(trial.BasePd, trial.EnvMask, jamStrength: jamStrength);
            var entityId = DetectionEntityId.FromTrial(trial.ObserverId, trial.SensorId, trial.TargetId);
            var draw = SeededRng.UnitFloat(seed, RngDomain.Detection, entityId, simTick, drawIndex++);
            var detected = draw < pd;
            results.Add(new DetectionRollResult(trial, pd, draw, detected));
        }

        return results;
    }
}