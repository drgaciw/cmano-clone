namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Policy;

/// <summary>Binds doctrine inheritance projection to UI-ready panel state (req 13 P0).</summary>
public static class DoctrineInheritancePanelBinder
{
    public static DoctrineInheritancePanelState Bind(DoctrineInheritanceEntry? entry)
    {
        if (entry == null)
        {
            return new DoctrineInheritancePanelState(
                "UNIT: —",
                "ROE: —",
                "SALVO: —",
                "SOURCE: —",
                "OVERRIDE: UNAVAILABLE",
                false,
                Array.Empty<RoeLevelOption>());
        }

        var options = BuildRoeOptions(entry.EffectiveRoeLabel);
        var canOverride = !entry.IsInheritedFromMission || entry.HasLocalOverride;

        return new DoctrineInheritancePanelState(
            $"UNIT: {entry.UnitId}",
            entry.EffectiveRoeLabel,
            entry.EffectiveMaxSalvoLabel,
            entry.InheritanceSource,
            entry.OverrideButtonLabel,
            canOverride,
            options);
    }

    private static IReadOnlyList<RoeLevelOption> BuildRoeOptions(string currentRoeLabel)
    {
        var roeLevels = new[] { RoeLevel.HoldFire, RoeLevel.WeaponsTight, RoeLevel.WeaponsFree };
        var options = new List<RoeLevelOption>();

        foreach (var roe in roeLevels)
        {
            var label = roe.ToString();
            var isCurrent = currentRoeLabel.Contains(label, StringComparison.OrdinalIgnoreCase);
            options.Add(new RoeLevelOption(label, label, isCurrent));
        }

        return options;
    }
}
