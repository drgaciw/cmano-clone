namespace ProjectAegis.Delegation.UnityAdapter.Save;

using System.Security.Cryptography;
using System.Text;
using Core;
using Data.Catalog;
using Data.Scenario;
using Data.Snapshots;
using Traits;
using ProjectAegis.Sim.Scenario;

public enum SaveFailureCode
{
    None = 0,
    TruncatedOrMalformedJson = 1,
    UnknownSchemaVersion = 2,
    PayloadTooLarge = 3,
    DepthLimitExceeded = 4,
    DuplicateOrUnknownField = 5,
    UnsupportedNetworkField = 6,
    MissingRequiredField = 7,
    IntegrityMismatch = 8,
    BuildMismatch = 9,
    ScenarioMismatch = 10,
    CatalogMismatch = 11,
    PolicyMismatch = 12,
    SeedMismatch = 13,
    InvalidSlotId = 14,
    SlotNotFound = 15,
    UnjournaledMutationDetected = 16,
    WriteFailed = 17,
    ReplayCommandFailed = 18,
    ReplayMismatch = 19,
    InvalidArgument = 20,
}

public readonly record struct SaveResult<T>(
    bool Succeeded,
    SaveFailureCode FailureCode,
    string? FailureReason,
    T? Value)
{
    public static SaveResult<T> Ok(T value) => new(true, SaveFailureCode.None, null, value);

    public static SaveResult<T> Fail(SaveFailureCode code, string reason) => new(false, code, reason, default);
}

public readonly record struct SaveOperationResult(
    bool Succeeded,
    SaveFailureCode FailureCode,
    string? FailureReason)
{
    public static SaveOperationResult Ok() => new(true, SaveFailureCode.None, null);

    public static SaveOperationResult Fail(SaveFailureCode code, string reason) => new(false, code, reason);
}

public sealed record SavePolicyHashEntry(
    string PolicyId,
    string PolicySha256);

public sealed record SaveIdentityBlock(
    int SchemaVersion,
    string BuildId,
    string ScenarioId,
    string ScenarioContentHash,
    string CatalogSnapshotId,
    string WholeCatalogHash,
    IReadOnlyList<SavePolicyHashEntry> PolicyHashes,
    int GlobalSeed);

public sealed record SavePositionBlock(
    ulong ObservedTick,
    ulong PipelineTick,
    ulong LastSequenceId);

public sealed record SaveTraitValues(
    double Aggression,
    double RiskTolerance,
    double ReactionDelay,
    double ErrorRate,
    double SituationalAwareness,
    double Decisiveness)
{
    public static SaveTraitValues FromTraitVector(TraitVector traits) =>
        new(
            traits.Aggression,
            traits.RiskTolerance,
            traits.ReactionDelay,
            traits.ErrorRate,
            traits.SituationalAwareness,
            traits.Decisiveness);

    public TraitVector ToTraitVector() =>
        new(Aggression, RiskTolerance, ReactionDelay, ErrorRate, SituationalAwareness, Decisiveness);
}

public sealed record SaveJournalArguments(
    long? EntityKeyValue = null,
    string? TargetId = null,
    string? AgentId = null,
    OrderKind? OrderKind = null,
    RiskLevel? RiskLevel = null,
    long? OrderId = null,
    string? SubCommandId = null,
    string? OptionId = null,
    int? ResolvedSalvo = null,
    double SimTime = 0.0,
    SaveTraitValues? Traits = null,
    string? PresetName = null,
    AutonomyLevel? AutonomyLevel = null,
    double? AttentionBudget = null,
    bool? IsFriendly = null,
    string? RoeLevelLabel = null,
    bool? ExplicitOverride = null,
    int? AccelerationFactor = null,
    string? WatchEventId = null,
    string? ContactId = null,
    string? Affiliation = null);

public sealed record SaveJournalRow(
    ulong SequenceOrder,
    ulong ObservedTick,
    ulong PipelineTick,
    string CommandKind,
    SaveJournalArguments Arguments);

public sealed record SaveInputJournalBlock(
    IReadOnlyList<SaveJournalRow> Rows);

