namespace ProjectAegis.Delegation.Resilience;

using Data.Evidence;
using Skills;

/// <summary>Structured evidence proving human override invariants held under an injected fault.</summary>
public sealed record PlayerOverrideEvidence(
    bool HumanControlEstablished,
    bool PendingOrdersPreserved,
    bool AgentReassignBlockedWhileHumanOrdersPending,
    bool SuspendedAgentRestoredOnRelease,
    string FaultInjectedSummary)
{
    /// <summary>True only when all four override invariants held and a non-blank fault summary was recorded.</summary>
    public bool IsVerified =>
        HumanControlEstablished
        && PendingOrdersPreserved
        && AgentReassignBlockedWhileHumanOrdersPending
        && SuspendedAgentRestoredOnRelease
        && !string.IsNullOrWhiteSpace(FaultInjectedSummary);
}

/// <summary>Auditable verification result for one AGD-08 degraded-mode resilience scenario.</summary>
public sealed record ResilienceScenarioCheck(
    string ScenarioId,
    DegradedModeCondition Condition,
    DegradedModeContext Context,
    DegradedModeAssessment Assessment,
    C2AuthorityProjection Authority,
    SkillEnvelopeValidation? EnvelopeValidation,
    PlayerOverrideEvidence OverrideEvidence,
    bool PlayerOverridePreserved,
    bool HiddenFallbackBlocked,
    string Notes);

/// <summary>Aggregated DRG-203 verification verdict across all five degraded conditions.</summary>
public sealed record ResilienceVerificationSummary(
    IReadOnlyList<ResilienceScenarioCheck> Scenarios,
    bool AllFailClosed,
    bool AllDisclosedToOperator,
    bool AllPlayerOverridesPreserved,
    bool AllFiveConditionsCovered);

/// <summary>
/// DRG-203: verifies AGD-08 agentic resilience across stale data, network partition,
/// authority withholding, resource conflict, and model/skill unavailability while preserving
/// player override authority and recording typed evidence into <see cref="EvidenceLedger"/>.
/// </summary>
public static class AgenticResilienceVerifier
{
    /// <summary>Evaluates one degraded-mode scenario against <see cref="DegradedModePolicy"/> and authority/envelope/override gates.</summary>
    public static ResilienceScenarioCheck VerifyScenario(
        string scenarioId,
        DegradedModeCondition primaryCondition,
        DegradedModeContext context,
        C2AuthorityProjection authority,
        PlayerOverrideEvidence overrideEvidence,
        SkillEnvelopeValidation? envelopeValidation = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException("ScenarioId is required.", nameof(scenarioId));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (authority is null)
        {
            throw new ArgumentNullException(nameof(authority));
        }

        if (overrideEvidence is null)
        {
            throw new ArgumentNullException(nameof(overrideEvidence));
        }

        var assessment = DegradedModePolicy.Evaluate(context);
        var conditionMatches = assessment.ActiveConditions.Contains(primaryCondition);
        var authorityRespectsPolicy = primaryCondition != DegradedModeCondition.AuthorityWithheld
            || authority.Targeting.Disposition != C2AuthorityDisposition.Permitted;
        var envelopeRespectsGate = envelopeValidation is null || !envelopeValidation.Ok;
        var playerOverridePreserved = overrideEvidence.HumanControlEstablished
            && overrideEvidence.PendingOrdersPreserved
            && overrideEvidence.AgentReassignBlockedWhileHumanOrdersPending
            && overrideEvidence.SuspendedAgentRestoredOnRelease
            && !string.IsNullOrWhiteSpace(overrideEvidence.FaultInjectedSummary);

        var hiddenFallbackBlocked = assessment.IsDegraded
            && conditionMatches
            && !assessment.AllowAutonomousOrSilentFallback
            && assessment.RequiresOperatorDisclosure
            && assessment.DisclosureCodes.Count > 0
            && !string.IsNullOrWhiteSpace(assessment.OperatorDisclosure)
            && authorityRespectsPolicy
            && envelopeRespectsGate;

        return new ResilienceScenarioCheck(
            ScenarioId: scenarioId,
            Condition: primaryCondition,
            Context: context,
            Assessment: assessment,
            Authority: authority,
            EnvelopeValidation: envelopeValidation,
            OverrideEvidence: overrideEvidence,
            PlayerOverridePreserved: playerOverridePreserved,
            HiddenFallbackBlocked: hiddenFallbackBlocked,
            Notes: notes ?? assessment.OperatorDisclosure);
    }

    /// <summary>Summarizes resilience checks across all evaluated degraded conditions.</summary>
    public static ResilienceVerificationSummary Summarize(IReadOnlyList<ResilienceScenarioCheck> checks)
    {
        if (checks is null)
        {
            throw new ArgumentNullException(nameof(checks));
        }

        var covered = new HashSet<DegradedModeCondition>();
        var allFailClosed = checks.Count > 0;
        var allDisclosed = checks.Count > 0;
        var allOverrides = checks.Count > 0;

        for (var i = 0; i < checks.Count; i++)
        {
            var c = checks[i];
            covered.Add(c.Condition);
            if (!c.HiddenFallbackBlocked || c.Assessment.Disposition == DegradedModeDisposition.Normal)
            {
                allFailClosed = false;
            }

            if (!c.Assessment.RequiresOperatorDisclosure || c.Assessment.DisclosureCodes.Count == 0)
            {
                allDisclosed = false;
            }

            if (!c.PlayerOverridePreserved)
            {
                allOverrides = false;
            }
        }

        var allFive = covered.Contains(DegradedModeCondition.ModelOrSkillUnavailable)
            && covered.Contains(DegradedModeCondition.StaleData)
            && covered.Contains(DegradedModeCondition.NetworkPartition)
            && covered.Contains(DegradedModeCondition.AuthorityWithheld)
            && covered.Contains(DegradedModeCondition.ResourceConflict);

        return new ResilienceVerificationSummary(
            Scenarios: checks,
            AllFailClosed: allFailClosed,
            AllDisclosedToOperator: allDisclosed,
            AllPlayerOverridesPreserved: allOverrides,
            AllFiveConditionsCovered: allFive);
    }

