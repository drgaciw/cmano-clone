using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

/// <summary>
/// BUG-missioncontacttargetclass-domain-filter-broken: catalog platform ids must
/// classify by <see cref="ICatalogReader.TryGetPlatformDomain"/>, not id-substring heuristics.
/// </summary>
public sealed class MissionContactTargetClassifierTests
{
    [Theory]
    [InlineData("f-219-sachsen-type-f124-2006", "surface")]
    [InlineData("k-31-visby-2009", "surface")]
    [InlineData("hostile-1", "surface")]
    [InlineData("tu-160-blackjack", "air")]
    [InlineData("jas-39e-gripen-ng-2021", "air")]
    [InlineData("ssn-774-virginia-blk-i-ii", "subsurface")]
    public void Classify_with_sqlite_catalog_reader_uses_platform_domain(string targetId, string domain)
    {
        var reader = CatalogReaderFactory.TryCreateBalticPatrolReader();
        if (reader is null)
        {
            return;
        }

        Assert.True(reader.TryGetPlatformDomain(targetId, out var actualDomain), $"missing catalog row for {targetId}");
        Assert.Equal(domain, actualDomain, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            MissionContactTargetClassifier.FromCatalogDomain(actualDomain),
            MissionContactTargetClassifier.Classify(targetId, reader));
    }

    [Theory]
    [InlineData("tu-160-blackjack")]
    [InlineData("jas-39e-gripen-ng-2021")]
    public void Classify_catalog_air_ids_as_air_only_when_reader_has_domain(string targetId)
    {
        var reader = CreateReader(
            new CatalogPlatformEntry(targetId, 0, 0, 100, Domain: "air"));

        Assert.Equal(MissionContactTargetClass.Air, MissionContactTargetClassifier.Classify(targetId, reader));
        Assert.True(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Air, targetId, reader));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Surface, targetId, reader));
    }

    [Theory]
    [InlineData("tu-160-blackjack")]
    [InlineData("jas-39e-gripen-ng-2021")]
    [InlineData("ssn-774-virginia-blk-i-ii")]
    public void Classify_without_catalog_row_does_not_infer_air_or_subsurface_from_id_substring(string targetId)
    {
        var reader = CreateReader();

        Assert.False(targetId.StartsWith("ucav", StringComparison.Ordinal));
        Assert.Equal(MissionContactTargetClass.Surface, MissionContactTargetClassifier.Classify(targetId, reader));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Air, targetId, reader));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Subsurface, targetId, reader));
    }

    [Theory]
    [InlineData("ssn-774-virginia-blk-i-ii")]
    [InlineData("pla-885-severodvinsk-yasen")]
    public void Classify_catalog_subsurface_ids_as_subsurface_when_reader_has_domain(string targetId)
    {
        var reader = CreateReader(
            new CatalogPlatformEntry(targetId, 0, 0, 100, Domain: "subsurface"));

        Assert.Equal(MissionContactTargetClass.Subsurface, MissionContactTargetClassifier.Classify(targetId, reader));
        Assert.True(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Subsurface, targetId, reader));
        Assert.False(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Air, targetId, reader));
    }

    [Theory]
    [InlineData("ucav-blue")]
    [InlineData("ucav-red")]
    public void Classify_legacy_ucav_prefix_still_air_on_catalog_miss(string targetId)
    {
        Assert.Equal(MissionContactTargetClass.Air, MissionContactTargetClassifier.Classify(targetId));
        Assert.Equal(MissionContactTargetClass.Air, MissionContactTargetClassifier.Classify(targetId, catalogReader: null));
        Assert.Equal(MissionContactTargetClass.Air, MissionContactTargetClassifier.Classify(targetId, CreateReader()));
    }

    [Theory]
    [InlineData("air", MissionContactTargetClass.Air)]
    [InlineData("aircraft", MissionContactTargetClass.Air)]
    [InlineData("subsurface", MissionContactTargetClass.Subsurface)]
    [InlineData("submarine", MissionContactTargetClass.Subsurface)]
    [InlineData("surface", MissionContactTargetClass.Surface)]
    [InlineData("ship", MissionContactTargetClass.Surface)]
    public void FromCatalogDomain_maps_catalog_domain_field(string domain, MissionContactTargetClass expected)
    {
        Assert.Equal(expected, MissionContactTargetClassifier.FromCatalogDomain(domain));
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

    [Fact]
    public void ToProfile_throws_on_unknown_target_class()
    {
        var dto = new ScenarioPolicyJsonDto
        {
            Id = "bad-target-class",
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
                        Id = "bad",
                        ObserverId = "u1",
                        TargetClass = "Helicopter",
                        Side = "friendly",
                        MissionCode = "ASW",
                        Roe = "WeaponsFree",
                        UnitIds = ["u1"],
                    },
                ],
            },
        };

        var ex = Assert.Throws<InvalidDataException>(() => ScenarioPolicyJsonLoader.ToProfile(dto));
        Assert.Contains("targetClass", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static InMemoryCatalogReader CreateReader(params CatalogPlatformEntry[] platforms) =>
        new([], platforms: platforms);
}
