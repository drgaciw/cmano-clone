namespace ProjectAegis.Delegation.Gameplay;

/// <summary>
/// Authoritative escort world rules. Time, geometry and contact knowledge are headless;
/// casualties are supplied only from the combat resolver, never from scheduled demonstration events.
/// </summary>
public sealed class EscortMissionState
{
    private long _steps;
    private double _activeObservation;
    private double _passiveObservation;
    private double _lastFriendlyTrackTime = double.NegativeInfinity;

    /// <summary>Fixed simulation step shared by kinematics and the engagement clock.</summary>
    public const double FixedDeltaSeconds = 1.0 / 60.0;

    /// <summary>Constructs a fresh world from validated mission data.</summary>
    public EscortMissionState(EscortMissionDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        definition.Validate();
    }

    /// <summary>The immutable mission tuning.</summary>
    public EscortMissionDefinition Definition { get; }
    /// <summary>Elapsed fixed simulation time.</summary>
    public double SimTime => _steps * FixedDeltaSeconds;
    /// <summary>Convoy east position in nautical miles.</summary>
    public double ConvoyXNm { get; private set; }
    /// <summary>Escort east position in nautical miles.</summary>
    public double EscortXNm { get; private set; }
    /// <summary>Whether the convoy currently holds position.</summary>
    public bool ConvoyHolding { get; private set; }
    /// <summary>Whether the escort currently holds position.</summary>
    public bool EscortHolding { get; private set; }
    /// <summary>Current emissions posture.</summary>
    public bool RadarActive { get; private set; }
    /// <summary>Whether the enemy has enough information to target the convoy.</summary>
    public bool EnemyHasTrack => _activeObservation >= Definition.RadarExposureSeconds || SimTime >= Definition.EnemyVisualDetectionSeconds;
    /// <summary>The player's observed surface-contact state, with no enemy knowledge leak.</summary>
    public string ContactState { get; private set; } = "Unknown";
    /// <summary>Whether the friendly side has a usable identified fire-control solution.</summary>
    public bool HasFriendlyTrack => ThreatAlive && (SimTime - _lastFriendlyTrackTime <= Definition.TrackMemorySeconds);
    /// <summary>Authoritative convoy liveness from combat resolution.</summary>
    public bool ConvoyAlive { get; private set; } = true;
    /// <summary>Authoritative escort liveness from combat resolution.</summary>
    public bool EscortAlive { get; private set; } = true;
    /// <summary>Authoritative threat liveness from combat resolution.</summary>
    public bool ThreatAlive { get; private set; } = true;
    /// <summary>Terminal world state.</summary>
    public bool IsComplete { get; private set; }
    /// <summary>True only when the convoy reached the exit alive before its deadline.</summary>
    public bool Succeeded { get; private set; }
    /// <summary>Stable terminal explanation.</summary>
    public string Outcome { get; private set; } = string.Empty;
    /// <summary>Current mission delegation doctrine.</summary>
    public EscortPosture Posture { get; private set; } = EscortPosture.Manual;

    /// <summary>Applies a validated emissions command.</summary>
    public void SetRadar(bool active) => RadarActive = active;
    /// <summary>Applies a validated delegation doctrine command.</summary>
    public void SetPosture(EscortPosture posture) => Posture = posture;
    /// <summary>Applies a convoy order after its command queue drains.</summary>
    public void SetConvoyHolding(bool hold) => ConvoyHolding = hold;
    /// <summary>Applies an escort order after its command queue drains.</summary>
    public void SetEscortHolding(bool hold) => EscortHolding = hold;

    /// <summary>Advances motion and side-limited contact observation by exactly one simulation step.</summary>
    public void Advance()
    {
        if (IsComplete) return;
        _steps++;
        if (RadarActive) _activeObservation += FixedDeltaSeconds;
        else _passiveObservation += FixedDeltaSeconds;
        if (ThreatAlive)
        {
            if ((RadarActive && _activeObservation >= Definition.ActiveTrackSeconds) || (!RadarActive && _passiveObservation >= Definition.PassiveTrackSeconds))
            {
                ContactState = "Identified";
                _lastFriendlyTrackTime = SimTime;
            }
            else if (ContactState != "Identified" && (_activeObservation >= Definition.ActiveTrackSeconds / 2 || _passiveObservation >= Definition.PassiveTrackSeconds / 2))
                ContactState = "Classified";
            else if ((ContactState is "Unknown" or "Detected") && (RadarActive || _passiveObservation >= Definition.PassiveTrackSeconds / 3))
                ContactState = "Detected";
        }
        else ContactState = "Lost";
    }

    /// <summary>Advances positions after this step's queued movement orders have drained.</summary>
    public void AdvanceKinematics()
    {
        if (IsComplete) return;
        if (ConvoyAlive && !ConvoyHolding)
            ConvoyXNm = Math.Min(Definition.ExitDistanceNm, ConvoyXNm + Definition.ConvoySpeedKnots * FixedDeltaSeconds / 3600.0);
        if (EscortAlive && !EscortHolding)
        {
            var destination = Math.Min(Definition.ExitDistanceNm, ConvoyXNm + Definition.EscortLeadDistanceNm);
            EscortXNm = Math.Min(destination, EscortXNm + Definition.EscortSpeedKnots * FixedDeltaSeconds / 3600.0);
        }
    }

    /// <summary>Consumes resolver-confirmed destroyed units before evaluating the objective.</summary>
    public void ObserveCombat(bool convoyAlive, bool escortAlive, bool threatAlive)
    {
        ConvoyAlive = convoyAlive;
        EscortAlive = escortAlive;
        ThreatAlive = threatAlive;
        if (!ThreatAlive) ContactState = "Lost";
    }

    /// <summary>Evaluates outcome once per completed simulation step; terminal outcomes never change.</summary>
    public void EvaluateObjective()
    {
        if (IsComplete) return;
        if (!ConvoyAlive) Finish(false, "Convoy destroyed before reaching the exit.");
        else if (ConvoyXNm >= Definition.ExitDistanceNm - 1e-9 && SimTime <= Definition.DeadlineSeconds + 1e-9)
            Finish(true, "Convoy reached the exit alive.");
        else if (SimTime >= Definition.DeadlineSeconds) Finish(false, "Deadline expired before the convoy reached the exit.");
    }

    /// <summary>Live distance from a shooter to its mission target, in nautical miles.</summary>
    public double RangeNm(bool enemyShooter)
    {
        var x = Definition.EnemyStartXNm - (enemyShooter ? ConvoyXNm : EscortXNm);
        return Math.Sqrt(x * x + Definition.EnemyStartYNm * Definition.EnemyStartYNm);
    }

    private void Finish(bool succeeded, string outcome)
    {
        IsComplete = true;
        Succeeded = succeeded;
        Outcome = outcome;
    }
}
