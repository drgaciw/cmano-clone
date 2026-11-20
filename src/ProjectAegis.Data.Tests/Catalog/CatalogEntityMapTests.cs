using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogEntityMapTests
{
    [Test]
    public void Entity_map_includes_sensor_and_audit_tables()
    {
        Assert.That(CatalogEntityMap.TryGetTable("CatalogSensorBinding", out var sensor), Is.True);
        Assert.That(sensor.TableName, Is.EqualTo("sensor"));

        Assert.That(CatalogEntityMap.TryGetTable("CatalogChangeLogEntry", out var audit), Is.True);
        Assert.That(audit.TableName, Is.EqualTo("catalog_change_log"));
    }
    [TestCase("CatalogMount", "platform_mount", "platform_id ASC, mount_id ASC")]
    [TestCase("CatalogLoadout", "platform_loadout", "platform_id ASC, loadout_id ASC")]
    [TestCase("CatalogMagazineEntry", "platform_magazine", "platform_id ASC, loadout_id ASC, mount_id ASC, weapon_id ASC")]
    [TestCase("CatalogCommsBinding", "platform_comms", "platform_id ASC, link_id ASC")]
    [TestCase("CatalogLinkEntry", "link_catalog", "link_id ASC")]
    [TestCase("CatalogPlatformBinding", "platform", "platform_id ASC")]
    [TestCase("CatalogWeaponRecord", "weapon_catalog", "weapon_id ASC")]
    public void Entity_map_includes_phase_b_catalog_types_with_stable_order_by(
        string entityName,
        string tableName,
        string orderBy)
    {
        Assert.That(CatalogEntityMap.TryGetTable(entityName, out var binding), Is.True);
        Assert.That(binding.TableName, Is.EqualTo(tableName));
        Assert.That(binding.DeterministicOrderBy, Is.EqualTo(orderBy));
    }
}