using ProjectAegis.Ai.Host.Authoring;
using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Ai.Host.Tests.Authoring;

/// <summary>DRG-740 acceptance against tests/ai-fixtures/authoring (fake only).</summary>
[TestFixture]
public sealed class AuthoringProposePipelineFixtureTests
{
    [TestCase("valid-draft")]
    [TestCase("unknown-ids")]
    [TestCase("ambiguous-request")]
    [TestCase("unsupported-role")]
    [TestCase("duplicate-op")]
    [TestCase("stale-revision")]
    [TestCase("embedded-instructions")]
    public void Fixture_yields_expected_outcome(string caseId)
    {
        var (request, draft, expected) = AuthoringFixtureLoader.Load(caseId);
        Assert.That(request.IsRetry, Is.EqualTo(string.Equals(caseId, "duplicate-op", StringComparison.Ordinal)));

        var pipeline = new AuthoringProposePipeline();
        var result = pipeline.Propose(request, draft);

        Assert.That(result.OutcomeClass.ToString(), Is.EqualTo(expected.OutcomeClass));
        Assert.That(result.SubmitAllowedWithoutHuman, Is.EqualTo(expected.SubmitAllowedWithoutHuman));
        Assert.That(result.SubmitAllowedWithoutHuman, Is.False);
        // Always read ProvenanceSource so NotAccessedPositionalProperty stays clear.
        var expectedProvenance = expected.ProvenanceSource;
        Assert.That(result.Metrics, Is.Not.Null);
        Assert.That(result.Metrics.ValidDraftCount >= 0, Is.True);
        Assert.That(result.Metrics.DiagnosticCount >= 0, Is.True);
        Assert.That(result.Metrics.ConflictCount >= 0, Is.True);
        Assert.That(result.Metrics.IdempotentNoOpCount >= 0, Is.True);

        if (expected.ConflictCode is not null)
        {
            Assert.That(result.ConflictCode, Is.EqualTo(expected.ConflictCode));
        }

        if (expected.Diagnostics.Count > 0)
        {
            foreach (var d in expected.Diagnostics)
            {
                Assert.That(result.Diagnostics.Any(x => string.Equals(x.Code, d.Code, StringComparison.Ordinal)
                         && (d.MessageContains is null
                             || x.Message.Contains(d.MessageContains, StringComparison.OrdinalIgnoreCase))), Is.True);
            }
        }

        if (string.Equals(expected.OutcomeClass, "ValidDraft", StringComparison.Ordinal))
        {
            Assert.That(result.DraftDocument, Is.Not.Null);
            Assert.That(result.ValidationReport, Is.Not.Null);
            Assert.That(result.Metrics.ValidDraftCount, Is.EqualTo(1));
            Assert.That(result.ProvenanceTags.Single().Source, Is.EqualTo("ai"));
            Assert.That(result.ProvenanceTags.Single().Source, Is.EqualTo(expectedProvenance ?? "ai"));
            Assert.That(result.TransparencyLabel, Is.EqualTo(AuthoringTransparencyLabel.AgentAssisted));
            if (expected.Missions is not null)
            {
                foreach (var m in expected.Missions)
                {
                    Assert.That(result.Missions, Does.Contain(m));
                }
            }

            if (expected.MustNotContain is { Count: > 0 })
            {
                var blob = System.Text.Json.JsonSerializer.Serialize(result.DraftDocument);
                foreach (var fragment in expected.MustNotContain)
                {
                    Assert.That(blob, Does.Not.Contain(fragment));
                }
            }
        }
        else
        {
            Assert.That(result.DraftDocument, Is.Null);
            Assert.That(result.Missions, Is.Empty);
            Assert.That(expectedProvenance, Is.Null);
            if (result.OutcomeClass == AuthoringOutcomeClass.Diagnostics)
            {
                Assert.That(result.Metrics.DiagnosticCount >= 1, Is.True);
            }
            else if (result.OutcomeClass == AuthoringOutcomeClass.Conflict)
            {
                Assert.That(result.Metrics.ConflictCount, Is.EqualTo(1));
            }
            else if (result.OutcomeClass == AuthoringOutcomeClass.IdempotentNoOp)
            {
                Assert.That(result.Metrics.IdempotentNoOpCount, Is.EqualTo(1));
            }
        }

        if (expected.MissionCountDelta is 0)
        {
            Assert.That(result.OutcomeClass, Is.EqualTo(AuthoringOutcomeClass.IdempotentNoOp));
        }
    }

    [Test]
    public void Accepted_proposal_retry_is_idempotent_without_second_apply()
    {
        var (request, draft, _) = AuthoringFixtureLoader.Load("valid-draft");
        var store = new AuthoringIdempotencyStore();
        var pipeline = new AuthoringProposePipeline(idempotency: store);

        var first = pipeline.Propose(request, draft);
        Assert.That(first.OutcomeClass, Is.EqualTo(AuthoringOutcomeClass.ValidDraft));
        Assert.That(first.Metrics.ValidDraftCount, Is.EqualTo(1));
        Assert.That(first.ValidationReport, Is.Not.Null);

        var second = pipeline.Propose(request, draft);
        Assert.That(second.OutcomeClass, Is.EqualTo(AuthoringOutcomeClass.IdempotentNoOp));
        Assert.That(second.Metrics.IdempotentNoOpCount, Is.EqualTo(1));
        Assert.That(second.Diagnostics.Any(d => d.Code == "DUPLICATE_IDEMPOTENCY_KEY"), Is.True);
    }

    [Test]
    public void Keyword_stub_baseline_still_available_unmodified()
    {
        // Pin: AiAuthoringServices remains the Data-layer fallback; Host does not replace it.
        var stub = AiAuthoringServices.NlScaffold(
            "Blue forces patrol the Baltic strait chokepoint");
        Assert.That(stub.Missions, Does.Contain("patrol-blue-1"));
        Assert.That(stub.Explanation, Does.StartWith("Scaffolded from NL brief using keyword parse."));
    }
}
