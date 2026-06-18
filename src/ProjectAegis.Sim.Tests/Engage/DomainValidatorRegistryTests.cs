using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class DomainValidatorRegistryTests
{
    [Fact]
    public void MvpStubs_allow_air_and_surface_engagements()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Air);

        Assert.True(registry.Validate(CombatDomain.Air, in ctx).Allowed);
        var surfaceCtx = ctx with { CombatDomain = CombatDomain.Surface };
        Assert.True(registry.Validate(CombatDomain.Surface, in surfaceCtx).Allowed);
    }

    [Fact]
    public void Registry_iterates_validators_in_stable_domain_order()
    {
        var registry = DomainValidatorRegistry.MvpStubs;
        var domains = registry.Validators.Select(v => v.Domain).ToArray();
        Assert.Equal([CombatDomain.Air, CombatDomain.Surface], domains);
    }

    [Fact]
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
        Assert.True(result.Launched);
    }

    [Fact]
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
        Assert.True(result.Launched);
    }

    [Fact]
    public void MvpFallback_combatDomainsEnabled_defaults_false()
    {
        Assert.False(ScenarioEngageDefaults.MvpFallback.CombatDomainsEnabled);
    }

    [Fact]
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
        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.AirAspectBlock, result.AbortReason);
    }

    [Fact]
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
        Assert.False(result.Allowed);
        Assert.Equal(FireAbortReason.AirAspectBlock, result.AbortReason);

        var mapped = MapDomainDenialForTest(result.AbortReason!.Value);
        Assert.Equal(AbortReasonCatalog.Engage.AIR_ASPECT_BLOCK, EngagementAbortReasonCodes.ToLogCode(mapped));
    }

    [Fact]
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

        Assert.True(flagOffResult.Launched);
        Assert.True(flagOffDefaultResult.Launched);
        Assert.Equal(flagOffDefaultResult.Launched, flagOffResult.Launched);
        Assert.Equal(flagOffDefaultResult.AbortReason, flagOffResult.AbortReason);
    }

    private static EngagementAbortReason MapDomainDenialForTest(FireAbortReason reason) =>
        reason switch
        {
            FireAbortReason.NoFireControlTrack => EngagementAbortReason.NoFireControlTrack,
            FireAbortReason.EmconOff => EngagementAbortReason.EmconOff,
            FireAbortReason.AirAspectBlock => EngagementAbortReason.AirAspectBlock,
            _ => EngagementAbortReason.DomainNoSolution,
        };

    private sealed class DenyAllDomainValidator(CombatDomain domain) : IDomainValidator
    {
        public CombatDomain Domain { get; } = domain;

        public DomainValidateResult Validate(in EngageContext ctx) =>
            DomainValidateResult.Deny(FireAbortReason.NoFireControlTrack);
    }
}