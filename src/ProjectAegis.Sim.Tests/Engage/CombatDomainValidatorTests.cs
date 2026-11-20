using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class CombatDomainValidatorTests
{
    [Test]
    public void Mount_offline_aborts_before_domain_rules()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            MountOnline: false);
        Assert.That(CombatDomainValidator.Validate(CombatDomain.Air, in ctx), Is.EqualTo(EngagementAbortReason.MountOffline));
    }

    [Test]
    public void Subsurface_requires_identified_contact()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            CombatDomain: CombatDomain.Subsurface,
            ContactIdentified: false);
        Assert.That(CombatDomainValidator.Validate(CombatDomain.Subsurface, in ctx), Is.EqualTo(EngagementAbortReason.DomainNoSolution));
    }

    [Test]
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

        Assert.That(validator.Validate(in ctx).Allowed, Is.True);
    }

    [Test]
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
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.SurfaceAspectBlock));
    }

    [Test]
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

        Assert.That(validator.Validate(in ctx).Allowed, Is.True);
    }

    [Test]
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
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.SubsurfaceAspectBlock));
    }

    [Test]
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

        Assert.That(validator.Validate(in ctx).Allowed, Is.True);
    }

    [Test]
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
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.LandAspectBlock));
    }

    [Test]
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
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.LandAspectBlock));
    }

    [Test]
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

        Assert.That(validator.Validate(in ctx).Allowed, Is.True);
    }

    [Test]
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
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.MineAspectBlock));
    }

    [Test]
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
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.MineAspectBlock));
    }

    [Test]
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

        Assert.That(validator.Validate(in ctx).Allowed, Is.True);
    }

    [Test]
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
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.FacilityAspectBlock));
    }

    [Test]
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
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(FireAbortReason.FacilityAspectBlock));
    }
}