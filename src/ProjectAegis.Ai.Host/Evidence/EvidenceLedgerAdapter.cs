using System.Globalization;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Data.Evidence;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>
/// Adapts an exported <see cref="EvidenceDocument"/> into DRG-201 typed
/// <see cref="MissionThreadEvidenceRecord"/> entries and a correlated <see cref="EvidenceLedger"/>
/// without altering <see cref="AiContractSchema"/> v2 schemas or simulation authority.
/// </summary>
public static class EvidenceLedgerAdapter
{
    /// <summary>Builds an <see cref="EvidenceLedger"/> from a projected <see cref="EvidenceDocument"/>.</summary>
    public static EvidenceLedger FromEvidenceDocument(
        EvidenceDocument document,
        string ledgerId,
        int seed = 0,
        string? replayWorldHash = null,
        string? capabilityManifestRef = null,
        BaselineContextProvenance? baselineContext = null,
        OracleRunCorrelation? oracleEvaluation = null,
        CiBuildCorrelation? ciCorrelation = null,
        IEnumerable<DefectFollowUpLink>? defectFollowUps = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(ledgerId);

        var typedEntries = new List<MissionThreadEvidenceRecord>(document.Entries.Count);
        foreach (var entry in document.Entries)
        {
            var mapped = MapEntry(entry);
            if (mapped is not null)
            {
                typedEntries.Add(mapped);
            }
        }

        return EvidenceLedgerBuilder.Build(
            ledgerId: ledgerId,
            scenarioId: document.Header.ScenarioId,
            seed: seed,
            entries: typedEntries,
            replayFingerprint: document.Header.OrderLogFingerprint,
            replayWorldHash: replayWorldHash,
            capabilityManifestRef: capabilityManifestRef,
            baselineContext: baselineContext,
            oracleEvaluation: oracleEvaluation,
            ciCorrelation: ciCorrelation,
            defectFollowUps: defectFollowUps);
    }

