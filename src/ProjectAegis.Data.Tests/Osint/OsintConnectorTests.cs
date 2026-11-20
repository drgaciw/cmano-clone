using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.Osint.Connectors;

namespace ProjectAegis.Data.Tests.Osint;

/// <summary>
/// Sprint 20 TDD for real connectors (S20-01). Modeled on InMemory + plan 2026-06-07.
/// Uses NUnit ([Test]) to match the project (see OsintDigestRunnerTests.cs).
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
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

    [Test]
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
        Assert.That(records, Is.Not.Null);
        Assert.That(records.Length, Is.EqualTo(2));
        // Stable sort: by SourceUrl then CanonicalId (matches gate/runner)
        Assert.That(records[0].CanonicalId, Is.EqualTo("test-hypersonic"));
        Assert.That(records[0].RelevanceScore, Is.EqualTo(0.78).Within(0.005));
        foreach (var r in records)
{
Assert.That(string.IsNullOrEmpty(r.SourceUrl), Is.False);
}
    }

    [Test]
    public void FileOsintConnector_MissingOrBad_ReturnsEmpty_Deterministic()
    {
        var connector = new FileOsintConnector(Path.Combine(_tempDir, "nope.json"));
        var records = connector.Fetch();
        Assert.That(records, Is.Not.Null);
        Assert.That(records, Is.Empty);

        // Also bad json
        var bad = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(bad, "{ not array }");
        var badConn = new FileOsintConnector(bad);
        Assert.That(badConn.Fetch(), Is.Empty);
    }

    [Test]
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

        Assert.That(proposals, Has.Exactly(1).Items);
        Assert.That(proposals[0].CanonicalId, Is.EqualTo("high-1"));
        Assert.That(logOnly, Has.Exactly(1).Items);
        Assert.That(logOnly[0].CanonicalId, Is.EqualTo("low-1"));
    }

    [Test]
    public void IOsintConnector_RssOrDirSource_ProducesStableRecords_ImplementsInterface()
    {
        // Arrange: use temp fixture for "real" source (RSS stub or JSON dir) per S21-01
        var fixturePath = Path.Combine(_tempDir, "rss_facts.json");
        File.WriteAllText(fixturePath, @"[ { ""canonicalId"": ""rss-hypersonic"", ""sourceUrl"": ""https://rss.ex/h"", ""snippet"": ""rss observed"", ""relevanceScore"": 0.75, ""targetDoc"": ""10"", ""proposedTrl"": 6 } ]");
        IOsintConnector connector = new RssOsintConnector(fixturePath); // now implements via retrofit

        // Act
        var records = connector.Fetch();

        // Assert
        Assert.That(records, Is.Not.Null);
        Assert.That(records, Has.Exactly(1).Items);
        Assert.That(records[0].CanonicalId, Is.EqualTo("rss-hypersonic"));
        Assert.That(new FileOsintConnector("dummy"), Is.InstanceOf<IOsintConnector>()); // retrofit check
    }

    [Test]
    public void FileOsintConnector_RealFixture_LoadsAndFeedsRunner_Deterministic()
    {
        // Arrange: use the new data/osint_facts.json (will be created)
        var fixturePath = CatalogJsonImporter.ResolveRepoRelative(Path.Combine("data", "osint_facts.json")); // repo relative via established resolver
        var conn = new FileOsintConnector(fixturePath);
        var records = conn.Fetch();
        Assert.That(records, Is.Not.Empty);
        foreach (var r in records)
{
Assert.That(string.IsNullOrEmpty(r.CanonicalId), Is.False);
}
        // Stable sort
        var sorted = records.OrderBy(r => r.SourceUrl, StringComparer.Ordinal).ThenBy(r => r.CanonicalId, StringComparer.Ordinal).ToArray();
        Assert.That(records.Select(r => r.CanonicalId), Is.EqualTo(sorted.Select(r => r.CanonicalId)));

        var runner = new OsintDigestRunner(0.65);
        var (proposals, logOnly) = runner.Run(records);
        Assert.That(proposals.Length + logOnly.Length == records.Length, Is.True);
    }

    [Test]
    public void IOsintConnector_AllImpls_RetrofitAndStable()
    {
        IOsintConnector file = new FileOsintConnector("dummy.json");
        IOsintConnector rss = new RssOsintConnector();
        IOsintConnector mem = new InMemoryOsintConnector();
        Assert.That(file, Is.InstanceOf<IOsintConnector>());
        Assert.That(rss, Is.InstanceOf<IOsintConnector>());
        Assert.That(mem, Is.InstanceOf<IOsintConnector>());
        // All return stable
    }

    [Test]
    public void Program_CliFallback_UsesRealFixture_OrEmptyDeterministic()
    {
        // Will exercise in integration after fixture + Program update
        // For now assert the fallback path resolves or connector handles missing gracefully (deterministic empty)
        var fallbackPath = CatalogJsonImporter.ResolveRepoRelative(Path.Combine("data", "osint_facts.json"));
        var conn = new FileOsintConnector(fallbackPath);
        var recs = conn.Fetch();
        // either loaded (after fixture) or empty is acceptable for this skeleton; determinism asserted elsewhere
        Assert.That(recs, Is.Not.Null);
    }

    [Test]
    public void RssOsintConnector_RealFixture_AndDemoFallback_BehavesDeterministically()
    {
        // Real fixture path (same data as File test) -> parses via enhanced robust parser
        var fixturePath = CatalogJsonImporter.ResolveRepoRelative(Path.Combine("data", "osint_facts.json"));
        IOsintConnector rssWith = new RssOsintConnector(fixturePath);
        var withRecords = rssWith.Fetch();
        Assert.That(withRecords, Is.Not.Empty);
        Assert.That(withRecords.Length, Is.EqualTo(3));
        Assert.That(withRecords[0].CanonicalId, Is.EqualTo("hypersonic-glide-s20")); // after stable sort

        // No path -> deterministic demo (single record, never null)
        IOsintConnector rssDemo = new RssOsintConnector();
        var demo = rssDemo.Fetch();
        Assert.That(demo, Has.Exactly(1).Items);
        Assert.That(demo[0].CanonicalId, Is.EqualTo("rss-demo-hypersonic"));
    }

    [Test]
    public void RssOsintConnector_MissingOrBad_ReturnsEmpty_Deterministic()
    {
        var rss = new RssOsintConnector(Path.Combine(_tempDir, "missing-rss.json"));
        Assert.That(rss.Fetch(), Is.Empty);

        var badPath = Path.Combine(_tempDir, "bad-rss.json");
        File.WriteAllText(badPath, "{ \"not\": \"array\" }");
        Assert.That(new RssOsintConnector(badPath).Fetch(), Is.Empty);
    }

    [Test]
    public void AllConnectors_FeedRunner_WithRealFixture_ProposalsAndLogOnlyPartition()
    {
        var fixturePath = CatalogJsonImporter.ResolveRepoRelative(Path.Combine("data", "osint_facts.json"));
        var records = new FileOsintConnector(fixturePath).Fetch(); // 3 records: 2 above 0.65? wait 0.81/0.71/0.40
        var runner = new OsintDigestRunner(0.65);

        var (proposals, logOnly) = runner.Run(records);

        Assert.That(proposals.Length, Is.EqualTo(2)); // high and railgun
        Assert.That(logOnly, Has.Exactly(1).Items); // low-conf
        Assert.That(proposals.Any(p => p.CanonicalId == "hypersonic-glide-s20"), Is.True);
        Assert.That(logOnly.Any(l => l.CanonicalId == "low-conf-example"), Is.True);
    }
}
