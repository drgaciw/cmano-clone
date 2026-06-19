using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class CombatDomainValidatorTests
{
    [Fact]
    public void Mount_offline_aborts_before_domain_rules()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            MountOnline: false);
        Assert.Equal(EngagementAbortReason.MountOffline, CombatDomainValidator.Validate(CombatDomain.Air, in ctx));
    }

    [Fact]
    public void Subsurface_requires_identified_contact()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Subsurface,
            ContactIdentified: false);
        Assert.Equal(EngagementAbortReason.DomainNoSolution, CombatDomainValidator.Validate(CombatDomain.Subsurface, in ctx));
    }

    [Fact]
    public void Surface_aspect_validator_allows_in_envelope()
    {
        var validator = new SurfaceAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Surface,
            SurfaceAspectInEnvelope: true);

        Assert.True(validator.Validate(in ctx).Allowed);
    }

    [Fact]
    public void Surface_aspect_validator_denies_out_of_aspect_with_abort_code()
    {
        var validator = new SurfaceAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Surface,
            SurfaceAspectInEnvelope: false);

        var result = validator.Validate(in ctx);
        Assert.False(result.Allowed);
        Assert.Equal(FireAbortReason.SurfaceAspectBlock, result.AbortReason);
    }

    [Fact]
    public void Subsurface_aspect_validator_allows_in_envelope()
    {
        var validator = new SubsurfaceAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Subsurface,
            SubsurfaceAspectInEnvelope: true);

        Assert.True(validator.Validate(in ctx).Allowed);
    }

    [Fact]
    public void Subsurface_aspect_validator_denies_out_of_aspect_with_abort_code()
    {
        var validator = new SubsurfaceAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Subsurface,
            SubsurfaceAspectInEnvelope: false);

        var result = validator.Validate(in ctx);
        Assert.False(result.Allowed);
        Assert.Equal(FireAbortReason.SubsurfaceAspectBlock, result.AbortReason);
    }

    [Fact]
    public void Land_aspect_validator_allows_in_envelope()
    {
        var validator = new LandAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Land,
            LandAspectInEnvelope: true);

        Assert.True(validator.Validate(in ctx).Allowed);
    }

    [Fact]
    public void Land_aspect_validator_denies_out_of_aspect_with_abort_code()
    {
        var validator = new LandAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Land,
            LandAspectInEnvelope: false);

        var result = validator.Validate(in ctx);
        Assert.False(result.Allowed);
        Assert.Equal(FireAbortReason.LandAspectBlock, result.AbortReason);
    }

    [Fact]
    public void Land_aspect_validator_denies_out_of_range_at_envelope_boundary()
    {
        var validator = new LandAspectDomainValidator();
        var ctx = new EngageContext(
            100_001,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Land,
            LandAspectInEnvelope: true);

        var result = validator.Validate(in ctx);
        Assert.False(result.Allowed);
        Assert.Equal(FireAbortReason.LandAspectBlock, result.AbortReason);
    }

    [Fact]
    public void Mine_aspect_validator_allows_in_envelope()
    {
        var validator = new MineAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Mine,
            MineAspectInEnvelope: true);

        Assert.True(validator.Validate(in ctx).Allowed);
    }

    [Fact]
    public void Mine_aspect_validator_denies_out_of_aspect_with_abort_code()
    {
        var validator = new MineAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Mine,
            MineAspectInEnvelope: false);

        var result = validator.Validate(in ctx);
        Assert.False(result.Allowed);
        Assert.Equal(FireAbortReason.MineAspectBlock, result.AbortReason);
    }

    [Fact]
    public void Mine_aspect_validator_denies_out_of_range_at_envelope_boundary()
    {
        var validator = new MineAspectDomainValidator();
        var ctx = new EngageContext(
            100_001,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Mine,
            MineAspectInEnvelope: true);

        var result = validator.Validate(in ctx);
        Assert.False(result.Allowed);
        Assert.Equal(FireAbortReason.MineAspectBlock, result.AbortReason);
    }

    [Fact]
    public void Facility_aspect_validator_allows_in_envelope()
    {
        var validator = new FacilityAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Facility,
            FacilityAspectInEnvelope: true);

        Assert.True(validator.Validate(in ctx).Allowed);
    }

    [Fact]
    public void Facility_aspect_validator_denies_out_of_aspect_with_abort_code()
    {
        var validator = new FacilityAspectDomainValidator();
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Facility,
            FacilityAspectInEnvelope: false);

        var result = validator.Validate(in ctx);
        Assert.False(result.Allowed);
        Assert.Equal(FireAbortReason.FacilityAspectBlock, result.AbortReason);
    }

    [Fact]
    public void Facility_aspect_validator_denies_out_of_range_at_envelope_boundary()
    {
        var validator = new FacilityAspectDomainValidator();
        var ctx = new EngageContext(
            100_001,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Facility,
            FacilityAspectInEnvelope: true);

        var result = validator.Validate(in ctx);
        Assert.False(result.Allowed);
        Assert.Equal(FireAbortReason.FacilityAspectBlock, result.AbortReason);
    }
}