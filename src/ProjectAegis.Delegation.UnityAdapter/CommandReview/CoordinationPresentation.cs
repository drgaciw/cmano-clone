namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using System.Text;
using Bridge;
using Controllers;
using Core;
using MissionIntent;
using ProjectAegis.Sim.Comms;
using Targets;

/// <summary>Projected responsibility boundary row for task-group review (DRG-175).</summary>
public sealed record CoordinationResponsibilityBoundary(
    string Kind,
    string OwnerUnitId,
    string ScopeLabel,
    string StatusLabel,
    string Detail);

/// <summary>Projected per-unit effect of a candidate task-group decision (DRG-175).</summary>
public sealed record CoordinationUnitEffectPreview(
    string UnitId,
    string RoleLabel,
    bool IncludedInGroupScope,
    string EffectStatus,
    string EffectSummary);

/// <summary>Preview of one <see cref="CoordinationDecision"/> including allow/block reason and per-unit effects.</summary>
public sealed record CoordinationCommandEffectPreview(
    CoordinationDecision Decision,
    string CommandId,
    OrderKind? TargetOrderKind,
    bool IsAllowed,
    string? BlockReasonCode,
    string? BlockExplanation,
    IReadOnlyList<CoordinationUnitEffectPreview> UnitEffects,
    string SummaryLine);

/// <summary>Read-only presentation model for one task group (DRG-175).</summary>
public sealed record CoordinationGroupPresentation(
    string GroupId,
    string IntentHeaderLine,
    string ControllerLine,
    IReadOnlyList<CoordinationResponsibilityBoundary> Boundaries,
    IReadOnlyList<CoordinationCommandEffectPreview> DecisionPreviews,
    IReadOnlyList<string> GapLines,
    string ActiveBlockReasonCode,
    string ActiveBlockExplanation)
{
    /// <summary>Returns the preview for the specified decision.</summary>
    public CoordinationCommandEffectPreview GetPreview(CoordinationDecision decision) =>
        DecisionPreviews.FirstOrDefault(p => p.Decision == decision)
        ?? new CoordinationCommandEffectPreview(
            decision,
            CommandId: string.Empty,
            TargetOrderKind: null,
            IsAllowed: false,
            BlockReasonCode: CoordinationCommandBridge.ReasonUnknownGroup,
            BlockExplanation: CoordinationBlockExplainer.Explain(CoordinationCommandBridge.ReasonUnknownGroup),
            UnitEffects: Array.Empty<CoordinationUnitEffectPreview>(),
            SummaryLine: $"{decision}: unavailable");
}

/// <summary>Complete read-only coordination presentation frame for UI Toolkit hosts (DRG-175).</summary>
public sealed record CoordinationPresentationState(
    string AdvisoryBadge,
    IReadOnlyList<CoordinationGroupPresentation> Groups,
    string Fingerprint)
{
    /// <summary>Empty coordination presentation state.</summary>
    public static CoordinationPresentationState Empty { get; } = new(
        CoordinationPresentation.AdvisoryBannerText,
        Array.Empty<CoordinationGroupPresentation>(),
        "coord-ui:empty");

    /// <summary>Finds a projected group presentation by identifier, or returns the first group if omitted.</summary>
    public CoordinationGroupPresentation? FindGroup(string? groupId)
    {
        if (Groups.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            return Groups[0];
        }

        return Groups.FirstOrDefault(g => string.Equals(g.GroupId, groupId, StringComparison.Ordinal))
            ?? Groups[0];
    }
}

/// <summary>Maps command-facade block reason codes to clear, non-empty human explanations (DRG-175).</summary>
public static class CoordinationBlockExplainer
{
    /// <summary>Block reason when timeline history inspection is active.</summary>
    public const string ReasonTimelineInspecting = "TIMELINE_INSPECTING";

