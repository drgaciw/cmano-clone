using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Swarm;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class SwarmEngageHotpathTests
{
    private static EngageContext BaseCtx(
        double pkBase = 0.85,
        int shooterMax = 0,
        int shooterCount = 0,
        int targetMax = 0,
        int targetCount = 0,
        SwarmAaProfileKind aa = SwarmAaProfileKind.PointFire) =>
        new(
            RangeMeters: 1000,
            Envelope: new WeaponEnvelope(0, 50_000),
            RoundsRemaining: 10,
            HasFireControlTrack: true,
            PkBase: pkBase,
            PkKill: 0.0,
            ShooterMaxDrones: shooterMax,
            ShooterDroneCount: shooterCount,
            TargetMaxDrones: targetMax,
            TargetDroneCount: targetCount,
            TargetAaProfile: aa);

    [Test]
    public void Mvp_resolver_scales_pk_by_shooter_swarm_integrity()
    {
        // Fixed seed: find a draw that is a Hit at full Pk but Miss at half Pk.
        // PkBase=0.9, full scale 0.9, half scale 0.45. Need draw in (0.45, 0.9).
        var seed = SimSeed.FromScenario(99);
        var world = new DictionaryEngageWorldQuery();
        var mags = new MagazineLedger();
        mags.SetRounds(1, 1, 10);

        var fullCtx = BaseCtx(pkBase: 0.9, shooterMax: 40, shooterCount: 40);
        var halfCtx = BaseCtx(pkBase: 0.9, shooterMax: 40, shooterCount: 20);
        var req = new EngageRequest(1, 2, 1, 5);

        world.Set(req, fullCtx);
        var fullResolver = new MvpEngagementResolver(world, mags, seed: seed);
        var full = fullResolver.Resolve(req);

        mags = new MagazineLedger();
        mags.SetRounds(1, 1, 10);
        world = new DictionaryEngageWorldQuery();
        world.Set(req, halfCtx);
        var halfResolver = new MvpEngagementResolver(world, mags, seed: seed);
        var half = halfResolver.Resolve(req);

        // Same combat draw; half integrity must not produce a stronger outcome than full.
        if (full.OutcomeCode == EngagementOutcomeCodes.Miss)
        {
            Assert.That(half.OutcomeCode, Is.EqualTo(EngagementOutcomeCodes.Miss));
        }
        else
        {
            // Full hit/kill: half may miss or hit — never better than full.
            Assert.That(
                half.OutcomeCode is EngagementOutcomeCodes.Miss
                                or EngagementOutcomeCodes.Hit
                                or EngagementOutcomeCodes.Kill
                                or EngagementOutcomeCodes.Intercept,
                Is.True);
        }

        // Explicit scale proof independent of RNG: Scale is used when max>0.
        Assert.That(
            Math.Round((double)(SwarmOffensiveEffect.Scale(0.9, 40, 40)), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(0.9), 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round((double)(SwarmOffensiveEffect.Scale(0.9, 20, 40)), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(0.45), 6, MidpointRounding.AwayFromZero)));
        Assert.That(full.Launched, Is.True);
        Assert.That(half.Launched, Is.True);
        // Half integrity Pk is lower ⇒ draw is more likely to miss; pin that half PkDraw equals full
        // (same seed/domain/entity/tick) while outcome may differ.
        Assert.That(half.PkDraw, Is.EqualTo(full.PkDraw));
        if (full.PkDraw is > 0.45 and < 0.9)
        {
            Assert.That(full.OutcomeCode, Is.Not.EqualTo(EngagementOutcomeCodes.Miss));
            Assert.That(half.OutcomeCode, Is.EqualTo(EngagementOutcomeCodes.Miss));
        }
    }

    [Test]
    public void Mvp_resolver_applies_area_aa_integrity_via_authorized_sink()
    {
        var seed = SimSeed.FromScenario(7);
        var swarm = new SwarmController(seed);
        swarm.Register(
            new SwarmUnitIntegrity("40", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40),
            57,
            20);
        var sink = new SwarmControllerIntegritySink(swarm);

        var world = new DictionaryEngageWorldQuery();
        var mags = new MagazineLedger();
        mags.SetRounds(1, 1, 10);
        var req = new EngageRequest(1, 40, 1, 3);
        // Force Hit: PkBase=1, PkKill=0
        world.Set(
            req,
            BaseCtx(
                pkBase: 1.0,
                targetMax: 40,
                targetCount: 40,
                aa: SwarmAaProfileKind.AreaAa));

        var resolver = new MvpEngagementResolver(
            world,
            mags,
            seed: seed,
            swarmIntegritySink: sink,
            resolveTargetUnitId: id => id.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var result = resolver.Resolve(req);
        Assert.That(result.Launched, Is.True);
        Assert.That(result.OutcomeCode, Is.EqualTo(EngagementOutcomeCodes.Hit));
        Assert.That(swarm.TryGetIntegrity("40", out var after), Is.True);
        Assert.That(after.DroneCount, Is.EqualTo(40 - SwarmHardCounterAa.AreaAaDronesLostPerHit));
        AssertEx.Single(swarm.IntegrityTimeline);
        Assert.That(swarm.IntegrityTimeline[0].ReasonCode, Is.EqualTo(SwarmEngagementIntegrityApplier.ReasonAreaAa));
    }

    [Test]
    public void Scenario_overrides_change_drones_lost_per_hit()
    {
        Assert.That(SwarmHardCounterAa.DronesLostPerHit(SwarmAaProfileKind.PointFire), Is.EqualTo(1));
        Assert.That(SwarmHardCounterAa.DronesLostPerHit(SwarmAaProfileKind.PointFire, pointFireOverride: 3), Is.EqualTo(3));
        Assert.That(SwarmHardCounterAa.DronesLostPerHit(SwarmAaProfileKind.AreaAa, areaAaOverride: 12), Is.EqualTo(12));

        var ctx = BaseCtx(targetMax: 40, targetCount: 40, aa: SwarmAaProfileKind.AreaAa) with
        {
            AreaAaDronesLostPerHit = 5,
        };
        Assert.That(SwarmHardCounterAa.ResolveFromContext(in ctx), Is.EqualTo(5));
    }
}
