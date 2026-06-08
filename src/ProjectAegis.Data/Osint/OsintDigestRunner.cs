namespace ProjectAegis.Data.Osint;

using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;

/// <summary>Headless OSINT digest → proposal gate → catalog write gate (staging only, DSA-1.x / S19-05).</summary>
public sealed class OsintDigestRunner
{
    /// <summary>DSA-1.3 — MVP must not open a 24/7 real-time social listener.</summary>
    public const bool EnableRealtimeSocialStream = false;

    private static readonly JsonSerializerOptions DigestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly double _proposalThreshold;

    public OsintDigestRunner(double proposalThreshold = OsintProposalGate.DefaultProposalConfidenceThreshold)
    {
        _proposalThreshold = proposalThreshold;
    }

    /// <summary>Partition discoveries via proposal gate (connector / MCP search path).</summary>
    public (OsintDiscoveryRecord[] Proposals, OsintDiscoveryRecord[] LogOnly) Run(IEnumerable<OsintDiscoveryRecord> discoveries)
    {
        if (discoveries == null)
        {
            return (Array.Empty<OsintDiscoveryRecord>(), Array.Empty<OsintDiscoveryRecord>());
        }

        var sorted = discoveries
            .OrderBy(d => d.SourceUrl, StringComparer.Ordinal)
            .ThenBy(d => d.CanonicalId, StringComparer.Ordinal)
            .ToArray();

        return OsintProposalGate.Partition(sorted, _proposalThreshold);
    }

    public static string ResolveFixtureDigestPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("src", "ProjectAegis.Data.Tests", "Fixtures", "osint-digest-fixture.json"));

    public static OsintDigestRunResult RunFromDigestFile(
        string databasePath,
        string digestPath,
        ICatalogClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path required.", nameof(databasePath));
        }

        if (!File.Exists(digestPath))
        {
            throw new FileNotFoundException($"OSINT digest not found: {digestPath}");
        }

        var parsed = ReadDiscoveries(digestPath);
        var deduped = DedupeDiscoveries(parsed);
        var (proposals, logOnly) = OsintProposalGate.Partition(deduped);
        var sourceFile = Path.GetFileName(digestPath);

        string? batchId = null;
        if (proposals.Length > 0)
        {
            if (!File.Exists(databasePath))
            {
                CatalogSeedBootstrap.SeedBalticPatrol(databasePath, overwrite: false);
            }

            var bindings = MapProposalsToBindings(proposals, sourceFile);
            var catalogClock = clock ?? new FixedCatalogClock(0);
            using var gate = new CatalogWriteGate(databasePath, catalogClock);
            batchId = gate.ProposeSensorBatch(
                bindings,
                "agent",
                "osint-digest-runner",
                $"osint_digest:{sourceFile}");
        }

        return new OsintDigestRunResult(
            parsed.Count,
            proposals.Length,
            logOnly.Length,
            batchId);
    }

    public static IReadOnlyList<OsintDiscoveryRecord> ReadDiscoveries(string digestPath)
    {
        if (!File.Exists(digestPath))
        {
            throw new FileNotFoundException($"OSINT digest not found: {digestPath}");
        }

        var json = File.ReadAllText(digestPath);
        return ReadDiscoveriesFromJson(json);
    }

    public static IReadOnlyList<OsintDiscoveryRecord> ReadDiscoveriesFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var digest = JsonSerializer.Deserialize<OsintDigestFile>(json, DigestJsonOptions);
        if (digest?.Discoveries is not { Count: > 0 })
        {
            return [];
        }

        return digest.Discoveries
            .Select(d => new OsintDiscoveryRecord(
                d.CanonicalId,
                d.SourceUrl,
                d.Snippet,
                d.RelevanceScore,
                d.TargetDoc,
                d.ProposedTrl,
                d.ObservedUtcTicks))
            .ToArray();
    }

    public static OsintDiscoveryRecord[] DedupeDiscoveries(IEnumerable<OsintDiscoveryRecord> discoveries) =>
        discoveries
            .GroupBy(d => d.CanonicalId, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(d => d.RelevanceScore)
                .ThenByDescending(d => d.SourceUrl, StringComparer.Ordinal)
                .First())
            .ToArray();

    public static (string PlatformId, string SensorId) ResolvePlatformSensorIds(string canonicalId)
    {
        if (string.IsNullOrWhiteSpace(canonicalId))
        {
            throw new ArgumentException("Canonical ID required.", nameof(canonicalId));
        }

        var slash = canonicalId.IndexOf('/');
        if (slash > 0 && slash < canonicalId.Length - 1)
        {
            return (canonicalId[..slash], canonicalId[(slash + 1)..]);
        }

        return (canonicalId, canonicalId);
    }

    public static CatalogSensorBinding[] MapProposalsToBindings(
        IReadOnlyList<OsintDiscoveryRecord> proposals,
        string sourceFile)
    {
        return proposals
            .Select(proposal =>
            {
                var (platformId, sensorId) = ResolvePlatformSensorIds(proposal.CanonicalId);
                return new CatalogSensorBinding(
                    platformId,
                    sensorId,
                    BasePd: 0.5,
                    SourceFactId: $"osint:{proposal.TargetDoc}",
                    Confidence: proposal.RelevanceScore,
                    SourceFile: sourceFile,
                    ReviewState: CatalogReviewStates.Provisional,
                    TrlLevel: proposal.ProposedTrl,
                    ValueTier: CatalogProvenanceTier.InterpretedValue,
                    CitationRef: proposal.SourceUrl);
            })
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class OsintDigestFile
    {
        [JsonPropertyName("discoveries")]
        public List<OsintDiscoveryDto> Discoveries { get; init; } = [];
    }

    private sealed class OsintDiscoveryDto
    {
        [JsonPropertyName("canonicalId")]
        public string CanonicalId { get; init; } = "";

        [JsonPropertyName("sourceUrl")]
        public string SourceUrl { get; init; } = "";

        [JsonPropertyName("snippet")]
        public string Snippet { get; init; } = "";

        [JsonPropertyName("relevanceScore")]
        public double RelevanceScore { get; init; }

        [JsonPropertyName("targetDoc")]
        public string TargetDoc { get; init; } = "";

        [JsonPropertyName("proposedTrl")]
        public int ProposedTrl { get; init; }

        [JsonPropertyName("observedUtcTicks")]
        public long ObservedUtcTicks { get; init; }
    }
}

public sealed record OsintDigestRunResult(
    int ParsedTotal,
    int ProposalCount,
    int LogOnlyCount,
    string? BatchId);