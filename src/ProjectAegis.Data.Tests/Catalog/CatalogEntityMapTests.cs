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
}