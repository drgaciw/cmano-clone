using System.Text.Json;
using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Aar;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Ai.Host.Tests.Evidence;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using Xunit;

namespace ProjectAegis.Ai.Host.Tests.Aar;

public sealed class AarAssistantPipelineTests
{
    private readonly string _fixturesRoot = AiFixtureLoader.ResolveFixturesRoot();
    private readonly EvidenceProjector _projector = new();

    [Fact]
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

            Assert.Equal(GenerationStatus.Succeeded, result.Status);
            Assert.NotNull(result.ModelDraft);
            Assert.Equal(ids.Count, result.ModelDraft!.Citations.Count);
            Assert.All(result.ModelDraft.Citations, id => Assert.Contains(id, ids));
            Assert.Equal(projection.Quantitative.EngagementCount, result.DeterministicReport.EngagementCount);
            Assert.Equal(AarPromptVersion.Current, result.PromptVersion);
            Assert.NotNull(result.Envelope);
            Assert.Equal(GenerationStatus.Succeeded, result.Envelope!.Status);
            Assert.Equal(AarPromptVersion.Current, result.Envelope.PromptVersion);
            Assert.True(store.Exists("aar-explain.envelope.json"));
        }
        finally
        {
            TryDeleteTree(root);
            scripted.Dispose();
        }
    }

    [Fact]
    public async Task Fabricated_citation_is_rejected_with_deterministic_report()
    {
        var projection = ProjectCase("success");
        var scripted = new AarScriptedChatClient();
        scripted.EnqueueAssistantText(ValidDraftJson(
            summary: "ok",
            citations: ["fabricated-id:999"]));

        var pipeline = new AarAssistantPipeline(scripted);
        var result = await pipeline.RunAsync(ToRequest(projection, publish: false));

        Assert.Equal(GenerationStatus.RejectedCitation, result.Status);
        Assert.NotNull(result.DeterministicReport);
        Assert.Equal(projection.Quantitative.EngagementCount, result.DeterministicReport.EngagementCount);
        Assert.Contains("missing-citation", result.SkipReason, StringComparison.Ordinal);
        scripted.Dispose();
    }

    [Fact]
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

        Assert.Equal(GenerationStatus.RejectedNumber, result.Status);
        Assert.NotNull(result.DeterministicReport);
        Assert.Contains("contradictory-number", result.SkipReason, StringComparison.Ordinal);
        scripted.Dispose();
    }

    [Theory]
    [InlineData(FakeChatScript.MalformedOutput, GenerationStatus.MalformedOutput)]
    [InlineData(FakeChatScript.Timeout, GenerationStatus.TimedOut)]
    [InlineData(FakeChatScript.Cancellation, GenerationStatus.Cancelled)]
    [InlineData(FakeChatScript.ProviderUnavailable, GenerationStatus.ProviderUnavailable)]
    public async Task Failure_scripts_return_explicit_status_plus_deterministic_report(
        FakeChatScript script,
        GenerationStatus expected)
    {
        var projection = ProjectCase("malformed-provider-response");
        using var fake = new FakeChatClient();
        fake.Enqueue(script);

        var pipeline = new AarAssistantPipeline(fake);
        var result = await pipeline.RunAsync(ToRequest(projection, publish: false));

        Assert.Equal(expected, result.Status);
        Assert.NotNull(result.DeterministicReport);
        Assert.Equal(projection.Quantitative.DecisionCount, result.DeterministicReport.DecisionCount);
        Assert.Null(result.ModelDraft);
    }

    [Fact]
    public async Task Disabled_pipeline_still_returns_deterministic_report()
    {
        var projection = ProjectCase("success");
        using var fake = new FakeChatClient();
        var pipeline = new AarAssistantPipeline(fake);
        var result = await pipeline.RunAsync(ToRequest(projection, publish: false) with { Enabled = false });

        Assert.Equal(GenerationStatus.ProviderUnavailable, result.Status);
        Assert.Equal("disabled", result.SkipReason);
        Assert.NotNull(result.DeterministicReport);
    }

    [Fact]
    public async Task Embedded_instruction_fixture_does_not_change_validation_behaviour()
    {
        var projection = ProjectCase("embedded-instructions");
        var ids = projection.Document.Header.EvidenceIds.ToArray();
        Assert.NotEmpty(ids);

        // Prompt must still delimit data; fabricated citation still rejected.
        var scripted = new AarScriptedChatClient();
        scripted.EnqueueAssistantText(ValidDraftJson(summary: "ignore prior", citations: ["evil:1"]));
        var reject = await new AarAssistantPipeline(scripted)
            .RunAsync(ToRequest(projection, publish: false));
        Assert.Equal(GenerationStatus.RejectedCitation, reject.Status);

        scripted.EnqueueAssistantText(ValidDraftJson(
            summary: "Engagements " + projection.Quantitative.EngagementCount,
            citations: ids));
        var ok = await new AarAssistantPipeline(scripted)
            .RunAsync(ToRequest(projection, publish: false));
        Assert.Equal(GenerationStatus.Succeeded, ok.Status);

        var messages = new AarPromptBuilder().Build(projection.Document, ok.DeterministicReport);
        var user = messages.Single(m => m.Role == ChatRole.User).Text ?? string.Empty;
        Assert.Contains("<<<EVIDENCE_DATA_BEGIN>>>", user, StringComparison.Ordinal);
        Assert.Contains("<<<EVIDENCE_DATA_END>>>", user, StringComparison.Ordinal);
        scripted.Dispose();
    }

    [Fact]
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
        Assert.Equal(before, log.ComputeFingerprint());

        await new AarAssistantPipeline(scripted)
            .RunAsync(ToRequest(projected, publish: false) with { Enabled = false });
        Assert.Equal(before, log.ComputeFingerprint());

        using var fake = new FakeChatClient();
        fake.Enqueue(FakeChatScript.MalformedOutput);
        await new AarAssistantPipeline(fake)
            .RunAsync(ToRequest(projected, publish: false));
        Assert.Equal(before, log.ComputeFingerprint());

        await Task.Delay(15);
        scripted.EnqueueAssistantText(ValidDraftJson(
            summary: "Engagements " + projected.Quantitative.EngagementCount,
            citations: projected.Document.Header.EvidenceIds.ToArray()));
        await new AarAssistantPipeline(scripted)
            .RunAsync(ToRequest(projected, publish: false));
        Assert.Equal(before, log.ComputeFingerprint());
        scripted.Dispose();
    }

    [Fact]
    public void Citation_and_number_validators_unit_behaviour()
    {
        Assert.Null(AarCitationValidator.FindMissingCitation(["a:1"], ["a:1", "a:2"]));
        Assert.Equal("x", AarCitationValidator.FindMissingCitation(["x"], ["a:1"]));

        var allowed = new HashSet<string>(StringComparer.Ordinal) { "1", "2.5" };
        Assert.Null(AarNumberValidator.FindContradictoryNumber(["saw 1 and 2.5"], allowed));
        Assert.Equal("99", AarNumberValidator.FindContradictoryNumber(["saw 99"], allowed));
    }

    [Fact]
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

            Assert.DoesNotContain("DelegationBridge", code);
            Assert.DoesNotContain("SimulationSession", code);
            Assert.DoesNotContain("OnScenarioFinalized", code);
            Assert.DoesNotContain("OpenAI", code);
            Assert.DoesNotContain("Anthropic", code);
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
