namespace ProjectAegis.Delegation.ResourceRank;

/// <summary>Named exclusion / not-preferred reasons for resource ranking (never silent drops).</summary>
public static class ResourceRankReasonCode
{
    public const string ExcludedByCommitment = "EXCLUDED_BY_COMMITMENT";
    public const string ExcludedByAvailability = "EXCLUDED_BY_AVAILABILITY";
    public const string ExcludedByPolicy = "EXCLUDED_BY_POLICY";
    public const string ExcludedByEngage = "EXCLUDED_BY_ENGAGE";
    public const string NotPreferredLowerEffect = "NOT_PREFERRED_LOWER_EFFECT";
    public const string NotPreferredTime = "NOT_PREFERRED_TIME";
    public const string NotPreferredConservation = "NOT_PREFERRED_CONSERVE_MAGAZINE";
}
