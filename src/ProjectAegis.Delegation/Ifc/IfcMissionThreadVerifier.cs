namespace ProjectAegis.Delegation.Ifc;

using Comms;
using Data.Catalog;
using Data.Evidence;
using Decision;
using Projection;
using ResourceRank;
using SensorToShooter;
using Skills;
using TargetabilityAccept;
using ThreatAssessment;
using ProjectAegis.Sim.Scenario;

/// <summary>Five required DRG-193 Integrated Fire Control mission-thread verification paths.</summary>
public enum IfcCaseKind
{
    Normal = 1,
    DegradedLink = 2,
    LostNode = 3,
    ConflictingAllocation = 4,
    AuthorityWithheld = 5,
}

/// <summary>Fail-closed outcome disposition for an IFC mission-thread correlation.</summary>
public enum IfcOutcomeDisposition
{
    Permitted = 0,
    Withheld = 1,
    Blocked = 2,
}

/// <summary>Per-contact input facts correlated by <see cref="IfcMissionThreadVerifier"/>.</summary>
public sealed record IfcContactEvaluationInput(
    string ContactId,
    string TargetId,
    IfcCaseKind CaseKind,
    C2AuthorityProjection Authority,
    WeaponRecommendation? Recommendation = null,
    ResourceRankSnapshot? ResourceRank = null,
    DegradedModeContext? DegradedContext = null,
    EngagementRecord? Engagement = null,
    EngagementOutcomeRecord? EngagementOutcome = null,
    CommsState CommsState = CommsState.Nominal,
    string NetworkOrUnitId = "net-primary",
    int? ShareLagTicks = null);

/// <summary>Correlated IFC mission-thread evaluation for one contact.</summary>
public sealed record IfcMissionThreadEvaluation(
    string ContactId,
    string TargetId,
    IfcCaseKind CaseKind,
    TargetabilityAcceptContactRow Targetability,
    WeaponRecommendation? Recommendation,
    ResourceRankSnapshot? ResourceRank,
    C2AuthorityProjection Authority,
    DegradedModeAssessment DegradedAssessment,
    IfcOutcomeDisposition Disposition,
    string OutcomeReasonCode,
    bool HasConflictingAllocation,
    bool EngagementLaunched,
    string? EngagementOutcomeCode,
    CommsState CommsState,
    string NetworkOrUnitId,
    int? ShareLagTicks,
    ulong SimTick,
    double SimTime);

/// <summary>Batch evaluation result across one or more contacts.</summary>
public sealed record IfcMissionThreadBatchResult(
    IReadOnlyList<IfcMissionThreadEvaluation> Evaluations);

/// <summary>
/// DRG-193 (KCX-01 / VER-03): Integrated Fire Control mission-thread correlator and verifier.
/// Correlates kill-chain state, contact provenance, sensor-to-shooter chain, targetability acceptance,
/// resource allocation ranking, weapon recommendation, C2 authority, degraded-mode policy, and
/// engagement outcome without duplicating combat rules or mutating simulation authority.
/// </summary>
public static class IfcMissionThreadVerifier
{
    /// <summary>
    /// Evaluates IFC mission-thread correlation directly from a <see cref="DecisionLog"/> by projecting
    /// kill-chain, provenance, and sensor-to-shooter snapshots once and delegating to <see cref="EvaluateBatch"/>.
    /// </summary>
    public static IfcMissionThreadBatchResult Evaluate(
        DecisionLog log,
        ulong currentSimTick,
        IReadOnlyList<IfcContactEvaluationInput> inputs,
        IKillChainFireControlSource? fireControl = null,
        ISensorToShooterShooterSource? shooters = null,
        ICatalogReader? catalog = null,
        string weaponId = CatalogWeaponIds.MvpDefault,
        ScenarioCommsDisplaySettings? commsDisplay = null,
        int staleThresholdTicks = ContactProvenanceProjection.DefaultStaleThresholdTicks)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        var killChain = KillChainContactStateProjection.Project(
            log,
            currentSimTick,
            fireControl,
            staleThresholdTicks);
        var provenance = ContactProvenanceProjection.Project(
            log,
            currentSimTick,
            catalog,
            commsDisplay: commsDisplay,
            staleThresholdTicks: staleThresholdTicks);
        var sensorToShooter = SensorToShooterProjection.Project(
            killChain,
            shooters,
            catalog,
            weaponId);

