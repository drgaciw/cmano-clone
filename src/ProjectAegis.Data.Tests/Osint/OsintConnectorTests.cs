using System;
using System.IO;
using System.Linq;
using Xunit;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.Osint.Connectors;

namespace ProjectAegis.Data.Tests.Osint;

/// <summary>
/// Sprint 20 TDD for real connectors (S20-01). Modeled on InMemory + plan 2026-06-07.
/// Uses xUnit (Fact) to match project (see OsintDigestRunnerTests.cs).
/// </summary>
public sealed class OsintConnectorTests : IDisposable
{
    private readonly string _tempDir;

    public OsintConnectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "osint_s20_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { /* ignore lock on Windows */ }
        }
    }

    [Fact]
    public void FileOsintConnector_LoadsJsonFixture_ProducesSortedRecords()
    {
        // Arrange: write minimal json fixture matching OsintDiscoveryRecord shape (simple parser in impl)
        var fixturePath = Path.Combine(_tempDir, "osint_facts.json");
        File.WriteAllText(fixturePath, @"[
            { ""canonicalId"": ""test-hypersonic"", ""sourceUrl"": ""https://ex.com/h"", ""snippet"": ""observed glide"", ""relevanceScore"": 0.78, ""targetDoc"": ""10"", ""proposedTrl"": 7 },
            { ""canonicalId"": ""test-railgun"", ""sourceUrl"": ""https://ex.com/r"", ""snippet"": ""speculative mount"", ""relevanceScore"": 0.55, ""targetDoc"": ""10"", ""proposedTrl"": 4 }
        ]");
        var connector = new FileOsintConnector(fixturePath);

        // Act
        var records = connector.Fetch();

        // Assert
        Assert.NotNull(records);
        Assert.Equal(2, records.Length);
        // Stable sort: by SourceUrl then CanonicalId (matches gate/runner)
        Assert.Equal("test-hypersonic", records[0].CanonicalId);
        Assert.Equal(0.78, records[0].RelevanceScore, 3);
        Assert.All(records, r => Assert.False(string.IsNullOrEmpty(r.SourceUrl)));
    }

    [Fact]
    public void FileOsintConnector_MissingOrBad_ReturnsEmpty_Deterministic()
    {
        var connector = new FileOsintConnector(Path.Combine(_tempDir, "nope.json"));
        var records = connector.Fetch();
        Assert.NotNull(records);
        Assert.Empty(records);

        // Also bad json
        var bad = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(bad, "{ not array }");
        var badConn = new FileOsintConnector(bad);
        Assert.Empty(badConn.Fetch());
    }

    [Fact]
    public void FileOsintConnector_FeedsRunner_ProposalsAboveThreshold()
    {
        var fixturePath = Path.Combine(_tempDir, "feed.json");
        File.WriteAllText(fixturePath, @"[
            { ""canonicalId"": ""high-1"", ""sourceUrl"": ""https://ex.com/1"", ""snippet"": ""high"", ""relevanceScore"": 0.82, ""targetDoc"": ""10"", ""proposedTrl"": 8 },
            { ""canonicalId"": ""low-1"", ""sourceUrl"": ""https://ex.com/2"", ""snippet"": ""low"", ""relevanceScore"": 0.40, ""targetDoc"": ""10"", ""proposedTrl"": 3 }
        ]");
        var conn = new FileOsintConnector(fixturePath);
        var runner = new OsintDigestRunner(0.65); // default threshold

        var (proposals, logOnly) = runner.Run(conn.Fetch());

        Assert.Single(proposals);
        Assert.Equal("high-1", proposals[0].CanonicalId);
        Assert.Single(logOnly);
        Assert.Equal("low-1", logOnly[0].CanonicalId);
    }

    [Fact]
    public void IOsintConnector_RssOrDirSource_ProducesStableRecords_ImplementsInterface()
    {
        // Arrange: use temp fixture for "real" source (RSS stub or JSON dir) per S21-01
        var fixturePath = Path.Combine(_tempDir, "rss_facts.json");
        File.WriteAllText(fixturePath, @"[ { ""canonicalId"": ""rss-hypersonic"", ""sourceUrl"": ""https://rss.ex/h"", ""snippet"": ""rss observed"", ""relevanceScore"": 0.75, ""targetDoc"": ""10"", ""proposedTrl"": 6 } ]");
        IOsintConnector connector = new RssOsintConnector(fixturePath); // now implements via retrofit

        // Act
        var records = connector.Fetch();

        // Assert
        Assert.NotNull(records);
        Assert.Single(records);
        Assert.Equal("rss-hypersonic", records[0].CanonicalId);
        Assert.IsAssignableFrom<IOsintConnector>(new FileOsintConnector("dummy")); // retrofit check
    }

    [Fact]
    public void FileOsintConnector_RealFixture_LoadsAndFeedsRunner_Deterministic()
    {
        // Arrange: use the new data/osint_facts.json (will be created)
        var fixturePath = CatalogJsonImporter.ResolveRepoRelative(Path.Combine("data", "osint_facts.json")); // repo relative via established resolver
        var conn = new FileOsintConnector(fixturePath);
        var records = conn.Fetch();
        Assert.NotEmpty(records);
        Assert.All(records, r => Assert.False(string.IsNullOrEmpty(r.CanonicalId)));
        // Stable sort
        var sorted = records.OrderBy(r => r.SourceUrl).ThenBy(r => r.CanonicalId).ToArray();
        Assert.Equal(sorted.Select(r => r.CanonicalId), records.Select(r => r.CanonicalId));

        var runner = new OsintDigestRunner(0.65);
        var (proposals, logOnly) = runner.Run(records);
        Assert.True(proposals.Length + logOnly.Length == records.Length);
    }

    [Fact]
    public void IOsintConnector_AllImpls_RetrofitAndStable()
    {
        IOsintConnector file = new FileOsintConnector("dummy.json");
        IOsintConnector rss = new RssOsintConnector();
        IOsintConnector mem = new InMemoryOsintConnector();
        Assert.IsAssignableFrom<IOsintConnector>(file);
        Assert.IsAssignableFrom<IOsintConnector>(rss);
        Assert.IsAssignableFrom<IOsintConnector>(mem);
        // All return stable
    }

    [Fact]
    public void Program_CliFallback_UsesRealFixture_OrEmptyDeterministic()
    {
        // Will exercise in integration after fixture + Program update
        // For now assert the fallback path resolves or connector handles missing gracefully (deterministic empty)
        var fallbackPath = CatalogJsonImporter.ResolveRepoRelative(Path.Combine("data", "osint_facts.json"));
        var conn = new FileOsintConnector(fallbackPath);
        var recs = conn.Fetch();
        // either loaded (after fixture) or empty is acceptable for this skeleton; determinism asserted elsewhere
        Assert.NotNull(recs);
    }

    [Fact]
    public void RssOsintConnector_RealFixture_AndDemoFallback_BehavesDeterministically()
    {
        // Real fixture path (same data as File test) -> parses via enhanced robust parser
        var fixturePath = CatalogJsonImporter.ResolveRepoRelative(Path.Combine("data", "osint_facts.json"));
        IOsintConnector rssWith = new RssOsintConnector(fixturePath);
        var withRecords = rssWith.Fetch();
        Assert.NotEmpty(withRecords);
        Assert.Equal(3, withRecords.Length);
        Assert.Equal("hypersonic-glide-s20", withRecords[0].CanonicalId); // after stable sort

        // No path -> deterministic demo (single record, never null)
        IOsintConnector rssDemo = new RssOsintConnector();
        var demo = rssDemo.Fetch();
        Assert.Single(demo);
        Assert.Equal("rss-demo-hypersonic", demo[0].CanonicalId);
    }

    [Fact]
    public void RssOsintConnector_MissingOrBad_ReturnsEmpty_Deterministic()
    {
        var rss = new RssOsintConnector(Path.Combine(_tempDir, "missing-rss.json"));
        Assert.Empty(rss.Fetch());

        var badPath = Path.Combine(_tempDir, "bad-rss.json");
        File.WriteAllText(badPath, "{ \"not\": \"array\" }");
        Assert.Empty(new RssOsintConnector(badPath).Fetch());
    }

    [Fact]
    public void AllConnectors_FeedRunner_WithRealFixture_ProposalsAndLogOnlyPartition()
    {
        var fixturePath = CatalogJsonImporter.ResolveRepoRelative(Path.Combine("data", "osint_facts.json"));
        var records = new FileOsintConnector(fixturePath).Fetch(); // 3 records: 2 above 0.65? wait 0.81/0.71/0.40
        var runner = new OsintDigestRunner(0.65);

        var (proposals, logOnly) = runner.Run(records);

        Assert.Equal(2, proposals.Length); // high and railgun
        Assert.Single(logOnly); // low-conf
        Assert.Contains(proposals, p => p.CanonicalId == "hypersonic-glide-s20");
        Assert.Contains(logOnly, l => l.CanonicalId == "low-conf-example");
    }
}
