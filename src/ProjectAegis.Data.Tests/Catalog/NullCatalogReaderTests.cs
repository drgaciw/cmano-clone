using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class NullCatalogReaderTests
{
    [Test]
    public void Instance_exposes_scaffold_layer_version()
    {
        Assert.That(NullCatalogReader.Instance.LayerVersion, Is.EqualTo("p0-scaffold"));
    }
}
