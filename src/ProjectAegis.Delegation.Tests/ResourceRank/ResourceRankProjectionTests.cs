using ProjectAegis.Delegation.ResourceRank;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.ResourceRank;

public sealed class ResourceRankProjectionTests
{
    private static EngageContext InZoneContext(
        double pkBase,
        int roundsRemaining,
        double rangeMeters = 50_000) =>
        new(
            RangeMeters: rangeMeters,
            Envelope: new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: roundsRemaining,
            HasFireControlTrack: true,
            RadarEmconActive: true,
            MountOnline: true,
            PkBase: pkBase,
            SalvoSize: 1,
            DlzPersonality: DlzPersonality.Normal);

    private static ResourceRankCandidateInput Candidate(
        string shooterId,
        string weaponId,
        string weaponLabel,
        in EngageContext ctx,
        ResourceRankAvailabilityFacts? availability = null) =>
        new(
            ContactId: "contact-1",
            TargetId: "1001",
            ShooterUnitId: shooterId,
            WeaponId: weaponId,
            WeaponLabel: weaponLabel,
            EngageContext: ctx,
            Policy: EffectivePolicy.DefaultFree,
            Availability: availability ?? ResourceRankAvailabilityFacts.None,
            Posture: ResourceRankPosture.Defensive);

    [Test]
    public void Two_candidates_rank_with_stable_preferred_order()
    {
        var sm2 = Candidate(
            "2001",
            "sm-2",
            "SM-2 Block IIIA",
            InZoneContext(pkBase: 0.90, roundsRemaining: 4));

        var essm = Candidate(
            "2002",
            "essm",
            "ESSM",
            InZoneContext(pkBase: 0.70, roundsRemaining: 8, rangeMeters: 55_000));

        var snapshot = ResourceRankProjection.Project(new[] { essm, sm2 });

        Assert.That(snapshot.Kind, Is.EqualTo(ResourceRankKind.AdvisoryRanking));
        Assert.That(snapshot.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.IsAutomaticEngagement, Is.False);
        Assert.That(snapshot.RankedCandidates, Has.Count.EqualTo(2));

        var preferred = snapshot.RankedCandidates[0];
        var alternative = snapshot.RankedCandidates[1];

        Assert.That(preferred.Disposition, Is.EqualTo(ResourceRankDisposition.Preferred));
        Assert.That(preferred.WeaponId, Is.EqualTo("sm-2"));
        Assert.That(preferred.Rank, Is.EqualTo(1));
        Assert.That(preferred.Scores.Total, Is.GreaterThan(alternative.Scores.Total));

        Assert.That(alternative.Disposition, Is.EqualTo(ResourceRankDisposition.Alternative));
        Assert.That(alternative.WeaponId, Is.EqualTo("essm"));
        Assert.That(alternative.Rank, Is.EqualTo(2));
        Assert.That(alternative.ReasonCode, Is.Not.Null.And.Not.Empty);
        Assert.That(alternative.ReasonPlain, Is.Not.Empty);
        Assert.That(snapshot.StatusLine, Does.Contain("advisory").IgnoreCase);
        Assert.That(snapshot.StatusLine, Does.Contain("not weapons release").IgnoreCase);

        var rerun = ResourceRankProjection.Project(new[] { essm, sm2 });
        Assert.That(
            ResourceRankProjection.ComputeFingerprint(snapshot),
            Is.EqualTo(ResourceRankProjection.ComputeFingerprint(rerun)));
        Assert.That(preferred.WeaponId, Is.EqualTo(rerun.RankedCandidates[0].WeaponId));
    }

    [Test]
    public void Candidate_excluded_by_commitment_has_named_reason()
    {
        var committed = Candidate(
            "2001",
            "sm-2",
            "SM-2 Block IIIA",
            new EngageContext(
                RangeMeters: 50_000,
                Envelope: new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 4,
                HasFireControlTrack: true,
                RadarEmconActive: true,
                MountOnline: true,
                PkBase: 0.90,
                SalvoSize: 2,
                DlzPersonality: DlzPersonality.Normal),
            availability: new ResourceRankAvailabilityFacts(RoundsCommittedElsewhere: 3));

        var free = Candidate(
            "2002",
            "essm",
            "ESSM",
            InZoneContext(pkBase: 0.70, roundsRemaining: 8));

        var snapshot = ResourceRankProjection.Project(new[] { committed, free });

        var excluded = snapshot.RankedCandidates.Single(c => c.WeaponId == "sm-2");
        Assert.That(excluded.Disposition, Is.EqualTo(ResourceRankDisposition.Excluded));
        Assert.That(excluded.ReasonCode, Is.EqualTo(ResourceRankReasonCode.ExcludedByCommitment));
        Assert.That(excluded.ReasonPlain, Does.Contain("committed elsewhere").IgnoreCase);
        Assert.That(excluded.StatusLine, Does.Contain("EXCLUDED"));
        Assert.That(excluded.Rank, Is.EqualTo(0));

        var preferred = snapshot.RankedCandidates.Single(c => c.Disposition == ResourceRankDisposition.Preferred);
        Assert.That(preferred.WeaponId, Is.EqualTo("essm"));
        Assert.That(snapshot.IsFireOrder, Is.False);
    }

    [Test]
    public void Empty_input_returns_empty_snapshot()
    {
        var snapshot = ResourceRankProjection.Project(Array.Empty<ResourceRankCandidateInput>());
        Assert.That(snapshot.RankedCandidates, Is.Empty);
        Assert.That(ResourceRankProjection.ComputeFingerprint(snapshot), Is.EqualTo("rr:empty"));
    }
}
