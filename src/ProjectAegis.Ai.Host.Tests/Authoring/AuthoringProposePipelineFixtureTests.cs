using ProjectAegis.Ai.Host.Authoring;
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
        var pipeline = new AuthoringProposePipeline();

        var result = pipeline.Propose(request, draft);

        Assert.Equal(expected.OutcomeClass, result.OutcomeClass.ToString());
        Assert.False(result.SubmitAllowedWithoutHuman);

        if (expected.ConflictCode is not null)
        {
            Assert.Equal(expected.ConflictCode, result.ConflictCode);
        }

        if (expected.Diagnostics is { Count: > 0 })
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
            Assert.Equal("ai", result.ProvenanceTags.Single().Source);
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

        var second = pipeline.Propose(request, draft);
        Assert.Equal(AuthoringOutcomeClass.IdempotentNoOp, second.OutcomeClass);
        Assert.Contains(second.Diagnostics, d => d.Code == "DUPLICATE_IDEMPOTENCY_KEY");
    }

    [Fact]
    public void Keyword_stub_baseline_still_available_unmodified()
    {
        // Pin: AiAuthoringServices remains the Data-layer fallback; Host does not replace it.
        var stub = ProjectAegis.Data.Scenario.Authoring.AiAuthoringServices.NlScaffold(
            "Blue forces patrol the Baltic strait chokepoint");
        Assert.Contains("patrol-blue-1", stub.Missions);
        Assert.StartsWith("Scaffolded from NL brief using keyword parse.", stub.Explanation);
    }
}
