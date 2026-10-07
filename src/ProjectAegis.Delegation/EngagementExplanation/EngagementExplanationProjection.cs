namespace ProjectAegis.Delegation.EngagementExplanation;

using CombatEvents;
using EngageExplainContract;
using EngageNextAction;
using Projection;
using Skills;
using TargetabilityAccept;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-168: explains one engagement leg from the DRG-165 combat-event snapshot (events, Slice A targetability
/// facts, execution facts) and the DRG-215 explanation contract. Presentation fence (ADR-010 §2–3): no order log,
/// world snapshot, or projection rebuilds — missing facts stay UNKNOWN.
/// </summary>
public static class EngagementExplanationProjection
{
    public const string Unknown = "UNKNOWN";

    private const string TargetabilityPrefix = "targetability:";

    private static readonly HashSet<string> DoctrineCodes = new(StringComparer.Ordinal)
    {
        nameof(FireAbortReason.RoeHoldFire),
        nameof(FireAbortReason.WeaponsTight),
        nameof(FireAbortReason.WraRange),
        nameof(FireAbortReason.WraSalvo),
        nameof(FireAbortReason.EmconOff),
        nameof(FireAbortReason.AutoEngageDenied),
        nameof(FireAbortReason.ExpendUnauthorized),
        TargetabilityAcceptCauseCodes.SharedTrackNoRelease,
        TargetabilityAcceptCauseCodes.WeaponsReleaseRequired,
        TargetabilityAcceptCauseCodes.ApprovalRequired,
        AbortReasonCatalog.Engage.ROE_HOLD_FIRE,
        AbortReasonCatalog.Engage.ROE_WEAPONS_TIGHT,
        AbortReasonCatalog.Engage.WRA_SALVO,
        AbortReasonCatalog.Engage.EMCON_OFF,
        AbortReasonCatalog.Engage.BLACK_PROJECT_REQUIRED,
        AbortReasonCatalog.Engage.TECHNOLOGY_LEVEL_EXCEEDED,
    };

    /// <summary>
    /// Builds the explanation for the exact shooter / target / correlation leg. Returns
    /// <see cref="EngagementExplanationSurface.Empty"/> when the snapshot has no matching events.
    /// </summary>
    public static EngagementExplanationSurface Build(
        CombatEventSnapshot? events,
        string shooterId,
        string targetId,
        ulong correlationId)
    {
        if (events is null)
        {
            return EngagementExplanationSurface.Empty;
        }

        var leg = events.Events
            .Where(e => string.Equals(e.ShooterId, shooterId, StringComparison.Ordinal)
                && string.Equals(e.TargetId, targetId, StringComparison.Ordinal)
                && e.CorrelationId == correlationId)
            .ToArray();
        if (leg.Length == 0)
        {
            return EngagementExplanationSurface.Empty;
        }

        var refused = leg.LastOrDefault(e => e.Phase == CombatEventPhase.AuthorizationRefused);
        var executed = leg.LastOrDefault(e => e.Phase is CombatEventPhase.Firing
            or CombatEventPhase.InFlight
            or CombatEventPhase.TerminalOutcome);
        var authorized = leg.LastOrDefault(e => e.Phase == CombatEventPhase.Authorized);
        var status = refused is not null ? EngagementExplanationStatus.Refused
            : executed is not null ? EngagementExplanationStatus.Selected
            : authorized is not null ? EngagementExplanationStatus.Available
            : EngagementExplanationStatus.Unknown;
        var decisive = refused ?? executed ?? authorized ?? leg[^1];

        var contract = EngageExplainContractProjection.ProjectFromSnapshot(new EngageExplainCombatEventSnapshot(
            leg.Select(e => new EngageExplainCombatEventInput(
                (EngageExplainCombatEventPhase)e.Phase,
                e.ShooterId,
                e.TargetId,
                e.WeaponFamilyId,
                e.Outcome,
                e.CorrelationId,
                e.SimTime,
                e.SimTick,
                e.ExplanationRef)).ToArray()));
        var targetability = FindTargetability(events.Targetability, shooterId, targetId);
        var actorAuthority = targetability?.ShooterId == shooterId ? targetability : null;
        var execution = FindExecution(events.Execution, shooterId, targetId, correlationId);
        var refusalCode = refused?.Outcome;
        var refusalIsTargetability = refused?.ExplanationRef.StartsWith(TargetabilityPrefix, StringComparison.Ordinal)
            == true;
        var refusalIsDoctrine = refusalCode is not null && IsDoctrineCode(refusalCode);

        var hard = new[]
        {
            TrackConstraint(targetability),
            FiringSolutionConstraint(targetability, execution),
            EngagementConstraintRow(refusalCode, refusalIsTargetability, refusalIsDoctrine),
        };
        var doctrine = new[]
        {
            RoeConstraint(actorAuthority),
            TargetingAuthorityConstraint(actorAuthority),
            PolicyConstraint(refusalCode, refusalIsDoctrine, refusalIsTargetability),
        };

        return new EngagementExplanationSurface(
            shooterId,
            targetId,
            correlationId,
            decisive.SimTime,
            status,
            BuildHeadline(status, decisive, refusalIsTargetability),
            Array.AsReadOnly(hard),
            Array.AsReadOnly(doctrine),
            targetability is null ? Unknown : targetability.Confidence.ToString(),
            execution is null
                ? $"Weapon family: {decisive.WeaponFamilyId}"
                : $"Weapon family: {decisive.WeaponFamilyId} | Salvo: {execution.SalvoSize}",
            DescribeFiringSolution(targetability, execution),
            refusalCode is null ? null : ResolveActionableReason(shooterId, decisive.WeaponFamilyId, refusalCode),
            ResolveExplanationRef(status, decisive, contract));
    }

    private static CombatTargetabilityFact? FindTargetability(
        IReadOnlyList<CombatTargetabilityFact> facts,
        string shooterId,
        string targetId)
    {
        CombatTargetabilityFact? first = null;
        CombatTargetabilityFact? contactRefusal = null;
        for (var i = 0; i < facts.Count; i++)
        {
            var fact = facts[i];
            if (!string.Equals(fact.TargetId, targetId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(fact.ShooterId, shooterId, StringComparison.Ordinal))
            {
                if (fact.IsContactWideRefusal) contactRefusal ??= fact;
                continue;
            }

            if (fact.Disposition == TargetabilityAcceptDisposition.Permitted)
            {
                return fact;
            }

            first ??= fact;
        }

        return first ?? contactRefusal;
    }

    private static CombatExecutionFact? FindExecution(
        IReadOnlyList<CombatExecutionFact> facts,
        string shooterId,
        string targetId,
        ulong correlationId)
    {
        CombatExecutionFact? found = null;
        for (var i = 0; i < facts.Count; i++)
        {
            var fact = facts[i];
            if (fact.CorrelationId == correlationId
                && string.Equals(fact.ShooterId, shooterId, StringComparison.Ordinal)
                && string.Equals(fact.TargetId, targetId, StringComparison.Ordinal))
            {
                found = fact;
            }
        }

        return found;
    }

    private static bool IsDoctrineCode(string code) => DoctrineCodes.Contains(code);

    private static EngagementConstraint TrackConstraint(CombatTargetabilityFact? fact)
    {
        if (fact is null)
        {
            return Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Track,
                EngagementConstraintState.Unknown, null, "Track: UNKNOWN — no Slice A targetability fact");
        }

        if (fact.Disposition == TargetabilityAcceptDisposition.Permitted)
        {
            return Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Track,
                EngagementConstraintState.Satisfied, null, $"Track: targetable via {fact.ContactId}");
        }

        if (!IsDoctrineCode(fact.WithheldCauseCode))
        {
            return Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Track,
                EngagementConstraintState.Violated, fact.WithheldCauseCode,
                $"Track: {DescribeTargetabilityCause(fact.WithheldCauseCode)}");
        }

        return fact.SensorToShooterComplete switch
        {
            true => Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Track,
                EngagementConstraintState.Satisfied, null, $"Track: held via {fact.ContactId}; authority withheld"),
            false => Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Track,
                EngagementConstraintState.Violated, null, "Track: sensor-to-shooter chain broken"),
            _ => Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Track,
                EngagementConstraintState.Unknown, null, "Track: UNKNOWN — no sensor-to-shooter fact"),
        };
    }

    private static EngagementConstraint FiringSolutionConstraint(
        CombatTargetabilityFact? targetability,
        CombatExecutionFact? execution)
    {
        var text = $"Firing solution: {DescribeFiringSolution(targetability, execution)}";
        var state = execution is not null
            ? execution.HasFireControlTrack switch
            {
                true => EngagementConstraintState.Satisfied,
                false => EngagementConstraintState.Violated,
                _ => EngagementConstraintState.Unknown,
            }
            : targetability?.SensorToShooterComplete switch
            {
                true => EngagementConstraintState.Satisfied,
                false => EngagementConstraintState.Violated,
                _ => EngagementConstraintState.Unknown,
            };
        var code = state == EngagementConstraintState.Violated
            && execution is null
            && targetability is { Disposition: TargetabilityAcceptDisposition.Withheld }
            && !IsDoctrineCode(targetability.WithheldCauseCode)
                ? targetability.WithheldCauseCode
                : null;
        return Row(EngagementConstraintKind.Hard, EngagementConstraintNames.FiringSolution, state, code, text);
    }

    private static EngagementConstraint EngagementConstraintRow(
        string? refusalCode,
        bool refusalIsTargetability,
        bool refusalIsDoctrine)
    {
        if (refusalCode is null)
        {
            return Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Engagement,
                EngagementConstraintState.Unknown, null, "Engagement: no hard refusal reported");
        }

        if (refusalIsDoctrine)
        {
            return Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Engagement,
                EngagementConstraintState.Unknown, null, "Engagement: refusal is a doctrine constraint");
        }

        if (refusalIsTargetability)
        {
            return Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Engagement,
                EngagementConstraintState.Unknown, null, "Engagement: refused before weapon checks (see Track)");
        }

        return Row(EngagementConstraintKind.Hard, EngagementConstraintNames.Engagement,
            EngagementConstraintState.Violated, refusalCode,
            $"Engagement: {EngageExplainProjection.ExplainCode(refusalCode)}");
    }

    private static EngagementConstraint RoeConstraint(CombatTargetabilityFact? fact) =>
        fact is null
            ? Row(EngagementConstraintKind.Doctrine, EngagementConstraintNames.Roe,
                EngagementConstraintState.Unknown, null, "ROE: UNKNOWN — no Slice A authority fact")
            : fact.RoeAllowsEngage
                ? Row(EngagementConstraintKind.Doctrine, EngagementConstraintNames.Roe,
                    EngagementConstraintState.Satisfied, null, "ROE: permits engagement")
                : Row(EngagementConstraintKind.Doctrine, EngagementConstraintNames.Roe,
                    EngagementConstraintState.Violated, fact.TargetingReasonCode,
                    "ROE: does not permit engagement");

    private static EngagementConstraint TargetingAuthorityConstraint(CombatTargetabilityFact? fact)
    {
        if (fact is null)
        {
            return Row(EngagementConstraintKind.Doctrine, EngagementConstraintNames.TargetingAuthority,
                EngagementConstraintState.Unknown, null, "Targeting authority: UNKNOWN");
        }

        return fact.TargetingDisposition switch
        {
            C2AuthorityDisposition.Permitted => Row(EngagementConstraintKind.Doctrine,
                EngagementConstraintNames.TargetingAuthority, EngagementConstraintState.Satisfied, null,
                "Targeting authority: permitted"),
            C2AuthorityDisposition.ApprovalRequired => Row(EngagementConstraintKind.Doctrine,
                EngagementConstraintNames.TargetingAuthority, EngagementConstraintState.Violated,
                fact.TargetingReasonCode, "Targeting authority: approval required"),
            _ => Row(EngagementConstraintKind.Doctrine,
                EngagementConstraintNames.TargetingAuthority, EngagementConstraintState.Violated,
                fact.TargetingReasonCode, "Targeting authority: withheld"),
        };
    }

    private static EngagementConstraint PolicyConstraint(
        string? refusalCode,
        bool refusalIsDoctrine,
        bool refusalIsTargetability)
    {
        if (refusalCode is null || !refusalIsDoctrine)
        {
            return Row(EngagementConstraintKind.Doctrine, EngagementConstraintNames.Policy,
                EngagementConstraintState.Unknown, null, "Policy: no doctrine refusal reported");
        }

        var text = refusalIsTargetability
            ? DescribeTargetabilityCause(refusalCode)
            : EngageExplainProjection.ExplainCode(refusalCode);
        return Row(EngagementConstraintKind.Doctrine, EngagementConstraintNames.Policy,
            EngagementConstraintState.Violated, refusalCode, $"Policy: {text}");
    }

    private static string DescribeFiringSolution(
        CombatTargetabilityFact? targetability,
        CombatExecutionFact? execution)
    {
        if (execution is not null)
        {
            return execution.HasFireControlTrack switch
            {
                true => "Fire-control track present at execution",
                false => "No fire-control track at execution",
                _ => "UNKNOWN at execution",
            };
        }

        return targetability?.SensorToShooterComplete switch
        {
            true => "Sensor-to-shooter chain complete",
            false => targetability.Disposition == TargetabilityAcceptDisposition.Withheld
                ? $"Sensor-to-shooter chain broken ({targetability.WithheldCauseCode})"
                : "Sensor-to-shooter chain broken",
            _ => Unknown,
        };
    }

    private static string BuildHeadline(
        EngagementExplanationStatus status,
        CombatEvent decisive,
        bool refusalIsTargetability) =>
        status switch
        {
            EngagementExplanationStatus.Refused =>
                $"Refused — {(refusalIsTargetability ? DescribeTargetabilityCause(decisive.Outcome) : EngageExplainProjection.ExplainCode(decisive.Outcome))} [{decisive.Outcome}]",
            EngagementExplanationStatus.Selected =>
                $"Selected — {decisive.WeaponFamilyId} engaged {decisive.TargetId}: {decisive.Phase} / {decisive.Outcome}",
            EngagementExplanationStatus.Available =>
                $"Available — authorized to engage {decisive.TargetId} with {decisive.WeaponFamilyId}; no refusal reported",
            EngagementExplanationStatus.Unknown =>
                $"Intent accepted for {decisive.TargetId}; authorization not yet reported",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    private static string ResolveExplanationRef(
        EngagementExplanationStatus status,
        CombatEvent decisive,
        EngageExplainContractDto contract) =>
        status switch
        {
            EngagementExplanationStatus.Refused => contract.WhyWithheld ?? decisive.ExplanationRef,
            EngagementExplanationStatus.Available => contract.WhyPermitted ?? decisive.ExplanationRef,
            EngagementExplanationStatus.Selected or EngagementExplanationStatus.Unknown => decisive.ExplanationRef,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    private static string DescribeTargetabilityCause(string cause) =>
        cause switch
        {
            TargetabilityAcceptCauseCodes.Stale or TargetabilityAcceptCauseCodes.StaleTrack =>
                "contact track is stale",
            TargetabilityAcceptCauseCodes.CatalogMiss => "contact identity is not resolved in the catalog",
            TargetabilityAcceptCauseCodes.SilentComms => "contact report is out of communications",
            TargetabilityAcceptCauseCodes.MissingProvenance => "no authoritative track for this target",
            TargetabilityAcceptCauseCodes.LostSensor => "sensor contact lost",
            TargetabilityAcceptCauseCodes.NoFireControl or TargetabilityAcceptCauseCodes.NoFireControlAuthority =>
                "no fire-control track on the contact",
            TargetabilityAcceptCauseCodes.NoEligibleShooter => "no eligible shooter for this contact",
            TargetabilityAcceptCauseCodes.DegradedTrack => "track quality is degraded",
            TargetabilityAcceptCauseCodes.WeaponsTight => "weapons tight — target not positively identified hostile",
            TargetabilityAcceptCauseCodes.RoeHoldFire => "ROE hold fire",
            TargetabilityAcceptCauseCodes.SharedTrackNoRelease => "shared track is not released for engagement",
            TargetabilityAcceptCauseCodes.WeaponsReleaseRequired => "weapons release authority required",
            TargetabilityAcceptCauseCodes.ApprovalRequired => "commander approval required",
            _ => $"targetability withheld ({cause})",
        };

    /// <summary>
    /// Actionable remedy for a refusal code; null when nothing the commander can do would lift it
    /// (for example a destroyed target or shooter).
    /// </summary>
    private static string? ResolveActionableReason(string shooterId, string weaponFamilyId, string code)
    {
        var next = EngageNextActionProjection.Project(new EngageNextActionInput(shooterId, weaponFamilyId, code))
            .Rows.FirstOrDefault()?.NextActionCode;
        if (next == EngageNextActionCodes.ReloadRearm)
        {
            return "Reload or rearm before requesting the engagement again.";
        }

        if (next == EngageNextActionCodes.Approval)
        {
            return "Request weapons-release approval from the commander.";
        }

        return code switch
        {
            TargetabilityAcceptCauseCodes.NoFireControl
                or TargetabilityAcceptCauseCodes.NoFireControlAuthority
                or nameof(FireAbortReason.NoFireControlTrack)
                or AbortReasonCatalog.Engage.NO_FIRE_CONTROL_TRACK =>
                "Acquire or designate a fire-control track on the target.",
            AbortReasonCatalog.Engage.DLZ_OUT
                or AbortReasonCatalog.Engage.OUT_OF_ENVELOPE
                or nameof(FireAbortReason.WraRange) =>
                "Close to within the weapon launch zone before engaging.",
            TargetabilityAcceptCauseCodes.Stale
                or TargetabilityAcceptCauseCodes.StaleTrack
                or TargetabilityAcceptCauseCodes.LostSensor
                or TargetabilityAcceptCauseCodes.MissingProvenance
                or TargetabilityAcceptCauseCodes.DegradedTrack
                or AbortReasonCatalog.Engage.CEC_REMOTE_TRACK_UNAVAILABLE =>
                "Re-acquire the contact with an organic or datalinked sensor.",
            TargetabilityAcceptCauseCodes.CatalogMiss =>
                "Classify the contact; its catalog identity is unresolved.",
            TargetabilityAcceptCauseCodes.SilentComms
                or nameof(FireAbortReason.CommsDenied) =>
                "Restore communications with the shooter.",
            TargetabilityAcceptCauseCodes.NoEligibleShooter =>
                "Assign a shooter with a compatible weapon in range.",
            nameof(FireAbortReason.EmconOff)
                or AbortReasonCatalog.Engage.EMCON_OFF =>
                "Change EMCON posture to permit fire-control emissions.",
            AbortReasonCatalog.Engage.MOUNT_OFFLINE =>
                "Restore the weapon mount before engaging.",
            AbortReasonCatalog.Engage.AIR_NOT_READY =>
                "Wait for the platform to complete air-operations readiness.",
            AbortReasonCatalog.Engage.AIR_ASPECT_BLOCK
                or AbortReasonCatalog.Engage.SURFACE_ASPECT_BLOCK
                or AbortReasonCatalog.Engage.SUBSURFACE_ASPECT_BLOCK
                or AbortReasonCatalog.Engage.LAND_ASPECT_BLOCK
                or AbortReasonCatalog.Engage.MINE_ASPECT_BLOCK
                or AbortReasonCatalog.Engage.FACILITY_ASPECT_BLOCK
                or AbortReasonCatalog.Engage.DOMAIN_NO_SOLUTION
                or nameof(FireAbortReason.AirAspectBlock)
                or nameof(FireAbortReason.SurfaceAspectBlock)
                or nameof(FireAbortReason.SubsurfaceAspectBlock)
                or nameof(FireAbortReason.LandAspectBlock)
                or nameof(FireAbortReason.MineAspectBlock)
                or nameof(FireAbortReason.FacilityAspectBlock) =>
                "Select a weapon that can engage this target domain.",
            nameof(FireAbortReason.WeaponsTight)
                or nameof(FireAbortReason.WraSalvo)
                or nameof(FireAbortReason.AutoEngageDenied)
                or nameof(FireAbortReason.ExpendUnauthorized)
                or TargetabilityAcceptCauseCodes.SharedTrackNoRelease
                or TargetabilityAcceptCauseCodes.WeaponsReleaseRequired
                or TargetabilityAcceptCauseCodes.ApprovalRequired
                or AbortReasonCatalog.Engage.WRA_SALVO =>
                "Request weapons-release approval from the commander.",
            _ => null,
        };
    }

    private static EngagementConstraint Row(
        EngagementConstraintKind kind,
        string name,
        EngagementConstraintState state,
        string? code,
        string text) =>
        new(kind, name, state, code, text);
}
