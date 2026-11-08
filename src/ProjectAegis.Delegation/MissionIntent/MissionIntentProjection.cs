namespace ProjectAegis.Delegation.MissionIntent;

using System.Text;

/// <summary>
/// DRG-229: headless mission-command intent projector.
/// Consumes injected group/unit, intent, constraint, and retask facts only — never issues orders.
/// </summary>
public static class MissionIntentProjection
{
    /// <summary>Projects an advisory mission-intent snapshot from injected facts.</summary>
    public static MissionIntentSnapshot Project(MissionIntentInput? input)
    {
        if (input is null || IsEmptyScope(input))
        {
            return MissionIntentSnapshot.Empty;
        }

        var constraints = OrderConstraints(input.ActiveConstraints);
        var intentCode = input.IntentCode ?? string.Empty;
        var statusLine = BuildStatusLine(input, intentCode, constraints);

        return new MissionIntentSnapshot(
            input.GroupId ?? string.Empty,
            input.UnitId ?? string.Empty,
            intentCode,
            constraints,
            input.AdvisoryRetask,
            MissionIntentKind.AdvisoryIntent,
            IsOrder: false,
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false,
            IsAutomaticEngagement: false,
            statusLine);
    }

    /// <summary>Replay-stable canonical form: same inputs yield the same string. Invariant culture; no wall clock.</summary>
    public static string ComputeFingerprint(MissionIntentSnapshot? snapshot)
    {
        if (snapshot is null || IsEmptyScope(snapshot))
        {
            return "mi:empty";
        }

        var builder = new StringBuilder();
        builder.Append("mi:");
        builder.Append(snapshot.GroupId);
        builder.Append('|');
        builder.Append(snapshot.UnitId);
        builder.Append('|');
        builder.Append(snapshot.IntentCode);
        builder.Append('|');
        AppendJoined(builder, snapshot.Constraints);
        builder.Append('|');
        builder.Append((int)snapshot.AdvisoryRetask);
        builder.Append('|');
        builder.Append((int)snapshot.Kind);
        builder.Append('|');
        builder.Append(snapshot.IsOrder ? '1' : '0');
        builder.Append(snapshot.IsWeaponsReleaseAuthorization ? '1' : '0');
        builder.Append(snapshot.IsFireOrder ? '1' : '0');
        builder.Append(snapshot.IsAutomaticEngagement ? '1' : '0');
        builder.Append('|');
        builder.Append(snapshot.StatusLine);
        return builder.ToString();
    }

    private static bool IsEmptyScope(MissionIntentInput input) =>
        string.IsNullOrWhiteSpace(input.GroupId) && string.IsNullOrWhiteSpace(input.UnitId);

    private static bool IsEmptyScope(MissionIntentSnapshot snapshot) =>
        string.IsNullOrWhiteSpace(snapshot.GroupId) && string.IsNullOrWhiteSpace(snapshot.UnitId);

    private static IReadOnlyList<string> OrderConstraints(IReadOnlyList<string>? constraints)
    {
        if (constraints is null || constraints.Count == 0)
        {
            return Array.Empty<string>();
        }

        return constraints
            .Where(static c => !string.IsNullOrWhiteSpace(c))
            .OrderBy(static c => c, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildStatusLine(
        MissionIntentInput input,
        string intentCode,
        IReadOnlyList<string> constraints)
    {
        var scope = !string.IsNullOrWhiteSpace(input.GroupId)
            ? $"group {input.GroupId}"
            : $"unit {input.UnitId}";

        var constraintSuffix = constraints.Count == 0
            ? string.Empty
            : $" — constraints [{string.Join(", ", constraints)}]";

        var retaskSuffix = input.AdvisoryRetask switch
        {
            MissionIntentRetaskAdvice.Withdraw => " — advisory retask WITHDRAW",
            MissionIntentRetaskAdvice.ReAttack => " — advisory retask RE-ATTACK",
            _ => string.Empty,
        };

        return intentCode switch
        {
            MissionIntentCode.Hold =>
                $"MI: HOLD — {scope}{constraintSuffix}{retaskSuffix} (advisory — no orders)",
            MissionIntentCode.NoStrike =>
                $"MI: NO STRIKE — {scope}{constraintSuffix}{retaskSuffix} (advisory — no orders)",
            MissionIntentCode.Attack =>
                $"MI: ATTACK — {scope}{constraintSuffix}{retaskSuffix} (advisory — no orders)",
            _ =>
                $"MI: INTENT {intentCode} — {scope}{constraintSuffix}{retaskSuffix} (advisory — no orders)",
        };
    }

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
