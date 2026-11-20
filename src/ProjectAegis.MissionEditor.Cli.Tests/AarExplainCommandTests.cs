using System.Text;
using System.Text.Json;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class AarExplainCommandTests
{
    [Test]
    public async Task Disabled_by_default_returns_deterministic_report_and_explicit_status()
    {
        var evidence = await WriteSampleEvidenceAsync();
        try
        {
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
            Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("ProviderUnavailable"));
            Assert.That(root.GetProperty("skipReason").GetString(), Is.EqualTo("disabled"));
            Assert.That(root.TryGetProperty("deterministicReport", out var report), Is.True);
            Assert.That(report.TryGetProperty("engagementCount", out _), Is.True);
        }
        finally
        {
            TryDelete(evidence);
        }
    }

    [Test]
    public async Task Enabled_fake_client_returns_succeeded_structured_result()
    {
        var evidence = await WriteSampleEvidenceAsync();
        var artifactRoot = Path.Combine(Path.GetTempPath(), "aegis-aar-cli-" + Guid.NewGuid().ToString("N"));
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
            Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("Succeeded"));
            Assert.That(root.GetProperty("envelopePublished").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("promptVersion").GetString(), Is.EqualTo("aar-explain-1"));
            Assert.That(root.GetProperty("modelDraft").GetProperty("summary").GetString(), Is.Not.Null);
            Assert.That(File.Exists(Path.Combine(artifactRoot, "aar-explain.envelope.json")), Is.True);
        }
        finally
        {
            TryDelete(evidence);
            TryDeleteTree(artifactRoot);
        }
    }

    [Test]
    public async Task Missing_evidence_returns_exit_1()
    {
        var sb = new StringBuilder();
        await using var writer = new StringWriter(sb);
        var code = await AarExplainCommand.RunAsync(
            null,
            enabled: false,
            artifactRoot: null,
            scriptPath: null,
            writer);
        Assert.That(code, Is.EqualTo(1));
        Assert.That(sb.ToString(), Does.Contain("MISSING_EVIDENCE"));
    }

    private static async Task<string> WriteSampleEvidenceAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), "aegis-ev-" + Guid.NewGuid().ToString("N") + ".jsonl");
        var doc = new EvidenceDocument(
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

        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Temp evidence path had no directory.");
        var fileName = Path.GetFileName(path);
        Assert.That(string.IsNullOrEmpty(fileName), Is.False);

        var writer = new JsonlEvidenceWriter();
        var result = await writer.WriteAsync(doc, directory, fileName);
        Assert.That(result.Published, Is.True);
        Assert.That(string.IsNullOrEmpty(result.PublishedPath), Is.False);
        return result.PublishedPath!;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // best-effort
        }
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
            // best-effort
        }
    }
}
