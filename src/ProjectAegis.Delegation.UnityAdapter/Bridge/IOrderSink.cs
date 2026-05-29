namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;

/// <summary>
/// Applies delegation orders back into the sim (movement, engage, etc.).
/// </summary>
public interface IOrderSink
{
    void ApplyOrder(EntityKey entity, in Order order);
}
