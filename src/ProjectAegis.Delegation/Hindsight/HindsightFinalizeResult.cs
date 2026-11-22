namespace ProjectAegis.Delegation.Hindsight;

/// <summary>Explicit outcome status for awaitable Hindsight session finalization (DRG-746).</summary>
public enum HindsightFinalizeStatus
{
    /// <summary>AAR/XP retention and reflection completed successfully.</summary>
    Succeeded = 0,

    /// <summary>Hindsight sidecar was disabled in <see cref="HindsightOptions"/>.</summary>
    Disabled = 1,

    /// <summary>Retention completed (when enabled), and reflection was skipped because no query was configured or AAR bank was disabled.</summary>
    Skipped = 2,

    /// <summary>Hindsight sidecar was unreachable, timed out, cancelled, or returned an error/null reflection.</summary>
    Unavailable = 3,
}

/// <summary>Result returned by <see cref="HindsightSessionFinalizer.OnScenarioFinalizedAsync"/> (DRG-746).</summary>
public sealed record HindsightFinalizeResult(
    HindsightFinalizeStatus Status,
    string? AarBankId,
    string? ReflectionText,
    string? Error = null)
{
    /// <summary>True when finalization and reflection completed successfully.</summary>
    public bool IsSuccess => Status == HindsightFinalizeStatus.Succeeded;
}
