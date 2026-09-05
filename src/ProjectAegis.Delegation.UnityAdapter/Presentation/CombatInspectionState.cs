namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>UI-local engagement inspection; never enters simulation commands or replay hashes.</summary>
public sealed class CombatInspectionState
{
    /// <summary>Explicitly inspected engagement key; null follows contact/unit selection.</summary>
    public string? SelectedKey { get; private set; }

    /// <summary>Selects or clears event inspection without selecting a shooter for a command.</summary>
    public void Select(string? key) => SelectedKey = key;
}
