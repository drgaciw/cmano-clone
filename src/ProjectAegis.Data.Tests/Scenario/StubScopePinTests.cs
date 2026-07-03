namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

/// <summary>
/// Pins the CURRENT demonstrative ("stub") behavior of four scenario-editor
/// features. These are documented as placeholders, not real analysis engines
/// (see production/qa/qa-plan-scenario-editor-2026-07-01.md unit #17 and
/// doc 11). These tests assert the EXACT current output shape — not
/// correctness — so a future refactor cannot silently regress the stub's
/// observable contract, and cannot silently start doing real
/// analysis/NL-understanding/LLM calls without a corresponding doc update.
/// If any of these tests need to change, that change should be deliberate
/// and visible in review, paired with a doc update.
/// </summary>
public sealed class StubScopePinTests
{
    // ---------------------------------------------------------------
    // Stub 1: TCA static-analysis — ScenarioDocumentEditor.AnalyzeTcaGraph
    // ---------------------------------------------------------------

    [Fact]
    public void AnalyzeTcaGraph_dead_count_is_just_a_relabeled_MISSION_NO_UNITS_finding_count()
    {
        var editor = ScenarioDocumentEditor.CreateNew();

        // m1 has no assigned units -> triggers MISSION_NO_UNITS in the real
        // validation engine. m2/m3 are clean so they don't add noise to the
        // counted buckets (dead/unreachable/contradictory/circular).
        editor.AddPatrolMission("m1", Array.Empty<string>(), new[]
        {
            new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
        });
        editor.AddStrikeMission("m2", new[] { "u1" }, new[] { "t1" });
        editor.AddFerryMission("m3", new[] { "u1" }, "base-x");

        var liveReport = editor.LiveValidate();
        Assert.Contains(liveReport.Findings, f => f.Code == "MISSION_NO_UNITS" && f.MissionId == "m1");

        var result = editor.AnalyzeTcaGraph();

        // "dead=1" is not independent dead-trigger detection: it is exactly
        // the MISSION_NO_UNITS finding count from the ordinary validation
        // engine, relabeled under a new name.
        Assert.Equal(
            "TCA static analysis: dead=1 unreachable=0 contradictory=0 circular=0; " +
            "graph nodes=[m1,m2,m3] edges=[m1->m2;m2->m3] (no cycles)",
            result);
    }

    [Fact]
    public void AnalyzeTcaGraph_edges_are_a_trivial_sequential_chain_not_a_real_dependency_graph()
    {
        var editor = ScenarioDocumentEditor.CreateNew();

        // Three missions with zero real event/dependency relationship between
        // them: different types, disjoint units, disjoint targets.
        editor.AddStrikeMission("alpha", new[] { "u1" }, new[] { "t1" });
        editor.AddFerryMission("bravo", new[] { "u2" }, "base-x");
        editor.AddStrikeMission("charlie", new[] { "u3" }, new[] { "t2" });

        var result = editor.AnalyzeTcaGraph();

        // Edges are always an insertion-order sequential chain regardless of
        // the (non-existent) real dependency analysis between these missions.
        Assert.Contains("graph nodes=[alpha,bravo,charlie]", result);
        Assert.Contains("edges=[alpha->bravo;bravo->charlie]", result);
    }

    // ---------------------------------------------------------------
    // Stub 2: AI scaffold / NL stub — AiAuthoringServices.NlScaffold
    // ---------------------------------------------------------------

    [Fact]
    public void NlScaffold_patrol_keyword_deterministically_maps_to_patrol_mission()
    {
        var result = AiAuthoringServices.NlScaffold("Blue forces patrol the Baltic strait chokepoint");

        Assert.Contains("patrol-blue-1", result.Missions);
        // Pin the "keyword parse" self-description — proves this is
        // deterministic keyword matching, not an LLM call.
        Assert.StartsWith("Scaffolded from NL brief using keyword parse.", result.Explanation);
    }

    [Fact]
    public void NlScaffold_brief_with_no_recognized_keywords_falls_back_to_hardcoded_patrol_default()
    {
        // No occurrence of any recognized keyword (patrol, defend, baltic,
        // strike, attack, offense, support, tanker, red, opfor).
        var result = AiAuthoringServices.NlScaffold("zzz qqq foobar nonsense text");

        // Guards doc 11 §9's promise that v1 has "no LLM in any blocking or
        // non-blocking authoring path": a real NL-understanding system would
        // not need a hardcoded fallback mission for unrecognized input.
        Assert.Single(result.Missions);
        Assert.Contains("patrol-default", result.Missions);
        Assert.DoesNotContain("patrol-blue-1", result.Missions);
    }

    // ---------------------------------------------------------------
    // Stub 3: Model-integrity demo rules — IncompatibleHostRule / BrokenRefRule
    // (IncompatibleHostRule/BrokenRefRule are `internal`; exercised here
    // through the public ScenarioValidationEngine, matching the idiom in
    // ScenarioValidationEngineTests.cs)
    // ---------------------------------------------------------------

    [Fact]
    public void IncompatibleHostRule_fires_on_air_substring_unit_when_no_carrier_unit_anywhere()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["air-unit-1"],
                    TargetIds = ["t1"],
                },
            ],
        };

        var report = new ScenarioValidationEngine().Validate(
            scenario, InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());

        // Substring match on "air", not real platform-compatibility modeling.
        Assert.Contains(report.Findings, f => f.Code == "INCOMPATIBLE_HOST" && f.UnitId == "air-unit-1");
    }

    [Fact]
    public void IncompatibleHostRule_does_not_fire_when_a_literal_carrier_unit_is_present()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["airbase-1"], // still contains the substring "air"
                    TargetIds = ["t1"],
                },
                new ScenarioMissionDto
                {
                    Id = "patrol-1",
                    Type = "Patrol",
                    AssignedUnitIds = ["carrier"], // literal id match required
                    PatrolZone =
                    [
                        new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                        new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                        new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                    ],
                },
            ],
        };

        var report = new ScenarioValidationEngine().Validate(
            scenario, InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());

        // Presence of a mission with the literal unit id "carrier" anywhere
        // in the scenario suppresses the finding, even though "airbase-1"
        // still contains "air" — proving this is naive substring/id matching.
        Assert.DoesNotContain(report.Findings, f => f.Code == "INCOMPATIBLE_HOST");
    }

    [Fact]
    public void BrokenRefRule_fires_only_on_literal_ref_colon_prefix_naming_convention()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["u1"],
                    TargetIds = ["ref:missing-unit"],
                },
            ],
        };

        var report = new ScenarioValidationEngine().Validate(
            scenario, InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());

        Assert.Contains(report.Findings, f => f.Code == "BROKEN_REF" && f.MissionId == "strike-1");
    }

    [Fact]
    public void BrokenRefRule_does_not_fire_on_a_dangling_reference_lacking_the_ref_prefix()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["u1"],
                    TargetIds = ["some-other-id"], // also dangling, but lacks the "ref:" prefix
                },
            ],
        };

        var report = new ScenarioValidationEngine().Validate(
            scenario, InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());

        // Not a general referential-integrity check — only the "ref:" naming
        // convention is caught, so this equally-dangling target id is missed.
        Assert.DoesNotContain(report.Findings, f => f.Code == "BROKEN_REF");
    }
}
