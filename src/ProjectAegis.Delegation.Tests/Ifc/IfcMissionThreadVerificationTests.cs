using System.Diagnostics;
using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Evidence;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Ifc;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.ResourceRank;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.TargetabilityAccept;
using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.Tests.Ifc;

/// <summary>
/// DRG-193 (KCX-01 / VER-03): Integrated Fire Control mission-thread verification
/// across normal, degraded-link, lost-node, conflicting-allocation, and authority-withheld
/// paths, including 100-contact &lt; 0.5 ms headless latency budget and EvidenceLedger recording.
/// </summary>
[TestFixture]
public sealed class IfcMissionThreadVerificationTests
{
    private const string BalticV2Hash = "17144800277401907079";

    [Test]
    public void Ifc_mission_thread_correlates_all_five_cases_into_typed_evidence_ledger()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 4));

        // 1. Normal fresh contact with FC, WeaponsFree, and available ammo.
        var normalLog = BuildFreshIdentifiedLog("c-normal", "hostile-1", tick: 10);
        var normalInput = BuildInput(
            contactId: "c-normal",
            targetId: "hostile-1",
            caseKind: IfcCaseKind.Normal,
            roe: RoeLevel.WeaponsFree,
            fireControlSatisfied: true,
            commsState: CommsState.Nominal,
            roundsRemaining: 4,
            roundsCommittedElsewhere: 0,
            engagement: new EngagementRecord(1, 10.0, 10, new TargetId("2001"), 901UL, Launched: true, VictimTargetId: new TargetId("hostile-1"), WeaponFamilyId: "missile", SalvoSize: 1, HasFireControlTrack: true),
            engagementOutcome: new EngagementOutcomeRecord(2, 10.0, 10, new TargetId("2001"), new TargetId("hostile-1"), 901UL, "Hit", 0.25));
        var normalResult = IfcMissionThreadVerifier.Evaluate(
            normalLog,
            currentSimTick: 10,
            new[] { normalInput },
            fireControl: new StubFireControl("c-normal"),
            shooters: shooters,
            catalog: catalog);

        Assert.That(normalResult.Evaluations, Has.Count.EqualTo(1));
        var normalEval = normalResult.Evaluations[0];
        Assert.That(normalEval.CaseKind, Is.EqualTo(IfcCaseKind.Normal));
        Assert.That(normalEval.Targetability.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted));
        Assert.That(normalEval.Recommendation!.Outcome, Is.EqualTo(WeaponRecommendationOutcome.Feasible));
        Assert.That(normalEval.Authority.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Permitted));
        Assert.That(normalEval.Disposition, Is.EqualTo(IfcOutcomeDisposition.Permitted));
        Assert.That(normalEval.DegradedAssessment.IsDegraded, Is.False);
        Assert.That(normalEval.EngagementLaunched, Is.True);
        Assert.That(normalEval.EngagementOutcomeCode, Is.EqualTo("Hit"));

        // 2. Degraded-link: CommsState.Degraded + stale track divisor -> fail closed with Stale/NetworkPartition.
        var degradedLog = BuildFreshIdentifiedLog("c-degraded", "hostile-1", tick: 1);
        degradedLog.AppendCommsStateChange(new CommsStateChangeRecord(0, 2.0, 2, "net-1", CommsState.Nominal, CommsState.Degraded, "jamming"));
        var degradedInput = BuildInput(
            contactId: "c-degraded",
            targetId: "hostile-1",
            caseKind: IfcCaseKind.DegradedLink,
            roe: RoeLevel.WeaponsFree,
            fireControlSatisfied: true,
            commsState: CommsState.Degraded,
            roundsRemaining: 4,
            roundsCommittedElsewhere: 0,
            degradedContext: new DegradedModeContext(StaleData: true, NetworkPartition: true, Detail: "degraded-link"));
        var degradedResult = IfcMissionThreadVerifier.Evaluate(
            degradedLog,
            currentSimTick: 25,
            new[] { degradedInput },
            fireControl: new StubFireControl("c-degraded"),
            shooters: shooters,
            catalog: catalog,
            commsDisplay: new ScenarioCommsDisplaySettings(2, 0.06f, 0.04f, degradedOrderDelayTicks: 0, degradedStaleThresholdDivisor: 2),
            staleThresholdTicks: 30);

        var degradedEval = degradedResult.Evaluations.Single();
        Assert.That(degradedEval.Targetability.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(degradedEval.Disposition, Is.Not.EqualTo(IfcOutcomeDisposition.Permitted));
        Assert.That(degradedEval.DegradedAssessment.IsDegraded, Is.True);
        Assert.That(degradedEval.DegradedAssessment.AllowAutonomousOrSilentFallback, Is.False);
        Assert.That(degradedEval.DegradedAssessment.RequiresOperatorDisclosure, Is.True);
        Assert.That(
            degradedEval.DegradedAssessment.ActiveConditions,
            Has.Some.EqualTo(DegradedModeCondition.NetworkPartition).Or.Some.EqualTo(DegradedModeCondition.StaleData));

        // 3. Lost-node: contact Lost / CommsState.Denied -> LostSensor, Withheld, OutOfCommsUnknown, zero silent escalation.
        var lostLog = new DecisionLog();
        lostLog.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", "c-lost", "hostile-1", "Unknown", "Detected"));
        lostLog.AppendContactChange(new ContactChangeRecord(0, 2.0, 2, "u1", "c-lost", "hostile-1", "Detected", "Lost"));
        lostLog.AppendCommsStateChange(new CommsStateChangeRecord(0, 2.0, 2, "net-1", CommsState.Degraded, CommsState.Denied, "node-lost"));
        var lostInput = BuildInput(
            contactId: "c-lost",
            targetId: "hostile-1",
            caseKind: IfcCaseKind.LostNode,
            roe: RoeLevel.WeaponsFree,
            fireControlSatisfied: false,
            commsState: CommsState.Denied,
            roundsRemaining: 4,
            roundsCommittedElsewhere: 0,
            engagement: new EngagementRecord(3, 3.0, 3, new TargetId("2001"), 902UL, Launched: false, AbortReasonCode: "COMMS_DENIED", VictimTargetId: new TargetId("hostile-1")));
        var lostResult = IfcMissionThreadVerifier.Evaluate(
            lostLog,
            currentSimTick: 3,
            new[] { lostInput },
            fireControl: null,
            shooters: shooters,
            catalog: catalog);

        var lostEval = lostResult.Evaluations.Single();
        Assert.That(lostEval.Targetability.SensorToShooter!.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.LostSensor));
        Assert.That(lostEval.Targetability.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(lostEval.Disposition, Is.EqualTo(IfcOutcomeDisposition.Blocked));
        Assert.That(lostEval.EngagementLaunched, Is.False);
        Assert.That(lostEval.DegradedAssessment.ActiveConditions, Does.Contain(DegradedModeCondition.NetworkPartition));

        // 4. Conflicting-allocation: RoundsCommittedElsewhere + salvo > RoundsRemaining -> ResourceConflict withheld.
        var conflictLog = BuildFreshIdentifiedLog("c-conflict", "hostile-1", tick: 10);
        var conflictInput = BuildInput(
            contactId: "c-conflict",
            targetId: "hostile-1",
            caseKind: IfcCaseKind.ConflictingAllocation,
            roe: RoeLevel.WeaponsFree,
            fireControlSatisfied: true,
            commsState: CommsState.Nominal,
            roundsRemaining: 2,
            roundsCommittedElsewhere: 2);
        var conflictResult = IfcMissionThreadVerifier.Evaluate(
            conflictLog,
            currentSimTick: 10,
            new[] { conflictInput },
            fireControl: new StubFireControl("c-conflict"),
            shooters: shooters,
            catalog: catalog);

        var conflictEval = conflictResult.Evaluations.Single();
        Assert.That(conflictEval.HasConflictingAllocation, Is.True);
        Assert.That(conflictEval.ResourceRank!.RankedCandidates[0].Disposition, Is.EqualTo(ResourceRankDisposition.Excluded));
        Assert.That(conflictEval.ResourceRank.RankedCandidates[0].ReasonCode, Is.EqualTo(ResourceRankReasonCode.ExcludedByCommitment));
        Assert.That(conflictEval.Disposition, Is.EqualTo(IfcOutcomeDisposition.Withheld));
        Assert.That(conflictEval.DegradedAssessment.ActiveConditions, Does.Contain(DegradedModeCondition.ResourceConflict));
        Assert.That(conflictEval.DegradedAssessment.RequiresOperatorDisclosure, Is.True);

        // 5. Authority-withheld: complete 4-link chain but HoldFire/PolicyUnavailable -> Withheld with explicit reason.
        var authLog = BuildFreshIdentifiedLog("c-auth", "hostile-1", tick: 10);
        var authInput = BuildInput(
            contactId: "c-auth",
            targetId: "hostile-1",
            caseKind: IfcCaseKind.AuthorityWithheld,
            roe: RoeLevel.HoldFire,
            fireControlSatisfied: true,
            commsState: CommsState.Nominal,
            roundsRemaining: 4,
            roundsCommittedElsewhere: 0);
        var authResult = IfcMissionThreadVerifier.Evaluate(
            authLog,
            currentSimTick: 10,
            new[] { authInput },
            fireControl: new StubFireControl("c-auth"),
            shooters: shooters,
            catalog: catalog);

        var authEval = authResult.Evaluations.Single();
        Assert.That(authEval.Targetability.SensorToShooter!.IsComplete, Is.True);
        Assert.That(authEval.Authority.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
        Assert.That(authEval.Targetability.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(authEval.Recommendation!.Outcome, Is.EqualTo(WeaponRecommendationOutcome.WithheldByPolicy));
        Assert.That(authEval.Disposition, Is.Not.EqualTo(IfcOutcomeDisposition.Permitted));
        Assert.That(authEval.OutcomeReasonCode, Is.Not.Null.And.Not.Empty);

        // Correlate all 5 evaluations into a unified EvidenceLedger.
        var combinedBatch = new IfcMissionThreadBatchResult(
            new[] { normalEval, degradedEval, lostEval, conflictEval, authEval });
        var ledger = IfcMissionThreadVerifier.BuildEvidenceLedger(
            ledgerId: "ifc-m2-5case",
            scenarioId: "ifc-5case-verification",
            seed: 42,
            result: combinedBatch,
            revision: TestRevision(),
            inputs: TestInputs("ifc-5case-verification", 42),
            artifactLocation: "src/ProjectAegis.Delegation.Tests/Ifc/IfcMissionThreadVerificationTests.cs",
            artifactSha256: new string('a', 64),
            collectedAtUtc: "2026-11-23T16:00:00Z");

        var kinds = ledger.Entries.Select(e => e.Kind).Distinct().ToArray();
        Assert.That(kinds, Is.EquivalentTo(new[]
        {
            MissionThreadEvidenceKind.Contact,
            MissionThreadEvidenceKind.DataLink,
            MissionThreadEvidenceKind.Authority,
            MissionThreadEvidenceKind.Recommendation,
            MissionThreadEvidenceKind.Engagement,
            MissionThreadEvidenceKind.Assessment,
        }));
        Assert.That(ledger.Entries.All(e => e.HasValidPayload()), Is.True);
        Assert.That(ledger.Entries.Any(e => e.Kind == MissionThreadEvidenceKind.DataLink && e.DataLink!.OutOfCommsUnknown), Is.True);
        Assert.That(ledger.ContentDigestSha256, Has.Length.EqualTo(64));

        Assert.That(ledger.AcceptanceRecords, Has.Count.EqualTo(1));
        var acceptance = ledger.AcceptanceRecords[0];
        Assert.That(acceptance.CriterionId, Is.EqualTo("KCX-01"));
        Assert.That(acceptance.RequirementId, Is.EqualTo("VER-03"));
        Assert.That(acceptance.Layer, Is.EqualTo(AcceptanceEvidenceLayer.Headless));
        Assert.That(acceptance.OwnerAcceptance.Status, Is.EqualTo(OwnerAcceptanceStatus.Pending));

        var validation = AcceptanceEvidenceValidator.Validate(
            acceptance,
            currentRevision: TestRevision(),
            currentInputs: TestInputs("ifc-5case-verification", 42));
        Assert.That(validation.IsSchemaValid, Is.True, string.Join("; ", validation.Errors));
        Assert.That(validation.RequiresReassessment, Is.False);
    }

    [Test]
    public void Ifc_mission_thread_100_active_contacts_evaluates_under_half_millisecond_after_warmup()
    {
        const int contactCount = 100;
        const int warmupIterations = 5;
        const int measuredIterations = 25;
        const double budgetMs = 0.5;

        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("2001", ScenarioEngageDefaults.MvpFallback, 12));

        var log = new DecisionLog();
        var contactIds = new string[contactCount];
        var inputs = new IfcContactEvaluationInput[contactCount];
        for (var i = 0; i < contactCount; i++)
        {
            var cid = $"c-{i:D3}";
            var tid = $"hostile-{i:D3}";
            contactIds[i] = cid;
            log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", cid, tid, "Unknown", "Detected"));
            log.AppendContactChange(new ContactChangeRecord(0, 5.0, 5, "u1", cid, tid, "Detected", "Identified"));
            inputs[i] = BuildInput(
                contactId: cid,
                targetId: tid,
                caseKind: i % 5 == 0 ? IfcCaseKind.AuthorityWithheld : IfcCaseKind.Normal,
                roe: i % 5 == 0 ? RoeLevel.HoldFire : RoeLevel.WeaponsFree,
                fireControlSatisfied: true,
                commsState: CommsState.Nominal,
                roundsRemaining: 8,
                roundsCommittedElsewhere: 0);
        }

        var fireControl = new StubFireControl(contactIds);
        var killChain = KillChainContactStateProjection.Project(log, currentSimTick: 10, fireControl);
        var provenance = ContactProvenanceProjection.Project(log, currentSimTick: 10, catalog);
        var sensorToShooter = SensorToShooterProjection.Project(killChain, shooters, catalog);

        for (var i = 0; i < warmupIterations; i++)
        {
            _ = IfcMissionThreadVerifier.EvaluateBatch(
                killChain,
                provenance,
                sensorToShooter,
                inputs,
                currentSimTick: 10,
                simTime: 10.0);
        }

        var samples = new double[measuredIterations];
        IfcMissionThreadBatchResult? last = null;
        for (var i = 0; i < measuredIterations; i++)
        {
            var sw = Stopwatch.StartNew();
            last = IfcMissionThreadVerifier.EvaluateBatch(
                killChain,
                provenance,
                sensorToShooter,
                inputs,
                currentSimTick: 10,
                simTime: 10.0);
            sw.Stop();
            samples[i] = sw.Elapsed.TotalMilliseconds;
        }

        Array.Sort(samples);
        var medianMs = samples[measuredIterations / 2];
        var p95Index = Math.Min(measuredIterations - 1, (int)Math.Ceiling(measuredIterations * 0.95) - 1);
        var p95Ms = samples[p95Index];

        TestContext.WriteLine(
            $"KCX-01 100-contact IFC EvaluateBatch: median={medianMs:F4} ms, p95={p95Ms:F4} ms, max={samples[^1]:F4} ms");

        Assert.That(last, Is.Not.Null);
        Assert.That(last!.Evaluations, Has.Count.EqualTo(contactCount));
        Assert.That(medianMs, Is.LessThan(budgetMs), $"KCX-01 median budget < {budgetMs} ms, got {medianMs:F4} ms");
        Assert.That(p95Ms, Is.LessThan(budgetMs), $"KCX-01 p95 budget < {budgetMs} ms, got {p95Ms:F4} ms");
    }

    [Test]
    public void Ifc_mission_thread_is_replay_stable_and_preserves_baltic_v2_hash()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 4));
        var log = BuildFreshIdentifiedLog("c-1", "hostile-1", tick: 10);
        var input = BuildInput("c-1", "hostile-1", IfcCaseKind.Normal, RoeLevel.WeaponsFree, true, CommsState.Nominal, 4, 0);

        var run1 = IfcMissionThreadVerifier.Evaluate(log, 10, new[] { input }, new StubFireControl("c-1"), shooters, catalog);
        var run2 = IfcMissionThreadVerifier.Evaluate(log, 10, new[] { input }, new StubFireControl("c-1"), shooters, catalog);

        var ledger1 = IfcMissionThreadVerifier.BuildEvidenceLedger(
            "ifc-replay", "ifc-replay-scenario", 7, run1,
            TestRevision(), TestInputs("ifc-replay-scenario", 7),
            "artifact.json", new string('b', 64), "2026-11-23T16:00:00Z");
        var ledger2 = IfcMissionThreadVerifier.BuildEvidenceLedger(
            "ifc-replay", "ifc-replay-scenario", 7, run2,
            TestRevision(), TestInputs("ifc-replay-scenario", 7),
            "artifact.json", new string('b', 64), "2026-11-23T16:00:00Z");

        var json1 = EvidenceLedgerSerializer.ToCanonicalJson(ledger1);
        var json2 = EvidenceLedgerSerializer.ToCanonicalJson(ledger2);

        Assert.That(ledger1.ContentDigestSha256, Is.EqualTo(ledger2.ContentDigestSha256));
        Assert.That(json1, Is.EqualTo(json2));
        Assert.That(json1, Does.Not.Contain(BalticV2Hash));
    }

    [TestCase(IfcCaseKind.DegradedLink)]
    [TestCase(IfcCaseKind.LostNode)]
    [TestCase(IfcCaseKind.ConflictingAllocation)]
    [TestCase(IfcCaseKind.AuthorityWithheld)]
    public void Ifc_evaluate_batch_does_not_coerce_fault_from_case_kind_label_and_fails_invariants(IfcCaseKind mislabeledCase)
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 4));
        var log = BuildFreshIdentifiedLog("c-nominal", "hostile-1", tick: 10);
        var nominalInputWithFaultLabel = BuildInput(
            contactId: "c-nominal",
            targetId: "hostile-1",
            caseKind: mislabeledCase,
            roe: RoeLevel.WeaponsFree,
            fireControlSatisfied: true,
            commsState: CommsState.Nominal,
            roundsRemaining: 4,
            roundsCommittedElsewhere: 0);

        var batch = IfcMissionThreadVerifier.Evaluate(
            log,
            currentSimTick: 10,
            new[] { nominalInputWithFaultLabel },
            fireControl: new StubFireControl("c-nominal"),
            shooters: shooters,
            catalog: catalog);

        var eval = batch.Evaluations.Single();
        Assert.That(eval.Disposition, Is.EqualTo(IfcOutcomeDisposition.Permitted),
            $"EvaluateBatch must not coerce '{mislabeledCase}' from CaseKind when inputs are nominal.");

        var ledger = IfcMissionThreadVerifier.BuildEvidenceLedger(
            ledgerId: "ifc-mislabeled",
            scenarioId: "ifc-mislabeled-scenario",
            seed: 42,
            result: batch,
            revision: TestRevision(),
            inputs: TestInputs("ifc-mislabeled-scenario", 42),
            artifactLocation: "src/ProjectAegis.Delegation.Tests/Ifc/IfcMissionThreadVerificationTests.cs",
            artifactSha256: new string('a', 64),
            collectedAtUtc: "2026-11-23T16:15:00Z");

        Assert.That(ledger.AcceptanceRecords.Single().Result, Is.Not.EqualTo("PASS"));
        Assert.That(ledger.OracleEvaluation!.Passed, Is.False);
    }

    [Test]
    public void Ifc_normal_case_resolving_to_non_permitted_fails_case_invariants()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 4));
        var log = BuildFreshIdentifiedLog("c-normal-blocked", "hostile-1", tick: 10);
        var input = BuildInput(
            contactId: "c-normal-blocked",
            targetId: "hostile-1",
            caseKind: IfcCaseKind.Normal,
            roe: RoeLevel.HoldFire,
            fireControlSatisfied: true,
            commsState: CommsState.Nominal,
            roundsRemaining: 4,
            roundsCommittedElsewhere: 0);

        var batch = IfcMissionThreadVerifier.Evaluate(
            log,
            currentSimTick: 10,
            new[] { input },
            fireControl: new StubFireControl("c-normal-blocked"),
            shooters: shooters,
            catalog: catalog);

        Assert.That(batch.Evaluations.Single().Disposition, Is.Not.EqualTo(IfcOutcomeDisposition.Permitted));

        var ledger = IfcMissionThreadVerifier.BuildEvidenceLedger(
            ledgerId: "ifc-normal-blocked",
            scenarioId: "ifc-normal-blocked-scenario",
            seed: 42,
            result: batch,
            revision: TestRevision(),
            inputs: TestInputs("ifc-normal-blocked-scenario", 42),
            artifactLocation: "src/ProjectAegis.Delegation.Tests/Ifc/IfcMissionThreadVerificationTests.cs",
            artifactSha256: new string('a', 64),
            collectedAtUtc: "2026-11-23T16:15:00Z");

        Assert.That(ledger.AcceptanceRecords.Single().Result, Is.Not.EqualTo("PASS"));
        Assert.That(ledger.OracleEvaluation!.Passed, Is.False);
    }

    [Test]
    public void Ifc_empty_batch_fails_closed_in_build_evidence_ledger()
    {
        var emptyBatch = new IfcMissionThreadBatchResult(Array.Empty<IfcMissionThreadEvaluation>());
        var ledger = IfcMissionThreadVerifier.BuildEvidenceLedger(
            ledgerId: "ifc-empty",
            scenarioId: "ifc-empty-scenario",
            seed: 42,
            result: emptyBatch,
            revision: TestRevision(),
            inputs: TestInputs("ifc-empty-scenario", 42),
            artifactLocation: "src/ProjectAegis.Delegation.Tests/Ifc/IfcMissionThreadVerificationTests.cs",
            artifactSha256: new string('a', 64),
            collectedAtUtc: "2026-11-23T16:15:00Z");

        Assert.That(ledger.AcceptanceRecords.Single().Result, Is.Not.EqualTo("PASS"));
        Assert.That(ledger.OracleEvaluation!.Passed, Is.False);
    }

    [Test]
    public void Ifc_build_evidence_ledger_is_order_invariant_across_input_permutations()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 4));
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", "c-a", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(0, 5.0, 5, "u1", "c-a", "hostile-1", "Detected", "Identified"));
        log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", "c-b", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(0, 5.0, 5, "u1", "c-b", "hostile-1", "Detected", "Identified"));

        var inputA = BuildInput("c-a", "hostile-1", IfcCaseKind.Normal, RoeLevel.WeaponsFree, true, CommsState.Nominal, 4, 0);
        var inputB = BuildInput("c-b", "hostile-1", IfcCaseKind.AuthorityWithheld, RoeLevel.HoldFire, true, CommsState.Nominal, 4, 0);
        var fireControl = new StubFireControl("c-a", "c-b");

        var batchAb = IfcMissionThreadVerifier.Evaluate(log, 10, new[] { inputA, inputB }, fireControl, shooters, catalog);
        var batchBa = IfcMissionThreadVerifier.Evaluate(log, 10, new[] { inputB, inputA }, fireControl, shooters, catalog);

        var ledgerAb = IfcMissionThreadVerifier.BuildEvidenceLedger(
            "ifc-order", "ifc-order-scenario", 11, batchAb,
            TestRevision(), TestInputs("ifc-order-scenario", 11),
            "artifact.json", new string('c', 64), "2026-11-23T16:15:00Z");
        var ledgerBa = IfcMissionThreadVerifier.BuildEvidenceLedger(
            "ifc-order", "ifc-order-scenario", 11, batchBa,
            TestRevision(), TestInputs("ifc-order-scenario", 11),
            "artifact.json", new string('c', 64), "2026-11-23T16:15:00Z");

        Assert.That(ledgerAb.ContentDigestSha256, Is.EqualTo(ledgerBa.ContentDigestSha256));
        Assert.That(EvidenceLedgerSerializer.ToCanonicalJson(ledgerAb), Is.EqualTo(EvidenceLedgerSerializer.ToCanonicalJson(ledgerBa)));
    }

    [Test]
    public void Ifc_evaluate_batch_rejects_duplicate_contact_ids()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 4));
        var log = BuildFreshIdentifiedLog("c-dup", "hostile-1", tick: 10);

        var first = BuildInput("c-dup", "hostile-1", IfcCaseKind.AuthorityWithheld, RoeLevel.HoldFire, true, CommsState.Nominal, 4, 0);
        var second = BuildInput("c-dup", "hostile-1", IfcCaseKind.Normal, RoeLevel.WeaponsFree, true, CommsState.Nominal, 4, 0);

        var killChain = KillChainContactStateProjection.Project(log, 10, new StubFireControl("c-dup"));
        var provenance = ContactProvenanceProjection.Project(log, 10, catalog);
        var sensorToShooter = SensorToShooterProjection.Project(killChain, shooters, catalog);

        Assert.Throws<ArgumentException>(() =>
            IfcMissionThreadVerifier.EvaluateBatch(killChain, provenance, sensorToShooter, new[] { first, second }, 10, 10.0));
    }

    private static DecisionLog BuildFreshIdentifiedLog(string contactId, string targetId, ulong tick)
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", contactId, targetId, "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(0, tick, tick, "u1", contactId, targetId, "Detected", "Identified"));
        return log;
    }

    private static IfcContactEvaluationInput BuildInput(
        string contactId,
        string targetId,
        IfcCaseKind caseKind,
        RoeLevel roe,
        bool fireControlSatisfied,
        CommsState commsState,
        int roundsRemaining,
        int roundsCommittedElsewhere,
        DegradedModeContext? degradedContext = null,
        EngagementRecord? engagement = null,
        EngagementOutcomeRecord? engagementOutcome = null)
    {
        var engageCtx = new EngageContext(
            RangeMeters: 50_000,
            Envelope: new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: roundsRemaining,
            HasFireControlTrack: fireControlSatisfied,
            RadarEmconActive: true,
            MountOnline: true,
            PkBase: 0.85,
            SalvoSize: 1,
            DlzPersonality: DlzPersonality.Normal);
        var policy = new EffectivePolicy(roe, AutoEngageAuthorized: roe == RoeLevel.WeaponsFree, ExpendAuthorized: false);
        var authority = C2AuthorityProjector.Project(new C2AuthorityProjectionContext(
            roe,
            roe == RoeLevel.WeaponsFree ? SkillLane.Read : SkillLane.Propose,
            roe == RoeLevel.WeaponsFree ? RequiredApproval.None : RequiredApproval.WeaponsRelease,
            TrackSource.Organic,
            fireControlSatisfied,
            CommandId: roe == RoeLevel.WeaponsFree ? null : "engage",
            HumanControlled: true));
        var recommendation = ThreatAssessmentProjection.Project(new ThreatAssessmentInput(
            contactId,
            targetId,
            "2001",
            "sm-2",
            "SM-2",
            in engageCtx,
            policy));
        var resourceRank = ResourceRankProjection.Project(new[]
        {
            new ResourceRankCandidateInput(
                contactId,
                targetId,
                "2001",
                "sm-2",
                "SM-2",
                in engageCtx,
                policy,
                new ResourceRankAvailabilityFacts(RoundsCommittedElsewhere: roundsCommittedElsewhere)),
        });

        return new IfcContactEvaluationInput(
            ContactId: contactId,
            TargetId: targetId,
            CaseKind: caseKind,
            Authority: authority,
            Recommendation: recommendation,
            ResourceRank: resourceRank,
            DegradedContext: degradedContext,
            Engagement: engagement,
            EngagementOutcome: engagementOutcome,
            CommsState: commsState,
            NetworkOrUnitId: "net-1");
    }

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

    private sealed class FixedShooterSource : ISensorToShooterShooterSource
    {
        private readonly SensorToShooterShooterCandidate[] _candidates;

        public FixedShooterSource(params SensorToShooterShooterCandidate[] candidates) =>
            _candidates = candidates;

        public IReadOnlyList<SensorToShooterShooterCandidate> GetCandidatesForTarget(string targetId) =>
            _candidates;
    }

    private sealed class StubFireControl : IKillChainFireControlSource
    {
        private readonly HashSet<string> _contactIds;

        public StubFireControl(params string[] contactIds) =>
            _contactIds = new HashSet<string>(contactIds, StringComparer.Ordinal);

        public bool HasFireControlTrack(string contactId, string targetId) =>
            _contactIds.Contains(contactId);
    }
}
