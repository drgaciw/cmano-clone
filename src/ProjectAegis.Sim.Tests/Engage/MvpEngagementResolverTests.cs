using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MvpEngagementResolverTests
{
    [Test]
    public void In_zone_with_ammo_launches_and_decrements_magazine()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var resolver = new MvpEngagementResolver(world, magazines);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.True);
        Assert.That(magazines.GetRounds(1, 0), Is.EqualTo(1));
    }

    [Test]
    public void Out_of_envelope_aborts()
    {
        var world = new DictionaryEngageWorldQuery();
        var resolver = new MvpEngagementResolver(world, new MagazineLedger());
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(200_000, new WeaponEnvelope(1_000, 100_000), 5, true));

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.OutOfEnvelope));
    }

    [Test]
    public void Radar_emcon_off_aborts_before_track_check()
    {
        var world = new DictionaryEngageWorldQuery();
        var resolver = new MvpEngagementResolver(world, new MagazineLedger());
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true, RadarEmconActive: false));

        var result = resolver.Resolve(request);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.EmconOff));
    }

    [Test]
    public void Wra_salvo_cap_denies_when_salvo_exceeds_max()
    {
        var world = new DictionaryEngageWorldQuery();
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.WeaponsFree, 1));
        var resolver = new MvpEngagementResolver(
            world,
            new MagazineLedger(),
            evaluator,
            _ => new EffectivePolicy(RoeLevel.WeaponsFree, 1));
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true, SalvoSize: 2));

        var result = resolver.Resolve(request);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.WraSalvo));
    }

    [Test]
    public void Hold_fire_policy_denies_before_envelope_check()
    {
        var world = new DictionaryEngageWorldQuery();
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.HoldFire));
        var resolver = new MvpEngagementResolver(
            world,
            new MagazineLedger(),
            evaluator,
            _ => new EffectivePolicy(RoeLevel.HoldFire));
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.RoeHoldFire));
    }

    /// <summary>Wave 2 adversarial: WeaponsTight reason identity through full resolver (doc 14 ENG-07).</summary>
    [Test]
    public void WeaponsTight_denies_with_WeaponsTight_not_RoeHoldFire()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.WeaponsTight));
        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            evaluator,
            _ => new EffectivePolicy(RoeLevel.WeaponsTight));
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.WeaponsTight));
        Assert.That(result.AbortReason, Is.Not.EqualTo(EngagementAbortReason.RoeHoldFire));
        Assert.That(magazines.GetRounds(1, 0), Is.EqualTo(2));
    }

    /// <summary>Wave 2 adversarial: WRA exact max allows launch on resolver path.</summary>
    [Test]
    public void Wra_salvo_exact_max_allows_launch()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 4);
        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.WeaponsFree, 2)),
            _ => new EffectivePolicy(RoeLevel.WeaponsFree, 2));
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 4, true, SalvoSize: 2));

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.True);
    }

    /// <summary>Wave 2 adversarial: MountOffline via full resolver + no magazine consume (doc 18 AC-2).</summary>
    [Test]
    public void Resolve_MountOnline_false_aborts_MountOffline_does_not_consume_magazine()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 4);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 4,
            HasFireControlTrack: true,
            MountOnline: false));

        var resolver = new MvpEngagementResolver(world, magazines);
        var result = resolver.Resolve(request);

        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.MountOffline));
        Assert.That(magazines.GetRounds(1, 0), Is.EqualTo(4));
    }

    /// <summary>Wave 2 adversarial: ASW unidentified contact aborts DomainNoSolution (doc 18 AC-3).</summary>
    [Test]
    public void Resolve_subsurface_ContactIdentified_false_aborts_DomainNoSolution()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(
            5_000,
            new WeaponEnvelope(100, 20_000),
            RoundsRemaining: 2,
            HasFireControlTrack: true,
            CombatDomain: CombatDomain.Subsurface,
            ContactIdentified: false,
            SubsurfaceAspectInEnvelope: true));

        var resolver = new MvpEngagementResolver(world, magazines, combatDomainsEnabled: true);
        var result = resolver.Resolve(request);

        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.DomainNoSolution));
        Assert.That(magazines.GetRounds(1, 0), Is.EqualTo(2));
    }

    [Test]
    public void Resolver_resolve_policy_denies_hold_fire_even_when_evaluator_default_allows()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        // Evaluator has no resolvePolicy override of its own; its internal fallback is
        // EffectivePolicy.DefaultFree (WeaponsFree). The resolver's own per-unit resolvePolicy
        // callback says HoldFire for this shooter, and that per-unit resolution must win —
        // it must not be silently discarded in favor of the evaluator's unrelated default.
        var evaluator = new PolicyEvaluator();
        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            evaluator,
            _ => new EffectivePolicy(RoeLevel.HoldFire));
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);

        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.RoeHoldFire));
    }

    [Test]
    public void Dlz_out_aborts_when_approaching_edge()
    {
        var world = new DictionaryEngageWorldQuery();
        var resolver = new MvpEngagementResolver(world, new MagazineLedger());
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(90_000, new WeaponEnvelope(1_000, 100_000), 5, true));

        var result = resolver.Resolve(request);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.DlzOut));
    }

    [Test]
    public void Killed_target_aborts_before_launch()
    {
        var world = new DictionaryEngageWorldQuery();
        var killed = new KilledTargetRegistry();
        killed.MarkKilled(2, "hostile-1");
        var resolver = new MvpEngagementResolver(world, new MagazineLedger(), killedTargets: killed);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.TargetDestroyed));
    }

    /// <summary>
    /// BUG-001 (QA Gauntlet, root-caused by c-sharp-architect): Resolve only checks
    /// _killedTargets.IsKilled(request.TargetId) and never checks the SHOOTER
    /// (request.ShooterUnitId). A unit already marked killed in the registry — e.g. because
    /// it was destroyed as someone else's target in an earlier tick, per the real
    /// SimulationSession/CombatOutcomeResolver kill-marking flow — must not be able to launch
    /// a brand-new engagement as a shooter in a later tick. This test drives the registry the
    /// same way "Killed_target_aborts_before_launch" above does (direct MarkKilled on the
    /// shooter's own unit id), then issues a fresh, otherwise-fully-valid engagement request
    /// from that dead shooter. EXPECTED (currently FAILING): Resolve denies the engagement
    /// (Launched == false). ACTUAL (buggy code): Resolve ignores shooter death entirely and
    /// launches, because the only IsKilled check in Resolve is keyed off TargetId, not
    /// ShooterUnitId.
    /// </summary>
    [Test]
    public void Killed_shooter_aborts_before_launch()
    {
        var world = new DictionaryEngageWorldQuery();
        var killed = new KilledTargetRegistry();
        killed.MarkKilled(1, "shooter-1"); // shooter (unit 1) was already destroyed this run.
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var resolver = new MvpEngagementResolver(world, magazines, killedTargets: killed);
        var request = new EngageRequest(1, 2, 0, 0); // dead shooter (1) attempts to fire at target 2.
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);

        Assert.That(result.Launched, Is.False, "a shooter already marked killed must not be allowed to launch a new engagement");
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.ShooterDestroyed));
        Assert.That(magazines.GetRounds(1, 0), Is.EqualTo(2)); // dead shooter must not burn rounds
    }
}
