using NUnit.Framework;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;

namespace ProjectAegis.Ai.LiveEval;

public sealed class FakeLiveEvalRunnerTests
{
    [Test]
    public async Task Fake_run_succeeds_with_zero_cost_and_resolved_citations()
    {
        var document = SampleDocument();
        var quantitative = new EvidenceQuantitativeSummary(1, 0, 1, 0, null);
        var runner = new FakeLiveEvalRunner();
        var result = await runner.RunCaseAsync("success-fake", document, quantitative);

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.Succeeded));
        Assert.That(result.CitationsResolved, Is.True);
        Assert.That(result.NumbersConsistent, Is.True);
        Assert.That(result.EstimatedCostUsd, Is.EqualTo(0m));
        Assert.That(result.ProviderId, Is.EqualTo("fake"));
        Assert.That(result.PromptVersion, Is.EqualTo("aar-explain-1"));
    }

    [Test]
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

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.ProviderUnavailable));
        Assert.That(result.CitationsResolved, Is.False);
        Assert.That(result.SkipReason, Does.Contain("Track A").IgnoreCase);
        Assert.That(result.EstimatedCostUsd, Is.EqualTo(0m));
    }

    [Test]
    public void Paid_mode_throws_without_owner_budget_path()
    {
        Assert.That(
            () => PaidProviderGate.EnsureFakeOrThrow(LiveEvalMode.PaidProvider),
            Throws.InvalidOperationException.With.Message.Contains("Paid LiveEval blocked"));
        Assert.That(PaidProviderGate.TryAuthorizePaidMode(out var reason), Is.False);
        Assert.That(reason, Does.Contain("AEGIS_LIVE_EVAL_ALLOW_PAID"));
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
