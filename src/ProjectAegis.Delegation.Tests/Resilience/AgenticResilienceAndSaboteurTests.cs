using System.Text.Json;
using NUnit.Framework;
using ProjectAegis.Data.Evidence;
using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Resilience;
using ProjectAegis.Delegation.ResourceRank;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.Tests.Resilience;

/// <summary>
/// DRG-203 (VER-05): Deterministic SeededRng property tests, 5-condition degraded-mode
/// resilience verification preserving player override, and saboteur calibration ledger correlation.
/// </summary>
[TestFixture]
public sealed class AgenticResilienceAndSaboteurTests
{
    [Test]
    public void Property_SeededRng_decision_pipeline_is_bit_identical_and_roe_rejection_is_never_bypassed_across_50_seeds()
    {
        var candidates = new[]
        {
            new ScoredIntent(OrderKind.Hold, 0.4, RiskLevel.Low),
            new ScoredIntent(OrderKind.Move, 0.7, RiskLevel.Low),
            new ScoredIntent(OrderKind.Engage, 0.95, RiskLevel.High),
        };
        var postKillCandidates = new[]
        {
            new ScoredIntent(OrderKind.Hold, 0.3, RiskLevel.Low),
            new ScoredIntent(OrderKind.Engage, 0.0, RiskLevel.High),
        };

        for (var i = 0; i < 50; i++)
        {
            var globalSeed = 1000 + (i * 37);
            var agentSalt = 17 + (i * 13);

            var rngA = new SeededRng(globalSeed, agentSalt);
            var rngB = new SeededRng(globalSeed, agentSalt);

            var traits = new TraitVector(
                Aggression: Math.Clamp((i % 10) * 0.1, 0.05, 1.0),
                RiskTolerance: 0.95,
                ReactionDelay: i % 3,
                ErrorRate: 0.0,
                SituationalAwareness: 1.0,
                Decisiveness: 0.25 + ((i % 7) * 0.1));
            var attention = new AttentionEvaluation(
                Budget: 20.0,
                Load: i % 2 == 0 ? 10.0 : 28.0,
                Degradation: new AttentionDegradation(
                    SlowerReactions: i % 2 != 0,
                    NarrowedFocus: i % 4 == 3,
                    SimplerDecisions: false));

            var choiceA = DecisionPipeline.Choose(candidates, traits, attention, rngA);
            var choiceB = DecisionPipeline.Choose(candidates, traits, attention, rngB);

            Assert.That(choiceA.Chosen.Kind, Is.EqualTo(choiceB.Chosen.Kind));
            Assert.That(choiceA.RngDraw, Is.EqualTo(choiceB.RngDraw));

            // Zero-scored post-kill Engage must never be selected when positive alternative exists.
            var postKillChoice = DecisionPipeline.Choose(postKillCandidates, traits, attention, new SeededRng(globalSeed, agentSalt + 1));
            Assert.That(postKillChoice.Chosen.Kind, Is.EqualTo(OrderKind.Hold));

            // ROE HoldFire rejection must never be bypassed by Manual autonomy or playerApproved=true.
            var gate = new AutonomyGate(new FixedRejectRoeFilter(FireAbortReason.RoeHoldFire));
            var engageOrder = new Order(new OrderId(1000L + i), new TargetId("u1"), i + 1.0, OrderKind.Engage, RiskLevel.High);
            foreach (var autonomy in new[] { AutonomyLevel.Manual, AutonomyLevel.Assisted, AutonomyLevel.SemiAutonomous, AutonomyLevel.FullAutonomous })
            {
                var verdict = gate.Evaluate(autonomy, engageOrder, playerApproved: true);
                Assert.That(verdict.Rejected, Is.True);
                Assert.That(verdict.ExecuteNow, Is.False);
                Assert.That(verdict.QueueForApproval, Is.False);
                Assert.That(verdict.PolicyDenialReason, Is.EqualTo(FireAbortReason.RoeHoldFire));
            }
        }
    }

    [Test]
    public void Resilience_all_five_degraded_conditions_fail_closed_with_disclosure_and_preserve_player_override()
    {
        var checks = new List<ResilienceScenarioCheck>();

        // Condition 1: StaleData / expired proposal
        var staleContext = new DegradedModeContext(StaleData: true, Detail: "expired-track-35-ticks");
        var staleEnvelope = ProposeEnvelope() with { SimTick = 60, CreatedSimTick = 10, TtlTicks = 20 };
        var staleValidation = SkillEnvelopeValidator.Validate(staleEnvelope, proposalApproved: false, currentSimTick: 60);
        Assert.That(staleValidation.Ok, Is.False);
        Assert.That(staleValidation.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonProposalExpired));
        var staleOverride = VerifyPlayerOverridePreservationUnderFault(DegradedModeCondition.StaleData);
        checks.Add(AgenticResilienceVerifier.VerifyScenario(
            "res-stale-data",
            DegradedModeCondition.StaleData,
            staleContext,
            C2AuthorityProjector.Project(OrganicAuthority(RoeLevel.WeaponsFree)),
            overrideEvidence: staleOverride,
            envelopeValidation: staleValidation));

