namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;

/// <summary>
/// DRG-66: presentation projection for the pending-approval queue.
/// Converts raw <see cref="PendingApprovalEntry"/> items into UI-ready lines
/// suitable for a list panel with APPROVE / REJECT buttons.
/// </summary>
public static class PendingApprovalProjection
{
    public const string EmptyStateLine = "No orders pending approval.";
    public const string HeaderLine = "PENDING APPROVAL";

    /// <summary>
    /// Projects the current <paramref name="entries"/> into a list of display rows.
    /// Returns an empty list when there are no pending approvals.
    /// </summary>
    public static IReadOnlyList<PendingApprovalRow> Project(
        IReadOnlyList<PendingApprovalEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return Array.Empty<PendingApprovalRow>();
        }

        var result = new List<PendingApprovalRow>(entries.Count);
        foreach (var entry in entries)
        {
            var order = entry.Order;
            result.Add(new PendingApprovalRow(
                OrderId: order.Id,
                TargetId: order.Target,
                SummaryLine: FormatSummary(order),
                RiskLabel: order.Risk == RiskLevel.High ? "RISK: HIGH" : "RISK: LOW",
                IsHighRisk: order.Risk == RiskLevel.High));
        }

        return result;
    }

    /// <summary>
    /// Returns a single-line aggregate badge for the HUD or top-bar.
    /// E.g. "PENDING: 3 (2 HIGH)" or "PENDING: —".
    /// </summary>
    public static string FormatBadge(IReadOnlyList<PendingApprovalEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return "PENDING: —";
        }

        var highCount = 0;
        foreach (var e in entries)
        {
            if (e.Order.Risk == RiskLevel.High)
            {
                highCount++;
            }
        }

        return highCount > 0
            ? $"PENDING: {entries.Count} ({highCount} HIGH)"
            : $"PENDING: {entries.Count}";
    }

    private static string FormatSummary(Order order) =>
        $"{order.Kind.ToString().ToUpperInvariant()} → {order.Target.Value}";
}

/// <summary>One row in the pending-approval panel list.</summary>
public sealed record PendingApprovalRow(
    OrderId OrderId,
    TargetId TargetId,
    string SummaryLine,
    string RiskLabel,
    bool IsHighRisk);
