using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Scenario;

/// <summary>
/// BUG-missioncontacttargetclass-domain-filter-broken: catalog platform ids must
/// classify by <see cref="ICatalogReader.TryGetPlatformDomain"/>, not id-substring heuristics.
/// </summary>
public sealed class MissionContactTargetClassifierTests
{

    [TestCase("f-219-sachsen-type-f124-2006", "surface")]
    [TestCase("k-31-visby-2009", "surface")]
    [TestCase("hostile-1", "surface")]
    [TestCase("tu-160-blackjack", "air")]
    [TestCase("jas-39e-gripen-ng-2021", "air")]
    [TestCase("ssn-774-virginia-blk-i-ii", "subsurface")]
    public void Classify_with_sqlite_catalog_reader_uses_platform_domain(string targetId, string domain)
    {
        var reader = CatalogReaderFactory.TryCreateBalticPatrolReader();
        if (reader is null)
        {
            return;
        }

        Assert.That(reader.TryGetPlatformDomain(targetId, out var actualDomain), Is.True, $"missing catalog row for {targetId}");
        Assert.That(actualDomain, Is.EqualTo(domain).IgnoreCase);
        Assert.That(
            MissionContactTargetClassifier.Classify(targetId, reader),
            Is.EqualTo(MissionContactTargetClassifier.FromCatalogDomain(actualDomain)));
    }

    [TestCase("tu-160-blackjack")]
    [TestCase("jas-39e-gripen-ng-2021")]
    public void Classify_catalog_air_ids_as_air_only_when_reader_has_domain(string targetId)
    {
        var reader = CreateReader(
            new CatalogPlatformEntry(targetId, 0, 0, 100, Domain: "air"));

        Assert.That(MissionContactTargetClassifier.Classify(targetId, reader), Is.EqualTo(MissionContactTargetClass.Air));
        Assert.That(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Air, targetId, reader), Is.True);
        Assert.That(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Surface, targetId, reader), Is.False);
    }

    [TestCase("tu-160-blackjack")]
    [TestCase("jas-39e-gripen-ng-2021")]
    [TestCase("ssn-774-virginia-blk-i-ii")]
    public void Classify_without_catalog_row_does_not_infer_air_or_subsurface_from_id_substring(string targetId)
    {
        var reader = CreateReader();

        Assert.That(targetId.StartsWith("ucav", StringComparison.Ordinal), Is.False);
        Assert.That(MissionContactTargetClassifier.Classify(targetId, reader), Is.EqualTo(MissionContactTargetClass.Surface));
        Assert.That(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Air, targetId, reader), Is.False);
        Assert.That(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Subsurface, targetId, reader), Is.False);
    }

    [TestCase("ssn-774-virginia-blk-i-ii")]
    [TestCase("pla-885-severodvinsk-yasen")]
    public void Classify_catalog_subsurface_ids_as_subsurface_when_reader_has_domain(string targetId)
    {
        var reader = CreateReader(
            new CatalogPlatformEntry(targetId, 0, 0, 100, Domain: "subsurface"));

        Assert.That(MissionContactTargetClassifier.Classify(targetId, reader), Is.EqualTo(MissionContactTargetClass.Subsurface));
        Assert.That(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Subsurface, targetId, reader), Is.True);
        Assert.That(MissionContactTargetClassifier.Matches(MissionContactTargetClass.Air, targetId, reader), Is.False);
    }

    [TestCase("ucav-blue")]
    [TestCase("ucav-red")]
    public void Classify_legacy_ucav_prefix_still_air_on_catalog_miss(string targetId)
    {
        Assert.That(MissionContactTargetClassifier.Classify(targetId), Is.EqualTo(MissionContactTargetClass.Air));
        Assert.That(MissionContactTargetClassifier.Classify(targetId, catalogReader: null), Is.EqualTo(MissionContactTargetClass.Air));
        Assert.That(MissionContactTargetClassifier.Classify(targetId, CreateReader()), Is.EqualTo(MissionContactTargetClass.Air));
    }

    [TestCase("air", MissionContactTargetClass.Air)]
    [TestCase("aircraft", MissionContactTargetClass.Air)]
    [TestCase("subsurface", MissionContactTargetClass.Subsurface)]
    [TestCase("submarine", MissionContactTargetClass.Subsurface)]
    [TestCase("surface", MissionContactTargetClass.Surface)]
    [TestCase("ship", MissionContactTargetClass.Surface)]
    public void FromCatalogDomain_maps_catalog_domain_field(string domain, MissionContactTargetClass expected)
    {
        Assert.That(MissionContactTargetClassifier.FromCatalogDomain(domain), Is.EqualTo(expected));
    }

    [Test]
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

        Assert.That(profile.MissionTimeline, Is.Not.Null);
        var trigger = AssertEx.Single(profile.MissionTimeline!.ContactTriggers);
        Assert.That(trigger.TargetClass, Is.EqualTo(MissionContactTargetClass.Subsurface));
        Assert.That(trigger.TargetClass, Is.Not.EqualTo(MissionContactTargetClass.Any));
    }

    [Test]
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
        Assert.That(ex!.Message, Does.Contain("targetClass").IgnoreCase);
    }

    private static InMemoryCatalogReader CreateReader(params CatalogPlatformEntry[] platforms) =>
        new([], platforms: platforms);
}
