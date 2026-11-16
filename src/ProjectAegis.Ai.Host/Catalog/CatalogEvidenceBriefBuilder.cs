using ProjectAegis.Data.PlatformAssistant;
using ProjectAegis.Data.WriteGate;

namespace ProjectAegis.Ai.Host.Catalog;

/// <summary>
/// Feeds retrieved evidence into a <see cref="PlatformDesignBrief"/> (DRG-748).
/// Evidence informs the Concept field; citations and assumptions are tracked separately.
/// </summary>
public static class CatalogEvidenceBriefBuilder
{
    /// <summary>
    /// Build a brief plus per-field evidence citations from accepted evidence docs.
    /// </summary>
    public static (PlatformDesignBrief Brief, IReadOnlyList<CatalogEvidenceCitation> Citations) Build(
        PlatformDesignBrief seed,
        IReadOnlyList<RetrievedCatalogEvidence> acceptedEvidence)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(acceptedEvidence);

        var snippets = new List<string>();
        var citations = new List<CatalogEvidenceCitation>();
        foreach (var doc in acceptedEvidence)
        {
            var snippet = Truncate(doc.Text, 160);
            snippets.Add($"[{doc.SourceId}@{doc.Revision}] {snippet}");
            citations.Add(new CatalogEvidenceCitation(
                Field: "concept",
                SourceId: doc.SourceId,
                SourceHash: doc.SourceHash,
                Revision: doc.Revision,
                Assumption: $"Public evidence '{doc.Title}' informs relative concept; not an instruction."));
        }

        if (citations.Count == 0)
        {
            throw new InvalidOperationException("At least one accepted evidence citation is required.");
        }

        // Bindings/damage/mobility values inherit the primary evidence citation for resolvability.
        var primary = citations[0];
        citations.Add(primary with { Field = "platform.binding" });
        citations.Add(primary with { Field = "platform.damage" });
        citations.Add(primary with
        {
            Field = "platform.mobility",
            Assumption = "Mobility scaled from catalog peers; primary evidence anchors provenance.",
        });

        var evidenceBlock = string.Join(" | ", snippets);
        var concept = string.IsNullOrWhiteSpace(seed.Concept)
            ? evidenceBlock
            : $"{seed.Concept} || evidence: {evidenceBlock}";

        var brief = seed with { Concept = Truncate(concept, 480) };
        return (brief, citations);
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
        {
            return value;
        }

        return value[..max];
    }
}
