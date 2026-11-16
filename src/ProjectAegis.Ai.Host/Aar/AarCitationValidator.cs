namespace ProjectAegis.Ai.Host.Aar;

/// <summary>Rejects citations whose evidence IDs are absent from the exported set.</summary>
public static class AarCitationValidator
{
    /// <summary>
    /// Returns null when every citation resolves; otherwise the first missing ID.
    /// </summary>
    public static string? FindMissingCitation(
        IReadOnlyList<string> citations,
        IReadOnlyList<string> allowedEvidenceIds)
    {
        ArgumentNullException.ThrowIfNull(citations);
        ArgumentNullException.ThrowIfNull(allowedEvidenceIds);

        var allowed = new HashSet<string>(allowedEvidenceIds, StringComparer.Ordinal);
        foreach (var citation in citations)
        {
            if (string.IsNullOrWhiteSpace(citation) || !allowed.Contains(citation))
            {
                return citation ?? string.Empty;
            }
        }

        return null;
    }
}
