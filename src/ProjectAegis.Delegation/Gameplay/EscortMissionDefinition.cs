namespace ProjectAegis.Delegation.Gameplay;

using System.Text.Json;

/// <summary>Validated, versioned tuning and briefing for a self-contained escort mission.</summary>
public sealed record EscortMissionDefinition
{
    /// <summary>Stable scenario identifier.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Player-facing title.</summary>
    public string Title { get; init; } = string.Empty;
    /// <summary>Pre-mission orders and tradeoffs.</summary>
    public string Briefing { get; init; } = string.Empty;
    /// <summary>Convoy distance to the exit, nautical miles.</summary>
    public double ExitDistanceNm { get; init; }
    /// <summary>Convoy transit speed, knots.</summary>
    public double ConvoySpeedKnots { get; init; }
    /// <summary>Escort transit speed, knots.</summary>
    public double EscortSpeedKnots { get; init; }
    /// <summary>Desired escort lead distance ahead of the convoy, nautical miles.</summary>
    public double EscortLeadDistanceNm { get; init; }
    /// <summary>Mission deadline in simulation seconds.</summary>
    public double DeadlineSeconds { get; init; }
    /// <summary>Map world width, nautical miles.</summary>
    public double TheaterWidthNm { get; init; }
    /// <summary>Initial threat east position, nautical miles.</summary>
    public double EnemyStartXNm { get; init; }
    /// <summary>Initial threat north position, nautical miles.</summary>
    public double EnemyStartYNm { get; init; }
    /// <summary>Friendly weapon range, nautical miles.</summary>
    public double FriendlyWeaponRangeNm { get; init; }
    /// <summary>Enemy weapon range, nautical miles.</summary>
    public double EnemyWeaponRangeNm { get; init; }
    /// <summary>Active radar time required to establish an identified fire-control track.</summary>
    public double ActiveTrackSeconds { get; init; }
    /// <summary>Passive observation time required to identify the surface contact.</summary>
    public double PassiveTrackSeconds { get; init; }
    /// <summary>Time before the enemy can visually acquire the convoy.</summary>
    public double EnemyVisualDetectionSeconds { get; init; }
    /// <summary>Enemy decision interval.</summary>
    public double EnemyReactionSeconds { get; init; }
    /// <summary>Friendly decision interval.</summary>
    public double AgentReactionSeconds { get; init; }
    /// <summary>Active emissions time before enemy targeting becomes possible.</summary>
    public double RadarExposureSeconds { get; init; }
    /// <summary>Track persistence after changing radar posture.</summary>
    public double TrackMemorySeconds { get; init; }
    /// <summary>Friendly starting magazine.</summary>
    public int FriendlyRounds { get; init; }
    /// <summary>Enemy starting magazine.</summary>
    public int EnemyRounds { get; init; }
    /// <summary>Cautious doctrine's retained defensive rounds.</summary>
    public int CautiousReserveRounds { get; init; }
    /// <summary>Balanced doctrine's retained defensive rounds.</summary>
    public int BalancedReserveRounds { get; init; }
    /// <summary>Friendly base hit probability.</summary>
    public double FriendlyPk { get; init; }
    /// <summary>Enemy base hit probability.</summary>
    public double EnemyPk { get; init; }
    /// <summary>Objective completion reward.</summary>
    public int SuccessPoints { get; init; }
    /// <summary>Reward for preserving the convoy.</summary>
    public int ConvoySurvivalPoints { get; init; }
    /// <summary>Reward for preserving the escort.</summary>
    public int EscortSurvivalPoints { get; init; }
    /// <summary>Cost per friendly round actually launched.</summary>
    public int RoundExpenditurePenalty { get; init; }
    /// <summary>Penalty for an actual unauthorized release, not a refused order.</summary>
    public int ViolationPenalty { get; init; }

    /// <summary>Loads the same checked-in mission data in tests and player builds.</summary>
    public static EscortMissionDefinition LoadDefault()
    {
        using var stream = typeof(EscortMissionDefinition).Assembly.GetManifestResourceStream("ProjectAegis.BalticEscortMission.json")
            ?? throw new InvalidOperationException("Embedded Baltic escort definition is missing.");
        using var reader = new StreamReader(stream);
        return FromJson(reader.ReadToEnd());
    }

    /// <summary>Loads an explicitly supplied mission document and validates its values.</summary>
    public static EscortMissionDefinition FromJson(string json)
    {
        var definition = JsonSerializer.Deserialize<EscortMissionDefinition>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Mission definition is empty.", nameof(json));
        definition.Validate();
        return definition;
    }

    /// <summary>Rejects values that would produce invalid clocks, geometry or combat probabilities.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Briefing))
            throw new ArgumentException("Mission id, title and briefing are required.");
        var positive = new[] { ExitDistanceNm, ConvoySpeedKnots, EscortSpeedKnots, DeadlineSeconds, TheaterWidthNm,
            FriendlyWeaponRangeNm, EnemyWeaponRangeNm, ActiveTrackSeconds, PassiveTrackSeconds,
            EnemyVisualDetectionSeconds, EnemyReactionSeconds, AgentReactionSeconds, RadarExposureSeconds, TrackMemorySeconds, EscortLeadDistanceNm };
        if (positive.Any(x => double.IsNaN(x) || double.IsInfinity(x) || x <= 0)
            || double.IsNaN(EnemyStartXNm) || double.IsInfinity(EnemyStartXNm)
            || double.IsNaN(EnemyStartYNm) || double.IsInfinity(EnemyStartYNm))
            throw new ArgumentException("Mission timing, speed and range values must be positive and finite.");
        if (ExitDistanceNm > TheaterWidthNm || EnemyStartXNm < 0 || EnemyStartXNm > TheaterWidthNm
            || Math.Abs(EnemyStartYNm) > TheaterWidthNm / 2)
            throw new ArgumentException("Mission positions must fit the supported theater map bounds.");
        if (FriendlyRounds < 1 || EnemyRounds < 1 || CautiousReserveRounds < 0 || BalancedReserveRounds < 0
            || CautiousReserveRounds > FriendlyRounds || BalancedReserveRounds > CautiousReserveRounds)
            throw new ArgumentException("Magazine sizes and reserve policies are inconsistent.");
        if (double.IsNaN(FriendlyPk) || double.IsNaN(EnemyPk) || FriendlyPk < 0 || FriendlyPk > 1 || EnemyPk < 0 || EnemyPk > 1)
            throw new ArgumentException("Combat probabilities must lie between zero and one.");
        if (SuccessPoints < 0 || ConvoySurvivalPoints < 0 || EscortSurvivalPoints < 0 || RoundExpenditurePenalty < 0 || ViolationPenalty < 0)
            throw new ArgumentException("Scoring values must be nonnegative.");
    }
}
