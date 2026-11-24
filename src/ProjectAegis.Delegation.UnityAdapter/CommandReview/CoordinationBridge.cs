namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using C2Nodes;
using Controllers;
using Core;
using MissionIntent;
using ProjectAegis.Sim.Comms;
using Targets;
using TaskGroupCoord;
using Bridge;

/// <summary>
/// Builds task-group coordination state from the registered runtime, snapshot, and optional
/// authored facts. It never invents roles, coverage areas, mission intent, or orders.
/// </summary>
public static class CoordinationBridge
{
    /// <summary>Build from runtime facts alone. Unauthored package, role, coverage, and intent remain unknown.</summary>
    public static CoordinationSnapshot Build(
        DelegationBridge? bridge,
        ISimWorldSnapshot? snapshot) =>
        Build(bridge, snapshot, facts: null);

    /// <summary>Build from the runtime plus optional scenario facts supplied at composition.</summary>
    public static CoordinationSnapshot Build(
        DelegationBridge? bridge,
        ISimWorldSnapshot? snapshot,
        ICoordinationFacts? facts)
    {
        if (bridge is null || snapshot is null)
        {
            return CoordinationSnapshot.Empty;
        }

        facts ??= snapshot as ICoordinationFacts;
        var packages = facts?.Packages ?? Array.Empty<PackageDefinition>();
        var boundedLog = BoundLogAt(bridge.Orchestrator.DecisionLog, snapshot.SimTime);
        var packageSnapshot = MissionPackageProjection.Project(
            packages,
            boundedLog,
            unitId => snapshot.IsMemberAlive(new TargetId(unitId)),
            currentSimTick: (ulong)Math.Max(0, (long)snapshot.SimTime),
            currentSimTime: snapshot.SimTime);
        var coverageByElement = (facts?.Coverage ?? Array.Empty<CoverageFact>())
            .Where(f => !string.IsNullOrWhiteSpace(f.ElementId))
            .GroupBy(f => f.ElementId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var intents = facts?.Intents ?? Array.Empty<MissionIntentInput>();
        var groups = new List<CoordinationGroupSnapshot>();

        foreach (var binding in bridge.Registry.Bindings
                     .Where(b => b.Target is GroupTarget)
                     .OrderBy(b => b.TargetId.Value, StringComparer.Ordinal))
        {
            var group = (GroupTarget)binding.Target;
            groups.Add(BuildGroup(
                bridge,
                snapshot,
                group,
                packageSnapshot,
                coverageByElement,
                intents,
                facts?.Assignments ?? Array.Empty<CoordinationGroupPackageAssignment>(),
                facts?.Escorts ?? Array.Empty<CoordinationEscortFact>(),
                facts?.SharedTargets ?? Array.Empty<CoordinationSharedTargetFact>()));
        }

        return groups.Count == 0 ? CoordinationSnapshot.Empty : new CoordinationSnapshot(groups);
    }

    private static CoordinationGroupSnapshot BuildGroup(
        DelegationBridge bridge,
        ISimWorldSnapshot snapshot,
        GroupTarget group,
        MissionPackageSnapshot packageSnapshot,
        IReadOnlyDictionary<string, CoverageFact> coverageByElement,
        IReadOnlyList<MissionIntentInput> intents,
        IReadOnlyList<CoordinationGroupPackageAssignment> assignments,
        IReadOnlyList<CoordinationEscortFact> escortFacts,
        IReadOnlyList<CoordinationSharedTargetFact> sharedTargetFacts)
    {
        var currentMembers = group.Members
            .Select(id => id.Value)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var detachedMembers = bridge.Registry.Bindings
            .Where(b => b.Target is UnitTarget unit
                && unit.IsDetachedFromGroup
                && unit.DetachedFromGroupId == group.Id)
            .Select(b => b.TargetId.Value)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var scopeMembers = currentMembers.Concat(detachedMembers).ToHashSet(StringComparer.Ordinal);
        var assignedPackageId = assignments.FirstOrDefault(a =>
            string.Equals(a.GroupId, group.Id.Value, StringComparison.Ordinal))?.PackageId;
        var package = string.IsNullOrWhiteSpace(assignedPackageId)
            ? null
            : packageSnapshot.Packages.FirstOrDefault(p =>
                string.Equals(p.PackageId, assignedPackageId, StringComparison.Ordinal));
        var elements = package is null
            ? Array.Empty<C2NodeElement>()
            : packageSnapshot.Elements
                .Where(e => string.Equals(e.Membership.PackageId, package.PackageId, StringComparison.Ordinal))
                .Where(e => scopeMembers.Contains(e.PlatformUnitId))
                .OrderBy(e => e.PlatformUnitId, StringComparer.Ordinal)
                .ThenBy(e => e.ElementId, StringComparer.Ordinal)
                .ToArray();

        var effects = BuildEffects(
            group.Id.Value, currentMembers, detachedMembers, elements, coverageByElement, snapshot);
        var gaps = BuildGaps(effects);
        var hasC2 = elements.Any(e =>
            e.Role == C2NodeRole.C2 && e.Availability == C2NodeAvailability.Available && !e.TaskOrgDetached);
        var coordination = package is null
            ? new TaskGroupCoordSnapshot(
                group.Id.Value,
                currentMembers,
                string.Empty,
                string.Empty,
                "UNKNOWN",
                TaskGroupCoordKind.AdvisoryCoordination,
                false,
                false,
                false,
                "TGC: UNKNOWN — no authored group/package association (advisory — no orders)")
            : TaskGroupCoordProjection.Project(new TaskGroupCoordInput(
                group.Id.Value,
                currentMembers,
                package.PackageId,
                package.PackageLabel,
                HasC2: hasC2,
                C2NodeId: elements.FirstOrDefault(e => e.Role == C2NodeRole.C2)?.ElementId ?? string.Empty,
                IsSplit: detachedMembers.Length > 0));
        var intentInput = intents.FirstOrDefault(i =>
            string.Equals(i.GroupId, group.Id.Value, StringComparison.Ordinal));
        var intent = MissionIntentProjection.Project(intentInput);
        var details = BuildGroupDetails(
            bridge,
            snapshot,
            group,
            currentMembers,
            scopeMembers,
            package,
            elements,
            effects,
            escortFacts,
            sharedTargetFacts);

        return new CoordinationGroupSnapshot(coordination, intent, effects, gaps, details);
    }

    private static CoordinationGroupDetails BuildGroupDetails(
        DelegationBridge bridge,
        ISimWorldSnapshot snapshot,
        GroupTarget group,
        string[] currentMembers,
        HashSet<string> scopeMembers,
        MissionPackageMembership? package,
        C2NodeElement[] elements,
        IReadOnlyList<CoordinationEffect> effects,
        IReadOnlyList<CoordinationEscortFact> escortFacts,
        IReadOnlyList<CoordinationSharedTargetFact> sharedTargetFacts)
    {
        var controllerKind = group.Slot.Active is HumanController
            ? "Human"
            : group.Slot.Active is AgentController
                ? "Agent"
                : "None";
        var delegationAutonomy = ResolveDelegationAutonomy(bridge, group);
        var simTick = (ulong)Math.Max(0, (long)snapshot.SimTime);
        var isOffGrid = OffGridOrderGate.EvaluateAt(bridge.Orchestrator.CommsGrid, group.Id.Value, simTick) != null;

        var stateByUnit = new Dictionary<string, CoordinationEffectState>(StringComparer.Ordinal);
        foreach (var effect in effects)
        {
            if (!stateByUnit.TryGetValue(effect.UnitId, out var existing)
                || (int)effect.State < (int)existing)
            {
                stateByUnit[effect.UnitId] = effect.State;
            }
        }

        var escorts = BuildEscorts(group.Id.Value, package, elements, escortFacts, stateByUnit);
        var sectors = BuildSectors(effects);
        var sharedTargets = BuildSharedTargets(group.Id.Value, snapshot, currentMembers, scopeMembers, elements, sharedTargetFacts);

        return new CoordinationGroupDetails(
            controllerKind,
            delegationAutonomy,
            group.PendingReplan,
            isOffGrid,
            escorts,
            sectors,
            sharedTargets);
    }

    private static AutonomyLevel ResolveDelegationAutonomy(DelegationBridge bridge, GroupTarget group)
    {
        var resolved = AutonomyLevel.Manual;
        for (var i = 0; i < group.Members.Count; i++)
        {
            if (!bridge.Registry.TryGetBinding(group.Members[i], out var memberBinding)
                || memberBinding.Target is not UnitTarget unit)
            {
                continue;
            }

            var agent = unit.Slot.SuspendedAgent ?? unit.Slot.Active as AgentController;
            if (agent is not null)
            {
                resolved = (AutonomyLevel)Math.Max((int)resolved, (int)agent.Autonomy);
            }
        }

        return resolved;
    }

    private static IReadOnlyList<CoordinationEscortAssignment> BuildEscorts(
        string groupId,
        MissionPackageMembership? package,
        C2NodeElement[] elements,
        IReadOnlyList<CoordinationEscortFact> escortFacts,
        IReadOnlyDictionary<string, CoordinationEffectState> stateByUnit)
    {
        var authored = escortFacts
            .Where(f => string.Equals(f.GroupId, groupId, StringComparison.Ordinal))
            .Select(f => new CoordinationEscortAssignment(
                f.EscortUnitId,
                f.ProtectedUnitId,
                f.CapabilityScope,
                stateByUnit.TryGetValue(f.EscortUnitId, out var st) ? st : CoordinationEffectState.UnknownRole))
            .OrderBy(e => e.EscortUnitId, StringComparer.Ordinal)
            .ThenBy(e => e.ProtectedUnitId, StringComparer.Ordinal)
            .ToArray();
        if (authored.Length > 0 || package is null)
        {
            return authored;
        }

        var c2UnitId = elements.FirstOrDefault(e => e.Role == C2NodeRole.C2)?.PlatformUnitId ?? string.Empty;
        return elements
            .Where(e =>
                e.CapabilityScope.Contains("escort", StringComparison.OrdinalIgnoreCase)
                || (e.Role == C2NodeRole.Shooter && !string.IsNullOrEmpty(c2UnitId) && !string.Equals(e.PlatformUnitId, c2UnitId, StringComparison.Ordinal)))
            .Select(e => new CoordinationEscortAssignment(
                e.PlatformUnitId,
                c2UnitId,
                e.CapabilityScope,
                stateByUnit.TryGetValue(e.PlatformUnitId, out var st) ? st : CoordinationEffectState.UnknownRole))
            .OrderBy(e => e.EscortUnitId, StringComparer.Ordinal)
            .ThenBy(e => e.ProtectedUnitId, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<CoordinationSectorResponsibility> BuildSectors(
        IReadOnlyList<CoordinationEffect> effects) =>
        effects
            .Where(e => !string.IsNullOrEmpty(e.Coverage.CoverageId) || e.Coverage.Status != CoverageStatus.Unknown)
            .Select(e => new CoordinationSectorResponsibility(
                e.Coverage.CoverageId,
                e.Coverage.Label,
                e.UnitId,
                e.ElementId,
                e.Role,
                e.Coverage.Status,
                e.Coverage.Geometry?.SourceRef,
                e.Coverage.ReasonCode))
            .OrderBy(s => s.CoverageId, StringComparer.Ordinal)
            .ThenBy(s => s.OwnerUnitId, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<CoordinationSharedTargetAssignment> BuildSharedTargets(
        string groupId,
        ISimWorldSnapshot snapshot,
        string[] currentMembers,
        HashSet<string> scopeMembers,
        C2NodeElement[] elements,
        IReadOnlyList<CoordinationSharedTargetFact> sharedTargetFacts)
    {
        var authored = sharedTargetFacts
            .Where(f => string.Equals(f.GroupId, groupId, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(f.TargetId))
            .Select(f => new CoordinationSharedTargetAssignment(
                f.TargetId,
                f.AssignedUnitIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                f.HasFireControlTrack,
                "AUTHORED_FACT",
                f.SourceRef))
            .OrderBy(t => t.TargetId, StringComparer.Ordinal)
            .ToArray();
        if (authored.Length > 0)
        {
            return authored;
        }

        if (snapshot.PreferredHostileByShooter is { Count: > 0 } preferred)
        {
            var grouped = preferred
                .Where(kvp => scopeMembers.Contains(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                .GroupBy(kvp => kvp.Value, StringComparer.Ordinal)
                .Select(g => new CoordinationSharedTargetAssignment(
                    g.Key,
                    g.Select(kvp => kvp.Key).OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                    snapshot.HasFireControlTrackOnPrimaryContact,
                    "PREFERRED_HOSTILE_MAP",
                    "snapshot:PreferredHostileByShooter"))
                .OrderBy(t => t.TargetId, StringComparer.Ordinal)
                .ToArray();
            if (grouped.Length > 0)
            {
                return grouped;
            }
        }

        if (snapshot.PrimaryHostileContactId is { } primary && !string.IsNullOrWhiteSpace(primary.Value))
        {
            var liveMembers = currentMembers
                .Where(m => snapshot.IsMemberAlive(new TargetId(m)))
                .ToArray();
            if (liveMembers.Length > 0)
            {
                var shooterUnits = elements
                    .Where(e => e.Role == C2NodeRole.Shooter && liveMembers.Contains(e.PlatformUnitId, StringComparer.Ordinal))
                    .Select(e => e.PlatformUnitId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToArray();
                var assigned = shooterUnits.Length > 0 ? shooterUnits : liveMembers;
                return new[]
                {
                    new CoordinationSharedTargetAssignment(
                        primary.Value,
                        assigned,
                        snapshot.HasFireControlTrackOnPrimaryContact,
                        "SNAPSHOT_PRIMARY_CONTACT",
                        "snapshot:PrimaryHostileContactId"),
                };
            }
        }

        return Array.Empty<CoordinationSharedTargetAssignment>();
    }

    private static IReadOnlyList<CoordinationEffect> BuildEffects(
        string groupId,
        IReadOnlyList<string> currentMembers,
        IReadOnlyList<string> detachedMembers,
        IReadOnlyList<C2NodeElement> elements,
        IReadOnlyDictionary<string, CoverageFact> coverageByElement,
        ISimWorldSnapshot snapshot)
    {
        var effects = new List<CoordinationEffect>();
        var membersWithRoles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in elements)
        {
            membersWithRoles.Add(element.PlatformUnitId);
            var detached = detachedMembers.Contains(element.PlatformUnitId, StringComparer.Ordinal) || element.TaskOrgDetached;
            var state = detached
                ? CoordinationEffectState.Detached
                : element.Availability == C2NodeAvailability.Unavailable
                    ? CoordinationEffectState.Lost
                    : element.Availability == C2NodeAvailability.LastKnown
                        ? CoordinationEffectState.UnknownAvailability
                    : CoordinationEffectState.Available;
            effects.Add(new CoordinationEffect(
                groupId,
                element.PlatformUnitId,
                element.ElementId,
                element.Role,
                state,
                ProjectCoverage(element, state, coverageByElement)));
        }

        foreach (var member in currentMembers.Concat(detachedMembers).OrderBy(id => id, StringComparer.Ordinal))
        {
            if (membersWithRoles.Contains(member))
            {
                continue;
            }

            var isAlive = snapshot.IsMemberAlive(new TargetId(member));
            effects.Add(new CoordinationEffect(
                groupId,
                member,
                string.Empty,
                Role: null,
                detachedMembers.Contains(member, StringComparer.Ordinal)
                    ? CoordinationEffectState.Detached
                    : isAlive ? CoordinationEffectState.UnknownRole : CoordinationEffectState.Lost,
                CoverageAssessment.Unknown));
        }

        return effects
            .OrderBy(e => e.UnitId, StringComparer.Ordinal)
            .ThenBy(e => e.ElementId, StringComparer.Ordinal)
            .ToArray();
    }

    private static CoverageAssessment ProjectCoverage(
        C2NodeElement element,
        CoordinationEffectState state,
        IReadOnlyDictionary<string, CoverageFact> coverageByElement)
    {
        if (!coverageByElement.TryGetValue(element.ElementId, out var fact))
        {
            return CoverageAssessment.Unknown;
        }

        if (state == CoordinationEffectState.UnknownAvailability)
        {
            return new CoverageAssessment(
                CoverageStatus.Unknown,
                fact.CoverageId,
                fact.Label,
                Geometry: null,
                ReasonCode: "CONTRIBUTOR_LAST_KNOWN");
        }

        var status = state == CoordinationEffectState.Available ? CoverageStatus.Covered : CoverageStatus.Gap;
        return new CoverageAssessment(
            status,
            fact.CoverageId,
            fact.Label,
            Geometry: IsUsableGeometry(fact.Geometry)
                ? new CoverageAreaGeometry(Array.AsReadOnly(fact.Geometry!.Boundary.ToArray()), fact.Geometry.SourceRef) : null,
            ReasonCode: status == CoverageStatus.Gap ? "CONTRIBUTOR_UNAVAILABLE" : null);
    }

    private static bool IsUsableGeometry(CoverageAreaGeometry? geometry) =>
        geometry is not null
        && !string.IsNullOrWhiteSpace(geometry.SourceRef)
        && geometry.Boundary is { Count: >= 3 }
        && geometry.Boundary.All(p =>
            double.IsFinite(p.NormalizedX)
            && double.IsFinite(p.NormalizedY)
            && p.NormalizedX is >= 0 and <= 1
            && p.NormalizedY is >= 0 and <= 1);

    private static Decision.DecisionLog BoundLogAt(
        Decision.DecisionLog source,
        double simTime)
    {
        var entries = source.ChronologicalEntries();
        if (!entries.Any(e => e.SimTime > simTime))
        {
            return source;
        }

        var bounded = new Decision.DecisionLog();
        foreach (var entry in entries)
        {
            if (entry.SimTime <= simTime)
            {
                bounded.Append(entry);
            }
        }

        return bounded;
    }

    private static IReadOnlyList<CoordinationGap> BuildGaps(IReadOnlyList<CoordinationEffect> effects)
    {
        var gaps = new List<CoordinationGap>();
        foreach (var effect in effects)
        {
            if (effect.Role is null)
            {
                gaps.Add(new CoordinationGap("UNKNOWN_ROLE", effect.UnitId, null, "No authored package role."));
                if (effect.State == CoordinationEffectState.Lost)
                {
                    gaps.Add(new CoordinationGap("LOST_MEMBER", effect.UnitId, null, "Member is not alive in the snapshot."));
                }
                continue;
            }

            if (effect.State is CoordinationEffectState.Lost or CoordinationEffectState.Detached)
            {
                var prefix = effect.State == CoordinationEffectState.Detached ? "DETACHED" : "LOST";
                gaps.Add(new CoordinationGap(
                    $"{prefix}_{effect.Role.Value.ToString().ToUpperInvariant()}",
                    effect.UnitId,
                    effect.Role,
                    $"{effect.Role} responsibility contributor is {effect.State.ToString().ToLowerInvariant()}."));
            }
        }

        foreach (var shared in effects
                     .Where(e => e.Role is not null && e.State == CoordinationEffectState.Available)
                     .GroupBy(e => e.UnitId, StringComparer.Ordinal)
                     .Where(g => g.Select(e => e.Role).Distinct().Count() > 1))
        {
            gaps.Add(new CoordinationGap(
                "SCARCE_SHARED_UNIT",
                shared.Key,
                Role: null,
                "One available unit owns multiple package responsibilities."));
        }

        return gaps
            .OrderBy(g => g.Code, StringComparer.Ordinal)
            .ThenBy(g => g.UnitId, StringComparer.Ordinal)
            .ToArray();
    }
}
