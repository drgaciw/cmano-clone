namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Policy;

/// <summary>
/// Binds doctrine inheritance projection to UI-ready panel state (req 13 P0 / req 20 T3).
/// Surfaces EMCON + WRA summary lines and positive-control flag from existing projections.
/// </summary>
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
                "EMCON: —",
                "WRA: —",
                "POSITIVE_CONTROL: —",
                "SOURCE: —",
                "OVERRIDE: UNAVAILABLE",
                false,
                Array.Empty<RoeLevelOption>());
        }

        var options = BuildRoeOptions(entry.EffectiveRoeLabel);
        var canOverride = !entry.IsInheritedFromMission || entry.HasLocalOverride;
        var wraLine = FormatWraSummary(entry.EffectiveMaxSalvoLabel);
        var positiveControlLine = FormatPositiveControlLine(entry.EffectiveRoeLabel);

        return new DoctrineInheritancePanelState(
            $"UNIT: {entry.UnitId}",
            entry.EffectiveRoeLabel,
            entry.EffectiveMaxSalvoLabel,
            entry.EffectiveEmconLabel,
            wraLine,
            positiveControlLine,
            entry.InheritanceSource,
            entry.OverrideButtonLabel,
            canOverride,
            options);
    }

    /// <summary>
    /// Derives a WRA summary line from the max-salvo projection label ("SALVO: N" → "WRA: max salvo N").
    /// </summary>
    public static string FormatWraSummary(string maxSalvoLabel)
    {
        if (string.IsNullOrWhiteSpace(maxSalvoLabel) || maxSalvoLabel.Contains('—'))
        {
            return "WRA: —";
        }

        const string prefix = "SALVO:";
        if (maxSalvoLabel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var value = maxSalvoLabel[prefix.Length..].Trim();
            return string.IsNullOrEmpty(value) ? "WRA: —" : $"WRA: max salvo {value}";
        }

        return $"WRA: {maxSalvoLabel}";
    }

    /// <summary>
    /// Surfaces <see cref="PositiveControlRequiredProjection"/> for the ROE label on the doctrine panel.
    /// </summary>
    public static string FormatPositiveControlLine(string roeLabel)
    {
        if (string.IsNullOrWhiteSpace(roeLabel) || roeLabel.Contains('—'))
        {
            return "POSITIVE_CONTROL: —";
        }

        foreach (RoeLevel level in Enum.GetValues(typeof(RoeLevel)))
        {
            if (roeLabel.Contains(level.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return PositiveControlRequiredProjection.IsRequired(level)
                    ? "POSITIVE_CONTROL: REQUIRED"
                    : "POSITIVE_CONTROL: NOT_REQUIRED";
            }
        }

        return "POSITIVE_CONTROL: —";
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
