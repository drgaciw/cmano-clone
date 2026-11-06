namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless symbology profile selection (S126-04 SYM-03). Holds only the selected profile;
/// it has no access to sim, order or replay state, so switching cannot mutate the world.
/// </summary>
public sealed class SymbologyProfileSwitch
{
    public SymbologyProfileSwitch(SymbologyProfile initial = SymbologyProfile.Legacy)
    {
        if (!SymbologyProfileCatalog.IsSelectable(initial))
        {
            throw new ArgumentException(SymbologyProfileCatalog.CivilianPendingReason, nameof(initial));
        }

        Current = initial;
    }

    /// <summary>Currently selected profile.</summary>
    public SymbologyProfile Current { get; private set; }

    /// <summary>Selects <paramref name="profile"/> when selectable; otherwise keeps the current profile.</summary>
    public bool TrySelect(SymbologyProfile profile, out string reason)
    {
        if (!SymbologyProfileCatalog.IsSelectable(profile))
        {
            reason = SymbologyProfileCatalog.CivilianPendingReason;
            return false;
        }

        Current = profile;
        reason = string.Empty;
        return true;
    }

    /// <summary>Toggles Legacy ↔ Military Tactical and returns the new profile.</summary>
    public SymbologyProfile Toggle()
    {
        Current = Current == SymbologyProfile.MilitaryTactical
            ? SymbologyProfile.Legacy
            : SymbologyProfile.MilitaryTactical;
        return Current;
    }
}