        // Condition 2: NetworkPartition / lost node
        var partitionContext = new DegradedModeContext(NetworkPartition: true, Detail: "comms-partition");
        var partitionOverride = VerifyPlayerOverridePreservationUnderFault(DegradedModeCondition.NetworkPartition);
        checks.Add(AgenticResilienceVerifier.VerifyScenario(
            "res-network-partition",
            DegradedModeCondition.NetworkPartition,
            partitionContext,
            C2AuthorityProjector.Project(OrganicAuthority(RoeLevel.WeaponsFree)),
            overrideEvidence: partitionOverride,
            envelopeValidation: null));

        // Condition 3: AuthorityWithheld / PolicyUnavailable
        var authContext = new DegradedModeContext(AuthorityWithheld: true, Detail: "policy-unavailable");
        var withheldAuthority = C2AuthorityProjector.Project(new C2AuthorityProjectionContext(
            RoeLevel.WeaponsFree,
            SkillLane.Propose,
            RequiredApproval.WeaponsRelease,
            TrackSource.Organic,
            FireControlSatisfied: true,
            PolicyUnavailable: true));
        Assert.That(withheldAuthority.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
        Assert.That(withheldAuthority.Targeting.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonPolicyUnavailable));
        var authOverride = VerifyPlayerOverridePreservationUnderFault(DegradedModeCondition.AuthorityWithheld);
        checks.Add(AgenticResilienceVerifier.VerifyScenario(
            "res-authority-withheld",
            DegradedModeCondition.AuthorityWithheld,
            authContext,
            withheldAuthority,
            overrideEvidence: authOverride,
            envelopeValidation: null));

        // Condition 4: ResourceConflict
        var resourceContext = new DegradedModeContext(ResourceConflict: true, Detail: "rounds-committed-elsewhere");
        var resourceOverride = VerifyPlayerOverridePreservationUnderFault(DegradedModeCondition.ResourceConflict);
        checks.Add(AgenticResilienceVerifier.VerifyScenario(
            "res-resource-conflict",
            DegradedModeCondition.ResourceConflict,
            resourceContext,
            C2AuthorityProjector.Project(OrganicAuthority(RoeLevel.WeaponsFree)),
            overrideEvidence: resourceOverride,
            envelopeValidation: null));

