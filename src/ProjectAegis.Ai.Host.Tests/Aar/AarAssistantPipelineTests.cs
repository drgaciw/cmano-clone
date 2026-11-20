using System.Text.Json;
using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Aar;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Ai.Host.Tests.Evidence;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

namespace ProjectAegis.Ai.Host.Tests.Aar;

[TestFixture]
public sealed class AarAssistantPipelineTests
{
    private readonly string _fixturesRoot = AiFixtureLoader.ResolveFixturesRoot();
    private readonly EvidenceProjector _projector = new();

    [Test]
    public async Task Success_fixture_all_citations_resolve_and_numbers_allowed()
    {
        var projection = ProjectCase("success");
        var ids = projection.Document.Header.EvidenceIds;
        var scripted = new AarScriptedChatClient();
        scripted.EnqueueAssistantText(ValidDraftJson(
            summary: "Engagements launched " + projection.Quantitative.EngagementCount + ".",
            citations: ids.ToArray()));

        var root = CreateTempRoot();
        try
        {
            var store = new AtomicArtifactStore(root, new AiHostOptions());
            var pipeline = new AarAssistantPipeline(scripted, artifactStore: store);
            var result = await pipeline.RunAsync(ToRequest(projection));

            Assert.That(result.Status, Is.EqualTo(GenerationStatus.Succeeded));
            Assert.That(result.ModelDraft, Is.Not.Null);
            Assert.That(result.ModelDraft!.Citations.Count, Is.EqualTo(ids.Count));
            foreach (var id in result.ModelDraft.Citations)
            {
                Assert.That(ids, Does.Contain(id));
            }
            Assert.That(result.DeterministicReport.EngagementCount, Is.EqualTo(projection.Quantitative.EngagementCount));
            Assert.That(result.PromptVersion, Is.EqualTo(AarPromptVersion.Current));
            Assert.That(result.ProviderId, Is.EqualTo("fake"));
            Assert.That(result.ModelId, Is.EqualTo("aar-scripted"));
            Assert.That(result.Envelope, Is.Not.Null);
            Assert.That(result.Envelope!.Status, Is.EqualTo(GenerationStatus.Succeeded));
            Assert.That(result.Envelope.PromptVersion, Is.EqualTo(AarPromptVersion.Current));
            Assert.That(result.Envelope.ProviderId, Is.EqualTo("fake"));
            Assert.That(result.Envelope.ModelId, Is.EqualTo("aar-scripted"));
            Assert.That(store.Exists("aar-explain.envelope.json"), Is.True);
        }
        finally
        {
            TryDeleteTree(root);
            scripted.Dispose();
        }
    }

