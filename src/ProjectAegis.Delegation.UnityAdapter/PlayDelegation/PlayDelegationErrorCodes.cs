namespace ProjectAegis.Delegation.UnityAdapter.PlayDelegation;

/// <summary>
/// Stable play-delegation error / block codes (S124-05 W2-DEL-04, S124-06 W2-DEL-02).
/// No-package failures reuse <see cref="PlayEntry.PlayEntryErrorCodes.NoPackage"/>.
/// </summary>
public static class PlayDelegationErrorCodes
{
    public const string NotExecuting = "NOT_EXECUTING";
    public const string TargetNotCommanded = "TARGET_NOT_COMMANDED";
    public const string UnknownPreset = "UNKNOWN_PRESET";
    public const string AssignDenied = "ASSIGN_DENIED";
    public const string NoAgent = "NO_AGENT";
    public const string RebriefDenied = "REBRIEF_DENIED";
}
