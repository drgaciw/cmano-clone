using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.ThreatAssessment;

/// <summary>
/// DRG-320: threat score and weapon-family choice stay advisory.
/// The projection and the recommend chrome have no call path into player command, attack enqueue, or order approval.
/// </summary>
public sealed class ThreatRecommendNeverFiresFenceTests
{
    private static readonly string[] ForbiddenCallPaths =
    [
        "TryIssuePlayerCommand",
        "TryEnqueueAttackOption",
        "TryApprovePendingOrder",
    ];

    [Test]
    public void Advisory_sources_do_not_name_fire_enqueue_or_approve()
    {
        foreach (var path in AdvisorySources())
        {
            var source = File.ReadAllText(path);
            foreach (var forbidden in ForbiddenCallPaths)
            {
                Assert.That(source, Does.Not.Contain(forbidden), $"{path} must not reference {forbidden}");
            }
        }
    }

    [Test]
    public void Recommend_chrome_hard_wires_fire_flags_off()
    {
        var chrome = File.ReadAllText(AdviceModelsPath());

        Assert.That(chrome, Does.Contain("FromWeaponRecommendation"));
        Assert.That(chrome, Does.Contain("IsWeaponsReleaseAuthorization: false"));
        Assert.That(chrome, Does.Contain("IsFireOrder: false"));
        Assert.That(chrome, Does.Contain("ThreatAssessmentProjection.LowConfidenceFloor"));
        Assert.That(chrome, Does.Not.Contain("TryIssuePlayerCommand"));
        Assert.That(chrome, Does.Not.Contain("TryEnqueueAttackOption"));
        Assert.That(chrome, Does.Not.Contain("TryApprovePendingOrder"));
    }

    [Test]
    public void Project_never_sets_release_fire_or_auto_engage()
    {
        var feasible = ThreatAssessmentProjection.Project(Input(EffectivePolicy.DefaultFree, rounds: 4));
        var tight = ThreatAssessmentProjection.Project(Input(new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2), rounds: 4));
        var empty = ThreatAssessmentProjection.Project(Input(EffectivePolicy.DefaultFree, rounds: 4) with { ContactId = "" });

        foreach (var recommendation in new[] { feasible, tight, empty, WeaponRecommendation.Empty })
        {
            Assert.That(recommendation.IsWeaponsReleaseAuthorization, Is.False);
            Assert.That(recommendation.IsFireOrder, Is.False);
            Assert.That(recommendation.IsAutomaticEngagement, Is.False);
            Assert.That(recommendation.RecommendationKind, Is.EqualTo(ThreatRecommendationKind.AdvisoryRecommendation));
            Assert.That(recommendation.JevModelPin, Is.EqualTo("jev-1.13.x"));
        }
    }

    [Test]
    public void Pin_fixture_is_jev_1_13_x_and_matches_the_draft_floor()
    {
        var pin = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "jev", "pins", "jev-1.13.x.json"));
        var advisory = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "jev", "fixtures", "vs2", "threat-advisory.json"));

        Assert.That(pin, Does.Contain("\"pinModel\": \"jev-1.13.x\""));
        Assert.That(pin, Does.Contain("\"package\": \"jev-1.13.x\""));
        Assert.That(pin, Does.Contain("threat_urgency"));
        Assert.That(pin, Does.Contain("weapon_family_recommend"));
        Assert.That(pin, Does.Contain("wra_exhaustion_warn"));
        Assert.That(pin, Does.Contain("\"lowConfidenceFloor\": 0.5"));
        Assert.That(ThreatAssessmentProjection.LowConfidenceFloor, Is.EqualTo(0.5));
        Assert.That(advisory, Does.Contain("\"fires\": false"));
        Assert.That(advisory, Does.Contain("\"enqueuesAccept\": false"));
        Assert.That(advisory, Does.Contain("jev-1.13.x"));
    }

    private static ThreatAssessmentInput Input(EffectivePolicy policy, int rounds) =>
        new(
            ContactId: "contact-1",
            TargetId: "1001",
            ShooterUnitId: "2001",
            WeaponId: "sm-2",
            WeaponLabel: "SM-2 Block IIIA",
            EngageContext: new EngageContext(
                RangeMeters: 50_000,
                Envelope: new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: rounds,
                HasFireControlTrack: true,
                RadarEmconActive: true,
                MountOnline: true,
                SalvoSize: 2,
                DlzPersonality: DlzPersonality.Normal),
            Policy: policy,
            Posture: ThreatAssessmentPosture.Defensive);

    private static IEnumerable<string> AdvisorySources()
    {
        var root = RepoRoot();
        var threat = Path.Combine(root, "src", "ProjectAegis.Delegation", "ThreatAssessment");
        foreach (var file in Directory.GetFiles(threat, "*.cs"))
        {
            yield return file;
        }

        yield return AdviceModelsPath();
    }

    private static string AdviceModelsPath() =>
        Path.Combine(RepoRoot(), "src", "ProjectAegis.Delegation.UnityAdapter", "CommandReview", "AdviceModels.cs");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "repo root not found");
        return dir!.FullName;
    }
}
