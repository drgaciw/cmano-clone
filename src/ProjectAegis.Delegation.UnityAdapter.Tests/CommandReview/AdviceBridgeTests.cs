using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.ResourceRank;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

[TestFixture]
public sealed class AdviceBridgeTests
{
    [Test]
    public void Current_grounded_evidence_builds_reviewable_advice()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 40, SimTime = 40, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 40, 40)] };
        var source = new AdviceEvidenceSource("c1", 40, Recommendation(), Ranking(), ["package:alpha"], true);

        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(40), frame, source);

        Action assertions = () =>
        {
            Assert.That(result.Availability, Is.EqualTo(AdviceAvailability.Available));
            Assert.That(result.Confidence, Is.EqualTo(.8).Within(.001));
            Assert.That(result.Evidence, Is.Not.Empty);
            Assert.That(result.HardConstraints, Does.Contain("advisory-only:no-authority-or-order"));
            Assert.That(result.ResourceCommitments, Has.Some.Contains("committed elsewhere: 1"));
            Assert.That(result.Alternatives, Has.Some.Contains("scores effect="));
            Assert.That(result.Alternatives, Has.Some.StartsWith("preferred:s1/w1:"));
            Assert.That(result.PolicyConstraints, Has.Some.StartsWith("range:"));
            Assert.That(result.IsWeaponsReleaseAuthorization, Is.False);
            Assert.That(result.IsFireOrder, Is.False);
        };
        Assert.Multiple(assertions);
    }

    [Test]
    public void Stale_or_partitioned_frame_fails_closed()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 8, SimTime = 8, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 8, 8)] };
        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(12), frame,
            new AdviceEvidenceSource("c1", 8, Recommendation(), Ranking(), [], true));

        Assert.That(result.Availability, Is.EqualTo(AdviceAvailability.Stale));
        Assert.That(result.Fallback, Does.Contain("refresh"));
        Assert.That(result.Confidence, Is.Null);
    }

    [Test]
    public void Withheld_authority_is_visible_and_never_upgraded()
    {
        var authority = new C2AuthorityProjection(new(RoeLevel.WeaponsTight, "WEAPONS TIGHT", C2AuthorityDisposition.Withheld, "ROE", false),
            new(C2AuthorityDisposition.Withheld, "NO_RELEASE", null), []);
        var frame = SliceAContactFrame.Empty with { SimTick = 4, SimTime = 4, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 4, 4)], Authorities = new Dictionary<string, C2AuthorityProjection> { ["c1"] = authority } };
        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(4), frame,
            new AdviceEvidenceSource("c1", 4, Recommendation(), Ranking(), [], true));

        Assert.That(result.PolicyConstraints, Does.Contain("authority:Withheld:NO_RELEASE"));
        Assert.That(result.IsWeaponsReleaseAuthorization, Is.False);
    }

    [Test]
    public void Unavailable_model_returns_explicit_fallback()
    {
        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(1), SliceAContactFrame.Empty with { SimTick = 1, SimTime = 1 },
            new AdviceEvidenceSource("c1", 1, null, null, [], false));
        Assert.That(result.Availability, Is.EqualTo(AdviceAvailability.ModelUnavailable));
        Assert.That(result.Fallback, Does.Contain("manual review"));
    }

    [Test]
    public void Standard_runtime_overload_reports_current_contact_without_inventing_weapon_facts()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 3, SimTime = 3, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 3, 3)] };
        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(3), frame);

        Assert.That(result.Availability, Is.EqualTo(AdviceAvailability.Available));
        Assert.That(result.Rationale, Does.Contain("contact and chain limitations"));
        Assert.That(result.PolicyConstraints, Does.Contain("authority:unknown:no implicit permission"));
        Assert.That(result.Assumptions, Has.Some.Contains("No weapon-specific"));
        Assert.That(result.Confidence, Is.Null);
    }

    [Test]
    public void Mismatched_or_old_projected_evidence_fails_closed()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 10, SimTime = 10, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 10, 10)] };
        var old = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(10), frame,
            new AdviceEvidenceSource("c1", 9, Recommendation(), Ranking(), [], true));
        var wrongThreat = Recommendation() with { TargetId = "other" };
        var mismatch = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(10), frame,
            new AdviceEvidenceSource("c1", 10, wrongThreat, Ranking(), [], true));

        Assert.That(old.Availability, Is.EqualTo(AdviceAvailability.Stale));
        Assert.That(mismatch.Availability, Is.EqualTo(AdviceAvailability.EvidenceUnavailable));
    }

    [TestCase(double.NaN)]
    [TestCase(10.0005)]
    [TestCase(-1)]
    public void Invalid_or_nonidentical_evidence_time_fails_closed(double evidenceTime)
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 10, SimTime = 10, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 10, 10)] };
        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(10), frame,
            new AdviceEvidenceSource("c1", evidenceTime, Recommendation(), Ranking(), [], true));
        Assert.That(result.Availability, Is.EqualTo(AdviceAvailability.Stale));
    }

    [Test]
    public void Selected_contact_controls_snapshot_capability_lookup()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 6, SimTime = 6, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 6, 6), new ContactPictureEntry("c2", "t2", "obs", "Tracked", 6, 6)] };
        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(6), frame, "c2");
        Assert.That(result.ContactId, Is.EqualTo("c2"));
    }

    [Test]
    public void Provider_cannot_replace_selected_contact_identity()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 6, SimTime = 6, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 6, 6), new ContactPictureEntry("c2", "t2", "obs", "Tracked", 6, 6)] };
        var snapshot = new Snapshot(6, new AdviceEvidenceSource("c1", 6, null, null, [], true));
        var result = AdviceBridge.Build(new DelegationBridge(1), snapshot, frame, "c2");
        Assert.That(result.Availability, Is.EqualTo(AdviceAvailability.Stale));
        Assert.That(result.ContactId, Is.EqualTo("c2"));
    }

    [Test]
    public void Stale_provenance_or_link_partition_downgrades_advice()
    {
        var provenance = new ContactProvenanceState("c1", new("obs", "t1", "sensor"), ContactProvenanceConfidence.High,
            ContactProvenanceFreshness.Stale, 5, new("Tracked", "t1", 5, 5), true,
            ContactProvenanceQualityState.Stale | ContactProvenanceQualityState.SilentComms);
        var frame = SliceAContactFrame.Empty with { SimTick = 10, SimTime = 10, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 5, 5)], Provenance = new([provenance]) };
        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(10), frame);

        Assert.That(result.Availability, Is.EqualTo(AdviceAvailability.Stale));
        Assert.That(result.Fallback, Does.Contain("reporting link"));
        Assert.That(result.Confidence, Is.Null.Or.Zero);
    }

    private static WeaponRecommendation Recommendation() => WeaponRecommendation.Empty with
    {
        ContactId = "c1", TargetId = "t1", ShooterUnitId = "s1", WeaponId = "w1", WeaponLabel = "Missile",
        Outcome = WeaponRecommendationOutcome.Feasible, Confidence = .8, Assumptions = ["track remains current"]
    };

    private static ResourceRankSnapshot Ranking() => ResourceRankSnapshot.Empty with
    {
        ContactId = "c1", TargetId = "t1", RankedCandidates =
        [
            ResourceRankRankedCandidate.Empty with { ContactId = "c1", TargetId = "t1", ShooterUnitId = "s1", WeaponId = "w1", WeaponLabel = "Missile", Disposition = ResourceRankDisposition.Preferred, Rank = 1, ReasonPlain = "best current fit" },
            ResourceRankRankedCandidate.Empty with { ContactId = "c1", TargetId = "t1", ShooterUnitId = "s2", WeaponId = "w2", WeaponLabel = "Gun", Disposition = ResourceRankDisposition.Excluded, ReasonCode = "COMMITTED", ReasonPlain = "committed elsewhere: 1" }
        ]
    };

    [Test]
    public void Current_frame_cannot_launder_a_future_contact_report()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 3, SimTime = 3,
            Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 4, 4)] };
        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(3), frame);
        Assert.That(result.Availability, Is.EqualTo(AdviceAvailability.Stale));
        Assert.That(result.Confidence, Is.Null);
    }

    [Test]
    public void Provider_declining_evidence_cannot_supply_a_recommendation_through_out_value()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 3, SimTime = 3, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 3, 3)] };
        var source = new AdviceEvidenceSource("c1", 3, Recommendation(), Ranking(), []);
        var result = AdviceBridge.Build(new DelegationBridge(1), new Snapshot(3, source, false), frame);
        Assert.That(result.Confidence, Is.Null);
        Assert.That(result.Alternatives, Has.None.Contains("s1/w1"));
    }

    private sealed class Snapshot(double time, AdviceEvidenceSource? advice = null, bool available = true) : ISimWorldSnapshot, IAdviceEvidenceSource
    {
        public double SimTime => time; public int ContactCount => 1; public int ActiveEngagementCount => 0;
        public bool IsMemberAlive(TargetId memberId) => true; public TargetId? PrimaryHostileContactId => new("t1");
        public bool HasFireControlTrackOnPrimaryContact => true; public bool ObserverRadarEmconActive => true;
        public bool TryGetAdviceEvidence(string contactId, out AdviceEvidenceSource evidence)
        {
            evidence = advice!;
            return available && advice is not null;
        }
    }
}
