using ProjectAegis.Ai.Host.Authoring;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Ai.Host.Tests.Authoring;

/// <summary>DRG-740 acceptance against tests/ai-fixtures/authoring (fake only).</summary>
public sealed class AuthoringProposePipelineFixtureTests
{
    [Theory]
    [InlineData("valid-draft")]
    [InlineData("unknown-ids")]
    [InlineData("ambiguous-request")]
    [InlineData("unsupported-role")]
    [InlineData("duplicate-op")]
    [InlineData("stale-revision")]
    [InlineData("embedded-instructions")]
    public void Fixture_yields_expected_outcome(string caseId)
    {
        var (request, draft, expected) = AuthoringFixtureLoader.Load(caseId);
        Assert.Equal(string.Equals(caseId, "duplicate-op", StringComparison.Ordinal), request.IsRetry);

        var pipeline = new AuthoringProposePipeline();
        var result = pipeline.Propose(request, draft);

        Assert.Equal(expected.OutcomeClass, result.OutcomeClass.ToString());
        Assert.Equal(expected.SubmitAllowedWithoutHuman, result.SubmitAllowedWithoutHuman);
        Assert.False(result.SubmitAllowedWithoutHuman);
        // Always read ProvenanceSource so NotAccessedPositionalProperty stays clear.
        var expectedProvenance = expected.ProvenanceSource;
        Assert.NotNull(result.Metrics);
        Assert.True(result.Metrics.ValidDraftCount >= 0);
        Assert.True(result.Metrics.DiagnosticCount >= 0);
        Assert.True(result.Metrics.ConflictCount >= 0);
        Assert.True(result.Metrics.IdempotentNoOpCount >= 0);

        if (expected.ConflictCode is not null)
        {
            Assert.Equal(expected.ConflictCode, result.ConflictCode);
        }

        if (expected.Diagnostics.Count > 0)
        {
            foreach (var d in expected.Diagnostics)
            {
                Assert.Contains(
                    result.Diagnostics,
                    x => string.Equals(x.Code, d.Code, StringComparison.Ordinal)
                         && (d.MessageContains is null
                             || x.Message.Contains(d.MessageContains, StringComparison.OrdinalIgnoreCase)));
            }
        }

        if (string.Equals(expected.OutcomeClass, "ValidDraft", StringComparison.Ordinal))
        {
            Assert.NotNull(result.DraftDocument);
            Assert.NotNull(result.ValidationReport);
            Assert.Equal(1, result.Metrics.ValidDraftCount);
            Assert.Equal("ai", result.ProvenanceTags.Single().Source);
            Assert.Equal(expectedProvenance ?? "ai", result.ProvenanceTags.Single().Source);
            Assert.Equal(AuthoringTransparencyLabel.AgentAssisted, result.TransparencyLabel);
            if (expected.Missions is not null)
            {
                foreach (var m in expected.Missions)
                {
                    Assert.Contains(m, result.Missions);
                }
            }

            if (expected.MustNotContain is { Count: > 0 })
            {
                var blob = System.Text.Json.JsonSerializer.Serialize(result.DraftDocument);
                foreach (var fragment in expected.MustNotContain)
                {
                    Assert.DoesNotContain(fragment, blob, StringComparison.Ordinal);
                }
            }
        }
        else
        {
            Assert.Null(result.DraftDocument);
            Assert.Empty(result.Missions);
            Assert.Null(expectedProvenance);
            if (result.OutcomeClass == AuthoringOutcomeClass.Diagnostics)
            {
                Assert.True(result.Metrics.DiagnosticCount >= 1);
            }
            else if (result.OutcomeClass == AuthoringOutcomeClass.Conflict)
            {
                Assert.Equal(1, result.Metrics.ConflictCount);
            }
            else if (result.OutcomeClass == AuthoringOutcomeClass.IdempotentNoOp)
            {
                Assert.Equal(1, result.Metrics.IdempotentNoOpCount);
            }
        }

        if (expected.MissionCountDelta is 0)
        {
            Assert.Equal(AuthoringOutcomeClass.IdempotentNoOp, result.OutcomeClass);
        }
    }

    [Fact]
    public void Accepted_proposal_retry_is_idempotent_without_second_apply()
    {
        var (request, draft, _) = AuthoringFixtureLoader.Load("valid-draft");
        var store = new AuthoringIdempotencyStore();
        var pipeline = new AuthoringProposePipeline(idempotency: store);

        var first = pipeline.Propose(request, draft);
        Assert.Equal(AuthoringOutcomeClass.ValidDraft, first.OutcomeClass);
        Assert.Equal(1, first.Metrics.ValidDraftCount);
        Assert.NotNull(first.ValidationReport);

        var second = pipeline.Propose(request, draft);
        Assert.Equal(AuthoringOutcomeClass.IdempotentNoOp, second.OutcomeClass);
        Assert.Equal(1, second.Metrics.IdempotentNoOpCount);
        Assert.Contains(second.Diagnostics, d => d.Code == "DUPLICATE_IDEMPOTENCY_KEY");
    }

    [Fact]
    public void Keyword_stub_baseline_still_available_unmodified()
    {
        // Pin: AiAuthoringServices remains the Data-layer fallback; Host does not replace it.
        var stub = AiAuthoringServices.NlScaffold(
            "Blue forces patrol the Baltic strait chokepoint");
        Assert.Contains("patrol-blue-1", stub.Missions);
        Assert.StartsWith("Scaffolded from NL brief using keyword parse.", stub.Explanation);
    }
}
