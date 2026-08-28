namespace ProjectAegis.Delegation.C2Nodes;

using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;

/// <summary>
/// DRG-213: folds authored mission-package definitions plus order-log membership and
/// availability evidence into a replay-stable headless projection. Read-only; no tick writes.
/// </summary>
public static class MissionPackageProjection
{
    /// <summary>
    /// Projects authored mission packages into a replay-stable snapshot. Folds platform alive
    /// state, order-log damage (one-way death), task-org detach/rejoin, and comms quality.
    /// Read-only; does not append to <paramref name="log"/>.
    /// </summary>
    /// <param name="definitions">Package composition authored outside the tick hotpath.</param>
    /// <param name="log">Optional order-log evidence for damage, detach/rejoin, and comms.</param>
    /// <param name="isPlatformAlive">Optional authoritative alive gate per platform unit id.</param>
    /// <param name="currentSimTick">Sim tick stamped on projected elements.</param>
    /// <param name="currentSimTime">Sim time stamped on projected elements.</param>
    /// <param name="activePackageId">Optional active package override; defaults to first definition.</param>
    public static MissionPackageSnapshot Project(
        IReadOnlyList<PackageDefinition>? definitions,
        DecisionLog? log = null,
        Func<string, bool>? isPlatformAlive = null,
        ulong currentSimTick = 0,
        double currentSimTime = 0,
        string? activePackageId = null)
    {
        if (definitions is null || definitions.Count == 0)
        {
            return MissionPackageSnapshot.Empty;
        }

        var orderedDefinitions = definitions
            .OrderBy(d => d.PackageId, StringComparer.Ordinal)
            .ToArray();

        var platformAlive = BuildPlatformAliveMap(orderedDefinitions, log, isPlatformAlive);
        var taskOrgDetached = BuildTaskOrgDetachedUnits(log);
        var commsState = FoldCommsState(log);
        var damageByUnit = FoldLatestPlatformDamage(log);

        var elements = new List<C2NodeElement>();
        var packages = new List<MissionPackageMembership>();

        for (var i = 0; i < orderedDefinitions.Length; i++)
        {
            var definition = orderedDefinitions[i];
            if (definition.Elements is null || definition.Elements.Count == 0)
            {
                packages.Add(new MissionPackageMembership(
                    definition.PackageId,
                    definition.Label,
                    Array.Empty<string>(),
                    Array.Empty<string>()));
                continue;
            }

            var elementIds = new List<string>(definition.Elements.Count);
            var unitIds = new HashSet<string>(StringComparer.Ordinal);

            var orderedElements = definition.Elements
                .OrderBy(e => e.ElementId, StringComparer.Ordinal)
                .ToArray();

            for (var j = 0; j < orderedElements.Length; j++)
            {
                var elementDef = orderedElements[j];
                if (string.IsNullOrEmpty(elementDef.ElementId)
                    || string.IsNullOrEmpty(elementDef.PlatformUnitId))
                {
                    continue;
                }

                unitIds.Add(elementDef.PlatformUnitId);
                elementIds.Add(elementDef.ElementId);

                var membershipKind = ResolveMembershipKind(elementDef.CapabilityScope);
                var membership = new C2NodeMembership(
                    definition.PackageId,
                    definition.Label,
                    membershipKind);

                var availability = ResolveAvailability(
                    elementDef,
                    platformAlive,
                    commsState,
                    damageByUnit,
                    currentSimTick);

                var correlationSequenceId = ResolveCorrelationSequenceId(
                    elementDef.PlatformUnitId,
                    damageByUnit,
                    log);

                var sourceRefs = BuildSourceRefs(
                    elementDef,
                    definition.PackageId,
                    membershipKind,
                    taskOrgDetached.Contains(elementDef.PlatformUnitId),
                    correlationSequenceId);

                elements.Add(new C2NodeElement(
                    elementDef.ElementId,
                    elementDef.PlatformUnitId,
                    elementDef.Role,
                    availability,
                    membership,
                    elementDef.CapabilityScope,
                    taskOrgDetached.Contains(elementDef.PlatformUnitId),
                    currentSimTick,
                    currentSimTime,
                    correlationSequenceId,
                    sourceRefs));
            }

            packages.Add(new MissionPackageMembership(
                definition.PackageId,
                definition.Label,
                elementIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                unitIds.OrderBy(id => id, StringComparer.Ordinal).ToArray()));
        }

        var resolvedActivePackageId = !string.IsNullOrEmpty(activePackageId)
            ? activePackageId
            : orderedDefinitions[0].PackageId;

        return new MissionPackageSnapshot(
            resolvedActivePackageId,
            elements
                .OrderBy(e => e.ElementId, StringComparer.Ordinal)
                .ToArray(),
            packages
                .OrderBy(p => p.PackageId, StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>
    /// Replay-stable canonical form: same definitions + log + tick yield the same string.
    /// Invariant culture; no wall clock.
    /// </summary>
    public static string ComputeFingerprint(MissionPackageSnapshot? snapshot)
    {
        if (snapshot is null
            || (snapshot.Elements.Count == 0 && snapshot.Packages.Count == 0))
        {
            return "pkg:empty";
        }

        var builder = new StringBuilder();
        builder.Append("pkg:active=");
        builder.Append(snapshot.ActivePackageId);
        builder.Append("#e=");
        builder.Append(snapshot.Elements.Count);

        foreach (var element in snapshot.Elements.OrderBy(e => e.ElementId, StringComparer.Ordinal))
        {
            builder.Append('|');
            builder.Append(element.ElementId);
            builder.Append(',');
            builder.Append(element.PlatformUnitId);
            builder.Append(',');
            builder.Append((int)element.Role);
            builder.Append(',');
            builder.Append((int)element.Availability);
            builder.Append(',');
            builder.Append((int)element.Membership.Kind);
            builder.Append(',');
            builder.Append(element.Membership.PackageId);
            builder.Append(',');
            builder.Append(element.CapabilityScope);
            builder.Append(',');
            builder.Append(element.TaskOrgDetached ? '1' : '0');
            builder.Append(',');
            builder.Append(element.LastSimTick);
            builder.Append(',');
            builder.Append(element.LastSimTime.ToString("R", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(element.CorrelationSequenceId);
            builder.Append(',');
            AppendJoined(builder, element.SourceRefs);
        }

        builder.Append("#pk=");
        builder.Append(snapshot.Packages.Count);
        foreach (var package in snapshot.Packages.OrderBy(p => p.PackageId, StringComparer.Ordinal))
        {
            builder.Append('|');
            builder.Append(package.PackageId);
            builder.Append(',');
            builder.Append(package.PackageLabel);
            builder.Append(',');
            AppendJoined(builder, package.ElementIds);
            builder.Append(',');
            AppendJoined(builder, package.UnitIds);
        }

        return builder.ToString();
    }

    private static C2NodeMembershipKind ResolveMembershipKind(string? capabilityScope)
    {
        if (!string.IsNullOrEmpty(capabilityScope)
            && capabilityScope.StartsWith("organic-", StringComparison.Ordinal))
        {
            return C2NodeMembershipKind.Organic;
        }

        return C2NodeMembershipKind.Package;
    }

    private static C2NodeAvailability ResolveAvailability(
        PackageElementDefinition elementDef,
        IReadOnlyDictionary<string, bool> platformAlive,
        CommsState commsState,
        IReadOnlyDictionary<string, PlatformDamageChangeRecord> damageByUnit,
        ulong currentSimTick)
    {
        if (!platformAlive.TryGetValue(elementDef.PlatformUnitId, out var alive) || !alive)
        {
            return C2NodeAvailability.Unavailable;
        }

        if (damageByUnit.TryGetValue(elementDef.PlatformUnitId, out var damage)
            && damage.NewHpPct <= 0.0)
        {
            return C2NodeAvailability.Unavailable;
        }

        if (commsState == CommsState.Denied
            && elementDef.Role is C2NodeRole.Relay or C2NodeRole.C2)
        {
            return C2NodeAvailability.Unavailable;
        }

        if (commsState == CommsState.Degraded
            && elementDef.Role is C2NodeRole.Relay or C2NodeRole.C2)
        {
            return C2NodeAvailability.LastKnown;
        }

        return C2NodeAvailability.Available;
    }

    private static ulong? ResolveCorrelationSequenceId(
        string platformUnitId,
        IReadOnlyDictionary<string, PlatformDamageChangeRecord> damageByUnit,
        DecisionLog? log)
    {
        if (damageByUnit.TryGetValue(platformUnitId, out var damage))
        {
            return damage.SequenceId;
        }

        if (log is null)
        {
            return null;
        }

        var latestDetach = log.GroupMemberDetaches
            .Where(d => string.Equals(d.UnitId.Value, platformUnitId, StringComparison.Ordinal))
            .OrderBy(d => d.SequenceId)
            .LastOrDefault();

        return latestDetach?.SequenceId;
    }

    private static string[] BuildSourceRefs(
        PackageElementDefinition elementDef,
        string packageId,
        C2NodeMembershipKind membershipKind,
        bool taskOrgDetached,
        ulong? correlationSequenceId)
    {
        var refs = new List<string>(6)
        {
            $"element:{elementDef.ElementId}",
            $"unit:{elementDef.PlatformUnitId}",
            $"package:{packageId}",
            $"scope:{elementDef.CapabilityScope}",
            $"membership:{membershipKind}",
        };

        if (taskOrgDetached)
        {
            refs.Add("task-org:detached");
        }

        if (correlationSequenceId is not null)
        {
            refs.Add($"seq:{correlationSequenceId.Value}");
        }

        refs.Sort(StringComparer.Ordinal);
        return refs.ToArray();
    }

    private static Dictionary<string, bool> BuildPlatformAliveMap(
        IReadOnlyList<PackageDefinition> definitions,
        DecisionLog? log,
        Func<string, bool>? isPlatformAlive)
    {
        var alive = new Dictionary<string, bool>(StringComparer.Ordinal);
        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            for (var j = 0; j < definition.Elements.Count; j++)
            {
                var unitId = definition.Elements[j].PlatformUnitId;
                if (string.IsNullOrEmpty(unitId) || alive.ContainsKey(unitId))
                {
                    continue;
                }

                alive[unitId] = isPlatformAlive?.Invoke(unitId) ?? true;
            }
        }

        if (log is null)
        {
            return alive;
        }

        foreach (var change in log.PlatformDamageChanges.OrderBy(c => c.SequenceId))
        {
            var unitId = change.UnitId.Value;
            if (!alive.ContainsKey(unitId))
            {
                continue;
            }

            if (change.NewHpPct <= 0.0)
            {
                alive[unitId] = false;
            }
        }

        return alive;
    }

    private static HashSet<string> BuildTaskOrgDetachedUnits(DecisionLog? log)
    {
        var detached = new HashSet<string>(StringComparer.Ordinal);
        if (log is null)
        {
            return detached;
        }

        var transitions = log.GroupMemberDetaches
            .Select(d => (UnitId: d.UnitId.Value, Detached: true, d.SequenceId))
            .Concat(log.GroupMemberRejoins.Select(r => (UnitId: r.UnitId.Value, Detached: false, r.SequenceId)))
            .OrderBy(t => t.SequenceId)
            .ToArray();

        for (var i = 0; i < transitions.Length; i++)
        {
            var transition = transitions[i];
            if (transition.Detached)
            {
                detached.Add(transition.UnitId);
            }
            else
            {
                detached.Remove(transition.UnitId);
            }
        }

        return detached;
    }

    private static CommsState FoldCommsState(DecisionLog? log)
    {
        if (log is null || log.CommsStateChanges.Count == 0)
        {
            return CommsState.Nominal;
        }

        var latest = log.CommsStateChanges
            .OrderBy(c => c.SequenceId)
            .Last();

        return latest.NewState;
    }

    private static Dictionary<string, PlatformDamageChangeRecord> FoldLatestPlatformDamage(DecisionLog? log)
    {
        var damageByUnit = new Dictionary<string, PlatformDamageChangeRecord>(StringComparer.Ordinal);
        if (log is null)
        {
            return damageByUnit;
        }

        foreach (var change in log.PlatformDamageChanges.OrderBy(c => c.SequenceId))
        {
            damageByUnit[change.UnitId.Value] = change;
        }

        return damageByUnit;
    }

    private static void AppendJoined(StringBuilder builder, IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('+');
            }

            builder.Append(values[i]);
        }
    }
}
