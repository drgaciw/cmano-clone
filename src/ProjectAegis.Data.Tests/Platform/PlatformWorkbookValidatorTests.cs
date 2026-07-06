using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>Req-21 / ADR-011 PLE-4.2: cross-sheet fitting validation over an exported workbook.</summary>
public sealed class PlatformWorkbookValidatorTests
{
    private static PlatformWorkbook Export(
        IReadOnlyList<CatalogMount> mounts,
        IReadOnlyList<CatalogLoadout> loadouts,
        IReadOnlyList<CatalogMagazineEntry> magazines)
    {
        var data = PlatformCatalogExportData.Empty with
        {
            Platforms = new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
            Mounts = mounts,
            Loadouts = loadouts,
            Magazines = magazines,
        };
        return new PlatformWorkbookExporter().Export(data, "baltic_patrol", new FixedCatalogClock(0));
    }

    private static readonly CatalogMount[] OneVls = { new("u1", "vls-fwd", "vls", 360.0, 32) };
    private static readonly CatalogLoadout[] OneLoadout = { new("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) };

    [Fact]
    public void Clean_fitting_has_no_findings()
    {
        var wb = Export(OneVls, OneLoadout, new[]
        {
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32),
        });

        Assert.Empty(PlatformWorkbookValidator.Validate(wb));
    }

    [Fact]
    public void Dangling_mount_reference_is_flagged()
    {
        var wb = Export(OneVls, OneLoadout, new[]
        {
            new CatalogMagazineEntry("u1", "asuw-default", "ghost-mount", "mvp-weapon", 4, 0, 4),
        });

        Assert.Contains(PlatformWorkbookValidator.Validate(wb), f => f.Code == PlatformWorkbookValidator.MagazineUnknownMount);
    }

    [Fact]
    public void Dangling_loadout_reference_is_flagged()
    {
        var wb = Export(OneVls, OneLoadout, new[]
        {
            new CatalogMagazineEntry("u1", "ghost-loadout", "vls-fwd", "mvp-weapon", 4, 0, 4),
        });

        Assert.Contains(PlatformWorkbookValidator.Validate(wb), f => f.Code == PlatformWorkbookValidator.MagazineUnknownLoadout);
    }

    [Fact]
    public void Over_capacity_is_flagged_as_error()
    {
        var wb = Export(OneVls, OneLoadout, new[]
        {
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 99, 0, 99),
        });

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(wb));
        Assert.Equal(PlatformWorkbookValidator.MagazineOverCapacity, finding.Code);
        Assert.Equal(ValidationSeverity.Error, finding.Severity);
    }

    [Fact]
    public void Cumulative_over_capacity_across_weapon_types_in_same_mount_is_flagged_as_error()
    {
        // Mixed-load VLS cell: two magazine rows for the same platform/loadout/mount, each individually
        // within the mount's 32-cell capacity, but summing to 40 > 32. A real fitting like this (e.g. a
        // VLS module loaded with both ESSM and Tomahawk rounds) must be rejected as over capacity even
        // though no single row exceeds the cap on its own.
        var wb = Export(OneVls, OneLoadout, new[]
        {
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "weapon-a", 20, 0, 20),
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "weapon-b", 20, 0, 20),
        });

        Assert.Contains(
            PlatformWorkbookValidator.Validate(wb),
            f => f.Code == PlatformWorkbookValidator.MagazineOverCapacity);
    }

    [Fact]
    public void Negative_magazine_quantity_is_rejected_and_cannot_mask_cumulative_overcapacity()
    {
        // weapon-a alone already exceeds the 32-cell capacity (50 rounds). A negative Quantity row
        // (weapon-b, -20) must never be allowed to offset that cumulative sum down to a "safe" 30 —
        // negative rounds-loaded is nonsensical data and must be its own error, not a silent discount
        // that can mask a genuine over-capacity fitting.
        var wb = Export(OneVls, OneLoadout, new[]
        {
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "weapon-a", 50, 0, 50),
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "weapon-b", -20, 0, 0),
        });

        var findings = PlatformWorkbookValidator.Validate(wb);

        Assert.Contains(findings, f => f.Code == PlatformWorkbookValidator.MagazineNegativeQuantity);
        Assert.Contains(findings, f => f.Code == PlatformWorkbookValidator.MagazineOverCapacity);
    }

    [Fact]
    public void Findings_are_sorted_deterministically()
    {
        var a = PlatformWorkbookValidator.Validate(Export(OneVls, OneLoadout, new[]
        {
            new CatalogMagazineEntry("u1", "ghost-loadout", "ghost-mount", "w", 4, 0, 4),
        }));
        var b = PlatformWorkbookValidator.Validate(Export(OneVls, OneLoadout, new[]
        {
            new CatalogMagazineEntry("u1", "ghost-loadout", "ghost-mount", "w", 4, 0, 4),
        }));

        Assert.Equal(a.Select(f => f.Code), b.Select(f => f.Code));
    }

    [Fact]
    public void Phase_A_validation_regression_unchanged_with_empty_Phase_B_sheets()
    {
        var wb = Export(OneVls, OneLoadout, new[]
        {
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32),
        });

        Assert.Empty(PlatformWorkbookValidator.Validate(wb));
    }

    [Fact]
    public void Phase_B_populated_export_passes_validator_when_fitting_is_clean()
    {
        var data = PlatformCatalogExportData.Empty with
        {
            Platforms = new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
            Mounts = OneVls,
            Loadouts = OneLoadout,
            Magazines = new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32) },
            Mobility = new[] { new CatalogMobility("u1", MaxSpeedKnots: 30, RangeNm: 4000) },
            Signatures = new[] { new CatalogSignature("u1", RcsBandDbsm: -10) },
            Emcon = new[] { new CatalogEmcon("u1", "restricted", "cmo-sensor-1", "standby") },
        };
        var wb = new PlatformWorkbookExporter().Export(data, "baltic_patrol", new FixedCatalogClock(0));

        Assert.Empty(PlatformWorkbookValidator.Validate(wb));
    }
}
