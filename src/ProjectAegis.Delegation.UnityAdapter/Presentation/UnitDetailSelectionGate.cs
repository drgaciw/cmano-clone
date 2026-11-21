using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// UAT D24: selection is exclusive (hostile contact clears the unit selection). The right unit
/// panel must then show "no unit selected" instead of a primary-unit fallback with Fire enabled.
/// </summary>
public static class UnitDetailSelectionGate
{
    public const string NoSelectionReason = "NO_SELECTION";

    /// <summary>Returns the entry unchanged when a unit is selected; otherwise disables all attack options.</summary>
    public static UnitDetailEntry? Apply(UnitDetailEntry? entry, string? selectedUnitId)
    {
        if (entry is null || !string.IsNullOrEmpty(selectedUnitId))
        {
            return entry;
        }

        var menu = new List<EngageAttackOptions.AttackOption>(entry.AttackMenu.Count);
        foreach (var option in entry.AttackMenu)
        {
            menu.Add(option with { Enabled = false, DisabledReason = NoSelectionReason });
        }

        return entry with { AttackMenu = menu, AttackOptionsLabel = "ATTACK: " + NoSelectionReason };
    }
}
