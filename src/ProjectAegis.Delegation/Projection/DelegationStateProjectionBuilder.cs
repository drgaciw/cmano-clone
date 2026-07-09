namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;

/// <summary>
/// Req 20 P0 / ADR-019: pure builder for <see cref="DelegationStateProjection"/> from controller
/// ownership (human / agent / mixed override). Presentation binds results only — no sim mutation.
/// </summary>
public static class DelegationStateProjectionBuilder
{
    /// <summary>
    /// Resolve ownership badge from the controller slot: human-only, agent-only, or mixed
    /// (human active with a suspended agent under C5 override).
    /// </summary>
    public static DelegationOwnerKind ResolveOwner(ControllerSlot slot)
    {
        if (slot is null)
        {
            throw new ArgumentNullException(nameof(slot));
        }

        var hasHuman = slot.Active is HumanController;
        var hasAgent = slot.Active is AgentController || slot.SuspendedAgent is not null;

        if (hasHuman && hasAgent)
        {
            return DelegationOwnerKind.Mixed;
        }

        if (hasAgent)
        {
            return DelegationOwnerKind.Agent;
        }

        // Unassigned or pure human — player-owned default for OOB badge.
        return DelegationOwnerKind.Human;
    }

    /// <summary>
    /// Build a single-unit projection. <paramref name="paused"/> is session gate state (bridge-owned),
    /// not order-log state.
    /// </summary>
    public static DelegationStateProjection FromSlot(
        string unitId,
        ControllerSlot slot,
        bool paused)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            throw new ArgumentException("Unit id is required.", nameof(unitId));
        }

        if (slot is null)
        {
            throw new ArgumentNullException(nameof(slot));
        }

        var owner = ResolveOwner(slot);
        var agent = ResolveAgent(slot);
        var autonomy = agent?.Autonomy ?? AutonomyLevel.Manual;
        var personality = agent?.PersonalitySlug ?? string.Empty;
        return new DelegationStateProjection(unitId, owner, autonomy, personality, paused);
    }

    /// <summary>Project every binding in registration order (stable for UI lists).</summary>
    public static IReadOnlyList<DelegationStateProjection> ProjectAll(
        IEnumerable<(string UnitId, ControllerSlot Slot)> units,
        Func<string, bool> isPaused)
    {
        if (units is null)
        {
            throw new ArgumentNullException(nameof(units));
        }

        if (isPaused is null)
        {
            throw new ArgumentNullException(nameof(isPaused));
        }

        var list = new List<DelegationStateProjection>();
        foreach (var (unitId, slot) in units)
        {
            list.Add(FromSlot(unitId, slot, isPaused(unitId)));
        }

        return list;
    }

    private static AgentController? ResolveAgent(ControllerSlot slot) =>
        slot.Active as AgentController ?? slot.SuspendedAgent;
}
