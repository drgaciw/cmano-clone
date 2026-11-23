using NUnit.Framework;
using ProjectAegis.Data.Evidence;

namespace ProjectAegis.Data.Tests.Evidence;

[TestFixture]
public sealed class MissionThreadEvidenceLedgerTests
{
    [Test]
    public void Typed_records_cover_all_six_mission_thread_categories_and_preserve_sim_authority()
    {
        Assert.That(MissionThreadEvidenceSchema.Version, Is.EqualTo(1));

        var records = new[]
        {
            new MissionThreadEvidenceRecord(
                "ev-contact-1",
                MissionThreadEvidenceKind.Contact,
                SimTick: 1,
                SimTime: 1.0,
                SequenceRef: "1",
                AuthoritativeSource: "DecisionLog:ContactChange",
                Contact: new ContactEvidenceFact("c-1", "hostile-1", "u1", "Tracked", "Track", "PROBABLE", "Fresh")),
            new MissionThreadEvidenceRecord(
                "ev-auth-1",
                MissionThreadEvidenceKind.Authority,
                SimTick: 2,
                SimTime: 2.0,
                SequenceRef: "2",
                AuthoritativeSource: "DecisionLog:PolicyDenial",
                Authority: new AuthorityEvidenceFact("agent-1", "u1", "WeaponsTight", "Withheld", "WeaponsTight", 42, "WeaponsRelease")),
            new MissionThreadEvidenceRecord(
                "ev-link-1",
                MissionThreadEvidenceKind.DataLink,
                SimTick: 3,
                SimTime: 3.0,
                SequenceRef: "3",
                AuthoritativeSource: "DecisionLog:CommsStateChange",
                DataLink: new DataLinkEvidenceFact("u1", "Partitioned", "Connected", OutOfCommsUnknown: true, ShareLagTicks: 2, ReasonCode: "JAMMED")),
            new MissionThreadEvidenceRecord(
                "ev-rec-1",
                MissionThreadEvidenceKind.Recommendation,
                SimTick: 4,
                SimTime: 4.0,
                SequenceRef: "4",
                AuthoritativeSource: "DecisionLog:AgentDecision",
                Recommendation: new RecommendationEvidenceFact("agent-1", "u1", "Engage", "42", "aggressive", "Proposed", null)),
            new MissionThreadEvidenceRecord(
                "ev-eng-1",
                MissionThreadEvidenceKind.Engagement,
                SimTick: 5,
                SimTime: 5.0,
                SequenceRef: "5",
                AuthoritativeSource: "DecisionLog:Engagement",
                Engagement: new EngagementEvidenceFact("u1", "hostile-1", 9001UL, Launched: true, "asm-fixture", 2, null, "Hit")),
            new MissionThreadEvidenceRecord(
                "ev-bda-1",
                MissionThreadEvidenceKind.Assessment,
                SimTick: 6,
                SimTime: 6.0,
                SequenceRef: "6",
                AuthoritativeSource: "DecisionLog:PlatformDamageChange",
                Assessment: new AssessmentEvidenceFact("c-1", "hostile-1", "Damaged", "2", 1.0, 0.4, "SimDamage")),
        };

        Assert.That(records.Select(r => r.Kind).Distinct().Count(), Is.EqualTo(6));
        foreach (var record in records)
        {
            Assert.That(record.PreservesSimAuthority, Is.True);
            Assert.That(record.HasValidPayload(), Is.True);
            Assert.That(record.Validate(), Is.Empty);
        }

        var invalid = new MissionThreadEvidenceRecord(
            "ev-bad",
            MissionThreadEvidenceKind.Engagement,
            SimTick: 7,
            SimTime: 7.0,
            SequenceRef: "7",
            AuthoritativeSource: "DecisionLog:Engagement");
        Assert.That(invalid.HasValidPayload(), Is.False);
        Assert.That(invalid.Validate(), Is.Not.Empty);
    }

