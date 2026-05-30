namespace ProjectAegis.Data.Catalog;

public sealed class NullCatalogReader : ICatalogReader
{
    public static readonly NullCatalogReader Instance = new();

    private NullCatalogReader()
    {
    }

    public string LayerVersion => "p0-scaffold";
}
