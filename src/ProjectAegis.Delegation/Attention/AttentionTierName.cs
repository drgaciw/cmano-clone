namespace ProjectAegis.Delegation.Attention;

/// <summary>
/// Named attention degradation tiers (req 04 AGD-12). Ordered by severity.
/// Values mirror the graded flags on <see cref="AttentionDegradation"/> —
/// the highest-severity true flag wins.
/// </summary>
public enum AttentionTierName
{
    /// <summary>Load is within budget; no degradation applied.</summary>
    Nominal = 0,

    /// <summary>Load exceeds budget — reaction delay multiplies (AGD-12).</summary>
    SlowerReactions = 1,

    /// <summary>Load exceeds 1.25× budget — decision pool is narrowed.</summary>
    NarrowedFocus = 2,

    /// <summary>Load exceeds 1.5× budget — simpler decisions under overload.</summary>
    SimplerDecisions = 3,
}

/// <summary>
/// Pure naming helpers for decision-time attention state.
/// Does not recompute load; only labels the tier implied by an existing evaluation
/// or by recorded load/budget pairs (explain path).
/// </summary>
public static class AttentionTierNaming
{
    public const string NominalDisplay = "Nominal";
    public const string SlowerReactionsDisplay = "SlowerReactions";
    public const string NarrowedFocusDisplay = "NarrowedFocus";
    public const string SimplerDecisionsDisplay = "SimplerDecisions";
    public const string UnknownDisplay = "—";

    /// <summary>
    /// Resolve the highest-severity named tier from an evaluation's degradation flags.
    /// Null evaluation → Nominal for display (host may still show "no sample" via HasSample).
    /// </summary>
    public static AttentionTierName FromEvaluation(AttentionEvaluation? evaluation)
    {
        if (evaluation is null)
        {
            return AttentionTierName.Nominal;
        }

        return FromDegradation(evaluation.Degradation);
    }

    public static AttentionTierName FromDegradation(AttentionDegradation? degradation)
    {
        if (degradation is null)
        {
            return AttentionTierName.Nominal;
        }

        if (degradation.SimplerDecisions)
        {
            return AttentionTierName.SimplerDecisions;
        }

        if (degradation.NarrowedFocus)
        {
            return AttentionTierName.NarrowedFocus;
        }

        if (degradation.SlowerReactions)
        {
            return AttentionTierName.SlowerReactions;
        }

        return AttentionTierName.Nominal;
    }

    /// <summary>
    /// Label a recorded load/budget pair using the same thresholds as
    /// <see cref="AttentionCalculator"/> (budget, 1.25×, 1.5×). Used for explain
    /// surfaces that only have DecisionRecord load/budget — does not invent load.
    /// </summary>
    public static AttentionTierName FromLoadBudget(double load, double budget)
    {
        if (budget <= 0)
        {
            return AttentionTierName.Nominal;
        }

        if (load > budget * 1.5)
        {
            return AttentionTierName.SimplerDecisions;
        }

        if (load > budget * 1.25)
        {
            return AttentionTierName.NarrowedFocus;
        }

        if (load > budget)
        {
            return AttentionTierName.SlowerReactions;
        }

        return AttentionTierName.Nominal;
    }

    public static string DisplayName(AttentionTierName tier) => tier switch
    {
        AttentionTierName.SlowerReactions => SlowerReactionsDisplay,
        AttentionTierName.NarrowedFocus => NarrowedFocusDisplay,
        AttentionTierName.SimplerDecisions => SimplerDecisionsDisplay,
        AttentionTierName.Nominal => NominalDisplay,
        _ => UnknownDisplay,
    };

    /// <summary>Screen-reader / keyboard label — never colour-only (a11y).</summary>
    public static string AccessibleLabel(AttentionTierName tier, double load, double budget)
    {
        var tierName = DisplayName(tier);
        if (budget <= 0)
        {
            return $"Attention {tierName}; load unavailable";
        }

        return $"Attention {tierName}; load {load:0.0} of budget {budget:0.0}";
    }
}
