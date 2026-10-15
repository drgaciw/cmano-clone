namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-319 top-of-attention list plus skill-unavailable disclosure.
/// Projection chrome only (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record JevAttentionPanel(
    string? FixtureId,
    bool ModelScoreAvailable,
    IReadOnlyList<JevAttentionRow> Rows,
    JevDisclosurePresentation Disclosure)
{
    /// <summary>
    /// Who produced the levels. A local rule score is labeled heuristic so it is not shown as a Jev result.
    /// </summary>
    public string AttributionLine
    {
        get
        {
            var fixture = string.IsNullOrEmpty(FixtureId) ? "unscoped" : FixtureId;
            if (ModelScoreAvailable)
            {
                return $"MODEL: {Disclosure.ModelPin} · {Disclosure.OpenRouterModel}  {fixture}";
            }

            return Disclosure.Visible
                ? $"OFFLINE  {fixture}"
                : $"HEURISTIC  {fixture}";
        }
    }
}
