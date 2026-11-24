using System.Text;
using System.Text.Json;
using ProjectAegis.Ai.Contracts;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// Headless UAT against the committed synthetic
/// <c>tests/ai-fixtures/aar-explain/evidence.jsonl</c>.
/// These tests never delete that file.
/// </summary>
[TestFixture]
public sealed class AarExplainCommittedFixtureTests
{
    [Test]
    public async Task Committed_fixture_disabled_returns_exit_0()
    {
        var evidence = RequireCommittedEvidencePath();
        var before = await File.ReadAllBytesAsync(evidence);

        var sb = new StringBuilder();
        await using var writer = new StringWriter(sb);
        var code = await AarExplainCommand.RunAsync(
            evidence,
            enabled: false,
            artifactRoot: null,
            scriptPath: null,
            writer);

        Assert.That(code, Is.EqualTo(0));
        using var doc = JsonDocument.Parse(sb.ToString());
        var root = doc.RootElement;
        Assert.That(root.GetProperty("ok").GetBoolean(), Is.True);
        Assert.That(root.GetProperty("verb").GetString(), Is.EqualTo("aar_explain"));
        Assert.That(root.GetProperty("enabled").GetBoolean(), Is.False);
        Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("ProviderUnavailable"));
        Assert.That(root.GetProperty("skipReason").GetString(), Is.EqualTo("disabled"));
        Assert.That(root.GetProperty("deterministicReport").GetProperty("engagementCount").GetInt32(), Is.EqualTo(1));
        Assert.That(root.GetProperty("deterministicReport").GetProperty("decisionCount").GetInt32(), Is.EqualTo(1));

        var after = await File.ReadAllBytesAsync(evidence);
        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public async Task Committed_fixture_enabled_fake_returns_exit_0()
    {
        var evidence = RequireCommittedEvidencePath();
        var before = await File.ReadAllBytesAsync(evidence);
        var artifactRoot = Path.Combine(Path.GetTempPath(), "aegis-aar-committed-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sb = new StringBuilder();
            await using var writer = new StringWriter(sb);
            var code = await AarExplainCommand.RunAsync(
                evidence,
                enabled: true,
                artifactRoot: artifactRoot,
                scriptPath: null,
                writer);

            Assert.That(code, Is.EqualTo(0));
            using var doc = JsonDocument.Parse(sb.ToString());
            var root = doc.RootElement;
            Assert.That(root.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("verb").GetString(), Is.EqualTo("aar_explain"));
            Assert.That(root.GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("Succeeded"));
            Assert.That(root.GetProperty("promptVersion").GetString(), Is.EqualTo("aar-explain-1"));
            Assert.That(root.GetProperty("envelopePublished").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("modelDraft").GetProperty("summary").GetString(), Is.Not.Null);
            Assert.That(File.Exists(Path.Combine(artifactRoot, "aar-explain.envelope.json")), Is.True);
        }
        finally
        {
            TryDeleteTree(artifactRoot);
        }

        var after = await File.ReadAllBytesAsync(evidence);
        Assert.That(after, Is.EqualTo(before));
        Assert.That(File.Exists(evidence), Is.True);
    }

    [Test]
    public async Task Committed_fixture_matches_writer_round_trip()
    {
        var evidence = RequireCommittedEvidencePath();
        var committed = await File.ReadAllBytesAsync(evidence);
        Assert.That(committed, Does.Not.Contain((byte)'\r'));
        Assert.That(committed, Does.Contain((byte)'\n'));
        AssertJsonlPinnedToLf(Path.GetDirectoryName(evidence)!);

        var synthetic = SyntheticDocument();
        var sb = new StringBuilder();
        sb.Append(JsonSerializer.Serialize(synthetic.Header)).Append('\n');
        foreach (var entry in synthetic.Entries)
        {
            sb.Append(JsonSerializer.Serialize(entry)).Append('\n');
        }

        var generated = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(sb.ToString());
        Assert.That(committed, Is.EqualTo(generated));

        var lines = (await File.ReadAllTextAsync(evidence, Encoding.UTF8))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(3));

        var header = JsonSerializer.Deserialize<EvidenceHeader>(lines[0]);
        Assert.That(header, Is.Not.Null);
        var entries = lines.Skip(1)
            .Select(line => JsonSerializer.Deserialize<EvidenceEntry>(line)!)
            .ToArray();

        Assert.That(header!.BuildIdentity, Is.EqualTo("build-cli"));
        Assert.That(header.ScenarioId, Is.EqualTo("scenario-cli"));
        Assert.That(header.EvidenceIds, Is.EqualTo(new[] { "fp:1", "fp:2" }));
        Assert.That(entries.Length, Is.EqualTo(2));
        Assert.That(entries[0].EvidenceId, Is.EqualTo("fp:1"));
        Assert.That(entries[0].Kind, Is.EqualTo(EvidenceEntryKind.Engagement));
        Assert.That(entries[1].EvidenceId, Is.EqualTo("fp:2"));
        Assert.That(entries[1].Kind, Is.EqualTo(EvidenceEntryKind.AgentDecision));
        Assert.That(File.Exists(evidence), Is.True);
    }

    private static void AssertJsonlPinnedToLf(string fixtureDirectory)
    {
        var attributes = File.ReadAllText(Path.Combine(RequireRepoRoot(), ".gitattributes"));
        Assert.That(attributes, Does.Match(@"\*\.jsonl\s+text\s+eol=lf"));
        Assert.That(fixtureDirectory, Does.EndWith(Path.Combine("aar-explain")));
    }

    private static string RequireCommittedEvidencePath()
    {
        var path = Path.Combine(RequireRepoRoot(), "tests", "ai-fixtures", "aar-explain", "evidence.jsonl");
        Assert.That(File.Exists(path), Is.True, "Committed aar_explain fixture is missing: " + path);
        return path;
    }

    private static string RequireRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 14 && dir != null; i++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException(
            "Could not locate repo root (ProjectAegis.sln) walking up from " + AppContext.BaseDirectory);
    }

    /// <summary>
    /// Same synthetic document as <c>AarExplainCommandTests.WriteSampleEvidenceAsync</c>.
    /// </summary>
    private static EvidenceDocument SyntheticDocument()
    {
        return new EvidenceDocument(
            new EvidenceHeader(
                AiContractSchema.Version,
                "build-cli",
                "scenario-cli",
                "cat-1",
                "digestfp",
                ViewingSide.Blue,
                DebriefMode: false,
                EvidenceIds: ["fp:1", "fp:2"]),
            [
                new EvidenceEntry(
                    "fp:1",
                    1,
                    EvidenceEntryKind.Engagement,
                    1.0,
                    ViewingSide.Blue,
                    "u1",
                    "engaged",
                    new Dictionary<string, string> { ["reason"] = "roe" }),
                new EvidenceEntry(
                    "fp:2",
                    2,
                    EvidenceEntryKind.AgentDecision,
                    2.0,
                    ViewingSide.Blue,
                    "u1",
                    "decided",
                    null),
            ]);
    }

    private static void TryDeleteTree(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch (IOException)
        {
            // best-effort temp cleanup only
        }
    }
}
