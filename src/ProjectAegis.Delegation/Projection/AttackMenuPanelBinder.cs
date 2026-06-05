namespace ProjectAegis.Delegation.Projection;

/// <summary>Maps attack menu options to UI Toolkit button names (req 14 / doc 20).</summary>
public static class AttackMenuPanelBinder
{
    public static string? ResolveButtonName(string optionId) =>
        optionId switch
        {
            "fire-single" => "attack-fire-single",
            "fire-salvo" => "attack-fire-salvo",
            "hold-fire" => "attack-hold-fire",
            _ => null,
        };

    public static EngageAttackOptions.AttackOption? FindOption(
        IReadOnlyList<EngageAttackOptions.AttackOption> menu,
        string optionId)
    {
        foreach (var option in menu)
        {
            if (string.Equals(option.Id, optionId, StringComparison.Ordinal))
            {
                return option;
            }
        }

        return null;
    }
}