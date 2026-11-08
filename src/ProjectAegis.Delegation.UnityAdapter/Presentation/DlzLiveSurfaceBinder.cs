using System.Globalization;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Sim.Engage;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-266: structured DLZ rows for contact hover and weapon-panel surfaces.
/// Binds read-only projection facts only — hosts must not re-derive sim truth (ADR-010 §2–3).
/// </summary>
public sealed record DlzLiveSurfaceState(
    string StateLine,
    string ShortLabel,
    string DeclutterToken,
    string CueClass,
    DlzState DlzState)
{
    /// <summary>Cleared DLZ surface when selection or range evidence is unavailable.</summary>
    public static DlzLiveSurfaceState Empty { get; } = new(
        "DLZ: —",
        "—",
        string.Empty,
        DlzCueClasses.Unknown,
        DlzState.Unknown);
}

/// <summary>Non-color USS cue tokens for DLZ rows (text + border class).</summary>
public static class DlzCueClasses
{
    public const string Unknown = "dlz-cue--unknown";
    public const string InZone = "dlz-cue--in-zone";
    public const string Approaching = "dlz-cue--approaching";
    public const string OutOfZone = "dlz-cue--out-of-zone";

    /// <summary>All cue classes hosts must clear before applying the active row cue.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Unknown, InZone, Approaching, OutOfZone };
}

/// <summary>Headless text declutter tokens paired with cue classes — never color-only state.</summary>
public static class DlzDeclutterTokens
{
    public const string Unknown = "[DLZ:UNKNOWN]";
    public const string In = "[DLZ:IN]";
    public const string Marginal = "[DLZ:MARGINAL]";
    public const string Out = "[DLZ:OUT]";
}

/// <summary>One live-surface DLZ label row with its USS cue class token.</summary>
public sealed record DlzLiveSurfaceRow(string ElementName, string Text, string CueClass);

/// <summary>Headless row binder for DLZ live-surface labels and cue classes.</summary>
public static class DlzLiveSurfacePanelBinder
{
    /// <summary>Maps bound state into element names, text, and cue classes for UI Toolkit classList swaps.</summary>
    public static IReadOnlyList<DlzLiveSurfaceRow> BindRows(DlzLiveSurfaceState state) =>
    [
        new("dlz-line", state.StateLine, state.CueClass),
    ];
}

/// <summary>Maps headless DLZ projections into per-surface labels and cue tokens.</summary>
public static class DlzLiveSurfaceBinder
{
    /// <summary>Builds a weapon-panel row from structured threat-range assessment facts.</summary>
    public static DlzLiveSurfaceState Bind(ThreatRangeAssessment? range)
    {
        if (range is null)
        {
            return DlzLiveSurfaceState.Empty;
        }

        return Build(
            range.DlzState,
            range.DlzLabel,
            range.RangeMeters,
            range.EnvelopeMinMeters,
            range.EnvelopeMaxMeters,
            range.InEnvelope);
    }

    /// <summary>Builds a weapon-panel row from an engage preview label (positional props only).</summary>
    public static DlzLiveSurfaceState BindFromEngagePreview(EngagePreview? preview)
    {
        if (preview is null)
        {
            return DlzLiveSurfaceState.Empty;
        }

        return Build(
            ParseDlzState(preview.DlzLabel),
            preview.DlzLabel,
            null,
            null,
            null,
            null);
    }

    /// <summary>
    /// Contact-hover surface: prefer exact threat-range assessment, then selected-unit engage preview.
    /// </summary>
    public static DlzLiveSurfaceState BindContact(
        string? contactId,
        ThreatRangeAssessment? threatRange,
        EngagePreview? unitEngagePreview)
    {
        if (string.IsNullOrWhiteSpace(contactId))
        {
            return DlzLiveSurfaceState.Empty;
        }

        if (threatRange is not null)
        {
            return Bind(threatRange);
        }

        if (unitEngagePreview is not null)
        {
            return BindFromEngagePreview(unitEngagePreview);
        }

        return new DlzLiveSurfaceState(
            "DLZ: UNKNOWN — select a friendly unit for range assessment",
            "UNKNOWN",
            DlzDeclutterTokens.Unknown,
            DlzCueClasses.Unknown,
            DlzState.Unknown);
    }

    internal static DlzState ParseDlzState(string dlzLabel)
    {
        if (string.IsNullOrWhiteSpace(dlzLabel))
        {
            return DlzState.Unknown;
        }

        const string prefix = "DLZ:";
        var start = dlzLabel.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0)
        {
            return DlzState.Unknown;
        }

        start += prefix.Length;
        while (start < dlzLabel.Length && char.IsWhiteSpace(dlzLabel[start]))
        {
            start++;
        }

        var end = dlzLabel.IndexOf('(', start);
        var token = (end < 0 ? dlzLabel[start..] : dlzLabel[start..end]).Trim();
        return Enum.TryParse(token, ignoreCase: false, out DlzState parsed)
            ? parsed
            : DlzState.Unknown;
    }

    private static DlzLiveSurfaceState Build(
        DlzState state,
        string dlzLabel,
        double? rangeMeters,
        double? envelopeMinMeters,
        double? envelopeMaxMeters,
        bool? inEnvelope)
    {
        var shortLabel = FormatShortLabel(state);
        var line = rangeMeters is not null && envelopeMinMeters is not null && envelopeMaxMeters is not null
            ? string.Format(
                CultureInfo.InvariantCulture,
                "{0} | {1:0} m | envelope {2:0}–{3:0} m",
                dlzLabel,
                rangeMeters.Value,
                envelopeMinMeters.Value,
                envelopeMaxMeters.Value)
            : dlzLabel;

        if (inEnvelope == false && state is not DlzState.OutOfZone)
        {
            line += " | outside envelope";
        }

        return new DlzLiveSurfaceState(
            line,
            shortLabel,
            FormatDeclutterToken(state),
            ResolveCue(state),
            state);
    }

    private static string FormatShortLabel(DlzState state) =>
        state switch
        {
            DlzState.InZone => "IN",
            DlzState.Approaching => "MARGINAL",
            DlzState.OutOfZone => "OUT",
            _ => "UNKNOWN",
        };

    private static string FormatDeclutterToken(DlzState state) =>
        state switch
        {
            DlzState.InZone => DlzDeclutterTokens.In,
            DlzState.Approaching => DlzDeclutterTokens.Marginal,
            DlzState.OutOfZone => DlzDeclutterTokens.Out,
            _ => DlzDeclutterTokens.Unknown,
        };

    private static string ResolveCue(DlzState state) =>
        state switch
        {
            DlzState.InZone => DlzCueClasses.InZone,
            DlzState.Approaching => DlzCueClasses.Approaching,
            DlzState.OutOfZone => DlzCueClasses.OutOfZone,
            _ => DlzCueClasses.Unknown,
        };
}