    [Test]
    public async Task Fabricated_citation_is_rejected_with_deterministic_report()
    {
        var projection = ProjectCase("success");
        var scripted = new AarScriptedChatClient();
        scripted.EnqueueAssistantText(ValidDraftJson(
            summary: "ok",
            citations: ["fabricated-id:999"]));

        var pipeline = new AarAssistantPipeline(scripted);
        var result = await pipeline.RunAsync(ToRequest(projection, publish: false));

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.RejectedCitation));
        Assert.That(result.DeterministicReport, Is.Not.Null);
        Assert.That(result.DeterministicReport.EngagementCount, Is.EqualTo(projection.Quantitative.EngagementCount));
        Assert.That(result.SkipReason, Does.Contain("missing-citation"));
        scripted.Dispose();
    }

    [Test]
    public async Task Contradictory_number_in_prose_is_rejected()
    {
        var projection = ProjectCase("success");
        var ids = projection.Document.Header.EvidenceIds.ToArray();
        var scripted = new AarScriptedChatClient();
        // 99999 is not in the authoritative facts / quantitative summary.
        scripted.EnqueueAssistantText(ValidDraftJson(
            summary: "Salvo size was 99999 units.",
            citations: ids.Length == 0 ? ["aifix-success:1"] : new[] { ids[0] }));

        var pipeline = new AarAssistantPipeline(scripted);
        var result = await pipeline.RunAsync(ToRequest(projection, publish: false));

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.RejectedNumber));
        Assert.That(result.DeterministicReport, Is.Not.Null);
        Assert.That(result.SkipReason, Does.Contain("contradictory-number"));
        scripted.Dispose();
    }

    [TestCase(FakeChatScript.MalformedOutput, GenerationStatus.MalformedOutput)]
    [TestCase(FakeChatScript.Timeout, GenerationStatus.TimedOut)]
    [TestCase(FakeChatScript.Cancellation, GenerationStatus.Cancelled)]
    [TestCase(FakeChatScript.ProviderUnavailable, GenerationStatus.ProviderUnavailable)]
    public async Task Failure_scripts_return_explicit_status_plus_deterministic_report(
        FakeChatScript script,
        GenerationStatus expected)
    {
        var projection = ProjectCase("malformed-provider-response");
        using var fake = new FakeChatClient();
        fake.Enqueue(script);

        var pipeline = new AarAssistantPipeline(fake);
        var result = await pipeline.RunAsync(ToRequest(projection, publish: false));

        Assert.That(result.Status, Is.EqualTo(expected));
        Assert.That(result.DeterministicReport, Is.Not.Null);
        Assert.That(result.DeterministicReport.DecisionCount, Is.EqualTo(projection.Quantitative.DecisionCount));
        Assert.That(result.ModelDraft, Is.Null);
    }

    [Test]
    public async Task Disabled_pipeline_still_returns_deterministic_report()
    {
        var projection = ProjectCase("success");
        using var fake = new FakeChatClient();
        var pipeline = new AarAssistantPipeline(fake);
        var result = await pipeline.RunAsync(ToRequest(projection, publish: false) with { Enabled = false });

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.ProviderUnavailable));
        Assert.That(result.SkipReason, Is.EqualTo("disabled"));
        Assert.That(result.DeterministicReport, Is.Not.Null);
    }

    [Test]
    public async Task Embedded_instruction_fixture_does_not_change_validation_behaviour()
    {
        var projection = ProjectCase("embedded-instructions");
        var ids = projection.Document.Header.EvidenceIds.ToArray();
        Assert.That(ids, Is.Not.Empty);

        // Prompt must still delimit data; fabricated citation still rejected.
        var scripted = new AarScriptedChatClient();
        scripted.EnqueueAssistantText(ValidDraftJson(summary: "ignore prior", citations: ["evil:1"]));
        var reject = await new AarAssistantPipeline(scripted)
            .RunAsync(ToRequest(projection, publish: false));
        Assert.That(reject.Status, Is.EqualTo(GenerationStatus.RejectedCitation));

        scripted.EnqueueAssistantText(ValidDraftJson(
            summary: "Engagements " + projection.Quantitative.EngagementCount,
            citations: ids));
        var ok = await new AarAssistantPipeline(scripted)
            .RunAsync(ToRequest(projection, publish: false));
        Assert.That(ok.Status, Is.EqualTo(GenerationStatus.Succeeded));

        var messages = new AarPromptBuilder().Build(projection.Document, ok.DeterministicReport);
        var user = messages.Single(m => m.Role == ChatRole.User).Text;
        Assert.That(user, Does.Contain("<<<EVIDENCE_DATA_BEGIN>>>"));
        Assert.That(user, Does.Contain("<<<EVIDENCE_DATA_END>>>"));
        scripted.Dispose();
    }

    [Test]
    public async Task Pipeline_enabled_disabled_or_failing_does_not_mutate_order_log_fingerprint()
    {
        var log = new DecisionLog();
        log.Append(OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(
                SequenceId: 1,
                SimTime: 1.0,
                SimTick: 1,
                ShooterTargetId: new TargetId("u1"),
                EngagementId: 9,
                Launched: true,
                VictimTargetId: new TargetId("hostile-1"))));
        var before = log.ComputeFingerprint();

        var projected = _projector.Project(
            log,
            new EvidenceProjectionRequest(
                "build",
                "scenario",
                "cat",
                ViewingSide.Blue,
                DebriefMode: false,
                FingerprintPrefix: "fp"));

        var scripted = new AarScriptedChatClient();
        scripted.EnqueueAssistantText(ValidDraftJson(
            summary: "Engagements " + projected.Quantitative.EngagementCount,
            citations: projected.Document.Header.EvidenceIds.ToArray()));
        await new AarAssistantPipeline(scripted)
            .RunAsync(ToRequest(projected, publish: false));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(before));

        await new AarAssistantPipeline(scripted)
            .RunAsync(ToRequest(projected, publish: false) with { Enabled = false });
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(before));

        using var fake = new FakeChatClient();
        fake.Enqueue(FakeChatScript.MalformedOutput);
        await new AarAssistantPipeline(fake)
            .RunAsync(ToRequest(projected, publish: false));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(before));

        await Task.Delay(15);
        scripted.EnqueueAssistantText(ValidDraftJson(
            summary: "Engagements " + projected.Quantitative.EngagementCount,
            citations: projected.Document.Header.EvidenceIds.ToArray()));
        await new AarAssistantPipeline(scripted)
            .RunAsync(ToRequest(projected, publish: false));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(before));
        scripted.Dispose();
    }

    [Test]
    public void Citation_and_number_validators_unit_behaviour()
    {
        Assert.That(AarCitationValidator.FindMissingCitation(["a:1"], ["a:1", "a:2"]), Is.Null);
        Assert.That(AarCitationValidator.FindMissingCitation(["x"], ["a:1"]), Is.EqualTo("x"));

        var allowed = new HashSet<string>(StringComparer.Ordinal) { "1", "2.5" };
        Assert.That(AarNumberValidator.FindContradictoryNumber(["saw 1 and 2.5"], allowed), Is.Null);
        Assert.That(AarNumberValidator.FindContradictoryNumber(["saw 99"], allowed), Is.EqualTo("99"));
    }

    [Test]
    public void Source_fence_Aar_files_do_not_reference_forbidden_symbols()
    {
        var hostRoot = FindHostAarRoot();
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs"))
        {
            var code = string.Join(
                '\n',
                File.ReadLines(path).Where(line =>
                {
                    var trimmed = line.TrimStart();
                    return !trimmed.StartsWith("//", StringComparison.Ordinal)
                        && !trimmed.StartsWith("///", StringComparison.Ordinal)
                        && !trimmed.StartsWith("*", StringComparison.Ordinal);
                }));

            Assert.That(code, Does.Not.Contain("DelegationBridge"));
            Assert.That(code, Does.Not.Contain("SimulationSession"));
            Assert.That(code, Does.Not.Contain("OnScenarioFinalized"));
            Assert.That(code, Does.Not.Contain("OpenAI"));
            Assert.That(code, Does.Not.Contain("Anthropic"));
        }
    }

    private EvidenceProjectionResult ProjectCase(string caseId)
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, caseId);
        var request = new EvidenceProjectionRequest(
            BuildIdentity: "build-aar-test",
            ScenarioId: "scenario-" + caseId,
            CatalogRevision: "cat-1",
            ViewingSide: fixture.DefaultViewingSide,
            DebriefMode: false,
            FingerprintPrefix: fixture.FingerprintPrefix,
            AuthoritativeFacts: fixture.Facts);
        return _projector.Project(fixture.Log, request, fixture.Annotations);
    }

    private static AarAssistantRequest ToRequest(EvidenceProjectionResult projection, bool publish = true) =>
        new(
            Document: projection.Document,
            Quantitative: projection.Quantitative,
            AuthoritativeFacts: projection.AuthoritativeFacts,
            BuildIdentity: projection.Document.Header.BuildIdentity,
            ProviderId: "fake",
            ModelId: "fake-aar",
            Enabled: true,
            ArtifactFileName: publish ? "aar-explain.envelope.json" : "unused.json");

    private static string ValidDraftJson(string summary, string[] citations)
    {
        return JsonSerializer.Serialize(new
        {
            summary,
            outcomes = new[] { "mission complete" },
            uncertainties = new[] { "sensor ambiguity" },
            citations,
        });
    }

    private static string CreateTempRoot() =>
        Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), "aegis-aar-" + Guid.NewGuid().ToString("N")))
            .FullName;

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

    private static string FindHostAarRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "ProjectAegis.Ai.Host", "Aar");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("ProjectAegis.Ai.Host/Aar not found");
    }
}
