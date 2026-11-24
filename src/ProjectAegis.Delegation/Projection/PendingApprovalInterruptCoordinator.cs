namespace ProjectAegis.Delegation.Projection;

using System;
using System.Collections.Generic;
using System.Globalization;
using Core;
using Orchestration;

/// <summary>DRG-269: one pending-approval interrupt card with authoritative sim-time DLZ countdown.</summary>
public sealed record PendingApprovalInterruptCard(
    OrderId OrderId,
    TargetId TargetId,
    string SummaryLine,
    string RiskLabel,
    bool IsHighRisk,
    double? RemainingSimSeconds,
    bool IsExpired,
    bool IsDlzUnknown,
    string CountdownLine,
    bool ForcedRealTimeIssued);

/// <summary>DRG-269: single-tick evaluation output for pending-approval interrupt coordination.</summary>
public sealed record PendingApprovalInterruptBatch(
    bool ShouldDropToRealTime,
    IReadOnlyList<OrderId> NewlyInterruptedOrderIds,
    IReadOnlyList<PendingApprovalInterruptCard> Cards)
{
    /// <summary>Empty interrupt batch when no orders are pending approval.</summary>
    public static PendingApprovalInterruptBatch Empty { get; } = new(
        ShouldDropToRealTime: false,
        NewlyInterruptedOrderIds: Array.Empty<OrderId>(),
        Cards: Array.Empty<PendingApprovalInterruptCard>());
}

/// <summary>DRG-269: fail-closed validation result for confirming or canceling a pending approval.</summary>
public sealed record PendingApprovalDecisionValidation(
    bool Allowed,
    string ReasonCode,
    string StatusLine);

/// <summary>
/// DRG-269 ([N-IRQ-01]): Coordinates pending-approval clock interrupts and authoritative sim-time DLZ countdowns.
/// Guarantees at most one 1x drop request per pending <see cref="OrderId"/>, suppresses interrupts in replay/observer
/// mode, halts countdowns automatically when sim time pauses, and renders UNKNOWN when authoritative DLZ data is absent.
/// </summary>
public sealed class PendingApprovalInterruptCoordinator
{
    public const string ReasonOk = "OK";
    public const string ReasonReplayReadOnly = "REPLAY_READ_ONLY";
    public const string ReasonObserverReadOnly = "OBSERVER_READ_ONLY";
    public const string ReasonApprovalStale = "APPROVAL_STALE";
    public const string ReasonDlzExpired = "DLZ_EXPIRED";

    private readonly HashSet<OrderId> _interruptedOrderIds = new();

    /// <summary>
    /// Evaluates current pending approval entries against authoritative <paramref name="currentSimTime"/> and
    /// optional authoritative DLZ expiry sim times. Issues at most one 1x drop per <see cref="OrderId"/>.
    /// </summary>
    public PendingApprovalInterruptBatch Evaluate(
        IReadOnlyList<PendingApprovalEntry>? pendingEntries,
        double currentSimTime,
        IReadOnlyDictionary<OrderId, double?>? dlzExpiryByOrder,
        bool isReplayViewer,
        bool isObserver)
    {
        if (pendingEntries is null || pendingEntries.Count == 0)
        {
            return PendingApprovalInterruptBatch.Empty;
        }

        var rows = PendingApprovalProjection.Project(pendingEntries);
        var newlyInterrupted = new List<OrderId>();
        var cards = new List<PendingApprovalInterruptCard>(rows.Count);
        var readOnly = isReplayViewer || isObserver;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var issuedNow = false;
            if (!readOnly && _interruptedOrderIds.Add(row.OrderId))
            {
                newlyInterrupted.Add(row.OrderId);
                issuedNow = true;
            }

            double? expiresAt = null;
            if (dlzExpiryByOrder is not null && dlzExpiryByOrder.TryGetValue(row.OrderId, out var expiry))
            {
                expiresAt = expiry;
            }

            double? remaining = null;
            var isDlzUnknown = true;
            var isExpired = false;
            string countdownLine;

            if (expiresAt.HasValue && !double.IsNaN(expiresAt.Value) && !double.IsInfinity(expiresAt.Value)
                && !double.IsNaN(currentSimTime) && !double.IsInfinity(currentSimTime))
            {
                isDlzUnknown = false;
                var delta = expiresAt.Value - currentSimTime;
                if (delta <= 0.0)
                {
                    isExpired = true;
                    remaining = 0.0;
                    countdownLine = "DLZ WINDOW: EXPIRED — approval stale";
                }
                else
                {
                    remaining = delta;
                    countdownLine = string.Format(
                        CultureInfo.InvariantCulture,
                        "DLZ WINDOW: {0:0.0}s remaining (sim time)",
                        delta);
                }
            }
            else
            {
                countdownLine = "DLZ WINDOW: UNKNOWN — authoritative DLZ data unavailable";
            }

            cards.Add(new PendingApprovalInterruptCard(
                OrderId: row.OrderId,
                TargetId: row.TargetId,
                SummaryLine: row.SummaryLine,
                RiskLabel: row.RiskLabel,
                IsHighRisk: row.IsHighRisk,
                RemainingSimSeconds: remaining,
                IsExpired: isExpired,
                IsDlzUnknown: isDlzUnknown,
                CountdownLine: countdownLine,
                ForcedRealTimeIssued: issuedNow));
        }

        return new PendingApprovalInterruptBatch(
            ShouldDropToRealTime: newlyInterrupted.Count > 0,
            NewlyInterruptedOrderIds: newlyInterrupted,
            Cards: cards);
    }

    /// <summary>
    /// Fail-closed validation before confirming or canceling a pending approval through the authority command boundary.
    /// </summary>
    public PendingApprovalDecisionValidation ValidateDecision(
        OrderId orderId,
        IReadOnlyList<PendingApprovalEntry>? livePending,
        double currentSimTime,
        double? expiresAtSimTime,
        bool isReplayViewer,
        bool isObserver)
    {
        if (isReplayViewer)
        {
            return new PendingApprovalDecisionValidation(
                Allowed: false,
                ReasonCode: ReasonReplayReadOnly,
                StatusLine: "READ-ONLY (REPLAY): approval commands disabled.");
        }

        if (isObserver)
        {
            return new PendingApprovalDecisionValidation(
                Allowed: false,
                ReasonCode: ReasonObserverReadOnly,
                StatusLine: "OBSERVER MODE: approval commands disabled.");
        }

        var exists = false;
        if (livePending is not null)
        {
            for (var i = 0; i < livePending.Count; i++)
            {
                if (livePending[i].Order.Id == orderId)
                {
                    exists = true;
                    break;
                }
            }
        }

        if (!exists)
        {
            return new PendingApprovalDecisionValidation(
                Allowed: false,
                ReasonCode: ReasonApprovalStale,
                StatusLine: "APPROVAL STALE: order is no longer pending.");
        }

        if (expiresAtSimTime.HasValue
            && !double.IsNaN(expiresAtSimTime.Value)
            && !double.IsInfinity(expiresAtSimTime.Value)
            && expiresAtSimTime.Value <= currentSimTime)
        {
            return new PendingApprovalDecisionValidation(
                Allowed: false,
                ReasonCode: ReasonDlzExpired,
                StatusLine: "DLZ EXPIRED: approval window closed; order cannot be confirmed.");
        }

        return new PendingApprovalDecisionValidation(
            Allowed: true,
            ReasonCode: ReasonOk,
            StatusLine: string.Empty);
    }
}
