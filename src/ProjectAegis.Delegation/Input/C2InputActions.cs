namespace ProjectAegis.Delegation.Input;

/// <summary>
/// Remappable input-action stub IDs for C2 keyboard parity (req 20 §Keyboard; a11y §6.3). These are
/// the single source of truth for the action IDs the sim resolves at session start (a11y §6.3 stub
/// contract). UI hosts bind default keys to these IDs; the remap table stores the resolved binding.
/// </summary>
/// <remarks>
/// a11y §6.3 defines <see cref="Cancel"/> and <see cref="FocusPrimaryThreat"/>. <see cref="CycleUnit"/>
/// is defined here per the UX spec interaction map (N / P cycle next/previous friendly unit) but is
/// NOT yet listed in accessibility-requirements.md §6.3 — cascade note: add <c>input.cycle_unit</c>
/// to a11y §6.3 so this constant and the doc agree. <see cref="Confirm"/> is defined here (Track T2,
/// req 20 §Order lifecycle) for the weapons-release confirmation gate — likewise NOT yet listed in
/// a11y §6.3; cascade note: add <c>input.confirm</c> alongside <c>input.cancel</c>.
/// </remarks>
public static class C2InputActions
{
    /// <summary>Cycle to the next / previous friendly unit (default N / P). UX spec §6.</summary>
    public const string CycleUnit = "input.cycle_unit";

    /// <summary>Centre the camera on the primary hostile (default F). a11y §6.3.</summary>
    public const string FocusPrimaryThreat = "input.focus_primary_threat";

    /// <summary>
    /// Close modal / cancel intent preview / cancel weapons-release gate (default Esc). a11y §6.3.
    /// </summary>
    public const string Cancel = "input.cancel";

    /// <summary>
    /// Confirm the weapons-release confirmation gate when ROE/doctrine requires positive control
    /// (default Enter). See <see cref="ProjectAegis.Delegation.Projection.WeaponsReleaseConfirmationGate"/>.
    /// </summary>
    public const string Confirm = "input.confirm";
}
