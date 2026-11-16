using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using Xunit;

namespace ProjectAegis.Ai.LiveEval;

public sealed class FakeLiveEvalRunnerTests
{
    [Fact]
    public async Task Fake_run_succeeds_with_zero_cost_and_resolved_citations()
    {
        var document = SampleDocument();
        var quantitative = new EvidenceQuantitativeSummary(1, 0, 1, 0, null);
        var runner = new FakeLiveEvalRunner();
        var result = await runner.RunCaseAsync("success-fake", document, quantitative);

        Assert.Equal(GenerationStatus.Succeeded, result.Status);
        Assert.True(result.CitationsResolved);
        Assert.True(result.NumbersConsistent);
        Assert.Equal(0m, result.EstimatedCostUsd);
        Assert.Equal("fake", result.ProviderId);
        Assert.Equal("aar-explain-1", result.PromptVersion);
    }

    [Fact]
    public async Task Track_A_tagged_metadata_is_refused()
    {
        var document = SampleDocument();
        var quantitative = new EvidenceQuantitativeSummary(0, 0, 0, 0, null);
        var meta = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sourceClass"] = "trackA-cmo-db3",
        };
        var result = await new FakeLiveEvalRunner()
            .RunCaseAsync("track-a", document, quantitative, meta);

        Assert.Equal(GenerationStatus.ProviderUnavailable, result.Status);
        Assert.False(result.CitationsResolved);
        Assert.Contains("Track A", result.SkipReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0m, result.EstimatedCostUsd);
    }

    [Fact]
    public void Paid_mode_throws_without_owner_budget_path()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PaidProviderGate.EnsureFakeOrThrow(LiveEvalMode.PaidProvider));
        Assert.Contains("Paid LiveEval blocked", ex.Message, StringComparison.Ordinal);
        Assert.False(PaidProviderGate.TryAuthorizePaidMode(out var reason));
        Assert.Contains("AEGIS_LIVE_EVAL_ALLOW_PAID", reason, StringComparison.Ordinal);
    }

    private static EvidenceDocument SampleDocument() =>
        new(
            new EvidenceHeader(
                AiContractSchema.Version,
                "build-live",
                "scenario-live",
                "cat-1",
                "digestfp",
                ViewingSide.Blue,
                DebriefMode: false,
                EvidenceIds: ["fp:1"]),
            [
                new EvidenceEntry(
                    "fp:1",
                    1,
                    EvidenceEntryKind.Engagement,
                    1.0,
                    ViewingSide.Blue,
                    "u1",
                    "engaged",
                    null),
            ]);
}
