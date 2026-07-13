namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Binds an <see cref="OrderLifecycleState"/> to chip display text and Phase 0 USS utility class
/// (<c>.order-state--*</c> in <c>unity/ProjectAegis/Assets/UI/AegisTokens.uss</c>). Per the AA a11y note
/// on those tokens, colour is a secondary cue only — every state pairs a distinct glyph with a text
/// label so colourblind/monochrome users can still distinguish states.
/// </summary>
public static class OrderStateChipBinder
{
    public readonly record struct ChipDisplay(string Text, string CssClass);

    public static ChipDisplay Bind(OrderLifecycleState state) => state switch
    {
        OrderLifecycleState.Accepted => new ChipDisplay("○ ACCEPTED", "order-state--accepted"),
        OrderLifecycleState.Queued => new ChipDisplay("◔ QUEUED", "order-state--queued"),
        OrderLifecycleState.Executing => new ChipDisplay("◑ EXECUTING", "order-state--executing"),
        OrderLifecycleState.Completed => new ChipDisplay("● COMPLETED", "order-state--completed"),
        OrderLifecycleState.Denied => new ChipDisplay("✕ DENIED", "order-state--denied"),
        OrderLifecycleState.Aborted => new ChipDisplay("⊘ ABORTED", "order-state--aborted"),
        _ => new ChipDisplay("— —", string.Empty),
    };
}
