using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

/// <summary>DRG-320: verify the production recommendation-to-dashboard route.</summary>
public sealed class ThreatRecommendationChromeTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void Current_threat_reaches_chrome_with_confidence_and_conditional_alternatives(bool lowConfidence)
    {
        var recommendation = ThreatAssessmentProjection.Project(new ThreatAssessmentInput(
            ContactId: "c1", TargetId: "t1", ShooterUnitId: "s1",
            WeaponId: "sm-2", WeaponLabel: "SM-2",
            EngageContext: new EngageContext(
                RangeMeters: 50_000, Envelope: new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 4, HasFireControlTrack: true, RadarEmconActive: true,
                MountOnline: true, SalvoSize: 2, DlzPersonality: DlzPersonality.Normal),
            Policy: lowConfidence ? new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 1) : EffectivePolicy.DefaultFree,
            Posture: ThreatAssessmentPosture.Defensive));
        var contacts = SliceAContactFrame.Empty with
        {
            SimTick = 4, SimTime = 4,
            Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 4, 4)]
        };
        var advice = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(), contacts,
            new AdviceEvidenceSource("c1", 4, recommendation, null, []));
        var section = CommandReviewDashboard.Build(null, advice, CoordinationSnapshot.Empty)
            .Single(row => row.Id == "advice");

        Assert.That(advice.Availability, Is.EqualTo(AdviceAvailability.Available));
        Assert.That(advice.IsFireOrder, Is.False);
        Assert.That(advice.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(section.Lines, Has.Some.Contains(lowConfidence ? "Assessment confidence: 0 %" : "Assessment confidence: 85 %"));
        Assert.That(section.Lines, Has.Some.Contains("urgency=immediate score=0.95 family=missile pin=jev-1.13.x"));
        if (lowConfidence)
        {
            Assert.That(section.Lines, Has.Some.Contains("alternatives=gun,energy,withhold"));
            Assert.That(section.Lines, Has.Some.Contains("wra-exhaustion-warn"));
        }
        else
        {
            Assert.That(section.Lines, Has.None.Contains("alternatives="));
            Assert.That(section.Lines, Has.None.Contains("wra-exhaustion-warn"));
        }
    }

    private sealed class Snapshot : ISimWorldSnapshot
    {
        public double SimTime => 4;
        public int ContactCount => 1;
        public int ActiveEngagementCount => 0;
        public bool IsMemberAlive(TargetId memberId) => true;
        public TargetId? PrimaryHostileContactId => new("t1");
        public bool HasFireControlTrackOnPrimaryContact => true;
        public bool ObserverRadarEmconActive => true;
    }
}