    /// <summary>Builds a deterministic, SHA-256 digested <see cref="EvidenceLedger"/> for resilience checks.</summary>
    public static EvidenceLedger BuildEvidenceLedger(
        string ledgerId,
        string scenarioId,
        int seed,
        ResilienceVerificationSummary summary,
        RevisionIdentity revision,
        InputDigestManifest inputs,
        string artifactLocation,
        string artifactSha256,
        string collectedAtUtc)
    {
        if (summary is null)
        {
            throw new ArgumentNullException(nameof(summary));
        }

        if (revision is null)
        {
            throw new ArgumentNullException(nameof(revision));
        }

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        var entries = new List<MissionThreadEvidenceRecord>(summary.Scenarios.Count * 3);
        var scenarios = new List<string>(summary.Scenarios.Count);
        var failures = new List<string>();
        var passedCount = 0;

        for (var i = 0; i < summary.Scenarios.Count; i++)
        {
            var check = summary.Scenarios[i];
            var seq = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var idPrefix = $"{scenarioId}:{i}:{check.ScenarioId}";
            scenarios.Add(check.ScenarioId);

            var checkPassed = check.HiddenFallbackBlocked && check.PlayerOverridePreserved;
            if (checkPassed)
            {
                passedCount++;
            }
            else
            {
                failures.Add($"{check.ScenarioId}: failed closed={check.HiddenFallbackBlocked}, override={check.PlayerOverridePreserved}");
            }

            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{idPrefix}:authority",
                Kind: MissionThreadEvidenceKind.Authority,
                SimTick: (ulong)(i + 1),
                SimTime: i + 1.0,
                SequenceRef: seq,
                AuthoritativeSource: "AgenticResilienceVerifier:Authority",
                Authority: new AuthorityEvidenceFact(
                    ActorId: check.ScenarioId,
                    TargetId: null,
                    RoeLevel: check.Authority.Roe.RoeLabel,
                    Disposition: check.Authority.Targeting.Disposition.ToString(),
                    ReasonCode: check.Authority.Targeting.ReasonCode ?? string.Join(";", check.Assessment.DisclosureCodes),
                    PolicySnapshotId: null,
                    RequiredApproval: check.Authority.Targeting.PendingApproval?.ToString())));

            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{idPrefix}:datalink",
                Kind: MissionThreadEvidenceKind.DataLink,
                SimTick: (ulong)(i + 1),
                SimTime: i + 1.0,
                SequenceRef: seq,
                AuthoritativeSource: "AgenticResilienceVerifier:DataLink",
                DataLink: new DataLinkEvidenceFact(
                    NetworkOrUnitId: check.ScenarioId,
                    CommsState: check.Condition == DegradedModeCondition.NetworkPartition ? "Partitioned" : "DegradedCheck",
                    PreviousState: null,
                    OutOfCommsUnknown: check.Condition == DegradedModeCondition.NetworkPartition,
                    ShareLagTicks: null,
                    ReasonCode: string.Join(";", check.Assessment.DisclosureCodes))));

            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{idPrefix}:assessment",
                Kind: MissionThreadEvidenceKind.Assessment,
                SimTick: (ulong)(i + 1),
                SimTime: i + 1.0,
                SequenceRef: seq,
                AuthoritativeSource: "AgenticResilienceVerifier:Assessment",
                Assessment: new AssessmentEvidenceFact(
                    ContactId: check.ScenarioId,
                    TargetId: check.Condition.ToString(),
                    AssessmentState: check.Assessment.Disposition.ToString(),
                    DamageLevel: check.PlayerOverridePreserved ? "OverridePreserved" : "OverrideViolated",
                    PreviousHpPct: null,
                    NewHpPct: null,
                    SourceKind: "DegradedModePolicy")));
        }

        if (!summary.AllFiveConditionsCovered)
        {
            failures.Add("Not all 5 AGD-08 degraded conditions were covered.");
        }

        var passed = failures.Count == 0
            && summary.AllFailClosed
            && summary.AllDisclosedToOperator
            && summary.AllPlayerOverridesPreserved
            && summary.AllFiveConditionsCovered;

        var acceptance = new RevisionBoundAcceptanceRecord(
            CriterionId: "AGD-08",
            RequirementId: "DRG-203",
            Revision: revision,
            Inputs: inputs,
            Layer: AcceptanceEvidenceLayer.Headless,
            RequiredLayer: AcceptanceEvidenceLayer.Headless,
            ExecutionCommandOrSteps: "AgenticResilienceVerifier.VerifyScenario + Summarize",
            Result: passed ? "PASS" : "FAIL",
            ArtifactLocation: artifactLocation,
            ArtifactSha256: artifactSha256,
            CollectedAtUtc: collectedAtUtc,
            Counts: new FloorVsObservedCounts(
                DeclaredFloor: 5,
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
            Notes: "DRG-203 agentic resilience & player-override verification.");

        var oracle = new OracleRunCorrelation(
            Profile: "agentic-resilience",
            Passed: passed,
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
}