    /// <summary>Returns a human-readable explanation for a coordination command block code.</summary>
    public static string Explain(string? reasonCode) => reasonCode switch
    {
        CoordinationCommandBridge.ReasonReplayAttached =>
            "Replay viewer is attached (read-only mode); group commands cannot mutate the order log.",
        CoordinationCommandBridge.ReasonUnknownGroup =>
            "Task group is not registered in the active delegation runtime or snapshot is unavailable.",
        CoordinationCommandBridge.ReasonEmptyGroup =>
            "Task group has zero active members to receive an expanded group order.",
        CoordinationCommandBridge.ReasonMissingMember =>
            "One or more group members are destroyed, missing from the snapshot, or detached; atomic group execution is blocked.",
        CoordinationCommandBridge.ReasonNotHumanControl =>
            "Task group controller slot is not under direct HumanController authority; take human group control before issuing Hold or Withdraw.",
        CoordinationCommandBridge.ReasonAuthorityWithheld =>
            "Delegation authority is withheld for this task-group scope.",
        CoordinationCommandBridge.ReasonIntentConstraint =>
            "Active mission-intent constraint (HOLD, NO_STRIKE, or ROE_WITHHOLD) prevents this group decision.",
        CoordinationCommandBridge.ReasonRetattackAdvisoryOnly =>
            "Reattack is advisory-only and never auto-enqueues weapons or fire orders; use explicit target-bound engagement controls.",
        CoordinationCommandBridge.ReasonEnqueueFailed =>
            "Failed to enqueue the group human order into the controller queue.",
        CoordinationCommandBridge.ReasonInvalidSimTime =>
            "Snapshot simulation time is non-finite or negative.",
        CoordinationCommandBridge.ReasonGroupDecisionPending =>
            "A prior approved group decision is already awaiting delayed execution for this group.",
        CoordinationCommandBridge.ReasonIntentScopeMismatch =>
            "Reviewed mission-intent snapshot belongs to a different task-group scope.",
        C2PlayerCommandBridge.ReasonOffGrid =>
            "Task group is currently off the side comms grid; command delivery is blocked until comms rejoin.",
        ReasonTimelineInspecting =>
            "Timeline history inspection is active; return to live combat before issuing group decisions.",
        "NO_PENDING_CONFIRM" =>
            "No staged assisted confirmation is pending.",
        _ when string.IsNullOrWhiteSpace(reasonCode) =>
            "Ready: deliberate Hold and Withdraw route via CoordinationCommandBridge.",
        _ =>
            $"Blocked ({reasonCode}): group command rejected by command-facade validation.",
    };

    /// <summary>Extracts a machine reason code from <c>DelegationBridgeHost.SubmitGroupDecision</c> feedback strings.</summary>
    public static bool TryExtractReasonCode(string? submitFeedback, out string reasonCode)
    {
        reasonCode = string.Empty;
        if (string.IsNullOrWhiteSpace(submitFeedback))
        {
            return false;
        }

        const string preventedPrefix = "Prevented:";
        if (submitFeedback.StartsWith(preventedPrefix, StringComparison.Ordinal))
        {
            reasonCode = submitFeedback.Substring(preventedPrefix.Length).Trim();
            return !string.IsNullOrEmpty(reasonCode);
        }

        if (submitFeedback.Contains("Return to live", StringComparison.OrdinalIgnoreCase))
        {
            reasonCode = ReasonTimelineInspecting;
            return true;
        }

        if (submitFeedback.Contains("already awaiting", StringComparison.OrdinalIgnoreCase))
        {
            reasonCode = CoordinationCommandBridge.ReasonGroupDecisionPending;
            return true;
        }

        return false;
    }
}

