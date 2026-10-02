using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Non-color USS cue tokens for Air Ops ready aggregate chrome (ADR-010 §2–3).</summary>
public static class AirOpsReadyCueClasses
{
    public const string Unknown = "air-ops-ready-cue--unknown";
    public const string AllReady = "air-ops-ready-cue--all";
    public const string Partial = "air-ops-ready-cue--partial";
    public const string NoneReady = "air-ops-ready-cue--none";

    /// <summary>All cue classes hosts must clear before applying the active aggregate cue.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Unknown, AllReady, Partial, NoneReady };
}

/// <summary>Headless declutter tokens for ready aggregate state — never color-only (accessibility).</summary>
public static class AirOpsReadyDeclutterTokens
{
    public const string NoData = "[AIR:NO-DATA]";
    public const string AllReady = "[AIR:ALL-READY]";
    public const string Partial = "[AIR:PARTIAL]";
    public const string NoneReady = "[AIR:NONE-READY]";
}

/// <summary>Label bundle for Air Ops ready / not-ready aggregate chrome (DRG-267).</summary>
public sealed record AirOpsReadyAggregateLabels(
    string HeaderLine,
    string ReadySummaryLine,
    int ReadyCount,
    int NotReadyCount,
    int TotalCount,
    string ReadyLineText,
    string CueClass,
    string? DeclutterToken,
    string Fingerprint,
    bool HasReadinessData);

/// <summary>One bound ready-aggregate row for UI Toolkit classList swaps.</summary>
public sealed record AirOpsReadyAggregateRow(string ElementName, string Text, string CueClass);

/// <summary>
/// Maps <see cref="AirOpsPresentation.Aggregate"/> to panel labels without re-deriving sim facts.
/// Presentation-only — ADR-010 §2–3, ADR-007, ADR-001.
/// </summary>
public static class AirOpsReadyAggregateBinder
{
    /// <summary>Binds immutable aggregate labels for UI Toolkit header and ready-summary rows.</summary>
    public static AirOpsReadyAggregateLabels Bind(AirOpsPresentation presentation)
    {
        if (presentation is null)
        {
            throw new ArgumentNullException(nameof(presentation));
        }

        var aggregate = presentation.Aggregate;
        var ready = aggregate.ReadyCount;
        var total = aggregate.TotalCount;
        var notReady = total - ready;
        var summary = aggregate.SummaryLine;
        var readyLine = FormatReadyLine(presentation.HasReadinessData, summary, ready, notReady, total);
        var cue = ResolveCueClass(presentation.HasReadinessData, ready, total);
        var declutter = ResolveDeclutterToken(presentation.HasReadinessData, ready, total);

        return new AirOpsReadyAggregateLabels(
            presentation.HeaderLine,
            summary,
            ready,
            notReady,
            total,
            readyLine,
            cue,
            declutter,
            AirOpsReadyAggregatePresenter.ComputeFingerprint(presentation),
            presentation.HasReadinessData);
    }

    /// <summary>Maps bound labels into element names, text, and cue classes for hosts.</summary>
    public static IReadOnlyList<AirOpsReadyAggregateRow> BindRows(AirOpsReadyAggregateLabels labels)
    {
        if (labels is null)
        {
            throw new ArgumentNullException(nameof(labels));
        }

        return
        [
            new("air-ops-ready-line", labels.ReadyLineText, labels.CueClass),
        ];
    }

    internal static string FormatReadyLine(
        bool hasReadinessData,
        string summaryLine,
        int readyCount,
        int notReadyCount,
        int totalCount)
    {
        if (!hasReadinessData)
        {
            return AirOpsApplyState.NoReadinessDataLine;
        }

        if (totalCount == 0)
        {
            return summaryLine;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} · NOT READY {1}",
            summaryLine,
            notReadyCount);
    }

    internal static string ResolveCueClass(bool hasReadinessData, int readyCount, int totalCount)
    {
        if (!hasReadinessData || totalCount == 0)
        {
            return AirOpsReadyCueClasses.Unknown;
        }

        if (readyCount == totalCount)
        {
            return AirOpsReadyCueClasses.AllReady;
        }

        if (readyCount == 0)
        {
            return AirOpsReadyCueClasses.NoneReady;
        }

        return AirOpsReadyCueClasses.Partial;
    }

    internal static string? ResolveDeclutterToken(bool hasReadinessData, int readyCount, int totalCount)
    {
        if (!hasReadinessData)
        {
            return AirOpsReadyDeclutterTokens.NoData;
        }

        if (totalCount == 0)
        {
            return null;
        }

        if (readyCount == totalCount)
        {
            return AirOpsReadyDeclutterTokens.AllReady;
        }

        if (readyCount == 0)
        {
            return AirOpsReadyDeclutterTokens.NoneReady;
        }

        return AirOpsReadyDeclutterTokens.Partial;
    }
}

/// <summary>Replay-stable fingerprint for unchanged-frame short-circuit in Air Ops hosts (DRG-267).</summary>
public static class AirOpsReadyAggregatePresenter
{
    /// <summary>Fingerprint from an applied presentation bundle.</summary>
    public static string ComputeFingerprint(AirOpsPresentation presentation)
    {
        if (presentation is null)
        {
            throw new ArgumentNullException(nameof(presentation));
        }

        if (!presentation.HasReadinessData)
        {
            return "ao:no-data";
        }

        var aggregate = presentation.Aggregate;
        if (presentation.Rows.Count == 0 && aggregate.TotalCount == 0)
        {
            return "ao:empty";
        }

        var builder = new StringBuilder();
        builder.Append("ao:r=");
        builder.Append(aggregate.ReadyCount.ToString(CultureInfo.InvariantCulture));
        builder.Append('/');
        builder.Append(aggregate.TotalCount.ToString(CultureInfo.InvariantCulture));
        builder.Append("|rows=");
        builder.Append(presentation.Rows.Count.ToString(CultureInfo.InvariantCulture));
        for (var i = 0; i < presentation.Rows.Count; i++)
        {
            var row = presentation.Rows[i];
            builder.Append('|');
            builder.Append(row.UnitId);
            builder.Append(',');
            builder.Append(row.ReadyForLaunch ? '1' : '0');
            builder.Append(',');
            builder.Append(row.CanLaunch ? '1' : '0');
            builder.Append(',');
            builder.Append(row.CanAbort ? '1' : '0');
        }

        return builder.ToString();
    }
}
