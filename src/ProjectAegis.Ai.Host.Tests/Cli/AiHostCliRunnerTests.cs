using System.Text;
using System.Text.Json;
using NUnit.Framework;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Cli;
using ProjectAegis.Ai.Host.Evidence;

namespace ProjectAegis.Ai.Host.Tests.Cli;

/// <summary>
/// DRG-772: verifies the standalone <see cref="AiHostCliRunner"/> stdin/stdout JSON IPC boundary
/// for <c>aar_explain</c> and <c>mission_plan_suggest</c>.
/// </summary>
[TestFixture]
public sealed class AiHostCliRunnerTests
{
    [Test]
    public async Task Aar_explain_disabled_returns_ipc_response_with_deterministic_report()
    {
        var evidence = RequireRepoFile("tests", "ai-fixtures", "aar-explain", "evidence.jsonl");
        var reqJson = JsonSerializer.Serialize(new AiHostAarExplainRequest(
            EvidencePath: evidence,
            Enabled: false,
            ArtifactRoot: null,
            ScriptPath: null));

        using var stdin = new StringReader(reqJson);
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var hostExit = await AiHostCliRunner.RunAsync(
            new[] { AiHostIpcVerbs.AarExplain },
            stdin,
            stdout,
            stderr);

        Assert.That(hostExit, Is.EqualTo(0), stderr.ToString());
        var frame = JsonSerializer.Deserialize<AiHostIpcResponse>(stdout.ToString().Trim());
        Assert.That(frame, Is.Not.Null);
        Assert.That(frame!.ExitCode, Is.EqualTo(0));

        using var doc = JsonDocument.Parse(frame.PayloadJson);
        Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.True);
        Assert.That(doc.RootElement.GetProperty("status").GetString(), Is.EqualTo("ProviderUnavailable"));
        Assert.That(doc.RootElement.GetProperty("skipReason").GetString(), Is.EqualTo("disabled"));
    }

    [Test]
    public async Task Mission_plan_suggest_valid_draft_and_conflict_round_trip()
    {
        var validReq = JsonSerializer.Serialize(new AiHostMissionPlanSuggestRequest(
            Intent: "Blue forces patrol the Baltic strait chokepoint with unit u1",
            FixtureCaseId: "valid-draft",
            ScriptPath: null,
            ExpectedEditVersion: null,
            CurrentEditVersion: null,
            AllowedUnitIds: null,
            AllowedTargetIds: null,
            SupportedMissionTypes: null,
            IdempotencyKey: null,
            DbRef: null));

        using (var stdin = new StringReader(validReq))
        using (var stdout = new StringWriter())
        using (var stderr = new StringWriter())
        {
            var hostExit = await AiHostCliRunner.RunAsync(
                new[] { AiHostIpcVerbs.MissionPlanSuggest },
                stdin,
                stdout,
                stderr);

            Assert.That(hostExit, Is.EqualTo(0), stderr.ToString());
            var frame = JsonSerializer.Deserialize<AiHostIpcResponse>(stdout.ToString().Trim());
            Assert.That(frame, Is.Not.Null);
            Assert.That(frame!.ExitCode, Is.EqualTo(0));
            using var doc = JsonDocument.Parse(frame.PayloadJson);
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(doc.RootElement.GetProperty("outcomeClass").GetString(), Is.EqualTo("ValidDraft"));
        }

        var conflictReq = JsonSerializer.Serialize(new AiHostMissionPlanSuggestRequest(
            Intent: "Blue patrol with u1",
            FixtureCaseId: "stale-revision",
            ScriptPath: null,
            ExpectedEditVersion: null,
            CurrentEditVersion: null,
            AllowedUnitIds: null,
            AllowedTargetIds: null,
            SupportedMissionTypes: null,
            IdempotencyKey: null,
            DbRef: null));

        using (var stdin = new StringReader(conflictReq))
        using (var stdout = new StringWriter())
        using (var stderr = new StringWriter())
        {
            var hostExit = await AiHostCliRunner.RunAsync(
                new[] { AiHostIpcVerbs.MissionPlanSuggest },
                stdin,
                stdout,
                stderr);

            Assert.That(hostExit, Is.EqualTo(0), stderr.ToString());
            var frame = JsonSerializer.Deserialize<AiHostIpcResponse>(stdout.ToString().Trim());
            Assert.That(frame, Is.Not.Null);
            Assert.That(frame!.ExitCode, Is.EqualTo(3));
            using var doc = JsonDocument.Parse(frame.PayloadJson);
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.False);
            Assert.That(doc.RootElement.GetProperty("code").GetString(), Is.EqualTo("CONFLICT"));
        }
    }

    [Test]
    public async Task Unknown_verb_and_invalid_json_return_protocol_exit_2()
    {
        using (var stdin = new StringReader("{}"))
        using (var stdout = new StringWriter())
        using (var stderr = new StringWriter())
        {
            var exit = await AiHostCliRunner.RunAsync(new[] { "nope" }, stdin, stdout, stderr);
            Assert.That(exit, Is.EqualTo(2));
            Assert.That(stderr.ToString(), Does.Contain("Unknown ProjectAegis.Ai.Host verb"));
        }

        using (var stdin = new StringReader("{not-valid-json"))
        using (var stdout = new StringWriter())
        using (var stderr = new StringWriter())
        {
            var exit = await AiHostCliRunner.RunAsync(new[] { AiHostIpcVerbs.AarExplain }, stdin, stdout, stderr);
            Assert.That(exit, Is.EqualTo(2));
            Assert.That(stderr.ToString(), Does.Contain("Invalid IPC JSON"));
        }
    }

    [Test]
    public async Task Committed_aar_evidence_fixture_matches_jsonl_evidence_writer_round_trip()
    {
        var evidence = RequireRepoFile("tests", "ai-fixtures", "aar-explain", "evidence.jsonl");
        var committed = await File.ReadAllBytesAsync(evidence);
        var parsed = await JsonlEvidenceWriter.ReadRoundTripAsync(evidence);

        var tempRoot = Path.Combine(Path.GetTempPath(), "aegis-aihost-ev-" + Guid.NewGuid().ToString("N"));
        try
        {
            var writer = new JsonlEvidenceWriter();
            var written = await writer.WriteAsync(parsed, tempRoot, "evidence.jsonl");
            Assert.That(written.Published, Is.True);
            var regenerated = await File.ReadAllBytesAsync(written.PublishedPath!);
            Assert.That(regenerated, Is.EqualTo(committed));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static string RequireRepoFile(params string[] relativeSegments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 14 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Could not locate repo file: " + string.Join("/", relativeSegments));
    }
}