    /// <summary>Maps one <see cref="EvidenceEntry"/> to a typed <see cref="MissionThreadEvidenceRecord"/> when applicable.</summary>
    public static MissionThreadEvidenceRecord? MapEntry(EvidenceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var attrs = entry.Attributes;
        var tick = ReadUlong(attrs, "simTick", (ulong)Math.Max(0, (long)entry.SimTime));
        var seqRef = entry.SequenceId.ToString(CultureInfo.InvariantCulture);
        var source = $"EvidenceDocument:{entry.Kind}";

        return entry.Kind switch
        {
            EvidenceEntryKind.ContactChange or EvidenceEntryKind.ContactOverride => new MissionThreadEvidenceRecord(
                EvidenceId: entry.EvidenceId,
                Kind: MissionThreadEvidenceKind.Contact,
                SimTick: tick,
                SimTime: entry.SimTime,
                SequenceRef: seqRef,
                AuthoritativeSource: source,
                Contact: new ContactEvidenceFact(
                    ContactId: ReadString(attrs, "contactId") ?? entry.UnitId ?? seqRef,
                    TargetId: ReadString(attrs, "targetId") ?? entry.UnitId ?? "unknown",
                    ObserverId: ReadString(attrs, "observerId") ?? "unknown",
                    LifecycleState: ReadString(attrs, "newState") ?? ReadString(attrs, "newValue") ?? entry.Summary ?? "Updated",
                    KillChainPhase: null,
                    Confidence: null,
                    QualityFlags: ReadString(attrs, "action"))),

            EvidenceEntryKind.PolicyDenial or EvidenceEntryKind.ControllerChange or EvidenceEntryKind.PolicyUpdate => new MissionThreadEvidenceRecord(
                EvidenceId: entry.EvidenceId,
                Kind: MissionThreadEvidenceKind.Authority,
                SimTick: tick,
                SimTime: entry.SimTime,
                SequenceRef: seqRef,
                AuthoritativeSource: source,
                Authority: new AuthorityEvidenceFact(
                    ActorId: ReadString(attrs, "agentId") ?? entry.UnitId ?? "policy",
                    TargetId: entry.UnitId,
                    RoeLevel: ReadString(attrs, "newValue") ?? ReadString(attrs, "reason") ?? entry.Summary ?? "Unknown",
                    Disposition: entry.Kind == EvidenceEntryKind.PolicyDenial ? "Withheld" : "Updated",
                    ReasonCode: ReadString(attrs, "reason") ?? ReadString(attrs, "newKind"),
                    PolicySnapshotId: ReadLong(attrs, "policySnapshotId"),
                    RequiredApproval: null)),

            EvidenceEntryKind.CommsStateChange or EvidenceEntryKind.CommsGridRejoin => new MissionThreadEvidenceRecord(
                EvidenceId: entry.EvidenceId,
                Kind: MissionThreadEvidenceKind.DataLink,
                SimTick: tick,
                SimTime: entry.SimTime,
                SequenceRef: seqRef,
                AuthoritativeSource: source,
                DataLink: new DataLinkEvidenceFact(
                    NetworkOrUnitId: ReadString(attrs, "unitId") ?? entry.UnitId ?? "comms",
                    CommsState: entry.Summary ?? "Updated",
                    PreviousState: null,
                    OutOfCommsUnknown: entry.Kind == EvidenceEntryKind.CommsStateChange,
                    ShareLagTicks: null,
                    ReasonCode: ReadString(attrs, "reason"))),

            EvidenceEntryKind.AgentDecision => new MissionThreadEvidenceRecord(
                EvidenceId: entry.EvidenceId,
                Kind: MissionThreadEvidenceKind.Recommendation,
                SimTick: tick,
                SimTime: entry.SimTime,
                SequenceRef: seqRef,
                AuthoritativeSource: source,
                Recommendation: new RecommendationEvidenceFact(
                    AgentId: ReadString(attrs, "agentId") ?? "agent",
                    TargetId: entry.UnitId ?? "unknown",
                    ChosenKind: ReadString(attrs, "chosenOrderKind") ?? ReadString(attrs, "chosenKind") ?? entry.Summary ?? "Hold",
                    PolicySnapshotId: ReadString(attrs, "policySnapshotId"),
                    PersonalitySlug: ReadString(attrs, "personality"),
                    Outcome: ReadString(attrs, "autonomyLevel"),
                    WithheldReasonCode: null)),

            EvidenceEntryKind.Engagement or EvidenceEntryKind.EngagementOutcome or EvidenceEntryKind.MagazineChange or EvidenceEntryKind.OrdnanceStateChange => new MissionThreadEvidenceRecord(
                EvidenceId: entry.EvidenceId,
                Kind: MissionThreadEvidenceKind.Engagement,
                SimTick: tick,
                SimTime: entry.SimTime,
                SequenceRef: seqRef,
                AuthoritativeSource: source,
                Engagement: new EngagementEvidenceFact(
                    ShooterUnitId: ReadString(attrs, "shooterUnitId") ?? entry.UnitId ?? "unknown",
                    TargetUnitId: ReadString(attrs, "victimUnitId"),
                    EngagementId: ReadUlong(attrs, "engagementId", entry.SequenceId),
                    Launched: string.Equals(ReadString(attrs, "launched"), "true", StringComparison.OrdinalIgnoreCase),
                    WeaponFamilyId: ReadString(attrs, "weaponFamilyId"),
                    SalvoSize: ReadInt(attrs, "salvoSize", 1),
                    AbortReasonCode: ReadString(attrs, "abortReasonCode") ?? ReadString(attrs, "reasonCode"),
                    OutcomeCode: ReadString(attrs, "outcomeCode") ?? entry.Summary)),

            EvidenceEntryKind.PlatformDamageChange => new MissionThreadEvidenceRecord(
                EvidenceId: entry.EvidenceId,
                Kind: MissionThreadEvidenceKind.Assessment,
                SimTick: tick,
                SimTime: entry.SimTime,
                SequenceRef: seqRef,
                AuthoritativeSource: source,
                Assessment: new AssessmentEvidenceFact(
                    ContactId: ReadString(attrs, "unitId") ?? entry.UnitId ?? seqRef,
                    TargetId: ReadString(attrs, "unitId") ?? entry.UnitId ?? "unknown",
                    AssessmentState: entry.Summary ?? "Damaged",
                    DamageLevel: ReadString(attrs, "damageLevel"),
                    PreviousHpPct: ReadDouble(attrs, "previousHpPct"),
                    NewHpPct: ReadDouble(attrs, "newHpPct"),
                    SourceKind: "PlatformDamageChange")),

            _ => null,
        };
    }

    private static string? ReadString(IReadOnlyDictionary<string, string>? attrs, string key) =>
        attrs is not null && attrs.TryGetValue(key, out var val) ? val : null;

    private static ulong ReadUlong(IReadOnlyDictionary<string, string>? attrs, string key, ulong fallback) =>
        attrs is not null && attrs.TryGetValue(key, out var raw)
        && ulong.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static long? ReadLong(IReadOnlyDictionary<string, string>? attrs, string key) =>
        attrs is not null && attrs.TryGetValue(key, out var raw)
        && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static int ReadInt(IReadOnlyDictionary<string, string>? attrs, string key, int fallback) =>
        attrs is not null && attrs.TryGetValue(key, out var raw)
        && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static double? ReadDouble(IReadOnlyDictionary<string, string>? attrs, string key) =>
        attrs is not null && attrs.TryGetValue(key, out var raw)
        && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
}
