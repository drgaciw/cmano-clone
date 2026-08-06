namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Lifecycle of a player order as surfaced to the C2 UI (req 20 §Order lifecycle; GDD TR-c2-006).
/// Presentation-only taxonomy derived from order-log records — the UI displays it and never mutates
/// sim state (ADR-010). Terminal states are <see cref="Completed"/>, <see cref="Denied"/>, and
/// <see cref="Aborted"/>.
/// </summary>
/// <remarks>
/// Progression per req 20: <c>accepted → queued → executing → completed | denied | aborted</c>.
/// The order-log → state projection that produces these values is implemented in Track T2.
/// </remarks>
public enum OrderLifecycleState
{
    /// <summary>Intent received by the bridge command API and logged; not yet scheduled.</summary>
    Accepted,

    /// <summary>Scheduled behind other orders for the unit; awaiting execution.</summary>
    Queued,

    /// <summary>Currently executing (plotted course underway / weapon in flight).</summary>
    Executing,

    /// <summary>Terminal: executed to completion.</summary>
    Completed,

    /// <summary>Terminal: rejected by ROE/doctrine/policy (links to the "Why can't I fire?" explain).</summary>
    Denied,

    /// <summary>Terminal: cancelled before or during execution (e.g. <c>PlayerOrderCancelled</c>).</summary>
    Aborted,
}
