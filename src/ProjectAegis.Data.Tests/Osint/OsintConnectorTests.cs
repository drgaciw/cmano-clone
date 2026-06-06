using System;
using System.IO;
using System.Linq;
using Xunit;
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
        IOsintConnector connector = new RssOsintConnector(fixturePath); // will fail - no interface yet

        // Act
        var records = connector.Fetch();

        // Assert
        Assert.NotNull(records);
        Assert.Single(records);
        Assert.Equal("rss-hypersonic", records[0].CanonicalId);
        Assert.IsAssignableFrom<IOsintConnector>(new FileOsintConnector("dummy")); // retrofit check
    }
}
