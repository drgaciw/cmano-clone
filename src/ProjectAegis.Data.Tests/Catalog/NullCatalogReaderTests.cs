using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class NullCatalogReaderTests
{
    [Fact]
    public void Instance_exposes_scaffold_layer_version()
    {
        Assert.Equal("p0-scaffold", NullCatalogReader.Instance.LayerVersion);
    }
}
