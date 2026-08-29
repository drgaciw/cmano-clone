namespace ProjectAegis.Delegation.PlatformDegrade;

using System.Text;

/// <summary>
/// DRG-227: headless own-unit platform degrade / damage-control projector.
/// Consumes injected unit subsystem facts only — never retasks, detaches, or writes catalog.
/// </summary>
public static class PlatformDegradeProjection
{
    /// <summary>Projects an advisory damage-control snapshot from injected unit facts.</summary>
    public static PlatformDegradeSnapshot Project(PlatformDegradeInput? input)
    {
        if (input is null || input.Units is null || input.Units.Count == 0)
        {
            return PlatformDegradeSnapshot.Empty;
        }

        var rows = new List<PlatformDegradeUnitRow>(input.Units.Count);
        for (var i = 0; i < input.Units.Count; i++)
        {
            var unit = input.Units[i];
            if (string.IsNullOrWhiteSpace(unit.UnitId))
            {
                continue;
            }

            rows.Add(ProjectUnitRow(unit, input.SimTick));
        }

        if (rows.Count == 0)
        {
            return PlatformDegradeSnapshot.Empty;
        }

        rows.Sort(static (a, b) => string.Compare(a.UnitId, b.UnitId, StringComparison.Ordinal));

        var statusLine = BuildStatusLine(rows);
        return new PlatformDegradeSnapshot(
            input.SimTick,
            rows,
            PlatformDegradeKind.AdvisoryDamageControl,
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false,
            IsAutomaticEngagement: false,
            statusLine);
    }

    /// <summary>Replay-stable canonical form: same inputs yield the same string. Invariant culture; no wall clock.</summary>
    public static string ComputeFingerprint(PlatformDegradeSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Units.Count == 0)
        {
            return "pdg:empty";
        }

        var builder = new StringBuilder();
        builder.Append("pdg:");
        builder.Append(snapshot.SimTick);
        builder.Append('|');
        builder.Append((int)snapshot.Kind);
        builder.Append('|');
        builder.Append(snapshot.IsWeaponsReleaseAuthorization ? '1' : '0');
        builder.Append(snapshot.IsFireOrder ? '1' : '0');
        builder.Append(snapshot.IsAutomaticEngagement ? '1' : '0');
        builder.Append('|');
        builder.Append(snapshot.StatusLine);
        builder.Append('|');

        for (var i = 0; i < snapshot.Units.Count; i++)
        {
            var row = snapshot.Units[i];
            builder.Append(row.UnitId);
            builder.Append(':');
            AppendJoined(builder, row.ActiveDegradeCodes);
            builder.Append(':');
            builder.Append((int)row.SeverityBand);
            builder.Append(':');
            builder.Append(row.SimTick);
            builder.Append(';');
        }

        return builder.ToString();
    }

    private static PlatformDegradeUnitRow ProjectUnitRow(PlatformDegradeUnitInput unit, ulong simTick)
    {
        var codes = new List<string>(4);
        var maxSeverity = PlatformDegradeSeverityBand.None;

        if (unit.MobilityDegraded)
        {
            codes.Add(PlatformDegradeCode.Mobility);
            maxSeverity = MaxSeverity(maxSeverity, unit.MobilitySeverity);
        }

        if (unit.SensorDegraded)
        {
            codes.Add(PlatformDegradeCode.Sensor);
            maxSeverity = MaxSeverity(maxSeverity, unit.SensorSeverity);
        }

        if (unit.WeaponDegraded)
        {
            codes.Add(PlatformDegradeCode.Weapon);
            maxSeverity = MaxSeverity(maxSeverity, unit.WeaponSeverity);
        }

        if (unit.CommsDegraded)
        {
            codes.Add(PlatformDegradeCode.Comms);
            maxSeverity = MaxSeverity(maxSeverity, unit.CommsSeverity);
        }

        if (codes.Count == 0)
        {
            return new PlatformDegradeUnitRow(
                unit.UnitId,
                new[] { PlatformDegradeCode.None },
                PlatformDegradeSeverityBand.None,
                simTick);
        }

        return new PlatformDegradeUnitRow(
            unit.UnitId,
            codes,
            maxSeverity,
            simTick);
    }

    private static PlatformDegradeSeverityBand MaxSeverity(
        PlatformDegradeSeverityBand current,
        PlatformDegradeSeverityBand candidate) =>
        (int)candidate > (int)current ? candidate : current;

    private static string BuildStatusLine(IReadOnlyList<PlatformDegradeUnitRow> rows)
    {
        var degradedCount = 0;
        for (var i = 0; i < rows.Count; i++)
        {
            if (!IsHealthy(rows[i]))
            {
                degradedCount++;
            }
        }

        return degradedCount == 0
            ? $"PDG: ALL UNITS NOMINAL — {rows.Count} tracked (advisory — no orders)"
            : $"PDG: {degradedCount}/{rows.Count} UNITS DEGRADED (advisory — no orders)";
    }

    private static bool IsHealthy(PlatformDegradeUnitRow row) =>
        row.SeverityBand == PlatformDegradeSeverityBand.None
        && row.ActiveDegradeCodes.Count == 1
        && string.Equals(row.ActiveDegradeCodes[0], PlatformDegradeCode.None, StringComparison.Ordinal);

    private static void AppendJoined(StringBuilder builder, IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(values[i]);
        }
    }
}
