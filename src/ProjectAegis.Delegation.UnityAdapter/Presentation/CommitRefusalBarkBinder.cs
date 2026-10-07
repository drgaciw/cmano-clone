using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Why a fire commit did not become a launch.</summary>
public enum CommitBarkKind
{
    /// <summary>The command façade rejected the commit before any order was issued.</summary>
    Refused = 1,

    /// <summary>The order was accepted but the order log recorded an engage abort or policy denial.</summary>
    Dropped = 2,
}

/// <summary>
/// S124-03 W2-C2-04: visible refuse/drop bark plus the message-log row carrying the same stable reason code.
/// Presentation-only; never a fire order (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record CommitRefusalBark(
    CommitBarkKind Kind,
    string ReasonCode,
    string? UnitId,
    string? OptionId,
    string BarkText,
    string CueClass,
    MessageLogLine LogLine,
    bool IsFireOrder);

/// <summary>Non-color USS cue tokens for the commit bark (text + border class).</summary>
public static class CommitRefusalBarkCueClasses
{
    public const string Refused = "commit-bark-cue--refused";
    public const string Dropped = "commit-bark-cue--dropped";

    /// <summary>All cue classes hosts must clear before applying the active cue.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Refused, Dropped };
}

/// <summary>Live-surface bark row with its USS cue class token.</summary>
public sealed record CommitRefusalBarkRow(string ElementName, string Text, string CueClass);

/// <summary>
/// Builds refuse/drop barks from the command façade failure reason or from already-projected
/// order-log entries. Drop barks reuse <see cref="MessageLogProjection"/> so the bark reason
/// always matches the message-log row.
/// </summary>
public static class CommitRefusalBarkBinder
{
    /// <summary>Message-log category for presentation-local refused commits (no order-log entry exists).</summary>
    public const string RefusedCategory = "COMMIT_REFUSED";

    /// <summary>Stable code when the command façade refused without a reason string.</summary>
    public const string EnqueueRejectedCode = "ENQUEUE_REJECTED";

    /// <summary>Stable code when an engage abort was logged without a reason code.</summary>
    public const string UnspecifiedCode = "UNSPECIFIED";

    /// <summary>UXML name for the bark line.</summary>
    public const string BarkElementName = "commit-bark-line";

    /// <summary>
    /// Bark for a commit the command façade refused. <paramref name="failureReason"/> is the façade's
    /// out reason verbatim; null/blank maps to <see cref="EnqueueRejectedCode"/>. The log line uses
    /// sequence 0 because it is presentation-local and never claims an order-log sequence.
    /// </summary>
    public static CommitRefusalBark Refused(string? unitId, string optionId, string? failureReason, double simTime)
    {
        if (optionId is null)
        {
            throw new ArgumentNullException(nameof(optionId));
        }

        var code = string.IsNullOrWhiteSpace(failureReason) ? EnqueueRejectedCode : failureReason.Trim();
        var hasUnit = !string.IsNullOrWhiteSpace(unitId);
        var bark = hasUnit
            ? $"{unitId} REFUSED {optionId}: {code}"
            : $"REFUSED {optionId}: {code}";
        var text = hasUnit
            ? $"Commit refused for {unitId}: {code} ({optionId})"
            : $"Commit refused: {code} ({optionId})";

        return new CommitRefusalBark(
            CommitBarkKind.Refused,
            code,
            hasUnit ? unitId : null,
            optionId,
            bark,
            CommitRefusalBarkCueClasses.Refused,
            new MessageLogLine(0, simTime, RefusedCategory, text, hasUnit ? unitId : null),
            IsFireOrder: false);
    }

    /// <summary>Bark for a refused commit using the strip's primary reason (the façade's refusal code).</summary>
    public static CommitRefusalBark Refused(string? unitId, CommitConstraintStripState strip, double simTime)
    {
        if (strip is null)
        {
            throw new ArgumentNullException(nameof(strip));
        }

        return Refused(unitId, strip.OptionId, strip.PrimaryReasonCode, simTime);
    }

    /// <summary>
    /// Bark for an accepted commit the order log later dropped (engage abort or policy denial).
    /// Returns false for any other entry, including launched engagements.
    /// </summary>
    public static bool TryDropped(OrderLogEntry entry, out CommitRefusalBark? bark)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        bark = null;
        string code;
        string unitId;
        switch (entry.Payload)
        {
            case EngagementRecord { Launched: false } engagement when entry.Kind == OrderLogEntryKind.Engagement:
                code = string.IsNullOrWhiteSpace(engagement.AbortReasonCode)
                    ? UnspecifiedCode
                    : engagement.AbortReasonCode!;
                unitId = engagement.ShooterTargetId.Value;
                break;
            case PolicyDenialRecord denial when entry.Kind == OrderLogEntryKind.PolicyDenial:
                code = denial.Reason.ToString();
                unitId = denial.TargetId.Value;
                break;
            default:
                return false;
        }

        var lines = MessageLogProjection.Project(new[] { entry });
        if (lines.Count == 0)
        {
            return false;
        }

        bark = new CommitRefusalBark(
            CommitBarkKind.Dropped,
            code,
            unitId,
            null,
            $"{unitId} DROPPED: {code}",
            CommitRefusalBarkCueClasses.Dropped,
            lines[0],
            IsFireOrder: false);
        return true;
    }

    /// <summary>Drop barks for <paramref name="entries"/> in log order, optionally for one unit.</summary>
    public static IReadOnlyList<CommitRefusalBark> ProjectDropped(
        IReadOnlyList<OrderLogEntry> entries,
        string? unitId = null)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        var barks = new List<CommitRefusalBark>();
        for (var i = 0; i < entries.Count; i++)
        {
            if (TryDropped(entries[i], out var bark)
                && (unitId is null || string.Equals(bark!.UnitId, unitId, StringComparison.Ordinal)))
            {
                barks.Add(bark!);
            }
        }

        return barks;
    }

    /// <summary>Maps a bark onto the bark element and cue class.</summary>
    public static CommitRefusalBarkRow BindRow(CommitRefusalBark bark)
    {
        if (bark is null)
        {
            throw new ArgumentNullException(nameof(bark));
        }

        return new CommitRefusalBarkRow(BarkElementName, bark.BarkText, bark.CueClass);
    }
}
