namespace ProjectAegis.Delegation.EmploymentLedger;

using ProjectAegis.Sim.Glossary;

/// <summary>
/// DRG-224: projects magazine / salvo employment facts into a deterministic advisory ledger.
/// Presentation-only — never enqueues orders, resolves combat, or issues fire.
/// </summary>
public static class EmploymentLedgerProjection
{
    /// <summary>
    /// Projects employment ledger rows for each supplied magazine fact. Rows are sorted by shooter
    /// then weapon family (ordinal). Every row and the snapshot carry <c>IsFireOrder=false</c>.
    /// </summary>
    public static EmploymentLedgerSnapshot Project(IReadOnlyList<EmploymentLedgerMagazineFacts>? magazines)
    {
        if (magazines is null || magazines.Count == 0)
        {
            return EmploymentLedgerSnapshot.Empty;
        }

        var rows = new List<EmploymentLedgerRow>(magazines.Count);
        for (var i = 0; i < magazines.Count; i++)
        {
            var facts = magazines[i];
            if (string.IsNullOrWhiteSpace(facts.ShooterId) || string.IsNullOrWhiteSpace(facts.WeaponFamilyId))
            {
                continue;
            }

            var salvoSize = Math.Max(1, facts.SalvoSize);
            rows.Add(new EmploymentLedgerRow(
                facts.ShooterId,
                facts.WeaponFamilyId,
                facts.RoundsRemaining,
                salvoSize,
                facts.LastEmploymentTick,
                ResolveWithholdReason(facts.RoundsRemaining, salvoSize),
                IsFireOrder: false));
        }

        if (rows.Count == 0)
        {
            return EmploymentLedgerSnapshot.Empty;
        }

        rows.Sort(static (a, b) =>
        {
            var shooter = string.Compare(a.ShooterId, b.ShooterId, StringComparison.Ordinal);
            return shooter != 0
                ? shooter
                : string.Compare(a.WeaponFamilyId, b.WeaponFamilyId, StringComparison.Ordinal);
        });

        return new EmploymentLedgerSnapshot(rows, IsFireOrder: false);
    }

    /// <summary>Projects a single magazine fact as a one-row ledger.</summary>
    public static EmploymentLedgerSnapshot Project(EmploymentLedgerMagazineFacts magazine) =>
        Project(new[] { magazine });

    private static string? ResolveWithholdReason(int roundsRemaining, int salvoSize)
    {
        if (roundsRemaining <= 0)
        {
            return AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE;
        }

        if (roundsRemaining < salvoSize)
        {
            return AbortReasonCatalog.Engage.NO_AMMO;
        }

        return null;
    }
}
