using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Excel;
using Xunit;

namespace ProjectAegis.Data.Excel.Tests;

/// <summary>
/// PE-W1 / PLE-1.2: unit coverage for the static enum catalog matrix (no I/O).
/// </summary>
public sealed class PlatformWorkbookEnumValidationTests
{
    [Fact]
    public void Catalog_includes_emcon_and_core_governance_columns()
    {
        Assert.Contains(
            PlatformWorkbookEnumCatalog.Columns,
            c => c.SheetName == PlatformEmconEnums.EmconSheetName
                 && c.ColumnName == PlatformEmconEnums.ConditionColumn);
        Assert.Contains(
            PlatformWorkbookEnumCatalog.Columns,
            c => c.SheetName == "Sensors" && c.ColumnName == "ReviewState");
        Assert.Contains(
            PlatformWorkbookEnumCatalog.Columns,
            c => c.SheetName == "Mounts" && c.ColumnName == "MountType");
        Assert.Contains(
            PlatformWorkbookEnumCatalog.Columns,
            c => c.SheetName == "LinkCatalog" && c.ColumnName == "LinkType");
        Assert.Contains(
            PlatformWorkbookEnumCatalog.Columns,
            c => c.SheetName == "Comms" && c.ColumnName == "ValueTier");
    }

    [Fact]
    public void Review_value_tier_and_link_lists_match_catalog_constants()
    {
        Assert.Equal(
            new[]
            {
                CatalogReviewStates.Approved,
                CatalogReviewStates.Provisional,
                CatalogReviewStates.Rejected,
            },
            PlatformWorkbookEnumCatalog.ReviewStates);

        Assert.Equal(
            new[]
            {
                CatalogProvenanceTier.SourceFact,
                CatalogProvenanceTier.InterpretedValue,
                CatalogProvenanceTier.GameplayAbstraction,
            },
            PlatformWorkbookEnumCatalog.ValueTiers);

        Assert.Equal(
            new[]
            {
                CatalogLinkTypes.Strategic,
                CatalogLinkTypes.Tactical,
                CatalogLinkTypes.Voice,
                CatalogLinkTypes.Satcom,
            },
            PlatformWorkbookEnumCatalog.LinkTypes);
    }

    [Fact]
    public void ForSheet_returns_only_matching_sheet_entries()
    {
        var emcon = PlatformWorkbookEnumCatalog.ForSheet(PlatformEmconEnums.EmconSheetName).ToArray();
        Assert.Equal(2, emcon.Length);
        Assert.All(emcon, c => Assert.Equal(PlatformEmconEnums.EmconSheetName, c.SheetName));

        var mobility = PlatformWorkbookEnumCatalog.ForSheet("Mobility").ToArray();
        Assert.Empty(mobility);
    }

    [Fact]
    public void Protected_pk_columns_include_meta_key_and_domain_ids()
    {
        Assert.Contains("Key", PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns);
        Assert.Contains("PlatformId", PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns);
        Assert.Contains("SensorId", PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns);
        Assert.Contains("EmitterId", PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns);
        Assert.True(PlatformWorkbookEnumCatalog.IsMetaSheet("_Meta"));
        Assert.False(PlatformWorkbookEnumCatalog.IsMetaSheet("Emcon"));
    }

    [Fact]
    public void Catalog_column_count_meets_ple_1_2_matrix_floor()
    {
        // Sensors(3)+Mounts(2)+Loadouts(2)+Comms(5)+LinkCatalog(1)+Emcon(2) = 15
        Assert.True(
            PlatformWorkbookEnumCatalog.Columns.Count >= 12,
            $"Expected ≥12 enum columns in catalog, got {PlatformWorkbookEnumCatalog.Columns.Count}");
    }
}
