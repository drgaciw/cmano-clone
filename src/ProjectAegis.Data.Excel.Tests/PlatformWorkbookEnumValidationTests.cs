using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Excel.Tests;

/// <summary>
/// PE-W1 / PLE-1.2: unit coverage for the static enum catalog matrix (no I/O).
/// </summary>
public sealed class PlatformWorkbookEnumValidationTests
{
    [Test]
    public void Catalog_includes_emcon_and_core_governance_columns()
    {
        Assert.That(
            PlatformWorkbookEnumCatalog.Columns,
            Has.Some.Matches<PlatformWorkbookEnumCatalog.EnumColumn>(
                c => c is not null
                     && c.SheetName == PlatformEmconEnums.EmconSheetName
                     && c.ColumnName == PlatformEmconEnums.ConditionColumn));
        Assert.That(
            PlatformWorkbookEnumCatalog.Columns,
            Has.Some.Matches<PlatformWorkbookEnumCatalog.EnumColumn>(
                c => c is not null && c.SheetName == "Sensors" && c.ColumnName == "ReviewState"));
        Assert.That(
            PlatformWorkbookEnumCatalog.Columns,
            Has.Some.Matches<PlatformWorkbookEnumCatalog.EnumColumn>(
                c => c is not null && c.SheetName == "Mounts" && c.ColumnName == "MountType"));
        Assert.That(
            PlatformWorkbookEnumCatalog.Columns,
            Has.Some.Matches<PlatformWorkbookEnumCatalog.EnumColumn>(
                c => c is not null && c.SheetName == "LinkCatalog" && c.ColumnName == "LinkType"));
        Assert.That(
            PlatformWorkbookEnumCatalog.Columns,
            Has.Some.Matches<PlatformWorkbookEnumCatalog.EnumColumn>(
                c => c is not null && c.SheetName == "Comms" && c.ColumnName == "ValueTier"));
    }

    [Test]
    public void Review_value_tier_and_link_lists_match_catalog_constants()
    {
        Assert.That(
            PlatformWorkbookEnumCatalog.ReviewStates,
            Is.EqualTo(new[]
            {
                CatalogReviewStates.Approved,
                CatalogReviewStates.Provisional,
                CatalogReviewStates.Rejected,
            }));

        Assert.That(
            PlatformWorkbookEnumCatalog.ValueTiers,
            Is.EqualTo(new[]
            {
                CatalogProvenanceTier.SourceFact,
                CatalogProvenanceTier.InterpretedValue,
                CatalogProvenanceTier.GameplayAbstraction,
            }));

        Assert.That(
            PlatformWorkbookEnumCatalog.LinkTypes,
            Is.EqualTo(new[]
            {
                CatalogLinkTypes.Strategic,
                CatalogLinkTypes.Tactical,
                CatalogLinkTypes.Voice,
                CatalogLinkTypes.Satcom,
            }));
    }

    [Test]
    public void ForSheet_returns_only_matching_sheet_entries()
    {
        var emcon = PlatformWorkbookEnumCatalog.ForSheet(PlatformEmconEnums.EmconSheetName).ToArray();
        Assert.That(emcon, Has.Length.EqualTo(2));
        Assert.That(
            emcon,
            Is.All.Matches<PlatformWorkbookEnumCatalog.EnumColumn>(
                c => c is not null && c.SheetName == PlatformEmconEnums.EmconSheetName));

        var mobility = PlatformWorkbookEnumCatalog.ForSheet("Mobility").ToArray();
        Assert.That(mobility, Is.Empty);
    }

    [Test]
    public void Protected_pk_columns_include_meta_key_and_domain_ids()
    {
        Assert.That(PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns, Does.Contain("Key"));
        Assert.That(PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns, Does.Contain("PlatformId"));
        Assert.That(PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns, Does.Contain("SensorId"));
        Assert.That(PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns, Does.Contain("EmitterId"));
        Assert.That(PlatformWorkbookEnumCatalog.IsMetaSheet("_Meta"), Is.True);
        Assert.That(PlatformWorkbookEnumCatalog.IsMetaSheet("Emcon"), Is.False);
    }

    [Test]
    public void Catalog_column_count_meets_ple_1_2_matrix_floor()
    {
        // Sensors(3)+Mounts(2)+Loadouts(2)+Comms(5)+LinkCatalog(1)+Emcon(2) = 15
        Assert.That(
            PlatformWorkbookEnumCatalog.Columns.Count >= 12,
            Is.True,
            $"Expected ≥12 enum columns in catalog, got {PlatformWorkbookEnumCatalog.Columns.Count}");
    }
}
