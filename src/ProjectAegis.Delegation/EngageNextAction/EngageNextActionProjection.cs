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

    /// <summary>Maps a known withhold reason to an advisory action code; unknown reasons remain unmapped.</summary>
    public static string? ResolveNextActionCode(string? withholdReason)
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

        if (ContainsToken(withholdReason, "NO_FIRE_CONTROL") || ContainsToken(withholdReason, "STALE"))
        {
            return EngageNextActionCodes.AcquireTrack;
        }

        if (ContainsToken(withholdReason, "EMCON"))
        {
            return EngageNextActionCodes.ReviewEmissions;
        }

        if (ContainsToken(withholdReason, "DLZ") || ContainsToken(withholdReason, "WRA_RANGE")
            || ContainsToken(withholdReason, "OUT_OF_ENVELOPE"))
        {
            return EngageNextActionCodes.Reposition;
        }

        if (ContainsToken(withholdReason, "COMMS"))
        {
            return EngageNextActionCodes.RestoreComms;
        }

        if (ContainsToken(withholdReason, "MOUNT_OFFLINE"))
        {
            return EngageNextActionCodes.RepairMount;
        }

        if (ContainsToken(withholdReason, "AIR_NOT_READY"))
        {
            return EngageNextActionCodes.WaitForReadiness;
        }

        if (ContainsToken(withholdReason, "SPOOF") || ContainsToken(withholdReason, "NOT_IDENTIFIED"))
        {
            return EngageNextActionCodes.VerifyContact;
        }

        if (ContainsToken(withholdReason, "SALVO"))
        {
            return EngageNextActionCodes.ReviewSalvo;
        }

        return null;
    }

    /// <summary>Plain-language next step. Advice never changes doctrine or issues an order.</summary>
    public static string ExplainNextAction(string? withholdReason)
    {
        if (string.IsNullOrWhiteSpace(withholdReason))
        {
            return string.Empty;
        }

        return ResolveNextActionCode(withholdReason) switch
        {
            EngageNextActionCodes.ReloadRearm => "Rearm or select a shooter with sufficient rounds; preserve the defensive reserve.",
            EngageNextActionCodes.Approval => "Review identification and rules of engagement, then request authorization if appropriate.",
            EngageNextActionCodes.AcquireTrack => "Acquire or designate a fresh fire-control track; check sensor coverage and track age.",
            EngageNextActionCodes.ReviewEmissions => "Review the exposure risk before enabling radar, or seek an eligible passive or remote track.",
            EngageNextActionCodes.Reposition => "Check weapon range and doctrine limits, then reposition or choose a suitable shooter.",
            EngageNextActionCodes.RestoreComms => "Restore communications or review the unit's authorized local plan; repeated fire requests will not restore the link.",
            EngageNextActionCodes.RepairMount => "Restore the weapon mount or select an operational mount or shooter.",
            EngageNextActionCodes.WaitForReadiness => "Wait until the platform is ready, or select a ready asset.",
            EngageNextActionCodes.VerifyContact => "Verify the contact with independent observations before seeking weapon release.",
            EngageNextActionCodes.ReviewSalvo => "Assess previous shot effects and review the authorized salvo limit before requesting another engagement.",
            _ => "Review the engagement details and current doctrine before issuing another order.",
        };
    }

    private static bool IsAmmoWithhold(string withholdReason) =>
        ContainsToken(withholdReason, "WINCHESTER")
        || ContainsToken(withholdReason, "NO_AMMO");

    private static bool IsRoeWithhold(string withholdReason) =>
        ContainsToken(withholdReason, "ROE")
        || ContainsToken(withholdReason, "WEAPONS_TIGHT")
        || ContainsToken(withholdReason, "RoeHoldFire");

    private static bool ContainsToken(string value, string token) =>
        value.Contains(token, StringComparison.OrdinalIgnoreCase)
        || value.Replace("_", string.Empty).Contains(
            token.Replace("_", string.Empty), StringComparison.OrdinalIgnoreCase);
}