/// <summary>
/// Projects read-only task-group responsibility boundaries, command-effect previews, and block explanations
/// without mutating simulation or controller state (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class CoordinationPresentation
{
    /// <summary>Advisory banner disclosing read-only projection vs command-facade routing.</summary>
    public const string AdvisoryBannerText =
        "COORDINATION · Read-only projection; group orders route via CoordinationCommandBridge";

    /// <summary>Builds presentation state across all task groups in <paramref name="snapshot"/>.</summary>
    public static CoordinationPresentationState Build(
        CoordinationSnapshot? snapshot,
        DelegationBridge? bridge = null,
        ISimWorldSnapshot? world = null,
        bool isTimelineInspecting = false,
        bool hasPendingScopeForGroup = false) =>
        Build(
            snapshot,
            bridge,
            world,
            isTimelineInspecting,
            hasPendingScopeForGroup ? _ => true : null);

    /// <summary>Builds presentation state with per-group pending-scope lookup.</summary>
    public static CoordinationPresentationState Build(
        CoordinationSnapshot? snapshot,
        DelegationBridge? bridge,
        ISimWorldSnapshot? world,
        bool isTimelineInspecting,
        Func<string, bool>? hasPendingScope)
    {
        if (snapshot is null || snapshot.Groups.Count == 0)
        {
            return CoordinationPresentationState.Empty;
        }

        var groups = new List<CoordinationGroupPresentation>(snapshot.Groups.Count);
        foreach (var group in snapshot.Groups)
        {
            groups.Add(BuildGroup(group, bridge, world, isTimelineInspecting, hasPendingScope));
        }

        var state = new CoordinationPresentationState(AdvisoryBannerText, groups, string.Empty);
        return state with { Fingerprint = ComputeFingerprint(state) };
    }

    /// <summary>Previews per-unit effects and allow/block status for one <see cref="CoordinationDecision"/>.</summary>
    public static CoordinationCommandEffectPreview PreviewGroupDecision(
        CoordinationGroupSnapshot group,
        CoordinationDecision decision,
        DelegationBridge? bridge = null,
        ISimWorldSnapshot? world = null,
        bool isTimelineInspecting = false,
        bool hasPendingScope = false)
    {
        if (group is null)
        {
            throw new ArgumentNullException(nameof(group));
        }

        var commandId = decision switch
        {
            CoordinationDecision.Hold => "hold",
            CoordinationDecision.Withdraw => "rtb",
            _ => "reattack (advisory)",
        };
        OrderKind? targetKind = decision switch
        {
            CoordinationDecision.Hold => OrderKind.Hold,
            CoordinationDecision.Withdraw => OrderKind.ReturnToBase,
            _ => null,
        };

        var blockReason = ResolveBlockReason(
            group,
            decision,
            bridge,
            world,
            isTimelineInspecting,
            hasPendingScope);
        var isAllowed = blockReason is null;
        var explanation = blockReason is null ? null : CoordinationBlockExplainer.Explain(blockReason);
        var unitEffects = BuildUnitEffectPreviews(group, decision, blockReason, world);

        var willApplyCount = unitEffects.Count(u => string.Equals(u.EffectStatus, "WILL_APPLY", StringComparison.Ordinal));
        var excludedCount = unitEffects.Count(u => string.Equals(u.EffectStatus, "EXCLUDED_DETACHED", StringComparison.Ordinal));
        var summaryLine = decision == CoordinationDecision.Reattack
            ? $"Reattack: ADVISORY ONLY ({CoordinationCommandBridge.ReasonRetattackAdvisoryOnly}) — never auto-enqueues fire orders"
            : isAllowed
                ? $"{decision} ({commandId} -> {targetKind}): READY · expands to {willApplyCount} unit(s)"
                    + (excludedCount > 0 ? $" ({excludedCount} detached excluded)" : string.Empty)
                : $"{decision} ({commandId}): BLOCKED ({blockReason}) — {explanation}";

        return new CoordinationCommandEffectPreview(
            decision,
            commandId,
            targetKind,
            isAllowed,
            blockReason,
            explanation,
            unitEffects,
            summaryLine);
    }

    /// <summary>Formats bounded lines for <see cref="CommandReviewDashboard"/>'s <c>groups</c> section.</summary>
    public static IReadOnlyList<string> BuildDashboardLines(CoordinationGroupSnapshot group)
    {
        if (group is null)
        {
            throw new ArgumentNullException(nameof(group));
        }
        var details = group.Details ?? CoordinationGroupDetails.Empty;
        var coord = group.Coordination;
        var intent = AsNullable<MissionIntentSnapshot>(group.Intent);
        var members = coord.Members;
        var constraints = AsNullable<IReadOnlyList<string>>(intent?.Constraints) ?? Array.Empty<string>();
        var effects = group.Effects;
        var gaps = AsNullable<IReadOnlyList<CoordinationGap>>(group.Gaps) ?? Array.Empty<CoordinationGap>();
        var lines = new List<string>
        {
            coord.StatusLine,
            intent?.StatusLine ?? "INTENT: —",
            "Members: " + string.Join(", ", members),
            "Constraints: " + string.Join(", ", constraints),
            "Retask advice: " + (intent?.AdvisoryRetask ?? MissionIntentRetaskAdvice.None),
            $"Controller: {details.ControllerKind} | Autonomy: {details.DelegationAutonomy} | OffGrid: {details.IsOffGrid} | PendingReplan: {details.PendingReplan}",
        };

        foreach (var escort in details.Escorts)
        {
            lines.Add(
                $"Escort: {escort.EscortUnitId} -> {(string.IsNullOrEmpty(escort.ProtectedUnitId) ? "group" : escort.ProtectedUnitId)} ({escort.CapabilityScope}) [{escort.State}]");
        }

        foreach (var sector in details.Sectors)
        {
            lines.Add(
                $"Sector: {sector.CoverageId} ({sector.Label}) | owner {sector.OwnerUnitId} ({sector.Role?.ToString() ?? "UNKNOWN"}) | {sector.Status}"
                + (string.IsNullOrEmpty(sector.SourceRef) ? string.Empty : $" | {sector.SourceRef}"));
        }

        foreach (var target in details.SharedTargets)
        {
            lines.Add(
                $"Shared target: {target.TargetId} | units [{string.Join(", ", target.AssignedUnitIds)}] | FCR={target.HasFireControlTrack} | {target.SourceKind}");
        }

        foreach (var effect in effects)
        {
            lines.Add($"{effect.UnitId} | role {effect.Role?.ToString() ?? "UNKNOWN"} | {effect.State} | coverage {effect.Coverage.Status}: {effect.Coverage.Label}");
        }

        foreach (var gap in gaps)
        {
            lines.Add($"GAP {gap.Code} | {gap.UnitId} | {gap.Detail}");
        }

        var holdPreview = PreviewGroupDecision(group, CoordinationDecision.Hold);
        var withdrawPreview = PreviewGroupDecision(group, CoordinationDecision.Withdraw);
        var reattackPreview = PreviewGroupDecision(group, CoordinationDecision.Reattack);
        lines.Add($"Command effects: Hold={holdPreview.BlockReasonCode ?? "READY"} | Withdraw={withdrawPreview.BlockReasonCode ?? "READY"} | Reattack={reattackPreview.BlockReasonCode ?? "ADVISORY"}");

        return lines;
    }

    /// <summary>Computes a deterministic fingerprint for change detection in UI hosts.</summary>
    public static string ComputeFingerprint(CoordinationPresentationState? state)
    {
        if (state is null || state.Groups.Count == 0)
        {
            return "coord-ui:empty";
        }

        var sb = new StringBuilder();
        sb.Append(state.AdvisoryBadge).Append('|').Append(state.Groups.Count);
        foreach (var g in state.Groups)
        {
            sb.Append('|').Append(g.GroupId)
                .Append(':').Append(g.IntentHeaderLine)
                .Append(':').Append(g.ControllerLine)
                .Append(':').Append(g.ActiveBlockReasonCode);
            foreach (var b in g.Boundaries)
            {
                sb.Append(';').Append(b.Kind).Append('/').Append(b.OwnerUnitId).Append('/').Append(b.StatusLabel).Append('/').Append(b.Detail);
            }

            foreach (var d in g.DecisionPreviews)
            {
                sb.Append(';').Append((int)d.Decision).Append('=').Append(d.IsAllowed ? '1' : '0').Append('/').Append(d.BlockReasonCode);
                foreach (var u in d.UnitEffects)
                {
                    sb.Append(',').Append(u.UnitId).Append(':').Append(u.EffectStatus);
                }
            }
        }

        return sb.ToString();
    }

    private static CoordinationGroupPresentation BuildGroup(
        CoordinationGroupSnapshot group,
        DelegationBridge? bridge,
        ISimWorldSnapshot? world,
        bool isTimelineInspecting,
        Func<string, bool>? hasPendingScope)
    {
        var coord = group.Coordination;
        var groupId = coord.GroupId;
        var details = group.Details ?? CoordinationGroupDetails.Empty;
        var pending = hasPendingScope?.Invoke(groupId) ?? false;

        var boundaries = BuildBoundaries(group, details);
        var previews = new[]
        {
            PreviewGroupDecision(group, CoordinationDecision.Hold, bridge, world, isTimelineInspecting, pending),
            PreviewGroupDecision(group, CoordinationDecision.Withdraw, bridge, world, isTimelineInspecting, pending),
            PreviewGroupDecision(group, CoordinationDecision.Reattack, bridge, world, isTimelineInspecting, pending),
        };

        var gaps = AsNullable<IReadOnlyList<CoordinationGap>>(group.Gaps) ?? Array.Empty<CoordinationGap>();
        var gapLines = gaps
            .Select(g => $"GAP {g.Code} · {g.UnitId} ({g.Role?.ToString() ?? "UNASSIGNED"}): {g.Detail}")
            .ToArray();

        var primaryBlocked = previews.FirstOrDefault(p => p.Decision != CoordinationDecision.Reattack && !p.IsAllowed);
        var activeReason = primaryBlocked?.BlockReasonCode ?? string.Empty;
        var activeExplanation = CoordinationBlockExplainer.Explain(activeReason);

        var pkgLabel = string.IsNullOrWhiteSpace(coord.AssignedPackageId)
            ? "unassigned"
            : $"{coord.AssignedPackageId} ({coord.GapCode})";
        var intent = AsNullable<MissionIntentSnapshot>(group.Intent);
        var constraints = AsNullable<IReadOnlyList<string>>(intent?.Constraints);
        var constraintsLabel = constraints is null || constraints.Count == 0
            ? "none"
            : string.Join(", ", constraints);
        var intentCode = intent?.IntentCode ?? "UNSPECIFIED";
        var advisoryRetask = intent?.AdvisoryRetask ?? MissionIntentRetaskAdvice.None;
        var intentHeader = $"{groupId} · pkg {pkgLabel} · intent {intentCode} [constraints: {constraintsLabel}] · retask {advisoryRetask}";
        var controllerLine = $"Controller: {details.ControllerKind} · Autonomy: {details.DelegationAutonomy}"
            + (details.IsOffGrid ? " · COMMS OFF GRID" : string.Empty)
            + (details.PendingReplan ? " · REPLAN PENDING" : string.Empty);

        return new CoordinationGroupPresentation(
            groupId,
            intentHeader,
            controllerLine,
            boundaries,
            previews,
            gapLines,
            activeReason,
            activeExplanation);
    }

    private static IReadOnlyList<CoordinationResponsibilityBoundary> BuildBoundaries(
        CoordinationGroupSnapshot group,
        CoordinationGroupDetails details)
    {
        var list = new List<CoordinationResponsibilityBoundary>();
        var fallbackGroupId = group.Coordination.GroupId;

        foreach (var escort in details.Escorts)
        {
            var status = escort.State switch
            {
                CoordinationEffectState.Available => "ACTIVE",
                CoordinationEffectState.Detached or CoordinationEffectState.Lost => "GAP",
                _ => "DEGRADED",
            };
            var protectedTarget = string.IsNullOrEmpty(escort.ProtectedUnitId) ? fallbackGroupId : escort.ProtectedUnitId;
            list.Add(new CoordinationResponsibilityBoundary(
                "Escort",
                escort.EscortUnitId,
                escort.CapabilityScope,
                status,
                $"Escort {escort.EscortUnitId} screens {protectedTarget} ({escort.CapabilityScope}) — {escort.State}"));
        }

        foreach (var sector in details.Sectors)
        {
            var status = sector.Status switch
            {
                CoverageStatus.Covered => "ACTIVE",
                CoverageStatus.Gap => "GAP",
                _ => "UNKNOWN",
            };
            list.Add(new CoordinationResponsibilityBoundary(
                "Sector",
                sector.OwnerUnitId,
                sector.CoverageId,
                status,
                $"Sector {sector.Label} ({sector.CoverageId}) owned by {sector.OwnerUnitId} [{sector.Role?.ToString() ?? "UNKNOWN"}] — {sector.Status}"
                + (string.IsNullOrEmpty(sector.ReasonCode) ? string.Empty : $" ({sector.ReasonCode})")));
        }

        foreach (var target in details.SharedTargets)
        {
            var assigned = target.AssignedUnitIds;
            list.Add(new CoordinationResponsibilityBoundary(
                "SharedTarget",
                string.Join(", ", assigned),
                target.TargetId,
                target.HasFireControlTrack ? "ACTIVE" : "DEGRADED",
                $"Shared target {target.TargetId}: [{string.Join(", ", assigned)}] (FCR={(target.HasFireControlTrack ? "Yes" : "No")}, {target.SourceKind})"));
        }

        foreach (var effect in group.Effects.Where(e => e.Role is not null))
        {
            var status = effect.State switch
            {
                CoordinationEffectState.Available => "ACTIVE",
                CoordinationEffectState.Detached or CoordinationEffectState.Lost => "GAP",
                _ => "DEGRADED",
            };
            list.Add(new CoordinationResponsibilityBoundary(
                "PackageRole",
                effect.UnitId,
                effect.Role!.Value.ToString(),
                status,
                $"Role {effect.Role} ({effect.ElementId}) on {effect.UnitId} — {effect.State}"));
        }

        return list;
    }

    private static string? ResolveBlockReason(
        CoordinationGroupSnapshot group,
        CoordinationDecision decision,
        DelegationBridge? bridge,
        ISimWorldSnapshot? world,
        bool isTimelineInspecting,
        bool hasPendingScope)
    {
        if (isTimelineInspecting)
        {
            return CoordinationBlockExplainer.ReasonTimelineInspecting;
        }

        if (bridge is not null && bridge.AttachReplayViewer)
        {
            return CoordinationCommandBridge.ReasonReplayAttached;
        }

        if (world is not null && (!double.IsFinite(world.SimTime) || world.SimTime < 0))
        {
            return CoordinationCommandBridge.ReasonInvalidSimTime;
        }

        if (hasPendingScope)
        {
            return CoordinationCommandBridge.ReasonGroupDecisionPending;
        }

        var groupId = group.Coordination.GroupId;
        var effects = group.Effects;
        var members = group.Coordination.Members;
        if (bridge is not null)
        {
            var groupBinding = bridge.Registry.Bindings.FirstOrDefault(b =>
                b.Target is GroupTarget && string.Equals(b.TargetId.Value, groupId, StringComparison.Ordinal));
            if (groupBinding?.Target is not GroupTarget groupTarget)
            {
                return CoordinationCommandBridge.ReasonUnknownGroup;
            }

            if (groupTarget.Slot.Active is not HumanController)
            {
                return CoordinationCommandBridge.ReasonNotHumanControl;
            }

            if (groupTarget.Members.Count == 0)
            {
                return CoordinationCommandBridge.ReasonEmptyGroup;
            }

            for (var i = 0; i < groupTarget.Members.Count; i++)
            {
                var memberId = groupTarget.Members[i];
                if (!bridge.Registry.TryGetBinding(memberId, out var memberBinding)
                    || memberBinding.Target is not UnitTarget unit
                    || unit.IsDetachedFromGroup
                    || (world is not null && !world.IsMemberAlive(memberId))
                    || (world is null && effects.Any(e =>
                        e.State == CoordinationEffectState.Lost
                        && string.Equals(e.UnitId, memberId.Value, StringComparison.Ordinal))))
                {
                    return CoordinationCommandBridge.ReasonMissingMember;
                }
            }
        }
        else
        {
            var details = group.Details ?? CoordinationGroupDetails.Empty;
            if (!string.Equals(details.ControllerKind, "Human", StringComparison.Ordinal))
            {
                return CoordinationCommandBridge.ReasonNotHumanControl;
            }

            if (members.Count == 0)
            {
                return CoordinationCommandBridge.ReasonEmptyGroup;
            }

            if (effects.Any(e =>
                    e.State == CoordinationEffectState.Lost
                    && members.Contains(e.UnitId, StringComparer.Ordinal)))
            {
                return CoordinationCommandBridge.ReasonMissingMember;
            }
        }

        if (IsConstrained(decision, AsNullable<MissionIntentSnapshot>(group.Intent)))
        {
            return CoordinationCommandBridge.ReasonIntentConstraint;
        }

        if (decision == CoordinationDecision.Reattack)
        {
            return CoordinationCommandBridge.ReasonRetattackAdvisoryOnly;
        }

        if (bridge is not null)
        {
            var offGridReason = world is not null
                ? OffGridOrderGate.EvaluateAt(
                    bridge.Orchestrator.CommsGrid,
                    groupId,
                    (ulong)Math.Max(0, (long)world.SimTime))
                : OffGridOrderGate.Evaluate(bridge.Orchestrator.CommsGrid, groupId);
            if (offGridReason != null)
            {
                return C2PlayerCommandBridge.ReasonOffGrid;
            }
        }
        else if (group.Details?.IsOffGrid == true)
        {
            return C2PlayerCommandBridge.ReasonOffGrid;
        }

        return null;
    }

    private static IReadOnlyList<CoordinationUnitEffectPreview> BuildUnitEffectPreviews(
        CoordinationGroupSnapshot group,
        CoordinationDecision decision,
        string? blockReason,
        ISimWorldSnapshot? world)
    {
        var effects = group.Effects;
        var members = group.Coordination.Members;
        var effectsByUnit = effects
            .GroupBy(e => e.UnitId, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);
        var scopeSet = new HashSet<string>(members, StringComparer.Ordinal);
        var list = new List<CoordinationUnitEffectPreview>();

        foreach (var unitGroup in effectsByUnit)
        {
            var unitId = unitGroup.Key;
            var roles = unitGroup
                .Where(e => e.Role is not null)
                .Select(e => e.Role!.Value.ToString())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var roleLabel = roles.Length == 0 ? "UNASSIGNED" : string.Join("/", roles);
            var inScope = scopeSet.Contains(unitId);
            var isDetached = unitGroup.Any(e => e.State == CoordinationEffectState.Detached) || !inScope;
            var isLost = unitGroup.Any(e => e.State == CoordinationEffectState.Lost)
                || (world is not null && !world.IsMemberAlive(new TargetId(unitId)));

            string status;
            string summary;
            if (decision == CoordinationDecision.Reattack)
            {
                status = "ADVISORY_ONLY";
                summary = $"{unitId} ({roleLabel}): Reattack is advisory-only — no fire order emitted";
            }
            else if (isDetached)
            {
                status = "EXCLUDED_DETACHED";
                summary = $"{unitId} ({roleLabel}): Detached from task-group scope — unaffected by group {decision}";
            }
            else if (isLost)
            {
                status = "BLOCKED_LOST";
                summary = $"{unitId} ({roleLabel}): Member unavailable/lost — blocks atomic group execution";
            }
            else if (blockReason is not null)
            {
                status = "BLOCKED";
                summary = $"{unitId} ({roleLabel}): In group scope, blocked by {blockReason}";
            }
            else
            {
                var orderLabel = decision == CoordinationDecision.Hold ? "Hold" : "ReturnToBase";
                status = "WILL_APPLY";
                summary = $"{unitId} ({roleLabel}): Will receive expanded {orderLabel} order via CoordinationOrderSink";
            }

            list.Add(new CoordinationUnitEffectPreview(unitId, roleLabel, !isDetached, status, summary));
        }

        if (list.Count == 0 && members.Count > 0)
        {
            foreach (var member in members)
            {
                if (string.IsNullOrWhiteSpace(member))
                {
                    continue;
                }

                list.Add(new CoordinationUnitEffectPreview(
                    member,
                    "UNASSIGNED",
                    true,
                    decision == CoordinationDecision.Reattack ? "ADVISORY_ONLY" : blockReason is null ? "WILL_APPLY" : "BLOCKED",
                    $"{member}: {(blockReason ?? "ready")}"));
            }
        }

        return list;
    }

    private static bool IsConstrained(CoordinationDecision decision, MissionIntentSnapshot? intent)
    {
        var constraints = AsNullable<IReadOnlyList<string>>(intent?.Constraints);
        if (constraints is null)
        {
            return false;
        }

        if (decision == CoordinationDecision.Reattack)
        {
            return constraints.Contains(MissionIntentConstraintCode.NoStrike, StringComparer.Ordinal)
                || constraints.Contains(MissionIntentConstraintCode.RoeWithhold, StringComparer.Ordinal)
                || constraints.Contains(MissionIntentConstraintCode.Hold, StringComparer.Ordinal);
        }

        return decision == CoordinationDecision.Withdraw
            && constraints.Contains(MissionIntentConstraintCode.Hold, StringComparer.Ordinal);
    }

    private static T? AsNullable<T>(object? value) where T : class => value as T;
}
