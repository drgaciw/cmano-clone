using System;
using System.Collections.Generic;
using System.Globalization;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

public sealed record DecisionLogDeepLinkPresentation(
    bool Visible,
    bool Found,
    ulong SequenceId,
    string HeaderLine,
    string AttributionLine,
    string RationaleLine,
    string TelemetryLine,
    IReadOnlyList<string> AlternativeLines,
    IReadOnlyList<string> FieldLines,
    string UnavailableText)
{
    public static DecisionLogDeepLinkPresentation Empty { get; } = new(
        Visible: false,
        Found: false,
        SequenceId: 0,
        HeaderLine: string.Empty,
        AttributionLine: string.Empty,
        RationaleLine: string.Empty,
        TelemetryLine: string.Empty,
        AlternativeLines: Array.Empty<string>(),
        FieldLines: Array.Empty<string>(),
        UnavailableText: string.Empty);
}

/// <summary>
/// DRG-273: pure presentation binder for <see cref="DecisionLogDeepLinkDetail"/>.
/// Read-only; never mutates the log or opens combat-explanation surfaces.
/// </summary>
public static class DecisionLogDeepLinkBinder
{
    public static DecisionLogDeepLinkPresentation Bind(DecisionLogDeepLinkDetail? detail)
    {
        if (detail is null)
        {
            return DecisionLogDeepLinkPresentation.Empty;
        }

        if (!detail.Found)
        {
            return new DecisionLogDeepLinkPresentation(
                Visible: true,
                Found: false,
                SequenceId: detail.SequenceId,
                HeaderLine: $"SEQ #{detail.SequenceId} · UNAVAILABLE",
                AttributionLine: "Source: Unavailable",
                RationaleLine: detail.Summary,
                TelemetryLine: string.Empty,
                AlternativeLines: Array.Empty<string>(),
                FieldLines: Array.Empty<string>(),
                UnavailableText: detail.UnavailableReason ?? DecisionLogDeepLinkProjection.UnavailableReasonCode);
        }

        var header = string.Format(
            CultureInfo.InvariantCulture,
            "SEQ #{0} · t={1:F2}s (tick {2}) · {3}",
            detail.SequenceId,
            detail.SimTime,
            detail.SimTick,
            detail.Category);

        var attributionParts = new List<string>(3);
        if (!string.IsNullOrEmpty(detail.SourceKind))
        {
            attributionParts.Add($"Source: {detail.SourceKind}");
        }

        if (!string.IsNullOrEmpty(detail.ActorId))
        {
            attributionParts.Add($"Actor: {detail.ActorId}");
        }

        if (!string.IsNullOrEmpty(detail.UnitId))
        {
            attributionParts.Add($"Unit: {detail.UnitId}");
        }

        var attributionLine = attributionParts.Count > 0
            ? string.Join(" · ", attributionParts)
            : "Source: Session";

        var rationaleLine = !string.IsNullOrEmpty(detail.Rationale)
            ? $"{detail.Summary} — {detail.Rationale}"
            : detail.Summary;

        var telemetryParts = new List<string>(4);
        if (!string.IsNullOrEmpty(detail.AutonomyLevel))
        {
            telemetryParts.Add($"Autonomy: {detail.AutonomyLevel}");
        }

        if (detail.AttentionLoad.HasValue && detail.AttentionBudget.HasValue)
        {
            telemetryParts.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Attention: {0:F1}/{1:F1}",
                detail.AttentionLoad.Value,
                detail.AttentionBudget.Value));
        }

        if (detail.RngDraw.HasValue)
        {
            telemetryParts.Add(string.Format(CultureInfo.InvariantCulture, "RNG: {0:F4}", detail.RngDraw.Value));
        }

        if (detail.PolicySnapshotId.HasValue)
        {
            telemetryParts.Add($"PolicySnap: {detail.PolicySnapshotId.Value}");
        }

        var fieldLines = new List<string>(detail.Fields.Count);
        for (var i = 0; i < detail.Fields.Count; i++)
        {
            fieldLines.Add($"{detail.Fields[i].Key}: {detail.Fields[i].Value}");
        }

        return new DecisionLogDeepLinkPresentation(
            Visible: true,
            Found: true,
            SequenceId: detail.SequenceId,
            HeaderLine: header,
            AttributionLine: attributionLine,
            RationaleLine: rationaleLine,
            TelemetryLine: string.Join(" · ", telemetryParts),
            AlternativeLines: detail.ScoredAlternatives,
            FieldLines: fieldLines,
            UnavailableText: string.Empty);
    }
}