        return EvaluateBatch(
            killChain,
            provenance,
            sensorToShooter,
            inputs,
            currentSimTick,
            simTime: currentSimTick);
    }

    /// <summary>
    /// Fast-path batch correlation across pre-projected Slice A snapshots and per-contact inputs.
    /// Avoids JSON/SHA-256 serialization and redundant re-projections so 100+ active contacts evaluate in sub-millisecond time.
    /// </summary>
    public static IfcMissionThreadBatchResult EvaluateBatch(
        KillChainContactSnapshot killChain,
        ContactProvenanceSnapshot provenance,
        SensorToShooterSnapshot sensorToShooter,
        IReadOnlyList<IfcContactEvaluationInput> inputs,
        ulong currentSimTick = 0,
        double simTime = 0.0)
    {
        if (killChain is null)
        {
            throw new ArgumentNullException(nameof(killChain));
        }

        if (provenance is null)
        {
            throw new ArgumentNullException(nameof(provenance));
        }

        if (sensorToShooter is null)
        {
            throw new ArgumentNullException(nameof(sensorToShooter));
        }

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        if (inputs.Count == 0)
        {
            return new IfcMissionThreadBatchResult(Array.Empty<IfcMissionThreadEvaluation>());
        }

        var authorities = new Dictionary<string, C2AuthorityProjection>(inputs.Count, StringComparer.Ordinal);
        for (var i = 0; i < inputs.Count; i++)
        {
            var contactId = inputs[i].ContactId;
            if (string.IsNullOrWhiteSpace(contactId))
            {
                throw new ArgumentException($"Input at index {i} has an empty ContactId.", nameof(inputs));
            }

            if (authorities.ContainsKey(contactId))
            {
                throw new ArgumentException(
                    $"Duplicate ContactId '{contactId}' is not permitted in IFC batch evaluation.",
                    nameof(inputs));
            }

            authorities.Add(contactId, inputs[i].Authority);
        }

        var acceptSnapshot = TargetabilityAcceptProjection.ProjectPerContact(
            provenance,
            sensorToShooter,
            authorities);

        var rowsByContact = new Dictionary<string, TargetabilityAcceptContactRow>(
            acceptSnapshot.Contacts.Count,
            StringComparer.Ordinal);
        for (var i = 0; i < acceptSnapshot.Contacts.Count; i++)
        {
            var row = acceptSnapshot.Contacts[i];
            rowsByContact[row.ContactId] = row;
        }

        var evaluations = new IfcMissionThreadEvaluation[inputs.Count];
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            if (!rowsByContact.TryGetValue(input.ContactId, out var acceptRow))
            {
                acceptRow = new TargetabilityAcceptContactRow(
                    input.ContactId,
                    input.TargetId,
                    TargetabilityAcceptDisposition.Withheld,
                    TargetabilityAcceptCauseCodes.MissingProvenance,
                    Provenance: null,
                    SensorToShooter: null,
                    input.Authority);
            }

            var hasConflict = HasCommitmentConflict(input.ResourceRank);
            var isLostNode = input.CommsState == CommsState.Denied
                || (acceptRow.Provenance?.OutOfCommsUnknown == true)
                || (acceptRow.SensorToShooter?.PrimaryBreakCause == SensorToShooterBreakCause.LostSensor);
            var isDegradedLink = input.CommsState == CommsState.Degraded
                || (acceptRow.Provenance?.QualityState.HasFlag(ContactProvenanceQualityState.SilentComms) == true);
            var isStale = (input.DegradedContext?.StaleData == true)
                || (acceptRow.Provenance?.Freshness == ContactProvenanceFreshness.Stale)
                || (acceptRow.SensorToShooter?.PrimaryBreakCause == SensorToShooterBreakCause.StaleTrack);
            var isAuthorityWithheld = (input.DegradedContext?.AuthorityWithheld == true)
                || input.Authority.Targeting.Disposition != C2AuthorityDisposition.Permitted
                || input.Authority.Roe.TargetingDisposition != C2AuthorityDisposition.Permitted;
            var isModelUnavailable = input.DegradedContext?.ModelOrSkillUnavailable == true;

            var degradedContext = new DegradedModeContext(
                ModelOrSkillUnavailable: isModelUnavailable,
                StaleData: isStale,
                NetworkPartition: (input.DegradedContext?.NetworkPartition == true) || isLostNode || isDegradedLink,
                AuthorityWithheld: isAuthorityWithheld,
                ResourceConflict: (input.DegradedContext?.ResourceConflict == true) || hasConflict,
                Detail: input.DegradedContext?.Detail);

            var degradedAssessment = DegradedModePolicy.Evaluate(degradedContext);
            var (disposition, reasonCode) = ResolveCorrelationOutcome(
                acceptRow,
                input,
                hasConflict,
                isLostNode,
                degradedAssessment);

            var launched = input.Engagement?.Launched == true;
            var outcomeCode = input.EngagementOutcome?.OutcomeCode ?? input.Engagement?.AbortReasonCode;

            evaluations[i] = new IfcMissionThreadEvaluation(
                ContactId: input.ContactId,
                TargetId: input.TargetId,
                CaseKind: input.CaseKind,
                Targetability: acceptRow,
                Recommendation: input.Recommendation,
                ResourceRank: input.ResourceRank,
                Authority: input.Authority,
                DegradedAssessment: degradedAssessment,
                Disposition: disposition,
                OutcomeReasonCode: reasonCode,
                HasConflictingAllocation: hasConflict,
                EngagementLaunched: launched,
                EngagementOutcomeCode: outcomeCode,
                CommsState: input.CommsState,
                NetworkOrUnitId: input.NetworkOrUnitId,
                ShareLagTicks: input.ShareLagTicks,
                SimTick: currentSimTick,
                SimTime: simTime);
        }

        return new IfcMissionThreadBatchResult(evaluations);
    }

    /// <summary>
    /// Records an IFC batch evaluation into a canonical, SHA-256 digested <see cref="EvidenceLedger"/>
    /// with typed mission-thread records across all six <see cref="MissionThreadEvidenceKind"/> categories
    /// and a pending <see cref="RevisionBoundAcceptanceRecord"/> for <c>KCX-01</c> / <c>VER-03</c>.
    /// </summary>
    public static EvidenceLedger BuildEvidenceLedger(
        string ledgerId,
        string scenarioId,
        int seed,
        IfcMissionThreadBatchResult result,
        RevisionIdentity revision,
        InputDigestManifest inputs,
        string artifactLocation,
        string artifactSha256,
        string collectedAtUtc)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (revision is null)
        {
            throw new ArgumentNullException(nameof(revision));
        }

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        var ordered = new IfcMissionThreadEvaluation[result.Evaluations.Count];
        for (var i = 0; i < result.Evaluations.Count; i++)
        {
            ordered[i] = result.Evaluations[i];
        }

        Array.Sort(ordered, static (a, b) =>
        {
            var c = StringComparer.Ordinal.Compare(a.ContactId, b.ContactId);
            if (c != 0)
            {
                return c;
            }

            c = StringComparer.Ordinal.Compare(a.TargetId, b.TargetId);
            if (c != 0)
            {
                return c;
            }

            c = a.CaseKind.CompareTo(b.CaseKind);
            if (c != 0)
            {
                return c;
            }

            return a.SimTick.CompareTo(b.SimTick);
        });

        var entries = new List<MissionThreadEvidenceRecord>(ordered.Length * 6);
        var failures = new List<string>();
        var scenarios = new List<string>(ordered.Length);
        var passedCount = 0;

        if (ordered.Length == 0)
        {
            failures.Add("IFC mission-thread batch contains zero evaluations.");
        }

        for (var i = 0; i < ordered.Length; i++)
        {
            var eval = ordered[i];
            var seq = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var idPrefix = $"{scenarioId}:{i}:{eval.ContactId}";
            scenarios.Add($"{eval.CaseKind}:{eval.ContactId}");

            if (VerifyCaseInvariants(eval, out var invariantError))
            {
                passedCount++;
            }
            else if (invariantError is not null)
            {
                failures.Add(invariantError);
            }

            var prov = eval.Targetability.Provenance;
            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{idPrefix}:contact",
                Kind: MissionThreadEvidenceKind.Contact,
                SimTick: eval.SimTick,
                SimTime: eval.SimTime,
                SequenceRef: seq,
                AuthoritativeSource: "IfcMissionThreadVerifier:Contact",
                Contact: new ContactEvidenceFact(
                    ContactId: eval.ContactId,
                    TargetId: eval.TargetId,
                    ObserverId: prov?.Source.ObserverId ?? "unknown",
                    LifecycleState: prov?.LastKnown.LifecycleState ?? "Unknown",
                    KillChainPhase: eval.Targetability.SensorToShooter?.IsComplete == true ? "Target" : "Track",
                    Confidence: prov?.Confidence.ToString(),
                    QualityFlags: prov?.QualityState.ToString())));

            var outOfCommsUnknown = eval.CommsState == CommsState.Denied
                || (prov?.OutOfCommsUnknown == true);
            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{idPrefix}:datalink",
                Kind: MissionThreadEvidenceKind.DataLink,
                SimTick: eval.SimTick,
                SimTime: eval.SimTime,
                SequenceRef: seq,
                AuthoritativeSource: "IfcMissionThreadVerifier:DataLink",
                DataLink: new DataLinkEvidenceFact(
                    NetworkOrUnitId: eval.NetworkOrUnitId,
                    CommsState: eval.CommsState.ToString(),
                    PreviousState: null,
                    OutOfCommsUnknown: outOfCommsUnknown,
                    ShareLagTicks: eval.ShareLagTicks,
                    ReasonCode: eval.DegradedAssessment.DisclosureCodes.Count > 0
                        ? string.Join(";", eval.DegradedAssessment.DisclosureCodes)
                        : null)));

            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{idPrefix}:authority",
                Kind: MissionThreadEvidenceKind.Authority,
                SimTick: eval.SimTick,
                SimTime: eval.SimTime,
                SequenceRef: seq,
                AuthoritativeSource: "IfcMissionThreadVerifier:Authority",
                Authority: new AuthorityEvidenceFact(
                    ActorId: eval.ContactId,
                    TargetId: eval.TargetId,
                    RoeLevel: eval.Authority.Roe.RoeLabel,
                    Disposition: eval.Authority.Targeting.Disposition.ToString(),
                    ReasonCode: eval.Authority.Targeting.ReasonCode ?? eval.Targetability.WithheldCauseCode,
                    PolicySnapshotId: null,
                    RequiredApproval: eval.Authority.Targeting.PendingApproval?.ToString())));

            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{idPrefix}:recommendation",
                Kind: MissionThreadEvidenceKind.Recommendation,
                SimTick: eval.SimTick,
                SimTime: eval.SimTime,
                SequenceRef: seq,
                AuthoritativeSource: "IfcMissionThreadVerifier:Recommendation",
                Recommendation: new RecommendationEvidenceFact(
                    AgentId: eval.Recommendation?.ShooterUnitId ?? "unassigned",
                    TargetId: eval.TargetId,
                    ChosenKind: eval.Recommendation?.WeaponFamilyChoice ?? "withhold",
                    PolicySnapshotId: null,
                    PersonalitySlug: null,
                    Outcome: eval.Recommendation?.Outcome.ToString() ?? "None",
                    WithheldReasonCode: eval.HasConflictingAllocation
                        ? ResourceRankReasonCode.ExcludedByCommitment
                        : eval.Recommendation?.WithheldReasonCode)));

            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{idPrefix}:engagement",
                Kind: MissionThreadEvidenceKind.Engagement,
                SimTick: eval.SimTick,
                SimTime: eval.SimTime,
                SequenceRef: seq,
                AuthoritativeSource: "IfcMissionThreadVerifier:Engagement",
                Engagement: new EngagementEvidenceFact(
                    ShooterUnitId: eval.Recommendation?.ShooterUnitId ?? "none",
                    TargetUnitId: eval.TargetId,
                    EngagementId: (ulong)(i + 1),
                    Launched: eval.EngagementLaunched,
                    WeaponFamilyId: eval.Recommendation?.WeaponFamilyChoice,
                    SalvoSize: eval.EngagementLaunched ? 1 : 0,
                    AbortReasonCode: eval.EngagementLaunched ? null : eval.OutcomeReasonCode,
                    OutcomeCode: eval.EngagementOutcomeCode ?? eval.Disposition.ToString())));

            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{idPrefix}:assessment",
                Kind: MissionThreadEvidenceKind.Assessment,
                SimTick: eval.SimTick,
                SimTime: eval.SimTime,
                SequenceRef: seq,
                AuthoritativeSource: "IfcMissionThreadVerifier:Assessment",
                Assessment: new AssessmentEvidenceFact(
                    ContactId: eval.ContactId,
                    TargetId: eval.TargetId,
                    AssessmentState: eval.Disposition.ToString(),
                    DamageLevel: eval.CaseKind.ToString(),
                    PreviousHpPct: null,
                    NewHpPct: null,
                    SourceKind: eval.DegradedAssessment.IsDegraded ? "DegradedModePolicy" : "IfcNominal")));
        }

        var allPassed = failures.Count == 0 && ordered.Length > 0;
        var acceptance = new RevisionBoundAcceptanceRecord(
            CriterionId: "KCX-01",
            RequirementId: "VER-03",
            Revision: revision,
            Inputs: inputs,
            Layer: AcceptanceEvidenceLayer.Headless,
            RequiredLayer: AcceptanceEvidenceLayer.Headless,
            ExecutionCommandOrSteps: "IfcMissionThreadVerifier.Evaluate + BuildEvidenceLedger",
            Result: allPassed ? "PASS" : "FAIL",
            ArtifactLocation: artifactLocation,
            ArtifactSha256: artifactSha256,
            CollectedAtUtc: collectedAtUtc,
            Counts: new FloorVsObservedCounts(
                DeclaredFloor: ordered.Length > 0 ? ordered.Length : 1,
                ObservedPassed: passedCount,
                ObservedFailed: failures.Count,
                ObservedSkipped: 0),
            OwnerAcceptance: new OwnerAcceptanceDecision(
                OwnerAcceptanceStatus.Pending,
                Owner: null,
                DecisionDateUtc: null,
                DecisionReference: null,
                AcceptedRevision: null,
                AcceptedInputs: null),
            Notes: "DRG-193 headless IFC mission-thread verification.");

        var oracle = new OracleRunCorrelation(
            Profile: "ifc-mission-thread",
            Passed: allPassed,
            Scenarios: scenarios,
            Failures: failures,
            Warnings: Array.Empty<string>());

        return EvidenceLedgerBuilder.Build(
            ledgerId: ledgerId,
            scenarioId: scenarioId,
            seed: seed,
            entries: entries,
            oracleEvaluation: oracle,
            acceptanceRecords: new[] { acceptance });
    }

    private static bool HasCommitmentConflict(ResourceRankSnapshot? resourceRank)
    {
        if (resourceRank is null)
        {
            return false;
        }

        var candidates = resourceRank.RankedCandidates;
        var hasEligible = false;
        var hasCommitmentExclusion = false;
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (candidate.Disposition == ResourceRankDisposition.Excluded
                && string.Equals(candidate.ReasonCode, ResourceRankReasonCode.ExcludedByCommitment, StringComparison.Ordinal))
            {
                hasCommitmentExclusion = true;
            }
            else if (candidate.Disposition != ResourceRankDisposition.Excluded)
            {
                hasEligible = true;
            }
        }

        return hasCommitmentExclusion && !hasEligible;
    }

    private static (IfcOutcomeDisposition Disposition, string ReasonCode) ResolveCorrelationOutcome(
        TargetabilityAcceptContactRow acceptRow,
        IfcContactEvaluationInput input,
        bool hasConflict,
        bool isLostNode,
        DegradedModeAssessment degradedAssessment)
    {
        if (isLostNode)
        {
            return (IfcOutcomeDisposition.Blocked, acceptRow.WithheldCauseCode != TargetabilityAcceptCauseCodes.None
                ? acceptRow.WithheldCauseCode
                : TargetabilityAcceptCauseCodes.LostSensor);
        }

        if (acceptRow.Disposition == TargetabilityAcceptDisposition.Withheld)
        {
            var blocked = string.Equals(acceptRow.WithheldCauseCode, TargetabilityAcceptCauseCodes.LostSensor, StringComparison.Ordinal)
                || string.Equals(acceptRow.WithheldCauseCode, TargetabilityAcceptCauseCodes.StaleTrack, StringComparison.Ordinal)
                || string.Equals(acceptRow.WithheldCauseCode, TargetabilityAcceptCauseCodes.Stale, StringComparison.Ordinal)
                || string.Equals(acceptRow.WithheldCauseCode, TargetabilityAcceptCauseCodes.SilentComms, StringComparison.Ordinal)
                || string.Equals(acceptRow.WithheldCauseCode, TargetabilityAcceptCauseCodes.MissingProvenance, StringComparison.Ordinal)
                || string.Equals(acceptRow.WithheldCauseCode, TargetabilityAcceptCauseCodes.CatalogMiss, StringComparison.Ordinal);
            return (blocked ? IfcOutcomeDisposition.Blocked : IfcOutcomeDisposition.Withheld, acceptRow.WithheldCauseCode);
        }

        if (hasConflict)
        {
            return (IfcOutcomeDisposition.Withheld, ResourceRankReasonCode.ExcludedByCommitment);
        }

        if (degradedAssessment.IsDegraded)
        {
            var code = degradedAssessment.DisclosureCodes.Count > 0
                ? degradedAssessment.DisclosureCodes[0]
                : "DEGRADED_MODE";
            var disp = degradedAssessment.Disposition == DegradedModeDisposition.FailClosedWithhold
                ? IfcOutcomeDisposition.Blocked
                : IfcOutcomeDisposition.Withheld;
            return (disp, code);
        }

        if (input.Recommendation is { Outcome: WeaponRecommendationOutcome.WithheldByPolicy or WeaponRecommendationOutcome.WithheldByEngage })
        {
            return (
                IfcOutcomeDisposition.Withheld,
                input.Recommendation.WithheldReasonCode ?? "RECOMMENDATION_WITHHELD");
        }

        if (input.Engagement is { Launched: false })
        {
            return (
                IfcOutcomeDisposition.Blocked,
                input.Engagement.AbortReasonCode ?? "ENGAGEMENT_ABORTED");
        }

        return (IfcOutcomeDisposition.Permitted, "PERMITTED");
    }

    private static bool VerifyCaseInvariants(IfcMissionThreadEvaluation eval, out string? error)
    {
        if (eval.DegradedAssessment.AllowAutonomousOrSilentFallback)
        {
            error = $"{eval.ContactId}: hidden autonomous fallback was permitted.";
            return false;
        }

        if (eval.DegradedAssessment.IsDegraded && !eval.DegradedAssessment.RequiresOperatorDisclosure)
        {
            error = $"{eval.ContactId}: degraded assessment omitted operator disclosure.";
            return false;
        }

        if (eval.CaseKind == IfcCaseKind.Normal)
        {
            if (eval.Disposition != IfcOutcomeDisposition.Permitted)
            {
                error = $"{eval.ContactId}: normal IFC case resolved to '{eval.Disposition}' ({eval.OutcomeReasonCode}) instead of Permitted.";
                return false;
            }

            if (eval.DegradedAssessment.IsDegraded)
            {
                error = $"{eval.ContactId}: normal IFC case unexpectedly flagged degraded.";
                return false;
            }
        }
        else
        {
            if (eval.Disposition == IfcOutcomeDisposition.Permitted)
            {
                error = $"{eval.ContactId}: non-normal IFC case '{eval.CaseKind}' did not fail closed.";
                return false;
            }

            switch (eval.CaseKind)
            {
                case IfcCaseKind.DegradedLink:
                    if (eval.CommsState != CommsState.Degraded
                        && !eval.DegradedAssessment.ActiveConditions.Contains(DegradedModeCondition.NetworkPartition)
                        && !eval.DegradedAssessment.ActiveConditions.Contains(DegradedModeCondition.StaleData))
                    {
                        error = $"{eval.ContactId}: DegradedLink case lacked degraded comms/partition/stale evidence.";
                        return false;
                    }

                    break;

                case IfcCaseKind.LostNode:
                    if (eval.Disposition != IfcOutcomeDisposition.Blocked
                        || (eval.CommsState != CommsState.Denied
                            && eval.Targetability.Provenance?.OutOfCommsUnknown != true
                            && eval.Targetability.SensorToShooter?.PrimaryBreakCause != SensorToShooterBreakCause.LostSensor))
                    {
                        error = $"{eval.ContactId}: LostNode case did not block with lost-sensor / comms-denied evidence.";
                        return false;
                    }

                    break;

                case IfcCaseKind.ConflictingAllocation:
                    if (!eval.HasConflictingAllocation
                        && !eval.DegradedAssessment.ActiveConditions.Contains(DegradedModeCondition.ResourceConflict))
                    {
                        error = $"{eval.ContactId}: ConflictingAllocation case lacked resource-conflict evidence.";
                        return false;
                    }

                    break;

                case IfcCaseKind.AuthorityWithheld:
                    if (eval.Authority.Targeting.Disposition == C2AuthorityDisposition.Permitted
                        && eval.Authority.Roe.TargetingDisposition == C2AuthorityDisposition.Permitted
                        && !eval.DegradedAssessment.ActiveConditions.Contains(DegradedModeCondition.AuthorityWithheld))
                    {
                        error = $"{eval.ContactId}: AuthorityWithheld case lacked authority-withheld evidence.";
                        return false;
                    }

                    break;
            }
        }

        if (eval.Disposition != IfcOutcomeDisposition.Permitted && eval.EngagementLaunched)
        {
            error = $"{eval.ContactId}: engagement launched despite non-permitted disposition '{eval.Disposition}'.";
            return false;
        }

        error = null;
        return true;
    }
}
