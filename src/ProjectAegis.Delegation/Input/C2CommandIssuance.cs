namespace ProjectAegis.Delegation.Input;

using ProjectAegis.Delegation.Core;

/// <summary>
/// Pure validation + command-id → <see cref="OrderKind"/> mapping for player C2 issuance (CMD-31).
/// No sim / bridge side effects — hosts call this then enqueue via the bridge.
/// </summary>
public readonly record struct C2CommandResult(bool Ok, string? FailureReason, OrderKind? Kind);

/// <summary>
/// Static pure helpers for resolving toolbar / hotkey command IDs to order kinds.
/// </summary>
public static class C2CommandIssuance
{
    public const string ReasonUnknownCommand = "UNKNOWN_COMMAND";
    public const string ReasonNoSelection = "NO_SELECTION";

    /// <summary>
    /// Map a command id string to an <see cref="OrderKind"/>.
    /// Unknown or empty ids fail with <see cref="ReasonUnknownCommand"/>.
    /// </summary>
    public static bool TryResolve(string? commandId, out OrderKind kind, out string? reason)
    {
        kind = default;
        reason = null;

        if (string.IsNullOrWhiteSpace(commandId))
        {
            reason = ReasonUnknownCommand;
            return false;
        }

        switch (commandId.Trim().ToLowerInvariant())
        {
            case "hold":
                kind = OrderKind.Hold;
                return true;
            case "rtb":
                kind = OrderKind.ReturnToBase;
                return true;
            case "move":
            case "plot_course":
                // plot_course is a semantic alias for Move (course plotting)
                kind = OrderKind.Move;
                return true;
            case "engage":
                kind = OrderKind.Engage;
                return true;
            case "set_emcon":
                kind = OrderKind.SetEmcon;
                return true;
            case "set_sensors":
                kind = OrderKind.SetSensors;
                return true;
            default:
                reason = ReasonUnknownCommand;
                return false;
        }
    }

    /// <summary>
    /// Full pre-issue validation: selection required, then command resolution.
    /// </summary>
    public static C2CommandResult Validate(string? commandId, bool hasSelection)
    {
        if (!hasSelection)
        {
            return new C2CommandResult(false, ReasonNoSelection, null);
        }

        if (!TryResolve(commandId, out var kind, out var reason))
        {
            return new C2CommandResult(false, reason ?? ReasonUnknownCommand, null);
        }

        return new C2CommandResult(true, null, kind);
    }
}
