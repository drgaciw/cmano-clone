namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-319 top-of-attention list plus skill-unavailable disclosure.
/// Projection chrome only (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record JevAttentionPanel(
    string? FixtureId,
    bool ModelScoreAvailable,
    IReadOnlyList<JevAttentionRow> Rows,
    JevDisclosurePresentation Disclosure);
