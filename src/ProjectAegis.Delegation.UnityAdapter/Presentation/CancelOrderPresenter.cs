using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Outcome of a UI cancel-order affordance (req 20 §Order lifecycle, TR-c2-006 residual presentation
/// path). Success means <see cref="DelegationBridge.TryCancelHumanOrder"/> cancelled a pending order
/// and logged <c>PlayerOrderCancelled</c>.
/// </summary>
public sealed record CancelOrderResult(
    bool Success,
    string? UnitId,
    string? FailureReason)
{
    public static CancelOrderResult Failed(string? unitId, string failureReason) =>
        new(false, unitId, failureReason);

    public static CancelOrderResult Succeeded(string unitId) =>
        new(true, unitId, null);
}

/// <summary>
/// UI-facing pure helper that wraps the existing
/// <see cref="DelegationBridge.TryCancelHumanOrder"/> command (no bridge signature changes). Closes
/// the TR-c2-006 residual presentation path: right-panel / context cancel buttons call this, which
/// resolves unit id → entity and emits the logged <c>PlayerOrderCancelled</c> intent.
/// </summary>
public static class CancelOrderPresenter
{
    /// <summary>
    /// Cancel the oldest pending human order for <paramref name="unitId"/> at <paramref name="simTime"/>.
    /// </summary>
    public static CancelOrderResult TryCancel(
        DelegationBridge? bridge,
        string? unitId,
        double simTime)
    {
        if (bridge == null)
        {
            return CancelOrderResult.Failed(unitId, "no-bridge");
        }

        if (string.IsNullOrEmpty(unitId))
        {
            return CancelOrderResult.Failed(unitId, "no-unit");
        }

        if (!TryResolveEntity(bridge, unitId!, out var entity))
        {
            return CancelOrderResult.Failed(unitId, "unit-not-found");
        }

        if (!bridge.TryCancelHumanOrder(entity, simTime, out var failureReason))
        {
            return CancelOrderResult.Failed(unitId, failureReason ?? "cancel-failed");
        }

        return CancelOrderResult.Succeeded(unitId!);
    }

    /// <summary>
    /// Cancel by entity key when the host already resolved selection → entity (avoids a second lookup).
    /// Still routes through <see cref="DelegationBridge.TryCancelHumanOrder"/> only.
    /// </summary>
    public static CancelOrderResult TryCancel(
        DelegationBridge? bridge,
        EntityKey entity,
        string? unitId,
        double simTime)
    {
        if (bridge == null)
        {
            return CancelOrderResult.Failed(unitId, "no-bridge");
        }

        if (!bridge.TryCancelHumanOrder(entity, simTime, out var failureReason))
        {
            return CancelOrderResult.Failed(unitId, failureReason ?? "cancel-failed");
        }

        return CancelOrderResult.Succeeded(unitId ?? entity.Value.ToString());
    }

    private static bool TryResolveEntity(DelegationBridge bridge, string unitId, out EntityKey entity)
    {
        entity = default;
        foreach (var binding in bridge.Registry.Bindings)
        {
            if (string.Equals(binding.TargetId.Value, unitId, StringComparison.Ordinal))
            {
                entity = binding.Entity;
                return true;
            }
        }

        return false;
    }
}