        // Condition 5: ModelOrSkillUnavailable
        var modelContext = new DegradedModeContext(ModelOrSkillUnavailable: true, Detail: "advisory-model-offline");
        var unknownSkillEnvelope = ProposeEnvelope() with { SkillId = "c2.nonexistent.skill" };
        var unknownValidation = SkillEnvelopeValidator.Validate(unknownSkillEnvelope);
        Assert.That(unknownValidation.Ok, Is.False);
        Assert.That(unknownValidation.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonUnknownSkill));
        var modelOverride = VerifyPlayerOverridePreservationUnderFault(DegradedModeCondition.ModelOrSkillUnavailable);
        checks.Add(AgenticResilienceVerifier.VerifyScenario(
            "res-model-unavailable",
            DegradedModeCondition.ModelOrSkillUnavailable,
            modelContext,
            C2AuthorityProjector.Project(OrganicAuthority(RoeLevel.WeaponsFree)),
            overrideEvidence: modelOverride,
            envelopeValidation: unknownValidation));

        foreach (var check in checks)
        {
            Assert.That(check.Context, Is.Not.Null);
            Assert.That(check.Notes, Is.Not.Null);
            Assert.That(check.Assessment.IsDegraded, Is.True);
            Assert.That(check.Assessment.AllowAutonomousOrSilentFallback, Is.False);
            Assert.That(check.Assessment.RequiresOperatorDisclosure, Is.True);
            Assert.That(check.Assessment.DisclosureCodes, Is.Not.Empty);
            Assert.That(check.Assessment.OperatorDisclosure, Is.Not.Null.And.Not.Empty);
            Assert.That(check.HiddenFallbackBlocked, Is.True);
            Assert.That(check.PlayerOverridePreserved, Is.True);
            Assert.That(check.OverrideEvidence.IsVerified, Is.True);
            Assert.That(check.OverrideEvidence.FaultInjectedSummary, Is.Not.Null.And.Not.WhiteSpace);
        }

        Assert.That(checks[0].EnvelopeValidation, Is.Not.Null);
        var summary = AgenticResilienceVerifier.Summarize(checks);
        Assert.That(summary.AllFailClosed, Is.True);
        Assert.That(summary.AllDisclosedToOperator, Is.True);
        Assert.That(summary.AllPlayerOverridesPreserved, Is.True);
        Assert.That(summary.AllFiveConditionsCovered, Is.True);

        var ledger = AgenticResilienceVerifier.BuildEvidenceLedger(
            ledgerId: "resilience-m2-5cond",
            scenarioId: "agentic-resilience-5-conditions",
            seed: 42,
            summary: summary,
            revision: TestRevision(),
            inputs: TestInputs("agentic-resilience-5-conditions", 42),
            artifactLocation: "src/ProjectAegis.Delegation.Tests/Resilience/AgenticResilienceAndSaboteurTests.cs",
            artifactSha256: new string('c', 64),
            collectedAtUtc: "2026-11-23T16:05:00Z");

        Assert.That(ledger.OracleEvaluation!.Passed, Is.True);
        Assert.That(ledger.Entries, Is.Not.Empty);
        Assert.That(ledger.Entries.All(e => e.HasValidPayload()), Is.True);
        Assert.That(ledger.ContentDigestSha256, Has.Length.EqualTo(64));

        var acceptance = ledger.AcceptanceRecords.Single();
        Assert.That(acceptance.CriterionId, Is.EqualTo("AGD-08"));
        Assert.That(acceptance.RequirementId, Is.EqualTo("DRG-203"));
        Assert.That(acceptance.Layer, Is.EqualTo(AcceptanceEvidenceLayer.Headless));
        Assert.That(acceptance.OwnerAcceptance.Status, Is.EqualTo(OwnerAcceptanceStatus.Pending));
        var validation = AcceptanceEvidenceValidator.Validate(
            acceptance,
            currentRevision: TestRevision(),
            currentInputs: TestInputs("agentic-resilience-5-conditions", 42));
        Assert.That(validation.IsSchemaValid, Is.True, string.Join("; ", validation.Errors));
        Assert.That(validation.RequiresReassessment, Is.False);
    }

    [Test]
    public void Saboteur_sensitivity_calibration_report_correlates_to_evidence_ledger_and_flags_defect_survivors()
    {
        const string cleanReportJson = """
        {
          "summary": {
            "caught": 2,
            "survived": 1,
            "invalid": 0,
            "caughtDefects": 2,
            "survivedDefects": 0,
            "killRate": "2/2"
          },
          "results": [
            { "id": "00-noop-comment", "role": "control", "expectedOracles": [], "firedOracles": [], "outcome": "survived", "mode": "classic" },
            { "id": "01-pd-weakened", "role": "defect", "expectedOracles": ["replay_golden"], "firedOracles": ["replay_golden"], "outcome": "caught", "mode": "classic" },
            { "id": "04-rng-seed-ignored", "role": "defect", "expectedOracles": ["replay_golden"], "firedOracles": ["replay_golden"], "outcome": "caught", "mode": "classic" }
          ]
        }
        """;

        var cleanLedger = SaboteurCalibrationLedgerRecorder.Record(
            ledgerId: "saboteur-clean",
            scenarioId: "saboteur-curated-clean",
            seed: 42,
            reportJson: cleanReportJson,
            revision: TestRevision(),
            inputs: TestInputs("saboteur-curated-clean", 42),
            artifactLocation: "tools/qa-gauntlet/saboteur.py",
            artifactSha256: new string('d', 64),
            collectedAtUtc: "2026-11-23T16:06:00Z");

        Assert.That(cleanLedger.OracleEvaluation!.Profile, Is.EqualTo("saboteur-calibration"));
        Assert.That(cleanLedger.OracleEvaluation.Passed, Is.True);
        Assert.That(cleanLedger.DefectFollowUps, Is.Empty);
        Assert.That(cleanLedger.AcceptanceRecords.Single().CriterionId, Is.EqualTo("VER-05"));
        Assert.That(cleanLedger.AcceptanceRecords.Single().OwnerAcceptance.Status, Is.EqualTo(OwnerAcceptanceStatus.Pending));

        const string blindSpotReportJson = """
        {
          "summary": {
            "caught": 1,
            "survived": 1,
            "invalid": 0,
            "caughtDefects": 1,
            "survivedDefects": 1,
            "killRate": "1/2"
          },
          "results": [
            { "id": "01-pd-weakened", "role": "defect", "expectedOracles": ["replay_golden"], "firedOracles": ["replay_golden"], "outcome": "caught", "mode": "classic" },
            { "id": "09-missing-guard", "role": "defect", "expectedOracles": ["replay_golden"], "firedOracles": [], "outcome": "survived", "mode": "classic" }
          ]
        }
        """;

        var blindSpotLedger = SaboteurCalibrationLedgerRecorder.Record(
            ledgerId: "saboteur-blindspot",
            scenarioId: "saboteur-curated-blindspot",
            seed: 42,
            reportJson: blindSpotReportJson,
            revision: TestRevision(),
            inputs: TestInputs("saboteur-curated-blindspot", 42),
            artifactLocation: "tools/qa-gauntlet/saboteur.py",
            artifactSha256: new string('e', 64),
            collectedAtUtc: "2026-11-23T16:06:00Z");

        Assert.That(blindSpotLedger.OracleEvaluation!.Passed, Is.False);
        Assert.That(blindSpotLedger.OracleEvaluation.Failures, Is.Not.Empty);
        Assert.That(blindSpotLedger.DefectFollowUps, Has.Count.EqualTo(1));
        Assert.That(blindSpotLedger.DefectFollowUps[0].IssueId, Does.Contain("09-missing-guard"));

        const string falsePositiveReportJson = """
        {
          "summary": {
            "caught": 1,
            "survived": 0,
            "invalid": 1,
            "caughtDefects": 0,
            "survivedDefects": 0,
            "killRate": "0/0"
          },
          "results": [
            { "id": "00-noop-comment", "role": "control", "expectedOracles": [], "firedOracles": ["replay_golden"], "outcome": "caught", "mode": "classic" },
            { "id": "99-broken-patch", "role": "defect", "expectedOracles": ["swarm_unit"], "firedOracles": [], "outcome": "invalid-mutant", "mode": "swarm_unit" }
          ]
        }
        """;

        var falsePosLedger = SaboteurCalibrationLedgerRecorder.Record(
            ledgerId: "saboteur-invalid",
            scenarioId: "saboteur-curated-invalid",
            seed: 42,
            reportJson: falsePositiveReportJson,
            revision: TestRevision(),
            inputs: TestInputs("saboteur-curated-invalid", 42),
            artifactLocation: "tools/qa-gauntlet/saboteur.py",
            artifactSha256: new string('f', 64),
            collectedAtUtc: "2026-11-23T16:06:00Z");

        Assert.That(falsePosLedger.OracleEvaluation!.Passed, Is.False);
        Assert.That(falsePosLedger.OracleEvaluation.Failures, Has.Count.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void VerifyScenario_fails_closed_when_player_override_evidence_is_incomplete_or_blank_fault_summary()
    {
        var context = new DegradedModeContext(StaleData: true, Detail: "stale-track");
        var authority = C2AuthorityProjector.Project(OrganicAuthority(RoeLevel.WeaponsFree));

        var invalidEvidences = new[]
        {
            new PlayerOverrideEvidence(false, true, true, true, "stale track injected"),
            new PlayerOverrideEvidence(true, false, true, true, "stale track injected"),
            new PlayerOverrideEvidence(true, true, false, true, "stale track injected"),
            new PlayerOverrideEvidence(true, true, true, false, "stale track injected"),
            new PlayerOverrideEvidence(true, true, true, true, "   "),
        };

        foreach (var evidence in invalidEvidences)
        {
            var check = AgenticResilienceVerifier.VerifyScenario(
                "res-override-fail",
                DegradedModeCondition.StaleData,
                context,
                authority,
                overrideEvidence: evidence);

            Assert.That(check.PlayerOverridePreserved, Is.False);
            var summary = AgenticResilienceVerifier.Summarize(new[] { check });
            Assert.That(summary.AllPlayerOverridesPreserved, Is.False);
        }
    }

    [Test]
    public void Saboteur_parser_and_evaluator_fail_closed_on_missing_summary_fields_wrong_types_and_kill_rate_mismatch()
    {
        const string missingCaughtJson = """
        {
          "summary": { "survived": 1, "invalid": 0, "caughtDefects": 0, "survivedDefects": 0, "killRate": "0/0" },
          "results": [ { "id": "00-noop", "role": "control", "expectedOracles": [], "firedOracles": [], "outcome": "survived" } ]
        }
        """;
        Assert.Throws<JsonException>(() => SaboteurCalibrationLedgerRecorder.ParseReportJson(missingCaughtJson));

        const string wrongTypeSurvivedJson = """
        {
          "summary": { "caught": 0, "survived": "1", "invalid": 0, "caughtDefects": 0, "survivedDefects": 0, "killRate": "0/0" },
          "results": [ { "id": "00-noop", "role": "control", "expectedOracles": [], "firedOracles": [], "outcome": "survived" } ]
        }
        """;
        Assert.Throws<JsonException>(() => SaboteurCalibrationLedgerRecorder.ParseReportJson(wrongTypeSurvivedJson));

        const string missingKillRateJson = """
        {
          "summary": { "caught": 0, "survived": 1, "invalid": 0, "caughtDefects": 0, "survivedDefects": 0 },
          "results": [ { "id": "00-noop", "role": "control", "expectedOracles": [], "firedOracles": [], "outcome": "survived" } ]
        }
        """;
        Assert.Throws<JsonException>(() => SaboteurCalibrationLedgerRecorder.ParseReportJson(missingKillRateJson));

        const string mismatchedKillRateJson = """
        {
          "summary": {
            "caught": 2,
            "survived": 0,
            "invalid": 0,
            "caughtDefects": 2,
            "survivedDefects": 0,
            "killRate": "0/2"
          },
          "results": [
            { "id": "01-pd-weakened", "role": "defect", "expectedOracles": ["replay_golden"], "firedOracles": ["replay_golden"], "outcome": "caught", "mode": "classic" },
            { "id": "04-rng-seed-ignored", "role": "defect", "expectedOracles": ["replay_golden"], "firedOracles": ["replay_golden"], "outcome": "caught", "mode": "classic" }
          ]
        }
        """;
        var parsed = SaboteurCalibrationLedgerRecorder.ParseReportJson(mismatchedKillRateJson);
        var eval = SaboteurCalibrationLedgerRecorder.Evaluate(parsed);
        Assert.That(eval.Report, Is.SameAs(parsed));
        Assert.That(eval.Passed, Is.False);
        Assert.That(eval.Failures, Has.Some.Contains("killRate"));
    }

    private static PlayerOverrideEvidence VerifyPlayerOverridePreservationUnderFault(DegradedModeCondition condition)
    {
        IPolicyEvaluator? policyEvaluator = condition == DegradedModeCondition.AuthorityWithheld
            ? new FixedDenyPolicyEvaluator(FireAbortReason.RoeHoldFire)
            : null;
        var orchestrator = new DelegationOrchestrator(42, policyEvaluator);
        var group = new GroupTarget(new TargetId("grp-1"));
        var unit = new UnitTarget(new TargetId("u-1"));
        group.AddMember(unit.Id);
        orchestrator.Register(group);
        orchestrator.Register(unit);

        string faultSummary;
        switch (condition)
        {
            case DegradedModeCondition.StaleData:
                {
                    orchestrator.DecisionLog.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u-1", "c-stale", "hostile-1", "Unknown", "Identified"));
                    var provenance = ContactProvenanceProjection.Project(orchestrator.DecisionLog, currentSimTick: 60, catalog: null, staleThresholdTicks: 20);
                    Assert.That(provenance.Contacts.Single().Freshness, Is.EqualTo(ContactProvenanceFreshness.Stale));
                    var expired = ProposeEnvelope() with { SimTick = 60, CreatedSimTick = 10, TtlTicks = 20 };
                    var val = SkillEnvelopeValidator.Validate(expired, proposalApproved: false, currentSimTick: 60);
                    Assert.That(val.Ok, Is.False);
                    Assert.That(val.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonProposalExpired));
                    faultSummary = "Injected stale track (tick 1 vs 60) and expired SkillEnvelope (PROPOSAL_EXPIRED) into orchestrator DecisionLog.";
                    break;
                }
            case DegradedModeCondition.NetworkPartition:
                {
                    var offGridUnit = new UnitTarget(new TargetId("u-offgrid"));
                    orchestrator.Register(offGridUnit);
                    orchestrator.CommsGrid = new CommsGridRegistry(new[]
                    {
                    new ScenarioCommsGridTransition(10, "u-1", "OffGrid", "partition"),
                    new ScenarioCommsGridTransition(10, "u-offgrid", "OffGrid", "partition"),
                    new ScenarioCommsGridTransition(20, "u-1", "OnGrid", "rejoin"),
                });

                    var partAgent = orchestrator.CreateAgentFromPreset(new AgentId("agent-part"), PersonalityCatalog.All[0], AutonomyLevel.SemiAutonomous);
                    orchestrator.AssignAgentToTarget(partAgent, unit, EffectivePolicy.DefaultFree, isFriendly: true);

                    var partTaken = orchestrator.TryTakeDirectControl(unit, simTime: 1.0);
                    var partDetached = unit.IsDetachedFromGroup && group.PendingReplan && ReferenceEquals(unit.Slot.SuspendedAgent, partAgent);
                    var partHuman = (HumanController)unit.Slot.Active!;
                    partHuman.Enqueue(new Order(new OrderId(3002L), unit.Id, 2.0, OrderKind.Hold, RiskLevel.Low), executeSimTick: 22);
                    var partRedundant = orchestrator.TryTakeDirectControl(unit, simTime: 2.0) && partHuman.PendingOrderCount == 1;

                    // Advance CommsGrid to tick 10 partition: off-grid direct control must fail closed while queued on-grid order remains intact.
                    Assert.That(orchestrator.EvaluateOffGrid("u-offgrid", 10), Is.EqualTo(FireAbortReason.OffGrid));
                    Assert.That(orchestrator.TryTakeDirectControl(offGridUnit, simTime: 10.0), Is.False);
                    Assert.That(orchestrator.EvaluateOffGrid("u-1", 10), Is.EqualTo(FireAbortReason.OffGrid));
                    var partPreservedDuringOutage = partHuman.PendingOrderCount == 1;

                    var partReplacement = orchestrator.CreateAgentFromPreset(new AgentId("agent-2"), PersonalityCatalog.All[1], AutonomyLevel.Assisted);
                    var partReassignBlocked = !orchestrator.TryAssignAgentController(unit, partReplacement, isFriendly: true, simTime: 10.0).Allowed;

                    orchestrator.BeginExecution(2.0, 2);
                    orchestrator.Tick(new ObservedState(22.0, 1, 0, new Dictionary<TargetId, bool> { [unit.Id] = true }));
                    var partExecuted = orchestrator.ExecutedOrders.Any(o => o.Id == new OrderId(3002L));
                    var partReleased = orchestrator.TryReleaseDirectControl(unit, simTime: 23.0) && ReferenceEquals(unit.Slot.Active, partAgent) && !unit.IsDetachedFromGroup;

                    return new PlayerOverrideEvidence(
                        HumanControlEstablished: partTaken && partDetached,
                        PendingOrdersPreserved: partRedundant && partPreservedDuringOutage && partExecuted,
                        AgentReassignBlockedWhileHumanOrdersPending: partReassignBlocked,
                        SuspendedAgentRestoredOnRelease: partReleased,
                        FaultInjectedSummary: "Injected CommsGridRegistry OffGrid partition at tick 10 (TryTakeDirectControl blocked off-grid with OffGrid) while preserving queued human order through tick 20 OnGrid rejoin.");
                }
            case DegradedModeCondition.AuthorityWithheld:
                {
                    var aggressiveAgent = orchestrator.CreateAgent(
                        new AgentId("agent-withheld"),
                        PersonalityCatalog.All[0].Traits,
                        AutonomyLevel.FullAutonomous,
                        policy: new EngageOnlyPolicy());
                    var holdPolicy = new EffectivePolicy(
                        RoeLevel.HoldFire,
                        AutoEngageAuthorized: false,
                        ExpendAuthorized: false);
                    orchestrator.AssignAgentToTarget(aggressiveAgent, unit, holdPolicy, isFriendly: true);
                    orchestrator.BeginExecution(1.0, 1);
                    orchestrator.Tick(new ObservedState(
                        SimTime: 1.0,
                        ContactCount: 1,
                        ActiveEngagementCount: 0,
                        MemberAlive: new Dictionary<TargetId, bool> { [unit.Id] = true },
                        PrimaryHostileContactId: new TargetId("hostile-1")));
                    Assert.That(orchestrator.ExecutedOrders, Is.Empty);
                    Assert.That(orchestrator.DecisionLog.PolicyDenials.Any(d => d.Reason == FireAbortReason.RoeHoldFire), Is.True);

                    var holdTaken = orchestrator.TryTakeDirectControl(unit, simTime: 2.0);
                    var holdDetached = unit.IsDetachedFromGroup && group.PendingReplan && ReferenceEquals(unit.Slot.SuspendedAgent, aggressiveAgent);
                    var holdHuman = (HumanController)unit.Slot.Active!;
                    holdHuman.Enqueue(new Order(new OrderId(3001L), unit.Id, 2.0, OrderKind.Hold, RiskLevel.Low), executeSimTick: 5);
                    var holdRedundant = orchestrator.TryTakeDirectControl(unit, simTime: 2.5) && ((HumanController)unit.Slot.Active!).PendingOrderCount == 1;
                    var holdReplacement = orchestrator.CreateAgentFromPreset(new AgentId("agent-2"), PersonalityCatalog.All[1], AutonomyLevel.Assisted);
                    var holdReassignBlocked = !orchestrator.TryAssignAgentController(unit, holdReplacement, isFriendly: true, simTime: 3.0).Allowed;
                    orchestrator.Tick(new ObservedState(5.0, 1, 0, new Dictionary<TargetId, bool> { [unit.Id] = true }));
                    var holdExecuted = orchestrator.ExecutedOrders.Any(o => o.Id == new OrderId(3001L));
                    var holdReleased = orchestrator.TryReleaseDirectControl(unit, simTime: 6.0) && ReferenceEquals(unit.Slot.Active, aggressiveAgent) && !unit.IsDetachedFromGroup;

                    return new PlayerOverrideEvidence(
                        HumanControlEstablished: holdTaken && holdDetached,
                        PendingOrdersPreserved: holdRedundant && holdExecuted,
                        AgentReassignBlockedWhileHumanOrdersPending: holdReassignBlocked,
                        SuspendedAgentRestoredOnRelease: holdReleased,
                        FaultInjectedSummary: "Injected HoldFire IPolicyEvaluator (autonomous Engage denied with RoeHoldFire) while human Hold override executed and restored agent.");
                }
            case DegradedModeCondition.ResourceConflict:
                {
                    var competingUnit = new UnitTarget(new TargetId("u-2"));
                    orchestrator.Register(competingUnit);
                    var engageCtx = new EngageContext(
                        RangeMeters: 50_000,
                        Envelope: new WeaponEnvelope(1_000, 100_000),
                        RoundsRemaining: 2,
                        HasFireControlTrack: true,
                        RadarEmconActive: true,
                        MountOnline: true,
                        PkBase: 0.85,
                        SalvoSize: 1,
                        DlzPersonality: DlzPersonality.Normal);
                    var rank = ResourceRankProjection.Project(new[]
                    {
                    new ResourceRankCandidateInput(
                        "c-res",
                        "hostile-1",
                        unit.Id.Value,
                        "sm-2",
                        "SM-2",
                        in engageCtx,
                        EffectivePolicy.DefaultFree,
                        new ResourceRankAvailabilityFacts(RoundsCommittedElsewhere: 2)),
                });
                    Assert.That(rank.RankedCandidates[0].Disposition, Is.EqualTo(ResourceRankDisposition.Excluded));
                    Assert.That(rank.RankedCandidates[0].ReasonCode, Is.EqualTo(ResourceRankReasonCode.ExcludedByCommitment));
                    faultSummary = "Injected ResourceRank commitment conflict (2 rounds committed elsewhere to u-2 -> ExcludedByCommitment) alongside competing unit.";
                    break;
                }
            case DegradedModeCondition.ModelOrSkillUnavailable:
                {
                    var unknownEnvelope = ProposeEnvelope() with { SkillId = "c2.nonexistent.skill" };
                    var val = SkillEnvelopeValidator.Validate(unknownEnvelope);
                    Assert.That(val.Ok, Is.False);
                    Assert.That(val.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonUnknownSkill));
                    var unavailableAgent = orchestrator.CreateAgent(
                        new AgentId("agent-unavailable"),
                        PersonalityCatalog.All[0].Traits,
                        AutonomyLevel.SemiAutonomous,
                        policy: new HoldOnlyFallbackPolicy());
                    orchestrator.AssignAgentToTarget(unavailableAgent, unit, EffectivePolicy.DefaultFree, isFriendly: true);
                    Assert.That(orchestrator.TryPauseAgentActions(unit, simTime: 0.5), Is.True);
                    orchestrator.BeginExecution(1.0, 1);
                    orchestrator.Tick(new ObservedState(1.0, 1, 0, new Dictionary<TargetId, bool> { [unit.Id] = true }));
                    Assert.That(orchestrator.ExecutedOrders, Is.Empty);

                    var modTaken = orchestrator.TryTakeDirectControl(unit, simTime: 2.0);
                    var modDetached = unit.IsDetachedFromGroup && group.PendingReplan && ReferenceEquals(unit.Slot.SuspendedAgent, unavailableAgent);
                    var modHuman = (HumanController)unit.Slot.Active!;
                    modHuman.Enqueue(new Order(new OrderId(3005L), unit.Id, 2.0, OrderKind.Hold, RiskLevel.Low), executeSimTick: 5);
                    var modRedundant = orchestrator.TryTakeDirectControl(unit, simTime: 2.5) && ((HumanController)unit.Slot.Active!).PendingOrderCount == 1;
                    var modReplacement = orchestrator.CreateAgentFromPreset(new AgentId("agent-2"), PersonalityCatalog.All[1], AutonomyLevel.Assisted);
                    var modReassignBlocked = !orchestrator.TryAssignAgentController(unit, modReplacement, isFriendly: true, simTime: 3.0).Allowed;
                    orchestrator.Tick(new ObservedState(5.0, 1, 0, new Dictionary<TargetId, bool> { [unit.Id] = true }));
                    var modExecuted = orchestrator.ExecutedOrders.Any(o => o.Id == new OrderId(3005L));
                    var modReleased = orchestrator.TryReleaseDirectControl(unit, simTime: 6.0) && ReferenceEquals(unit.Slot.Active, unavailableAgent) && !unit.IsDetachedFromGroup;

                    return new PlayerOverrideEvidence(
                        HumanControlEstablished: modTaken && modDetached,
                        PendingOrdersPreserved: modRedundant && modExecuted,
                        AgentReassignBlockedWhileHumanOrdersPending: modReassignBlocked,
                        SuspendedAgentRestoredOnRelease: modReleased,
                        FaultInjectedSummary: "Injected unknown skill c2.nonexistent.skill (UNKNOWN_SKILL) and paused unavailable agent (AgentPaused) while human override executed.");
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
        }

        var agent = orchestrator.CreateAgentFromPreset(new AgentId("agent-1"), PersonalityCatalog.All[0], AutonomyLevel.SemiAutonomous);
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree, isFriendly: true);

        var taken = orchestrator.TryTakeDirectControl(unit, simTime: 1.0);
        var detachedAndSuspended = unit.IsDetachedFromGroup && group.PendingReplan && ReferenceEquals(unit.Slot.SuspendedAgent, agent);

        var human = (HumanController)unit.Slot.Active!;
        var orderId = new OrderId(2000L + (int)condition);
        human.Enqueue(new Order(orderId, unit.Id, 2.0, OrderKind.Hold, RiskLevel.Low), executeSimTick: 22);
        var enqueued = human.PendingOrderCount == 1;

        var redundantOk = orchestrator.TryTakeDirectControl(unit, simTime: 2.0)
            && ((HumanController)unit.Slot.Active!).PendingOrderCount == 1;

        var replacementAgent = orchestrator.CreateAgentFromPreset(new AgentId("agent-2"), PersonalityCatalog.All[1], AutonomyLevel.Assisted);
        var assignRefused = !orchestrator.TryAssignAgentController(unit, replacementAgent, isFriendly: true, simTime: 2.5).Allowed;

        orchestrator.BeginExecution(2.0, 2);
        orchestrator.Tick(new ObservedState(22.0, 1, 0, new Dictionary<TargetId, bool> { [unit.Id] = true }));
        var executed = orchestrator.ExecutedOrders.Any(o => o.Id == orderId);

        var released = orchestrator.TryReleaseDirectControl(unit, simTime: 23.0)
            && ReferenceEquals(unit.Slot.Active, agent)
            && !unit.IsDetachedFromGroup;

        return new PlayerOverrideEvidence(
            HumanControlEstablished: taken && detachedAndSuspended,
            PendingOrdersPreserved: enqueued && redundantOk && executed,
            AgentReassignBlockedWhileHumanOrdersPending: assignRefused,
            SuspendedAgentRestoredOnRelease: released,
            FaultInjectedSummary: faultSummary);
    }

    private static C2AuthorityProjectionContext OrganicAuthority(RoeLevel roe) =>
        new(roe, SkillLane.Propose, RequiredApproval.None, TrackSource.Organic, FireControlSatisfied: true);

    private static SkillEnvelope ProposeEnvelope() =>
        new(
            Lane: SkillLane.Propose,
            SkillId: SkillIds.PairingRecommend,
            InvocationId: "inv-res-1",
            ProposalId: "prop-res-1",
            SimTick: 10,
            SimTime: 10.0,
            CommandId: "engage",
            TtlTicks: 20,
            CreatedSimTick: 10,
            RequiredApproval: RequiredApproval.WeaponsRelease,
            InputsEvidence: [new EvidencePointer(EvidenceKind.Contact, "c-1", 1, "ContactPictureProjection", null)],
            Assumptions: ["organic"],
            Rationale: "resilience test",
            AuthorityBasis: new AuthorityBasis("pol-1", false, "WEAPONS_FREE", "active", TrackSource.Organic, true, false),
            PlayerOverride: new PlayerOverride(PlayerOverride.SubmitPath, "hold", PlayerOverride.HumanControllerRequirement, true),
            ReplayProvenance: new ReplayProvenance(SkillIds.PairingRecommend, "inv-res-1", 10, 10.0, null, null, false));

    private static RevisionIdentity TestRevision() =>
        new("4b5b4ca1", "sprint-6-m2", "delta-m2");

    private static InputDigestManifest TestInputs(string scenarioId, int seed) =>
        new(
            scenarioId,
            new string('1', 64),
            seed,
            "baltic-patrol-catalog",
            new string('2', 64),
            new[] { new HashedInputFile("data/scenarios/baltic-patrol.policy.json", new string('3', 64)) });

    private sealed class FixedRejectRoeFilter : IRoeFilter
    {
        private readonly FireAbortReason _reason;

        public FixedRejectRoeFilter(FireAbortReason reason) => _reason = reason;

        public RoeEvaluation Evaluate(Order order) => RoeEvaluation.Reject(_reason);
    }

    private sealed class FixedDenyPolicyEvaluator : IPolicyEvaluator
    {
        private readonly FireAbortReason _reason;

        public FixedDenyPolicyEvaluator(FireAbortReason reason) => _reason = reason;

        public PolicyVerdict Evaluate(in PolicyContext ctx, in ActionRequest request) =>
            new(false, _reason);
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits) =>
            new[] { new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High) };
    }

    private sealed class HoldOnlyFallbackPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits) =>
            new[] { new ScoredIntent(OrderKind.Hold, 1.0, RiskLevel.Low) };
    }
}
