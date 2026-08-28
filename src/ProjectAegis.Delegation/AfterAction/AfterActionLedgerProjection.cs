namespace ProjectAegis.Delegation.AfterAction;

using System.Globalization;
using System.Text;

/// <summary>
/// DRG-218: projects combat-event facts into a replay-linked after-action ledger.
/// Advisory only — does not fire, authorize, or enqueue orders.
/// </summary>
public static class AfterActionLedgerProjection
{
    /// <summary>Maps combat-event rows 1:1 into ledger entries without reconstructing facts.</summary>
    public static AfterActionLedgerSnapshot Project(CombatEventSnapshotConsume? snapshot)
    {
        if (snapshot is null || snapshot.Events.Count == 0)
        {
            return AfterActionLedgerSnapshot.Empty;
        }

        var entries = new AfterActionLedgerEntry[snapshot.Events.Count];
        for (var i = 0; i < snapshot.Events.Count; i++)
        {
            entries[i] = ToEntry(snapshot.Events[i]);
        }

        return new AfterActionLedgerSnapshot(entries);
    }

    /// <summary>Projects a raw combat-event row list (same contract as #583).</summary>
    public static AfterActionLedgerSnapshot Project(IReadOnlyList<CombatEventRowConsume>? events) =>
        Project(events is null || events.Count == 0
            ? CombatEventSnapshotConsume.Empty
            : new CombatEventSnapshotConsume(events));

    /// <summary>Returns ledger rows matching all supplied filter fields (ordinal equality).</summary>
    public static AfterActionLedgerSnapshot Filter(
        AfterActionLedgerSnapshot ledger,
        AfterActionLedgerFilter filter)
    {
        if (ledger.Entries.Count == 0)
        {
            return AfterActionLedgerSnapshot.Empty;
        }

        var matches = new List<AfterActionLedgerEntry>(ledger.Entries.Count);
        for (var i = 0; i < ledger.Entries.Count; i++)
        {
            var entry = ledger.Entries[i];
            if (!MatchesFilter(entry, filter))
            {
                continue;
            }

            matches.Add(entry);
        }

        return matches.Count == 0
            ? AfterActionLedgerSnapshot.Empty
            : new AfterActionLedgerSnapshot(matches);
    }

    /// <summary>Replay-stable canonical fingerprint. Invariant culture; ordinal ordering; no wall clock.</summary>
    public static string ComputeFingerprint(AfterActionLedgerSnapshot? ledger)
    {
        if (ledger is null || ledger.Entries.Count == 0)
        {
            return "aal:empty";
        }

        var builder = new StringBuilder();
        builder.Append("aal:e=");
        builder.Append(ledger.Entries.Count);
        for (var i = 0; i < ledger.Entries.Count; i++)
        {
            AppendEntry(builder, ledger.Entries[i]);
        }

        return builder.ToString();
    }

    private static AfterActionLedgerEntry ToEntry(CombatEventRowConsume row) =>
        new(
            row.ShooterId,
            row.TargetId,
            row.WeaponFamilyId,
            row.Outcome,
            row.CorrelationId,
            row.SimTime,
            row.SimTick,
            row.Phase,
            row.ExplanationRef);

    private static bool MatchesFilter(AfterActionLedgerEntry entry, AfterActionLedgerFilter filter)
    {
        if (filter.ShooterId is not null &&
            !string.Equals(entry.ShooterId, filter.ShooterId, StringComparison.Ordinal))
        {
            return false;
        }

        if (filter.TargetId is not null &&
            !string.Equals(entry.TargetId, filter.TargetId, StringComparison.Ordinal))
        {
            return false;
        }

        if (filter.WeaponFamilyId is not null &&
            !string.Equals(entry.WeaponFamilyId, filter.WeaponFamilyId, StringComparison.Ordinal))
        {
            return false;
        }

        if (filter.Outcome is not null &&
            !string.Equals(entry.Outcome, filter.Outcome, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static void AppendEntry(StringBuilder builder, AfterActionLedgerEntry entry)
    {
        builder.Append('|');
        builder.Append(entry.ShooterId);
        builder.Append(',');
        builder.Append(entry.TargetId);
        builder.Append(',');
        builder.Append(entry.WeaponFamilyId);
        builder.Append(',');
        builder.Append(entry.Outcome);
        builder.Append(',');
        builder.Append(entry.CorrelationId);
        builder.Append(',');
        builder.Append(entry.SimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(entry.SimTick);
        builder.Append(',');
        builder.Append((int)entry.Phase);
        builder.Append(',');
        builder.Append(entry.ExplanationRef);
    }
}
