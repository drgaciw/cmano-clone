namespace ProjectAegis.Data.Catalog;

/// <summary>Req-06 §6: classify persisted values (source vs interpreted vs gameplay).</summary>
public static class CatalogProvenanceTier
{
    public const string SourceFact = "source_fact";
    public const string InterpretedValue = "interpreted_value";
    public const string GameplayAbstraction = "gameplay_abstraction";

    public static bool IsKnown(string? tier) =>
        tier is SourceFact or InterpretedValue or GameplayAbstraction;

    public static string Normalize(string? tier) =>
        IsKnown(tier) ? tier! : GameplayAbstraction;
}