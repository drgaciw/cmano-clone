namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Engage;

/// <summary>Maps interactive attack menu selection to player orders (req 14 / doc 20).</summary>
public static class EngageAttackOrderResolver
{
    public sealed record ResolvedOrder(OrderKind Kind, int? SalvoSize);

    public static bool TryResolve(
        string optionId,
        in EngageContext ctx,
        EngagePreview preview,
        out ResolvedOrder resolved,
        out string? failureReason)
    {
        resolved = default!;
        failureReason = null;
        var options = EngageAttackOptions.Build(ctx, preview);
        var match = options.FirstOrDefault(o => string.Equals(o.Id, optionId, StringComparison.Ordinal));
        if (match == null)
        {
            failureReason = "UNKNOWN_OPTION";
            return false;
        }

        if (!match.Enabled)
        {
            failureReason = match.DisabledReason ?? "BLOCKED";
            return false;
        }

        switch (optionId)
        {
            case "fire-single":
                resolved = new ResolvedOrder(OrderKind.Engage, 1);
                return true;
            case "fire-salvo":
                resolved = new ResolvedOrder(OrderKind.Engage, Math.Max(1, ctx.SalvoSize));
                return true;
            case "hold-fire":
                resolved = new ResolvedOrder(OrderKind.Hold, null);
                return true;
            default:
                failureReason = "UNKNOWN_OPTION";
                return false;
        }
    }
}