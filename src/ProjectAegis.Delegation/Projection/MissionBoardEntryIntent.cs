namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Edit-mode toggle intent for Mission Board entry (req 20 §Mission and Editor Entry;
/// doc 11 Mission Board). P0 surface is <see cref="IntentKind"/> / string flag only —
/// no full editor shell.
/// </summary>
public sealed record MissionBoardEntryIntent(
    string IntentKind,
    bool EnterEditMode,
    string Detail = "");

/// <summary>
/// Pure resolver for edit-mode ↔ Mission Board entry. Replay always blocks the toggle.
/// </summary>
public static class MissionBoardEntryResolver
{
    /// <summary>Intent kind when entering edit mode / Mission Board.</summary>
    public const string EnterIntentKind = "mission_board_enter";

    /// <summary>Intent kind when leaving edit mode / Mission Board.</summary>
    public const string ExitIntentKind = "mission_board_exit";

    public const string FailureReplay = "REPLAY_BLOCKED";

    /// <summary>
    /// Returns an enter/exit intent that flips <paramref name="currentlyInEditMode"/>, or
    /// <c>null</c> when replay blocks the toggle.
    /// </summary>
    public static MissionBoardEntryIntent? TryToggle(bool currentlyInEditMode, bool isReplay, out string? failureReason)
    {
        if (isReplay)
        {
            failureReason = FailureReplay;
            return null;
        }

        failureReason = null;
        var enter = !currentlyInEditMode;
        return new MissionBoardEntryIntent(
            IntentKind: enter ? EnterIntentKind : ExitIntentKind,
            EnterEditMode: enter);
    }
}
