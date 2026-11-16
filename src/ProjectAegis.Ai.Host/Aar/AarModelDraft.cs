namespace ProjectAegis.Ai.Host.Aar;

/// <summary>
/// Model-supplied AAR fields only: prose + evidence IDs. Quantitative values are host-owned.
/// </summary>
public sealed record AarModelDraft(
    string Summary,
    IReadOnlyList<string> Outcomes,
    IReadOnlyList<string> Uncertainties,
    IReadOnlyList<string> Citations);
