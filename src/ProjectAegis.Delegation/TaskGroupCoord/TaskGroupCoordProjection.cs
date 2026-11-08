namespace ProjectAegis.Delegation.TaskGroupCoord;

using System.Text;

/// <summary>
/// DRG-223: headless task-group / formation coordination projector.
/// Consumes injected group, package, and C2 facts only — never issues orders or mutates groups.
/// </summary>
public static class TaskGroupCoordProjection
{
    /// <summary>Projects an advisory coordination snapshot from injected group facts.</summary>
    public static TaskGroupCoordSnapshot Project(TaskGroupCoordInput? input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.GroupId))
        {
            return TaskGroupCoordSnapshot.Empty;
        }

        var members = OrderMembers(input.Members);
        var gapCode = ResolveGapCode(input);
        var statusLine = BuildStatusLine(input, gapCode);

        return new TaskGroupCoordSnapshot(
            input.GroupId,
            members,
            input.PackageId ?? string.Empty,
            input.PackageLabel ?? string.Empty,
            gapCode,
            TaskGroupCoordKind.AdvisoryCoordination,
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false,
            IsAutomaticEngagement: false,
            statusLine);
    }

    /// <summary>Replay-stable canonical form: same inputs yield the same string. Invariant culture; no wall clock.</summary>
    public static string ComputeFingerprint(TaskGroupCoordSnapshot? snapshot)
    {
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.GroupId))
        {
            return "tgc:empty";
        }

        var builder = new StringBuilder();
        builder.Append("tgc:");
        builder.Append(snapshot.GroupId);
        builder.Append('|');
        AppendJoined(builder, snapshot.Members);
        builder.Append('|');
        builder.Append(snapshot.AssignedPackageId);
        builder.Append('|');
        builder.Append(snapshot.AssignedPackageLabel);
        builder.Append('|');
        builder.Append(snapshot.GapCode);
        builder.Append('|');
        builder.Append((int)snapshot.Kind);
        builder.Append('|');
        builder.Append(snapshot.IsWeaponsReleaseAuthorization ? '1' : '0');
        builder.Append(snapshot.IsFireOrder ? '1' : '0');
        builder.Append(snapshot.IsAutomaticEngagement ? '1' : '0');
        builder.Append('|');
        builder.Append(snapshot.StatusLine);
        return builder.ToString();
    }

    // Gap precedence (most specific wins): Split > NoC2 > Unassigned > None.
    // - Split: formation fragmented or members detached from the group.
    // - NoC2: no command node / C2 link present for the group.
    // - Unassigned: no mission package assigned.
    // - None: members present, package assigned, C2 present, and not split.
    private static string ResolveGapCode(TaskGroupCoordInput input)
    {
        if (input.IsSplit)
        {
            return TaskGroupCoordGapCode.Split;
        }

        if (!input.HasC2)
        {
            return TaskGroupCoordGapCode.NoC2;
        }

        if (string.IsNullOrWhiteSpace(input.PackageId))
        {
            return TaskGroupCoordGapCode.Unassigned;
        }

        return TaskGroupCoordGapCode.None;
    }

    private static IReadOnlyList<string> OrderMembers(IReadOnlyList<string>? members)
    {
        if (members is null || members.Count == 0)
        {
            return Array.Empty<string>();
        }

        return members
            .Where(static m => !string.IsNullOrWhiteSpace(m))
            .OrderBy(static m => m, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildStatusLine(TaskGroupCoordInput input, string gapCode) =>
        gapCode switch
        {
            TaskGroupCoordGapCode.None =>
                $"TGC: COORD OK — {input.PackageLabel} (advisory — no orders)",
            TaskGroupCoordGapCode.Split =>
                "TGC: GAP SPLIT — formation fragmented (advisory — no orders)",
            TaskGroupCoordGapCode.NoC2 =>
                "TGC: GAP NO C2 — no command node (advisory — no orders)",
            TaskGroupCoordGapCode.Unassigned =>
                "TGC: GAP UNASSIGNED — no mission package (advisory — no orders)",
            _ => "TGC: —",
        };

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
