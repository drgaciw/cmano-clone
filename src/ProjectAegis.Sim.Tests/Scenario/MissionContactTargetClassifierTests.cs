using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

/// <summary>
/// BUG-missioncontacttargetclass-domain-filter-broken: catalog platform ids must
/// classify by domain, not the ucav-prefix heuristic.
/// </summary>
public sealed class MissionContactTargetClassifierTests
{
    [Theory]
    [InlineData("f-219-sachsen-type-f124-2006")]
    [InlineData("k-31-visby-2009")]
    [InlineData("skr-admiral-grigorovich-pr-1135-6m")]
    [InlineData("hostile-1")]
    public void Classify_catalog_surface_ids_as_surface(string targetId)
    {
        Assert.Equal(MissionContactTargetClass.Surface, MissionContactTargetClassifier.Classify(targetId));
        Assert.True(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Surface, targetId));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Air, targetId));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Subsurface, targetId));
    }

    [Theory]
    [InlineData("tu-160-blackjack")]
    [InlineData("jas-39e-gripen-ng-2021")]
    [InlineData("f-16c-blk-52iq-falcon-2015")]
    [InlineData("mig-29-fulcrum-c-1999")]
    [InlineData("ka-27m-helix-a")]
    [InlineData("eurofighter-typhoon")]
    [InlineData("su-27sm-sm3-flanker-j-2013")]
    public void Classify_catalog_air_ids_as_air_without_ucav_prefix(string targetId)
    {
        // origin/main heuristic: StartsWith("ucav") → Air, else Surface. These ids must not
        // depend on that prefix or Air filters stay permanently false.
        Assert.False(targetId.StartsWith("ucav", StringComparison.Ordinal));
        Assert.Equal(MissionContactTargetClass.Air, MissionContactTargetClassifier.Classify(targetId));
        Assert.True(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Air, targetId));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Surface, targetId));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Subsurface, targetId));
    }

    [Fact]
    public void Classify_legacy_ucav_prefix_still_air_for_baltic_v3_goldens()
    {
        Assert.Equal(MissionContactTargetClass.Air, MissionContactTargetClassifier.Classify("ucav-blue"));
        Assert.Equal(MissionContactTargetClass.Air, MissionContactTargetClassifier.Classify("ucav-red"));
    }

    [Theory]
    [InlineData("ssn-774-virginia-blk-i-ii")]
    [InlineData("pla-885-severodvinsk-yasen")]
    [InlineData("a-19-gotland-2022")]
    [InlineData("kilo-pr-636")]
    public void Classify_catalog_subsurface_ids_as_subsurface_not_surface_or_any(string targetId)
    {
        Assert.False(targetId.StartsWith("ucav", StringComparison.Ordinal));
        Assert.Equal(MissionContactTargetClass.Subsurface, MissionContactTargetClassifier.Classify(targetId));
        Assert.True(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Subsurface, targetId));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Surface, targetId));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Air, targetId));
        Assert.True(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Any, targetId));
    }

    [Fact]
    public void ToProfile_parses_subsurface_target_class_instead_of_falling_through_to_any()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "asw-contact",
            FriendlyRoe = "WeaponsTight",
            OpposingRoe = "WeaponsFree",
            Mission = new ScenarioMissionJsonDto
            {
                Events =
                [
                    new ScenarioMissionEventJsonDto
                    {
                        Id = "mission-start",
                        FireAtTick = 0,
                        Kind = "MissionTransition",
                        Code = "Execution",
                    },
                ],
                Triggers =
                [
                    new ScenarioMissionContactTriggerJsonDto
                    {
                        Id = "asw-contact-roe",
                        ObserverId = "ssn-774-virginia-blk-i-ii",
                        TargetClass = "Subsurface",
                        Side = "friendly",
                        MissionCode = "ASW",
                        Roe = "WeaponsFree",
                        UnitIds = ["ssn-774-virginia-blk-i-ii"],
                    },
                ],
            },
        });

        Assert.NotNull(profile.MissionTimeline);
        var trigger = Assert.Single(profile.MissionTimeline!.ContactTriggers);
        Assert.Equal(MissionContactTargetClass.Subsurface, trigger.TargetClass);
        Assert.NotEqual(MissionContactTargetClass.Any, trigger.TargetClass);
    }
}
