namespace ProjectAegis.Data.Catalog;

/// <summary>Req-21 / doc 19: allowed link_catalog.link_type values.</summary>
public static class CatalogLinkTypes
{
    public const string Strategic = "strategic";
    public const string Tactical = "tactical";
    public const string Voice = "voice";
    public const string Satcom = "satcom";

    public static bool IsValid(string? linkType) =>
        linkType is Strategic or Tactical or Voice or Satcom;
}