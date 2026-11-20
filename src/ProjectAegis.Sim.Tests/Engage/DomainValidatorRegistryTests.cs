using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class DomainValidatorRegistryTests
{
    [Test]
    public void MvpStubs_allow_air_surface_and_subsurface_engagements()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Air);

        Assert.That(registry.Validate(CombatDomain.Air, in ctx).Allowed, Is.True);
        var surfaceCtx = ctx with { CombatDomain = CombatDomain.Surface };
        Assert.That(registry.Validate(CombatDomain.Surface, in surfaceCtx).Allowed, Is.True);
        var subsurfaceCtx = ctx with { CombatDomain = CombatDomain.Subsurface };
        Assert.That(registry.Validate(CombatDomain.Subsurface, in subsurfaceCtx).Allowed, Is.True);
        var landCtx = ctx with { CombatDomain = CombatDomain.Land };
        Assert.That(registry.Validate(CombatDomain.Land, in landCtx).Allowed, Is.True);
        var mineCtx = ctx with { CombatDomain = CombatDomain.Mine };
        Assert.That(registry.Validate(CombatDomain.Mine, in mineCtx).Allowed, Is.True);
        var facilityCtx = ctx with { CombatDomain = CombatDomain.Facility };
        Assert.That(registry.Validate(CombatDomain.Facility, in facilityCtx).Allowed, Is.True);
    }

    [Test]
    public void Registry_iterates_validators_in_stable_domain_order()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var domains = registry.Validators.Select(v => v.Domain).ToArray();
        Assert.That(
            domains,
            Is.EqualTo(new[] { CombatDomain.Air,
                        CombatDomain.Surface,
                        CombatDomain.Subsurface,
                        CombatDomain.Land,
                        CombatDomain.Mine,
                        CombatDomain.Facility, }));
    }

    [Test]
    public void CombatDomainsEnabled_false_skips_registry_in_resolver()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var ctx = new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true);
        world.Set(request, ctx);

        var denyRegistry = new DomainValidatorRegistry(
        [
            new DenyAllDomainValidator(CombatDomain.Air),
        ]);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: false,
            domainValidators: denyRegistry);

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.True);
    }

    [Test]
    public void CombatDomainsEnabled_true_invokes_registry_and_noop_allows()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var ctx = new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true);
        world.Set(request, ctx);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: true,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.True);
    }

    [Test]
    public void MvpFallback_combatDomainsEnabled_defaults_false()
    {
        Assert.That(ScenarioEngageDefaults.MvpFallback.CombatDomainsEnabled, Is.False);
    }

    [Test]
    public void CombatDomainsEnabled_true_aspect_blocked_aborts_with_AirAspectBlock()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            AirAspectInEnvelope: false);
        world.Set(request, ctx);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: true,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.AirAspectBlock));
    }

    [Test]
    public void Validator_deny_maps_to_AIR_ASPECT_BLOCK_order_log_code()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            AirAspectInEnvelope: false);

        var result = registry.Validate(CombatDomain.Air, in ctx);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.AirAspectBlock));

        var mapped = MapDomainDenialForTest(result.AbortReason!.Value);
        Assert.That(EngagementAbortReasonCodes.ToLogCode(mapped), Is.EqualTo(AbortReasonCatalog.Engage.AIR_ASPECT_BLOCK));
    }

    [Test]
    public void CombatDomainsEnabled_true_surface_aspect_blocked_aborts_with_SurfaceAspectBlock()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Surface,
            SurfaceAspectInEnvelope: false);
        world.Set(request, ctx);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: true,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.SurfaceAspectBlock));
    }

    [Test]
    public void CombatDomainsEnabled_true_subsurface_aspect_blocked_aborts_with_SubsurfaceAspectBlock()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Subsurface,
            SubsurfaceAspectInEnvelope: false);
        world.Set(request, ctx);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: true,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.SubsurfaceAspectBlock));
    }

    [Test]
    public void Validator_deny_maps_to_SURFACE_ASPECT_BLOCK_order_log_code()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Surface,
            SurfaceAspectInEnvelope: false);

        var result = registry.Validate(CombatDomain.Surface, in ctx);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.SurfaceAspectBlock));

        var mapped = MapDomainDenialForTest(result.AbortReason!.Value);
        Assert.That(EngagementAbortReasonCodes.ToLogCode(mapped), Is.EqualTo(AbortReasonCatalog.Engage.SURFACE_ASPECT_BLOCK));
    }

    [Test]
    public void Validator_deny_maps_to_SUBSURFACE_ASPECT_BLOCK_order_log_code()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Subsurface,
            SubsurfaceAspectInEnvelope: false);

        var result = registry.Validate(CombatDomain.Subsurface, in ctx);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.SubsurfaceAspectBlock));

        var mapped = MapDomainDenialForTest(result.AbortReason!.Value);
        Assert.That(EngagementAbortReasonCodes.ToLogCode(mapped), Is.EqualTo(AbortReasonCatalog.Engage.SUBSURFACE_ASPECT_BLOCK));
    }

    [Test]
    public void CombatDomainsEnabled_true_land_aspect_blocked_aborts_with_LandAspectBlock()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Land,
            LandAspectInEnvelope: false);
        world.Set(request, ctx);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: true,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.LandAspectBlock));
    }

    [Test]
    public void Validator_deny_maps_to_LAND_ASPECT_BLOCK_order_log_code()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Land,
            LandAspectInEnvelope: false);

        var result = registry.Validate(CombatDomain.Land, in ctx);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.LandAspectBlock));

        var mapped = MapDomainDenialForTest(result.AbortReason!.Value);
        Assert.That(EngagementAbortReasonCodes.ToLogCode(mapped), Is.EqualTo(AbortReasonCatalog.Engage.LAND_ASPECT_BLOCK));
    }

    [Test]
    public void CombatDomainsEnabled_true_mine_aspect_blocked_aborts_with_MineAspectBlock()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Mine,
            MineAspectInEnvelope: false);
        world.Set(request, ctx);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: true,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.MineAspectBlock));
    }

    [Test]
    public void CombatDomainsEnabled_true_mine_aspect_allowed_launches_successfully()
    {
        // ADR-009's MineAspectDomainValidator is a real (non-stub) geometry/aspect gate, exactly
        // like the Air/Surface/Subsurface/Land/Facility validators, and is meant to be the
        // authoritative gate for CombatDomain.Mine once combatDomainsEnabled is on (see
        // adr-009-combat-domain-validators.md: mines are a "clear extension point ... without
        // forking engage core"). But the legacy req-18 CombatDomainValidator.Validate — which
        // MvpEngagementResolver.Resolve always runs unconditionally, regardless of the
        // combatDomainsEnabled flag — still hardcodes `CombatDomain.Mine =>
        // EngagementAbortReason.DomainNoSolution` with no escape hatch. Unlike every other
        // domain (Facility has no legacy special-case at all; Air/Surface are unconditionally
        // allowed; Subsurface/Land are only conditionally blocked), Mine can therefore never
        // launch through the full resolver no matter what the new aspect validator decides.
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Mine,
            MineAspectInEnvelope: true);
        world.Set(request, ctx);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: true,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var result = resolver.Resolve(request);

        Assert.That(result.Launched, Is.True);
        Assert.That(magazines.GetRounds(1, 0), Is.EqualTo(1));
    }

    [Test]
    public void Validator_deny_maps_to_MINE_ASPECT_BLOCK_order_log_code()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Mine,
            MineAspectInEnvelope: false);

        var result = registry.Validate(CombatDomain.Mine, in ctx);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.MineAspectBlock));

        var mapped = MapDomainDenialForTest(result.AbortReason!.Value);
        Assert.That(EngagementAbortReasonCodes.ToLogCode(mapped), Is.EqualTo(AbortReasonCatalog.Engage.MINE_ASPECT_BLOCK));
    }

    [Test]
    public void CombatDomainsEnabled_true_facility_aspect_blocked_aborts_with_FacilityAspectBlock()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Facility,
            FacilityAspectInEnvelope: false);
        world.Set(request, ctx);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: true,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var result = resolver.Resolve(request);
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.FacilityAspectBlock));
    }

    [Test]
    public void Validator_deny_maps_to_FACILITY_ASPECT_BLOCK_order_log_code()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Facility,
            FacilityAspectInEnvelope: false);

        var result = registry.Validate(CombatDomain.Facility, in ctx);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.FacilityAspectBlock));

        var mapped = MapDomainDenialForTest(result.AbortReason!.Value);
        Assert.That(EngagementAbortReasonCodes.ToLogCode(mapped), Is.EqualTo(AbortReasonCatalog.Engage.FACILITY_ASPECT_BLOCK));
    }

    [Test]
    public void Baltic_flag_off_zero_abort_delta_despite_deny_registry()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var blockedCtx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            AirAspectInEnvelope: false);
        world.Set(request, blockedCtx);

        var denyRegistry = new DomainValidatorRegistry(
        [
            new AirAspectDomainValidator(),
            new SurfaceAspectDomainValidator(),
            new SubsurfaceAspectDomainValidator(),
            new LandAspectDomainValidator(),
            new MineAspectDomainValidator(),
            new FacilityAspectDomainValidator(),
        ]);

        var flagOffResolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: false,
            domainValidators: denyRegistry);
        var flagOffResult = flagOffResolver.Resolve(request);

        var flagOffDefaultResolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: false,
            domainValidators: DomainValidatorRegistry.MvpStubs);
        var flagOffDefaultResult = flagOffDefaultResolver.Resolve(request);

        Assert.That(flagOffResult.Launched, Is.True);
        Assert.That(flagOffDefaultResult.Launched, Is.True);
        Assert.That(flagOffResult.Launched, Is.EqualTo(flagOffDefaultResult.Launched));
        Assert.That(flagOffResult.AbortReason, Is.EqualTo(flagOffDefaultResult.AbortReason));
    }

    [Test]
    public void Baltic_flag_off_facility_domain_zero_delta_despite_facility_validator_in_registry()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var blockedCtx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Facility,
            FacilityAspectInEnvelope: false);
        world.Set(request, blockedCtx);

        var withoutFacilityRegistry = new DomainValidatorRegistry(
        [
            new AirAspectDomainValidator(),
            new SurfaceAspectDomainValidator(),
            new SubsurfaceAspectDomainValidator(),
            new LandAspectDomainValidator(),
            new MineAspectDomainValidator(),
        ]);

        var flagOffWithoutFacility = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: false,
            domainValidators: withoutFacilityRegistry);
        var flagOffWithFacility = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: false,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var withoutFacilityResult = flagOffWithoutFacility.Resolve(request);
        var withFacilityResult = flagOffWithFacility.Resolve(request);

        Assert.That(withFacilityResult.Launched, Is.EqualTo(withoutFacilityResult.Launched));
        Assert.That(withFacilityResult.AbortReason, Is.EqualTo(withoutFacilityResult.AbortReason));
    }

    [Test]
    public void Baltic_flag_off_mine_domain_zero_delta_despite_mine_validator_in_registry()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var blockedCtx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Mine,
            MineAspectInEnvelope: false);
        world.Set(request, blockedCtx);

        var withoutMineRegistry = new DomainValidatorRegistry(
        [
            new AirAspectDomainValidator(),
            new SurfaceAspectDomainValidator(),
            new SubsurfaceAspectDomainValidator(),
            new LandAspectDomainValidator(),
        ]);

        var flagOffWithoutMine = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: false,
            domainValidators: withoutMineRegistry);
        var flagOffWithMine = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: false,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var withoutMineResult = flagOffWithoutMine.Resolve(request);
        var withMineResult = flagOffWithMine.Resolve(request);

        Assert.That(withMineResult.Launched, Is.EqualTo(withoutMineResult.Launched));
        Assert.That(withMineResult.AbortReason, Is.EqualTo(withoutMineResult.AbortReason));
    }

    [TestCase(CombatDomain.Surface, false, true, true, true, true)]
    [TestCase(CombatDomain.Subsurface, true, false, true, true, true)]
    [TestCase(CombatDomain.Land, true, true, false, true, true)]
    [TestCase(CombatDomain.Facility, true, true, true, true, false)]
    public void Baltic_flag_off_zero_abort_delta_for_domain_aspect_blocks(
        CombatDomain domain,
        bool surfaceAspectInEnvelope,
        bool subsurfaceAspectInEnvelope,
        bool landAspectInEnvelope,
        bool mineAspectInEnvelope,
        bool facilityAspectInEnvelope)
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        var blockedCtx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: domain,
            SurfaceAspectInEnvelope: surfaceAspectInEnvelope,
            SubsurfaceAspectInEnvelope: subsurfaceAspectInEnvelope,
            LandAspectInEnvelope: landAspectInEnvelope,
            MineAspectInEnvelope: mineAspectInEnvelope,
            FacilityAspectInEnvelope: facilityAspectInEnvelope);
        world.Set(request, blockedCtx);

        var flagOffResolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: false,
            domainValidators: DomainValidatorRegistry.MvpStubs);
        var flagOnResolver = new MvpEngagementResolver(
            world,
            magazines,
            combatDomainsEnabled: true,
            domainValidators: DomainValidatorRegistry.MvpStubs);

        var flagOffResult = flagOffResolver.Resolve(request);
        var flagOnResult = flagOnResolver.Resolve(request);

        Assert.That(flagOffResult.Launched, Is.True);
        Assert.That(flagOnResult.Launched, Is.False);
    }

    private static EngagementAbortReason MapDomainDenialForTest(FireAbortReason reason) =>
        reason switch
        {
            FireAbortReason.NoFireControlTrack => EngagementAbortReason.NoFireControlTrack,
            FireAbortReason.EmconOff => EngagementAbortReason.EmconOff,
            FireAbortReason.AirAspectBlock => EngagementAbortReason.AirAspectBlock,
            FireAbortReason.SurfaceAspectBlock => EngagementAbortReason.SurfaceAspectBlock,
            FireAbortReason.SubsurfaceAspectBlock => EngagementAbortReason.SubsurfaceAspectBlock,
            FireAbortReason.LandAspectBlock => EngagementAbortReason.LandAspectBlock,
            FireAbortReason.MineAspectBlock => EngagementAbortReason.MineAspectBlock,
            FireAbortReason.FacilityAspectBlock => EngagementAbortReason.FacilityAspectBlock,
            _ => EngagementAbortReason.DomainNoSolution,
        };

    private sealed class DenyAllDomainValidator(CombatDomain domain) : IDomainValidator
    {
        public CombatDomain Domain { get; } = domain;

        public DomainValidateResult Validate(in EngageContext ctx) =>
            DomainValidateResult.Deny(FireAbortReason.NoFireControlTrack);
    }
}