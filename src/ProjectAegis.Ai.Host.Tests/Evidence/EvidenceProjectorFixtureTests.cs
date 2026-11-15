using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using Xunit;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

public sealed class EvidenceProjectorFixtureTests
{
    private readonly EvidenceProjector _projector = new();
    private readonly string _fixturesRoot = AiFixtureLoader.ResolveFixturesRoot();

    [Fact]
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

            Assert.Equal(expected, result.Document.Header.EvidenceIds);
            Assert.Equal(expected, result.Document.Entries.Select(e => e.EvidenceId).ToArray());
        }
    }

    [Fact]
    public void Two_sided_hidden_blue_view_excludes_red_hidden_rows()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "two-sided-hidden");
        var result = Project(fixture, ViewingSide.Blue, debrief: false);

        Assert.Equal(fixture.BlueVisibleIds, result.Document.Header.EvidenceIds);
        Assert.DoesNotContain("aifix-twoside:2", result.Document.Header.EvidenceIds);
        Assert.False(result.Document.Header.DebriefMode);
    }

    [Fact]
    public void Two_sided_hidden_debrief_includes_all_and_sets_flag()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "two-sided-hidden");
        Assert.True(fixture.DebriefRevealsAll);

        var result = Project(fixture, ViewingSide.Blue, debrief: true);

        Assert.Equal(fixture.ExpectedIds, result.Document.Header.EvidenceIds);
        Assert.True(result.Document.Header.DebriefMode);
        Assert.Contains("aifix-twoside:2", result.Document.Header.EvidenceIds);
    }

    [Fact]
    public void Identical_input_yields_identical_ordered_evidence()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "success");
        var a = Project(fixture, ViewingSide.Blue, debrief: false);
        var b = Project(fixture, ViewingSide.Blue, debrief: false);

        Assert.Equal(a.Document.Header.EvidenceIds, b.Document.Header.EvidenceIds);
        Assert.Equal(
            a.Document.Entries.Select(e => (e.EvidenceId, e.SequenceId, e.Kind, e.SimTime)),
            b.Document.Entries.Select(e => (e.EvidenceId, e.SequenceId, e.Kind, e.SimTime)));
    }

    [Fact]
    public void Empty_evidence_fixture_returns_empty_ids()
    {
        var fixture = AiFixtureLoader.LoadCase(_fixturesRoot, "empty-evidence");
        var result = Project(fixture, ViewingSide.Blue, debrief: false);

        Assert.Empty(result.Document.Header.EvidenceIds);
        Assert.Empty(result.Document.Entries);
        Assert.Equal(0, result.Quantitative.EngagementCount);
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
