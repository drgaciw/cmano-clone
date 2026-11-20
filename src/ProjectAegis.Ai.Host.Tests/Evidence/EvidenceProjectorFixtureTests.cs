using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using NUnit.Framework;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

[TestFixture]
public sealed class EvidenceProjectorFixtureTests
{
    private readonly EvidenceProjector _projector = new();
    private readonly string _fixturesRoot = AiFixtureLoader.ResolveFixturesRoot();

    [Test]
    public void Every_fixture_expected_evidence_ids_match_exactly()
    {
        foreach (var caseId in AiFixtureLoader.ListCaseIds(_fixturesRoot))
        {
            var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, caseId);
            var result = Project(fixture, fixture.DefaultViewingSide, debrief: false);

            // Default player view: use blueVisibleIds when the case encodes a side filter.
            var expected = fixture.BlueVisibleIds is { Count: > 0 }
                ? fixture.BlueVisibleIds
                : fixture.ExpectedIds;

            Assert.That(result.Document.Header.EvidenceIds, Is.EqualTo(expected));
            Assert.That(result.Document.Entries.Select(e => e.EvidenceId).ToArray(), Is.EqualTo(expected));
        }
    }

    [Test]
    public void Two_sided_hidden_blue_view_excludes_red_hidden_rows()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "two-sided-hidden");
        var result = Project(fixture, ViewingSide.Blue, debrief: false);

        Assert.That(result.Document.Header.EvidenceIds, Is.EqualTo(fixture.BlueVisibleIds));
        Assert.That(result.Document.Header.EvidenceIds, Does.Not.Contain("aifix-twoside:2"));
        Assert.That(result.Document.Header.DebriefMode, Is.False);
    }

    [Test]
    public void Two_sided_hidden_debrief_includes_all_and_sets_flag()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "two-sided-hidden");
        Assert.That(fixture.DebriefRevealsAll, Is.True);

        var result = Project(fixture, ViewingSide.Blue, debrief: true);

        Assert.That(result.Document.Header.EvidenceIds, Is.EqualTo(fixture.ExpectedIds));
        Assert.That(result.Document.Header.DebriefMode, Is.True);
        Assert.That(result.Document.Header.EvidenceIds, Does.Contain("aifix-twoside:2"));
    }

    [Test]
    public void Identical_input_yields_identical_ordered_evidence()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "success");
        var a = Project(fixture, ViewingSide.Blue, debrief: false);
        var b = Project(fixture, ViewingSide.Blue, debrief: false);

        Assert.That(b.Document.Header.EvidenceIds, Is.EqualTo(a.Document.Header.EvidenceIds));
        Assert.That(b.Document.Entries.Select(e => (e.EvidenceId, e.SequenceId, e.Kind, e.SimTime)), Is.EqualTo(a.Document.Entries.Select(e => (e.EvidenceId, e.SequenceId, e.Kind, e.SimTime))));
    }

    [Test]
    public void Empty_evidence_fixture_returns_empty_ids()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "empty-evidence");
        var result = Project(fixture, ViewingSide.Blue, debrief: false);

        Assert.That(result.Document.Header.EvidenceIds, Is.Empty);
        Assert.That(result.Document.Entries, Is.Empty);
        Assert.That(result.Quantitative.DecisionCount, Is.EqualTo(0));
        Assert.That(result.Quantitative.PolicyDenialCount, Is.EqualTo(0));
        Assert.That(result.Quantitative.EngagementCount, Is.EqualTo(0));
        Assert.That(result.Quantitative.ControllerChangeCount, Is.EqualTo(0));
        Assert.That(result.Quantitative.ObjectivesRatio, Is.Null);
        Assert.That(result.AarSection, Is.Not.Null);
        Assert.That(result.AarSection!.Rows, Is.Empty);
        Assert.That(result.AuthoritativeFacts, Is.Not.Null);
    }

    [Test]
    public void Success_fixture_exposes_quantitative_and_facts()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "success");
        var result = Project(fixture, ViewingSide.Blue, debrief: false);

        Assert.That(result.Quantitative.DecisionCount, Is.EqualTo(0));
        Assert.That(result.Quantitative.PolicyDenialCount, Is.EqualTo(0));
        Assert.That(result.Quantitative.EngagementCount, Is.EqualTo(1));
        Assert.That(result.Quantitative.ControllerChangeCount, Is.EqualTo(1));
        Assert.That(result.AarSection, Is.Not.Null);
        Assert.That(result.AuthoritativeFacts, Is.Not.Null);
        Assert.That(result.AuthoritativeFacts, Is.EqualTo(fixture.Facts));
    }

    [Test]
    public void Policy_denials_fixture_counts_denials()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "policy-denials");
        var result = Project(fixture, ViewingSide.Blue, debrief: false);

        Assert.That(result.Quantitative.PolicyDenialCount, Is.EqualTo(2));
        Assert.That(result.Document.Entries.Count, Is.EqualTo(2));
        Assert.That(result.AarSection, Is.Not.Null);
        Assert.That(result.AuthoritativeFacts, Is.Not.Null);
    }

    private EvidenceProjectionResult Project(
        LoadedAiFixture fixture,
        ViewingSide viewingSide,
        bool debrief)
    {
        var request = new EvidenceProjectionRequest(
            BuildIdentity: "test-build",
            ScenarioId: fixture.CaseId,
            CatalogRevision: "fixture-catalog",
            ViewingSide: viewingSide,
            DebriefMode: debrief,
            FingerprintPrefix: fixture.FingerprintPrefix,
            AuthoritativeFacts: fixture.Facts);

        return _projector.Project(fixture.Log, request, fixture.Annotations);
    }
}
