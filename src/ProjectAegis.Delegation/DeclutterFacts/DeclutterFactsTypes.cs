namespace ProjectAegis.Delegation.DeclutterFacts;

/// <summary>Presentation zoom-band tokens for salvo / burst declutter facts (CMB-UI-05 / DRG-230).</summary>
public static class DeclutterFactsZoomBand
{
    /// <summary>Tactical zoom band — finer declutter aggregation bucket.</summary>
    public const string Tactical = "tactical";

    /// <summary>Operational zoom band — coarser declutter aggregation bucket.</summary>
    public const string Operational = "operational";
}

/// <summary>
/// Read-only engagement / salvo member facts for headless declutter projection (DRG-230).
/// Fields mirror combat employment inputs without coupling to sim engage context.
/// </summary>
public sealed record DeclutterFactsEngagementFacts(
    string WeaponFamilyId,
    string ZoomBandToken,
    int RoundCount = 1);

/// <summary>
/// One presentation-facing salvo / burst declutter row. Counts and family tokens only — no UI
/// selection, hover, camera, or panel state. Advisory only — never a fire order.
/// </summary>
public sealed record DeclutterFactsRow(
    string WeaponFamilyId,
    int Count,
    string ZoomBandToken,
    bool IsFireOrder);

/// <summary>Ordered, replay-stable declutter aggregation for one zoom picture.</summary>
public sealed record DeclutterFactsSnapshot(
    IReadOnlyList<DeclutterFactsRow> Rows,
    bool IsFireOrder)
{
    /// <summary>Empty declutter picture — advisory only, never a fire order.</summary>
    public static DeclutterFactsSnapshot Empty { get; } =
        new(Array.Empty<DeclutterFactsRow>(), IsFireOrder: false);
}
