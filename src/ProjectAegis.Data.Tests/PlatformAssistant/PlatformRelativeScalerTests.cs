using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.PlatformAssistant;
using Xunit;

namespace ProjectAegis.Data.Tests.PlatformAssistant;

public sealed class PlatformRelativeScalerTests
{
    [Fact]
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

        Assert.True(light.Damage.MaxHp <= heavy.Damage.MaxHp);
        Assert.True(light.CombatRadiusNm <= heavy.CombatRadiusNm);
        Assert.True(light.Mobility.MaxSpeedKnots <= heavy.Mobility.MaxSpeedKnots);
    }

    [Fact]
    public void Unique_id_on_collision_suffixes()
    {
        var export = BuildVariedExport();
        var proposal = PlatformRelativeScaler.Scale(export, new PlatformDesignBrief(
            "peer-mid",
            "Collision",
            PeerPlatformIds: ["peer-light", "peer-mid"]));

        Assert.Equal("peer-mid-2", proposal.Binding.PlatformId);
        Assert.Equal("peer-mid-2", proposal.Damage.PlatformId);
    }

    [Fact]
    public void Provenance_citation_and_skills_include_assistant_pack()
    {
        var export = BuildVariedExport();
        var proposal = PlatformRelativeScaler.Scale(export, new PlatformDesignBrief(
            "new-plt",
            "New",
            WhatIf: true,
            PeerPlatformIds: ["peer-mid"]));

        Assert.StartsWith("assistant:", proposal.Binding.CitationRef, StringComparison.Ordinal);
        Assert.Contains(PlatformRelativeScaler.SkillCatalogGrounding, proposal.SkillsApplied);
        Assert.Contains(PlatformRelativeScaler.SkillRelativeScaling, proposal.SkillsApplied);
        Assert.Contains(PlatformRelativeScaler.SkillWhatIf, proposal.SkillsApplied);
        Assert.Contains("What-if", proposal.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Scale_sets_ApplyCorePosition_and_binding_core_fields()
    {
        var export = BuildVariedExport();
        var proposal = PlatformRelativeScaler.Scale(export, new PlatformDesignBrief(
            "new-core",
            "New Core",
            PeerPlatformIds: ["peer-mid"]));

        Assert.True(proposal.Binding.ApplyCorePosition);
        Assert.Equal(proposal.CombatRadiusNm, proposal.Binding.CombatRadiusNm, precision: 4);
        Assert.Equal(proposal.LatDeg, proposal.Binding.LatDeg, precision: 4);
        Assert.Equal(proposal.LonDeg, proposal.Binding.LonDeg, precision: 4);
        Assert.Equal(57.5, proposal.LatDeg, precision: 4);
        Assert.Equal(20.5, proposal.LonDeg, precision: 4);
    }

    [Fact]
    public void WeightedToward_light_closer_to_min()
    {
        var values = new[] { 10.0, 50.0, 90.0 };
        var light = PlatformRelativeScaler.WeightedToward(values, 0.25, 0);
        var heavy = PlatformRelativeScaler.WeightedToward(values, 0.75, 0);
        Assert.True(light < heavy);
        Assert.True(light <= 50);
        Assert.True(heavy >= 50);
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