    [Test]
    public void Unified_evidence_ledger_is_order_invariant_and_round_trips_with_sha256_digest()
    {
        var entriesA = new[]
        {
            new MissionThreadEvidenceRecord(
                "ev-2",
                MissionThreadEvidenceKind.Engagement,
                SimTick: 10,
                SimTime: 10.0,
                SequenceRef: "10",
                AuthoritativeSource: "DecisionLog:Engagement",
                Engagement: new EngagementEvidenceFact("u1", "hostile-1", 1001UL, true, "nsm", 1, null, "Hit")),
            new MissionThreadEvidenceRecord(
                "ev-1",
                MissionThreadEvidenceKind.Contact,
                SimTick: 5,
                SimTime: 5.0,
                SequenceRef: "5",
                AuthoritativeSource: "DecisionLog:ContactChange",
                Contact: new ContactEvidenceFact("c1", "hostile-1", "u1", "Detected", "Fix", "POSSIBLE", "Fresh")),
        };

        var entriesB = new[] { entriesA[1], entriesA[0] };

        var baseline = new BaselineContextProvenance(
            SchemaVersion: 1,
            CommitSha: "4b5b4ca1",
            Seed: 42,
            Ticks: 10,
            CatalogPath: "assets/data/catalog/baltic_patrol.db",
            CatalogSha256: "aaaa",
            Policies: [new HashedInputFile("data/scenarios/baltic-patrol.policy.json", "bbbb")],
            Results: new HashedInputFile("artifacts/gauntlet-oracle/results.csv", "cccc"),
            LocalDeltaIdentity: "clean");

        var ledgerA = EvidenceLedgerBuilder.Build(
            ledgerId: "ledger-baltic-42",
            scenarioId: "baltic-patrol",
            seed: 42,
            entries: entriesA,
            replayFingerprint: "fp-sha",
            replayWorldHash: "17144800277401907079",
            capabilityManifestRef: "baltic-replay-harness",
            baselineContext: baseline,
            oracleEvaluation: new OracleRunCorrelation("ci", true, ["baltic-patrol"], [], []),
            ciCorrelation: new CiBuildCorrelation("Buildkite", "gauntlet-oracle", "bk-101", "https://buildkite.example/101", "passed"),
            defectFollowUps: [new DefectFollowUpLink("DRG-201", "Typed mission-thread evidence", "Backlog")]);

        var ledgerB = EvidenceLedgerBuilder.Build(
            ledgerId: "ledger-baltic-42",
            scenarioId: "baltic-patrol",
            seed: 42,
            entries: entriesB,
            replayFingerprint: "fp-sha",
            replayWorldHash: "17144800277401907079",
            capabilityManifestRef: "baltic-replay-harness",
            baselineContext: baseline,
            oracleEvaluation: new OracleRunCorrelation("ci", true, ["baltic-patrol"], [], []),
            ciCorrelation: new CiBuildCorrelation("Buildkite", "gauntlet-oracle", "bk-101", "https://buildkite.example/101", "passed"),
            defectFollowUps: [new DefectFollowUpLink("DRG-201", "Typed mission-thread evidence", "Backlog")]);

        var jsonA = EvidenceLedgerSerializer.ToCanonicalJson(ledgerA);
        var jsonB = EvidenceLedgerSerializer.ToCanonicalJson(ledgerB);

        Assert.That(jsonA, Is.EqualTo(jsonB));
        Assert.That(ledgerA.ContentDigestSha256, Is.EqualTo(ledgerB.ContentDigestSha256));
        Assert.That(ledgerA.ContentDigestSha256, Has.Length.EqualTo(64));

        var roundTrip = EvidenceLedgerSerializer.FromJson(jsonA);
        Assert.That(roundTrip.ContentDigestSha256, Is.EqualTo(ledgerA.ContentDigestSha256));
        Assert.That(roundTrip.Entries[0].EvidenceId, Is.EqualTo("ev-1"));
        Assert.That(roundTrip.ReplayWorldHash, Is.EqualTo("17144800277401907079"));
        Assert.That(roundTrip.CiCorrelation?.RunId, Is.EqualTo("bk-101"));
        Assert.That(roundTrip.DefectFollowUps[0].IssueId, Is.EqualTo("DRG-201"));
    }

