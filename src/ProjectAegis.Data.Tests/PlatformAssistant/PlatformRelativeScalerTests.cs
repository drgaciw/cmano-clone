using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.PlatformAssistant;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.PlatformAssistant;

public sealed class PlatformRelativeScalerTests
{
    [Test]
    public void Light_role_produces_lower_hp_and_radius_than_heavy_with_same_peers()
    {
        var export = BuildVariedExport();
        var light = PlatformRelativeScaler.Scale(export, new PlatformDesignBrief(
            "opv-light",
            "OPV Light",
            Domain: "surface",
            RoleWeight: "light",
            PeerPlatformIds: ["peer-light", "peer-mid", "peer-heavy"]));
        var heavy = PlatformRelativeScaler.Scale(export, new PlatformDesignBrief(
            "opv-heavy",
            "OPV Heavy",
            Domain: "surface",
            RoleWeight: "heavy",
            PeerPlatformIds: ["peer-light", "peer-mid", "peer-heavy"]));

        Assert.That(light.Damage.MaxHp <= heavy.Damage.MaxHp, Is.True);
        Assert.That(light.CombatRadiusNm <= heavy.CombatRadiusNm, Is.True);
        Assert.That(light.Mobility.MaxSpeedKnots <= heavy.Mobility.MaxSpeedKnots, Is.True);
    }

    [Test]
    public void Unique_id_on_collision_suffixes()
    {
        var export = BuildVariedExport();
        var proposal = PlatformRelativeScaler.Scale(export, new PlatformDesignBrief(
            "peer-mid",
            "Collision",
            PeerPlatformIds: ["peer-light", "peer-mid"]));

        Assert.That(proposal.Binding.PlatformId, Is.EqualTo("peer-mid-2"));
        Assert.That(proposal.Damage.PlatformId, Is.EqualTo("peer-mid-2"));
    }

    [Test]
    public void Provenance_citation_and_skills_include_assistant_pack()
    {
        var export = BuildVariedExport();
        var proposal = PlatformRelativeScaler.Scale(export, new PlatformDesignBrief(
            "new-plt",
            "New",
            WhatIf: true,
            PeerPlatformIds: ["peer-mid"]));

        Assert.That(proposal.Binding.CitationRef, Does.StartWith("assistant:"));
        Assert.That(proposal.SkillsApplied, Does.Contain(PlatformRelativeScaler.SkillCatalogGrounding));
        Assert.That(proposal.SkillsApplied, Does.Contain(PlatformRelativeScaler.SkillRelativeScaling));
        Assert.That(proposal.SkillsApplied, Does.Contain(PlatformRelativeScaler.SkillWhatIf));
        Assert.That(proposal.Summary, Does.Contain("What-if").IgnoreCase);
    }

    [Test]
    public void Scale_sets_ApplyCorePosition_and_binding_core_fields()
    {
        var export = BuildVariedExport();
        var proposal = PlatformRelativeScaler.Scale(export, new PlatformDesignBrief(
            "new-core",
            "New Core",
            PeerPlatformIds: ["peer-mid"]));

        Assert.That(proposal.Binding.ApplyCorePosition, Is.True);
        Assert.That(proposal.Binding.CombatRadiusNm, Is.EqualTo(proposal.CombatRadiusNm).Within(0.0005));
        Assert.That(proposal.Binding.LatDeg, Is.EqualTo(proposal.LatDeg).Within(0.0005));
        Assert.That(proposal.Binding.LonDeg, Is.EqualTo(proposal.LonDeg).Within(0.0005));
        Assert.That(proposal.LatDeg, Is.EqualTo(57.5).Within(0.0005));
        Assert.That(proposal.LonDeg, Is.EqualTo(20.5).Within(0.0005));
    }

    [Test]
    public void WeightedToward_light_closer_to_min()
    {
        var values = new[] { 10.0, 50.0, 90.0 };
        var light = PlatformRelativeScaler.WeightedToward(values, 0.25, 0);
        var heavy = PlatformRelativeScaler.WeightedToward(values, 0.75, 0);
        Assert.That(light < heavy, Is.True);
        Assert.That(light <= 50, Is.True);
        Assert.That(heavy >= 50, Is.True);
    }

    private static PlatformCatalogExportData BuildVariedExport() =>
        new(
            Platforms:
            [
                new CatalogPlatformEntry("peer-light", 57, 20, 80),
                new CatalogPlatformEntry("peer-mid", 57.5, 20.5, 200),
                new CatalogPlatformEntry("peer-heavy", 58, 21, 400),
            ],
            Sensors: [],
            Mounts: [],
            Loadouts: [],
            Magazines: [],
            Comms: [],
            Links: [],
            Mobility:
            [
                new CatalogMobility("peer-light", MaxSpeedKnots: 18),
                new CatalogMobility("peer-mid", MaxSpeedKnots: 28),
                new CatalogMobility("peer-heavy", MaxSpeedKnots: 32),
            ],
            Damage:
            [
                new CatalogPlatformDamage("peer-light", MaxHp: 80, WithdrawThresholdPct: 40),
                new CatalogPlatformDamage("peer-mid", MaxHp: 200, WithdrawThresholdPct: 25),
                new CatalogPlatformDamage("peer-heavy", MaxHp: 400, WithdrawThresholdPct: 15),
            ]);
}
