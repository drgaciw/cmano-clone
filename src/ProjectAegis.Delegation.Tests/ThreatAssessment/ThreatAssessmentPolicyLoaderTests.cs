using NUnit.Framework;
using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.Tests.ThreatAssessment;

public sealed class ThreatAssessmentPolicyLoaderTests
{
    [Test]
    public void Authored_scenario_policy_loads_and_changes_advisory_scoring_without_changing_confidence()
    {
        var path = SamplePath();
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        var tuning = ThreatAssessmentPolicyLoader.LoadFromScenarioPolicy(path);
        var input = Input() with { Policy = profile.FriendlyDefault, Tuning = tuning };
        var authored = ThreatAssessmentProjection.Project(input);
        var baseline = ThreatAssessmentProjection.Project(input with { Tuning = ThreatAssessmentTuning.Default });

        Assert.That(profile.Id, Is.EqualTo("threat-advisory-calibration"));
        Assert.That(authored.ThreatUrgencyScore, Is.EqualTo(0.35).Within(1e-9));
        Assert.That(authored.ThreatUrgencyBand, Is.EqualTo("low"));
        Assert.That(authored.Confidence, Is.EqualTo(baseline.Confidence));
        Assert.That(authored.AlternativeFamilies, Is.EqualTo(new[] { "gun", "energy", "withhold" }));
        Assert.That(baseline.ThreatUrgencyScore, Is.EqualTo(0.95).Within(1e-9));
        Assert.That(baseline.ThreatUrgencyBand, Is.EqualTo("immediate"));
        Assert.That(baseline.AlternativeFamilies, Is.Empty);
        Assert.That(authored.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(authored.IsFireOrder, Is.False);
    }

    [Test]
    public void Authored_inclusive_floor_tolerates_roundoff_but_preserves_meaningful_differences()
    {
        var path = SamplePath();
        var input = Input();
        input = input with { EngageContext = input.EngageContext with { IsHypersonicTarget = true },
            Tuning = ThreatAssessmentPolicyLoader.LoadFromScenarioPolicy(path) };
        var atFloor = ThreatAssessmentProjection.Project(input);

        Assert.That(atFloor.ThreatUrgencyScore, Is.EqualTo(0.3 + 0.05 + 0.05));
        Assert.That(atFloor.ThreatUrgencyScore, Is.LessThan(0.4));
        Assert.That(atFloor.ThreatUrgencyBand, Is.EqualTo("watch"));
        WithPolicy(File.ReadAllText(path).Replace("\"inZoneBase\": 0.30", "\"inZoneBase\": 0.29999999999", StringComparison.Ordinal), adjustedPath =>
        {
            var belowFloor = ThreatAssessmentProjection.Project(input with
            { Tuning = ThreatAssessmentPolicyLoader.LoadFromScenarioPolicy(adjustedPath) });
            Assert.That(belowFloor.ThreatUrgencyScore, Is.LessThan(0.4 - 1e-12));
            Assert.That(belowFloor.ThreatUrgencyBand, Is.EqualTo("low"));
        });
    }

    [TestCase(50000, false, true, 0.35)]
    [TestCase(90000, false, true, 0.25)]
    [TestCase(110000, false, true, 0.15)]
    [TestCase(50000, true, true, 0.40)]
    [TestCase(50000, false, false, 0.32)]
    public void Loaded_bases_and_modifiers_reach_the_projector(double range, bool hypersonic, bool track, double expected)
    {
        WithPolicy("""
            {"id":"custom","threatAssessmentUrgency":{"inZoneBase":0.3,"approachingBase":0.2,
            "outOfZoneBase":0.1,"defensiveBonus":0.05,"hypersonicBonus":0.05,"missingFireControlPenalty":0.03}}
            """, path =>
        {
            var input = Input() with { Tuning = ThreatAssessmentPolicyLoader.LoadFromScenarioPolicy(path) };
            input = input with { EngageContext = input.EngageContext with { RangeMeters = range, IsHypersonicTarget = hypersonic, HasFireControlTrack = track } };
            Assert.That(ThreatAssessmentProjection.Project(input).ThreatUrgencyScore, Is.EqualTo(expected).Within(1e-9));
        });
    }

    [TestCase(0.35, 0.30, 0.20, 0.10, "immediate")]
    [TestCase(0.90, 0.35, 0.20, 0.10, "focus")]
    [TestCase(0.90, 0.70, 0.35, 0.10, "watch")]
    [TestCase(0.90, 0.70, 0.50, 0.35, "low")]
    [TestCase(0.90, 0.70, 0.50, 0.40, "ignore")]
    public void Loaded_band_floors_control_classification(double immediate, double focus, double watch, double low, string expected)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            id = "custom",
            threatAssessmentUrgency = new
            {
                inZoneBase = 0.3, defensiveBonus = 0.05,
                immediateFloor = immediate, focusFloor = focus, watchFloor = watch, lowFloor = low
            }
        });
        WithPolicy(json, path => Assert.That(ThreatAssessmentProjection.Project(Input() with
        { Tuning = ThreatAssessmentPolicyLoader.LoadFromScenarioPolicy(path) }).ThreatUrgencyBand, Is.EqualTo(expected)));
    }

    [Test]
    public void Omitted_section_preserves_the_existing_tuning_contract()
    {
        WithPolicy("{\"id\":\"legacy\",\"friendlyRoe\":\"WeaponsFree\"}", path =>
            Assert.That(ThreatAssessmentPolicyLoader.LoadFromScenarioPolicy(path), Is.SameAs(ThreatAssessmentTuning.Default)));
    }

    [Test]
    public void Root_id_casing_matches_the_existing_scenario_policy_loader()
    {
        WithPolicy("{\"Id\":\"custom\",\"threatAssessmentUrgency\":{\"inZoneBase\":0.1}}", path =>
        {
            Assert.That(ScenarioPolicyJsonLoader.LoadFromFile(path).Id, Is.EqualTo("custom"));
            var input = Input() with { Tuning = ThreatAssessmentPolicyLoader.LoadFromScenarioPolicy(path) };
            Assert.That(ThreatAssessmentProjection.Project(input).ThreatUrgencyScore, Is.EqualTo(0.25).Within(1e-9));
        });
    }

    [TestCase("{\"inZoneBase\":1e400}")]
    [TestCase("{\"defensiveBonus\":-0.1}")]
    [TestCase("{\"lowConfidenceFloor\":1.1}")]
    [TestCase("{\"focusFloor\":0.1,\"watchFloor\":0.9}")]
    [TestCase("{\"inZoneBase\":0.2,\"inZoneBase\":0.3}")]
    [TestCase("{\"inZonBase\":0.2}")]
    [TestCase("{\"inZoneBase\":\"NaN\"}")]
    [TestCase("null")]
    public void Invalid_authored_settings_fail_at_loading(string settings)
    {
        WithPolicy("{\"id\":\"invalid\",\"threatAssessmentUrgency\":" + settings + "}", path =>
            Assert.Throws<InvalidDataException>(() => ThreatAssessmentPolicyLoader.LoadFromScenarioPolicy(path)));
    }

    private static ThreatAssessmentInput Input() => new("contact-1", "target-1", "shooter-1", "sm-2", "SM-2",
        new EngageContext(RangeMeters: 50000, Envelope: new WeaponEnvelope(1000, 100000), RoundsRemaining: 4,
            HasFireControlTrack: true, RadarEmconActive: true, MountOnline: true, SalvoSize: 2, DlzPersonality: DlzPersonality.Normal),
        EffectivePolicy.DefaultFree, ThreatAssessmentPosture.Defensive);

    private static string SamplePath()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ProjectAegis.sln"))) directory = directory.Parent;
        Assert.That(directory, Is.Not.Null);
        return Path.Combine(directory!.FullName, "data", "scenarios", "threat-advisory-calibration.policy.json");
    }

    private static void WithPolicy(string json, Action<string> check)
    {
        var path = Path.GetTempFileName();
        try { File.WriteAllText(path, json); check(path); }
        finally { File.Delete(path); }
    }
}
