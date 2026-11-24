namespace ProjectAegis.Delegation.Projection;

using System;
using System.Collections.Generic;

/// <summary>
/// One row in the message-log alert banner (DRG-408).
/// </summary>
public sealed record MessageLogAlertBannerRow(
    ulong SequenceId,
    string Prefix,
    string Category,
    string Text,
    bool ShowAck,
    bool CanAck,
    bool CanDismiss,
    bool IsPauseAlert,
    bool IsDismissed,
    bool IsAcknowledged);

/// <summary>
/// Pure headless projection selecting which message-log alerts to surface in the HUD alert banner.
/// Prioritizes unresolved pause alerts; when none remain, surfaces the latest active non-dismissed
/// alert (such as a non-pause map balloon) so every balloon has a HUD dismiss path.
/// </summary>
public static class MessageLogAlertBannerProjection
{
    public static IReadOnlyList<MessageLogAlertBannerRow> Project(
        IReadOnlyList<MessageNotificationAlert>? unresolvedPauseAlerts,
        IReadOnlyList<MessageNotificationAlert>? activeAlerts,
        bool isReplayViewerAttached)
    {
        if (unresolvedPauseAlerts is not null && unresolvedPauseAlerts.Count > 0)
        {
            var rows = new List<MessageLogAlertBannerRow>(unresolvedPauseAlerts.Count);
            for (var i = 0; i < unresolvedPauseAlerts.Count; i++)
            {
                var alert = unresolvedPauseAlerts[i];
                rows.Add(new MessageLogAlertBannerRow(
                    alert.SequenceId,
                    "[PAUSE]",
                    alert.Category,
                    alert.Text,
                    ShowAck: true,
                    CanAck: !isReplayViewerAttached,
                    CanDismiss: !isReplayViewerAttached && !alert.IsDismissed,
                    IsPauseAlert: true,
                    IsDismissed: alert.IsDismissed,
                    IsAcknowledged: alert.IsAcknowledged));
            }

            return rows;
        }

        if (activeAlerts is not null && activeAlerts.Count > 0)
        {
            for (var i = activeAlerts.Count - 1; i >= 0; i--)
            {
                var alert = activeAlerts[i];
                if (alert.IsDismissed)
                {
                    continue;
                }

                if (!alert.TriggersMapBalloon && alert.TriggersPause)
                {
                    continue;
                }

                var prefix = alert.TriggersMapBalloon ? "[BALLOON]" : "[ALERT]";
                return new[]
                {
                    new MessageLogAlertBannerRow(
                        alert.SequenceId,
                        prefix,
                        alert.Category,
                        alert.Text,
                        ShowAck: false,
                        CanAck: false,
                        CanDismiss: !isReplayViewerAttached,
                        IsPauseAlert: alert.TriggersPause,
                        IsDismissed: alert.IsDismissed,
                        IsAcknowledged: alert.IsAcknowledged),
                };
            }
        }

        return Array.Empty<MessageLogAlertBannerRow>();
    }
}
