namespace ProjectAegis.Delegation.DeclutterFacts;

/// <summary>
/// DRG-230: projects salvo / burst engagement facts into deterministic declutter aggregation rows.
/// Presentation-only — never enqueues orders, resolves combat, or issues fire.
/// </summary>
public static class DeclutterFactsProjection
{
    /// <summary>
    /// Projects declutter rows by aggregating round counts per weapon family and zoom-band token.
    /// Rows are sorted by weapon family then zoom band (ordinal). Every row and the snapshot carry
    /// <c>IsFireOrder=false</c>.
    /// </summary>
    public static DeclutterFactsSnapshot Project(IReadOnlyList<DeclutterFactsEngagementFacts>? engagements)
    {
        if (engagements is null || engagements.Count == 0)
        {
            return DeclutterFactsSnapshot.Empty;
        }

        var aggregates = new Dictionary<(string WeaponFamilyId, string ZoomBandToken), int>();
        for (var i = 0; i < engagements.Count; i++)
        {
            var facts = engagements[i];
            if (string.IsNullOrWhiteSpace(facts.WeaponFamilyId)
                || string.IsNullOrWhiteSpace(facts.ZoomBandToken))
            {
                continue;
            }

            var roundCount = Math.Max(1, facts.RoundCount);
            var key = (facts.WeaponFamilyId, facts.ZoomBandToken);
            aggregates.TryGetValue(key, out var existing);
            aggregates[key] = existing + roundCount;
        }

        if (aggregates.Count == 0)
        {
            return DeclutterFactsSnapshot.Empty;
        }

        var rows = new List<DeclutterFactsRow>(aggregates.Count);
        foreach (var aggregate in aggregates)
        {
            rows.Add(new DeclutterFactsRow(
                aggregate.Key.WeaponFamilyId,
                aggregate.Value,
                aggregate.Key.ZoomBandToken,
                IsFireOrder: false));
        }

        rows.Sort(static (a, b) =>
        {
            var family = string.Compare(a.WeaponFamilyId, b.WeaponFamilyId, StringComparison.Ordinal);
            return family != 0
                ? family
                : string.Compare(a.ZoomBandToken, b.ZoomBandToken, StringComparison.Ordinal);
        });

        return new DeclutterFactsSnapshot(rows, IsFireOrder: false);
    }

    /// <summary>Projects a single engagement fact as a one-row declutter picture.</summary>
    public static DeclutterFactsSnapshot Project(DeclutterFactsEngagementFacts engagement) =>
        Project(new[] { engagement });
}
