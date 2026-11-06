namespace ProjectAegis.Delegation.EngageNextAction;

/// <summary>
/// DRG-226: projects the next corrective action that would unblock a withheld engagement.
/// Presentation-only — never enqueues orders, resolves combat, or issues fire.
/// </summary>
public static class EngageNextActionProjection
{
    /// <summary>
    /// Projects next-action rows for each supplied withhold fact. Rows are sorted by shooter
    /// then weapon family (ordinal). Every row and the snapshot carry <c>IsFireOrder=false</c>.
    /// </summary>
    public static EngageNextActionSnapshot Project(IReadOnlyList<EngageNextActionInput>? inputs)
    {
        if (inputs is null || inputs.Count == 0)
        {
            return EngageNextActionSnapshot.Empty;
        }

        var rows = new List<EngageNextActionRow>(inputs.Count);
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            if (string.IsNullOrWhiteSpace(input.ShooterId) || string.IsNullOrWhiteSpace(input.WeaponFamilyId))
            {
                continue;
            }

            rows.Add(new EngageNextActionRow(
                input.ShooterId,
                input.WeaponFamilyId,
                input.WithholdReason,
                ResolveNextActionCode(input.WithholdReason),
                IsFireOrder: false));
        }

        if (rows.Count == 0)
        {
            return EngageNextActionSnapshot.Empty;
        }

        rows.Sort(static (a, b) =>
        {
            var shooter = string.Compare(a.ShooterId, b.ShooterId, StringComparison.Ordinal);
            return shooter != 0
                ? shooter
                : string.Compare(a.WeaponFamilyId, b.WeaponFamilyId, StringComparison.Ordinal);
        });

        return new EngageNextActionSnapshot(rows, IsFireOrder: false);
    }

    /// <summary>Projects a single withhold fact as a one-row snapshot.</summary>
    public static EngageNextActionSnapshot Project(EngageNextActionInput input) =>
        Project(new[] { input });

    private static string? ResolveNextActionCode(string? withholdReason)
    {
        if (string.IsNullOrWhiteSpace(withholdReason))
        {
            return null;
        }

        if (IsAmmoWithhold(withholdReason))
        {
            return EngageNextActionCodes.ReloadRearm;
        }

        if (IsRoeWithhold(withholdReason))
        {
            return EngageNextActionCodes.Approval;
        }

        return null;
    }

    private static bool IsAmmoWithhold(string withholdReason) =>
        ContainsToken(withholdReason, "WINCHESTER")
        || ContainsToken(withholdReason, "NO_AMMO");

    private static bool IsRoeWithhold(string withholdReason) =>
        ContainsToken(withholdReason, "ROE")
        || ContainsToken(withholdReason, "WEAPONS_TIGHT")
        || ContainsToken(withholdReason, "RoeHoldFire");

    private static bool ContainsToken(string value, string token) =>
        value.Contains(token, StringComparison.OrdinalIgnoreCase);
}
