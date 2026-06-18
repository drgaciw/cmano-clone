using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogEntityMapTests
{
    [Fact]
    public void Entity_map_includes_sensor_and_audit_tables()
    {
        Assert.True(CatalogEntityMap.TryGetTable("CatalogSensorBinding", out var sensor));
        Assert.Equal("sensor", sensor.TableName);

        Assert.True(CatalogEntityMap.TryGetTable("CatalogChangeLogEntry", out var audit));
        Assert.Equal("catalog_change_log", audit.TableName);
    }

    [Theory]
    [InlineData("CatalogMount", "platform_mount", "platform_id ASC, mount_id ASC")]
    [InlineData("CatalogLoadout", "platform_loadout", "platform_id ASC, loadout_id ASC")]
    [InlineData("CatalogMagazineEntry", "platform_magazine", "platform_id ASC, loadout_id ASC, mount_id ASC, weapon_id ASC")]
    [InlineData("CatalogCommsBinding", "platform_comms", "platform_id ASC, link_id ASC")]
    [InlineData("CatalogPlatformBinding", "platform", "platform_id ASC")]
    [InlineData("CatalogWeaponRecord", "weapon_catalog", "weapon_id ASC")]
    public void Entity_map_includes_phase_b_catalog_types_with_stable_order_by(
        string entityName,
        string tableName,
        string orderBy)
    {
        Assert.True(CatalogEntityMap.TryGetTable(entityName, out var binding));
        Assert.Equal(tableName, binding.TableName);
        Assert.Equal(orderBy, binding.DeterministicOrderBy);
    }
}