public sealed record SaveCheckpointRow(
    ulong SimTick,
    ulong WorldHash,
    string LogFingerprint,
    ulong LastSequenceId);

public sealed record SaveChecksumsBlock(
    string JournalDigest,
    string OrderLogFingerprintSha256,
    ulong LastWorldHash,
    ulong CombinedWorldHash,
    IReadOnlyList<SaveCheckpointRow> CheckpointRows);

public sealed record SaveEnvelope(
    SaveIdentityBlock Identity,
    SavePositionBlock Position,
    SaveInputJournalBlock InputJournal,
    SaveChecksumsBlock Checksums,
    string EnvelopeHashSha256)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record SaveIdentityContext(
    string BuildId,
    string ScenarioId,
    string ScenarioContentHash,
    string CatalogSnapshotId,
    string WholeCatalogHash,
    IReadOnlyList<SavePolicyHashEntry> PolicyHashes,
    int GlobalSeed)
{
    public static SaveIdentityContext Create(
        string buildId,
        string scenarioPolicyId,
        string catalogSnapshotId,
        ICatalogReader catalogReader,
        int globalSeed,
        string? scenarioContentOverrideHash = null)
    {
        if (string.IsNullOrWhiteSpace(buildId))
        {
            throw new ArgumentException("BuildId must be non-empty.", nameof(buildId));
        }

        if (string.IsNullOrWhiteSpace(scenarioPolicyId))
        {
            throw new ArgumentException("ScenarioPolicyId must be non-empty.", nameof(scenarioPolicyId));
        }

        if (string.IsNullOrWhiteSpace(catalogSnapshotId))
        {
            throw new ArgumentException("CatalogSnapshotId must be non-empty.", nameof(catalogSnapshotId));
        }

        if (catalogReader is null)
        {
            throw new ArgumentNullException(nameof(catalogReader));
        }

        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var policyHash = ComputePolicyHashSha256(scenarioPolicyId);
        var scenarioHash = string.IsNullOrWhiteSpace(scenarioContentOverrideHash)
            ? policyHash
            : scenarioContentOverrideHash!;
        var wholeCatalogHash = CatalogFullSnapshotHasher.ComputeSha256Hex(catalogReader);
        var policyEntries = new[]
        {
            new SavePolicyHashEntry(scenarioPolicyId, policyHash),
        };

        return new SaveIdentityContext(
            buildId,
            scenarioPolicyId,
            scenarioHash,
            catalogSnapshotId,
            wholeCatalogHash,
            policyEntries,
            globalSeed);
    }

    public SaveIdentityBlock ToIdentityBlock() =>
        new(
            SaveEnvelope.CurrentSchemaVersion,
            BuildId,
            ScenarioId,
            ScenarioContentHash,
            CatalogSnapshotId,
            WholeCatalogHash,
            PolicyHashes.OrderBy(p => p.PolicyId, StringComparer.Ordinal).ToArray(),
            GlobalSeed);

    private static string ComputePolicyHashSha256(string scenarioPolicyId)
    {
        var dir = ScenarioDataPaths.TryResolveScenariosDirectory();
        if (!string.IsNullOrEmpty(dir))
        {
            var exactPath = Path.Combine(dir, scenarioPolicyId + ".policy.json");
            if (File.Exists(exactPath))
            {
                return Sha256Hex(File.ReadAllBytes(exactPath));
            }
        }

        var profile = ScenarioPolicyRepository.TryGet(scenarioPolicyId);
        var jsonDto = ScenarioPolicyJsonCatalog.TryGetJson(scenarioPolicyId);
        var fallback = $"policy:{scenarioPolicyId}|hasProfile:{profile != null}|hasJson:{jsonDto != null}|friendly:{profile?.FriendlyDefault.Roe}|opp:{profile?.OpposingDefault.Roe}|seed:{profile?.ReplaySettings.CheckpointIntervalTicks}";
        return Sha256Hex(Encoding.UTF8.GetBytes(fallback));
    }

    private static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            var b = hash[i];
            chars[i * 2] = GetHexNibble(b >> 4);
            chars[i * 2 + 1] = GetHexNibble(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) =>
        (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}
