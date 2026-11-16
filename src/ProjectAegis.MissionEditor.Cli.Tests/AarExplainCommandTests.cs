using System.Text;
using System.Text.Json;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class AarExplainCommandTests
{
    [Fact]
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

            Assert.Equal(0, code);
            using var doc = JsonDocument.Parse(sb.ToString());
            var root = doc.RootElement;
            Assert.True(root.GetProperty("ok").GetBoolean());
            Assert.Equal("ProviderUnavailable", root.GetProperty("status").GetString());
            Assert.Equal("disabled", root.GetProperty("skipReason").GetString());
            Assert.True(root.TryGetProperty("deterministicReport", out var report));
            Assert.True(report.TryGetProperty("engagementCount", out _));
        }
        finally
        {
            TryDelete(evidence);
        }
    }

    [Fact]
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

            Assert.Equal(0, code);
            using var doc = JsonDocument.Parse(sb.ToString());
            var root = doc.RootElement;
            Assert.True(root.GetProperty("ok").GetBoolean());
            Assert.Equal("Succeeded", root.GetProperty("status").GetString());
            Assert.True(root.GetProperty("envelopePublished").GetBoolean());
            Assert.Equal("aar-explain-1", root.GetProperty("promptVersion").GetString());
            Assert.NotNull(root.GetProperty("modelDraft").GetProperty("summary").GetString());
            Assert.True(File.Exists(Path.Combine(artifactRoot, "aar-explain.envelope.json")));
        }
        finally
        {
            TryDelete(evidence);
            TryDeleteTree(artifactRoot);
        }
    }

    [Fact]
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
        Assert.Equal(1, code);
        Assert.Contains("MISSING_EVIDENCE", sb.ToString(), StringComparison.Ordinal);
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
        Assert.False(string.IsNullOrEmpty(fileName));

        var writer = new JsonlEvidenceWriter();
        var result = await writer.WriteAsync(doc, directory, fileName);
        Assert.True(result.Published);
        Assert.False(string.IsNullOrEmpty(result.PublishedPath));
        return result.PublishedPath;
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
