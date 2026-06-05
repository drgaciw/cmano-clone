using ProjectAegis.Sim.Engage;
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
}