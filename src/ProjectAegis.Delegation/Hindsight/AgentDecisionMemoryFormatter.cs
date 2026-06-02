namespace ProjectAegis.Delegation.Hindsight;

using System.Globalization;
using ProjectAegis.Delegation.Decision;

/// <summary>Turns structured agent decisions into Hindsight retain content (§6.4 lazy NL).</summary>
public static class AgentDecisionMemoryFormatter
{
    public static string Format(AgentDecisionPayload payload, string? personalitySlug = null)
    {
        var attentionPct = payload.AttentionBudget <= 0
            ? 0
            : (int)Math.Round(payload.AttentionLoad / payload.AttentionBudget * 100, MidpointRounding.AwayFromZero);

        var alternatives = payload.ScoredIntents.Count == 0
            ? "none"
            : string.Join(
                ", ",
                payload.ScoredIntents.Select(i =>
                    $"{i.Kind} score={i.Score.ToString("0.##", CultureInfo.InvariantCulture)} risk={i.Risk}"));

        var personality = string.IsNullOrWhiteSpace(personalitySlug) ? "custom" : personalitySlug.Trim();

        return $"""
            sim_time={payload.SimTime} sim_tick={payload.SimTick} agent={payload.AgentId.Value} personality={personality} target={payload.TargetId.Value} autonomy={payload.AutonomyLevel}.
            Chose {payload.ChosenOrderKind} over alternatives [{alternatives}].
            Rationale: {payload.Rationale}
            Attention load {attentionPct}% ({payload.AttentionLoad.ToString("0.##", CultureInfo.InvariantCulture)}/{payload.AttentionBudget.ToString("0.##", CultureInfo.InvariantCulture)}).
            rng_draw={payload.RngDraw.ToString("0.###", CultureInfo.InvariantCulture)}.
            """;
    }
}