    [Test]
    public void Gauntlet_fingerprint_extractor_parses_real_decision_log_wire_format_tokens()
    {
        // Real DecisionLog.ComputeFingerprint() wire format: Kind|SequenceId|SimTime|SimTick|...
        const string fp =
            "PolicyUpdate|1|0|0|42|roe|WeaponsTight|WeaponsFree\n" +
            "ContactChange|6|1|1|u1|c1|hostile-1|Unknown|Detected\n" +
            "CommsStateChange|13|3|3|brigade-net|Nominal|Degraded|jamming\n" +
            "CommsStateChange|14|4|4|brigade-net|Degraded|Nominal|restored\n" +
            "AgentDecision|19|4.5|4|a1|Engage|Engage:1:High|Hold:0.2:Low|0.7963043056138354\n" +
            "Engagement|21|5.25|5|u1|77|True|Launched\n" +
            "EngagementOutcome|23|5.5|5|77|hostile-1|Kill|0.006501966157579321\n" +
            "PolicyDenial|24|6|u1|CommsDenied|Engage\n" +
            "PlatformDamageChange|25|6.5|6|hostile-1|1|0.4|Hit|2";

        var extracted = GauntletFingerprintEvidenceExtractor.Extract("gauntlet-t1-patrol-a", fp);
        Assert.That(extracted.All(e => e.HasValidPayload()), Is.True);

        var policyUpdate = extracted.Single(e => e.AuthoritativeSource == "GauntletCsvFingerprint:PolicyUpdate");
        Assert.That(policyUpdate.SequenceRef, Is.EqualTo("1"));
        Assert.That(policyUpdate.SimTick, Is.EqualTo(0UL));
        Assert.That(policyUpdate.Authority!.PolicySnapshotId, Is.EqualTo(42L));
        Assert.That(policyUpdate.Authority.RoeLevel, Is.EqualTo("WeaponsFree"));
        Assert.That(policyUpdate.Authority.ReasonCode, Does.Contain("WeaponsTight->WeaponsFree"));

        var contact = extracted.Single(e => e.Kind == MissionThreadEvidenceKind.Contact);
        Assert.That(contact.SimTick, Is.EqualTo(1UL));
        Assert.That(contact.Contact!.ObserverId, Is.EqualTo("u1"));
        Assert.That(contact.Contact.ContactId, Is.EqualTo("c1"));
        Assert.That(contact.Contact.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(contact.Contact.LifecycleState, Is.EqualTo("Detected"));

        var degradedComms = extracted.Single(e => e.Kind == MissionThreadEvidenceKind.DataLink && e.SequenceRef == "13");
        Assert.That(degradedComms.SimTick, Is.EqualTo(3UL));
        Assert.That(degradedComms.DataLink!.NetworkOrUnitId, Is.EqualTo("brigade-net"));
        Assert.That(degradedComms.DataLink.PreviousState, Is.EqualTo("Nominal"));
        Assert.That(degradedComms.DataLink.CommsState, Is.EqualTo("Degraded"));
        Assert.That(degradedComms.DataLink.ReasonCode, Is.EqualTo("jamming"));
        Assert.That(degradedComms.DataLink.OutOfCommsUnknown, Is.True);

        var recoveredComms = extracted.Single(e => e.Kind == MissionThreadEvidenceKind.DataLink && e.SequenceRef == "14");
        Assert.That(recoveredComms.SimTick, Is.EqualTo(4UL));
        Assert.That(recoveredComms.DataLink!.NetworkOrUnitId, Is.EqualTo("brigade-net"));
        Assert.That(recoveredComms.DataLink.PreviousState, Is.EqualTo("Degraded"));
        Assert.That(recoveredComms.DataLink.CommsState, Is.EqualTo("Nominal"));
        Assert.That(recoveredComms.DataLink.ReasonCode, Is.EqualTo("restored"));
        Assert.That(recoveredComms.DataLink.OutOfCommsUnknown, Is.False);

        var decision = extracted.Single(e => e.Kind == MissionThreadEvidenceKind.Recommendation);
        Assert.That(decision.SimTick, Is.EqualTo(4UL));
        Assert.That(decision.SimTime, Is.EqualTo(4.5).Within(1e-6));
        Assert.That(decision.Recommendation!.AgentId, Is.EqualTo("a1"));
        Assert.That(decision.Recommendation.ChosenKind, Is.EqualTo("Engage"));

        var engagement = extracted.Single(e => e.AuthoritativeSource == "GauntletCsvFingerprint:Engagement");
        Assert.That(engagement.SimTick, Is.EqualTo(5UL));
        Assert.That(engagement.SimTime, Is.EqualTo(5.25).Within(1e-6));
        Assert.That(engagement.Engagement!.ShooterUnitId, Is.EqualTo("u1"));
        Assert.That(engagement.Engagement.EngagementId, Is.EqualTo(77UL));
        Assert.That(engagement.Engagement.Launched, Is.True);
        Assert.That(engagement.Engagement.OutcomeCode, Is.EqualTo("Launched"));

        var outcome = extracted.Single(e => e.AuthoritativeSource == "GauntletCsvFingerprint:EngagementOutcome");
        Assert.That(outcome.Engagement!.EngagementId, Is.EqualTo(77UL));
        Assert.That(outcome.Engagement.TargetUnitId, Is.EqualTo("hostile-1"));
        Assert.That(outcome.Engagement.OutcomeCode, Is.EqualTo("Kill"));

        var denial = extracted.Single(e => e.AuthoritativeSource == "GauntletCsvFingerprint:PolicyDenial");
        Assert.That(denial.Authority!.TargetId, Is.EqualTo("u1"));
        Assert.That(denial.Authority.Disposition, Is.EqualTo("Denied"));
        Assert.That(denial.Authority.RoeLevel, Is.EqualTo("CommsDenied"));

        var bda = extracted.Single(e => e.Kind == MissionThreadEvidenceKind.Assessment);
        Assert.That(bda.SimTick, Is.EqualTo(6UL));
        Assert.That(bda.SimTime, Is.EqualTo(6.5).Within(1e-6));
        Assert.That(bda.Assessment!.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(bda.Assessment.PreviousHpPct, Is.EqualTo(1.0).Within(1e-6));
        Assert.That(bda.Assessment.NewHpPct, Is.EqualTo(0.4).Within(1e-6));
        Assert.That(bda.Assessment.AssessmentState, Is.EqualTo("Hit"));
        Assert.That(bda.Assessment.DamageLevel, Is.EqualTo("2"));
    }

    [Test]
    public void EvidenceLedgerBuilder_rejects_invalid_and_duplicate_entries_and_is_tie_break_invariant()
    {
        var missingPayload = new MissionThreadEvidenceRecord(
            "ev-missing-payload",
            MissionThreadEvidenceKind.Contact,
            SimTick: 1,
            SimTime: 1.0,
            SequenceRef: "1",
            AuthoritativeSource: "DecisionLog:ContactChange");
        Assert.Throws<ArgumentException>(() =>
            EvidenceLedgerBuilder.Build("ledger-bad-1", "baltic-patrol", 42, [missingPayload]));

        var emptyId = new MissionThreadEvidenceRecord(
            "",
            MissionThreadEvidenceKind.Contact,
            SimTick: 1,
            SimTime: 1.0,
            SequenceRef: "1",
            AuthoritativeSource: "DecisionLog:ContactChange",
            Contact: new ContactEvidenceFact("c1", "hostile-1", "u1", "Detected", "Fix", null, null));
        Assert.Throws<ArgumentException>(() =>
            EvidenceLedgerBuilder.Build("ledger-bad-2", "baltic-patrol", 42, [emptyId]));

        var dupA = new MissionThreadEvidenceRecord(
            "ev-dup",
            MissionThreadEvidenceKind.Contact,
            SimTick: 1,
            SimTime: 1.0,
            SequenceRef: "1",
            AuthoritativeSource: "DecisionLog:ContactChange",
            Contact: new ContactEvidenceFact("c1", "hostile-1", "u1", "Detected", "Fix", null, null));
        var dupB = dupA with { Contact = new ContactEvidenceFact("c2", "hostile-2", "u2", "Tracked", "Track", null, null) };
        Assert.Throws<ArgumentException>(() =>
            EvidenceLedgerBuilder.Build("ledger-dup", "baltic-patrol", 42, [dupA, dupB]));

        // Non-ID tie-breaking on DefectFollowUpLink sharing (IssueId, Title)
        var defect1 = new DefectFollowUpLink("DRG-201", "Evidence ledger", "Open", "first");
        var defect2 = new DefectFollowUpLink("DRG-201", "Evidence ledger", "Resolved", "second");
        var ledgerOrder1 = EvidenceLedgerBuilder.Build(
            "ledger-tie",
            "baltic-patrol",
            42,
            [dupA],
            defectFollowUps: [defect1, defect2]);
        var ledgerOrder2 = EvidenceLedgerBuilder.Build(
            "ledger-tie",
            "baltic-patrol",
            42,
            [dupA],
            defectFollowUps: [defect2, defect1]);

        Assert.That(EvidenceLedgerSerializer.ToCanonicalJson(ledgerOrder1),
            Is.EqualTo(EvidenceLedgerSerializer.ToCanonicalJson(ledgerOrder2)));
        Assert.That(ledgerOrder1.ContentDigestSha256, Is.EqualTo(ledgerOrder2.ContentDigestSha256));
    }
}
