using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-408: pure headless projection for the companion map balloon overlay
/// (ADR-010 §2–3, ADR-007). Anchors alerts to matching <see cref="MapSymbolEntry"/>
/// coordinates or places theater-wide events in the unanchored strip.
/// </summary>
public static class MapBalloonOverlayProjection
{
    public const string RootElementName = "map-balloon-overlay-root";
    public const string UnanchoredStripElementName = "map-balloon-unanchored-strip";
    public const string CanvasElementName = "map-balloon-canvas";

    public const string PauseBalloonCssClass = "map-balloon-card--pause";
    public const string AlertBalloonCssClass = "map-balloon-card--alert";

    public static string BalloonElementName(string balloonId) => $"map-balloon-card-{balloonId}";

    public static MapBalloonOverlayState Project(
        IReadOnlyList<MapBalloonAlert>? balloons,
        IReadOnlyList<MapSymbolEntry>? symbols,
        bool isInspectingHistory = false)
    {
        if (isInspectingHistory)
        {
            return MapBalloonOverlayState.InspectingHistory;
        }

        if (balloons is null || balloons.Count == 0)
        {
            return MapBalloonOverlayState.Empty;
        }

        var symbolById = IndexSymbols(symbols);
        var anchored = new List<MapBalloonOverlayRow>(balloons.Count);
        var unanchored = new List<MapBalloonOverlayRow>();

        for (var i = 0; i < balloons.Count; i++)
        {
            var alert = balloons[i];
            var css = alert.IsPauseTriggered ? PauseBalloonCssClass : AlertBalloonCssClass;
            // Wreck markers (IsDestroyed == true) remain rendered by MapPanelBinder / MapSymbolPool
            // and MapCueOverlayProjection, so KILL_CONFIRMED / fatal DAMAGE balloons intentionally
            // anchor to the wreck symbol coordinates and only demote to UnanchoredBalloons when absent.
            if (!string.IsNullOrEmpty(alert.SubjectId)
                && symbolById.TryGetValue(alert.SubjectId, out var symbol))
            {
                anchored.Add(new MapBalloonOverlayRow(
                    alert.BalloonId,
                    alert.SequenceId,
                    alert.Category,
                    alert.SubjectId,
                    alert.Text,
                    symbol.NormalizedX,
                    symbol.NormalizedY,
                    HasMapAnchor: true,
                    IsPauseTriggered: alert.IsPauseTriggered,
                    CssClass: css));
            }
            else
            {
                unanchored.Add(new MapBalloonOverlayRow(
                    alert.BalloonId,
                    alert.SequenceId,
                    alert.Category,
                    alert.SubjectId,
                    alert.Text,
                    NormalizedX: 0f,
                    NormalizedY: 0f,
                    HasMapAnchor: false,
                    IsPauseTriggered: alert.IsPauseTriggered,
                    CssClass: css));
            }
        }

        return new MapBalloonOverlayState(anchored, unanchored, IsInspectingHistory: false);
    }

    private static Dictionary<string, MapSymbolEntry> IndexSymbols(IReadOnlyList<MapSymbolEntry>? symbols)
    {
        var map = new Dictionary<string, MapSymbolEntry>(StringComparer.Ordinal);
        if (symbols is null)
        {
            return map;
        }

        for (var i = 0; i < symbols.Count; i++)
        {
            var symbol = symbols[i];
            if (!string.IsNullOrEmpty(symbol.SymbolId) && !map.ContainsKey(symbol.SymbolId))
            {
                map[symbol.SymbolId] = symbol;
            }
        }

        return map;
    }
}

public sealed record MapBalloonOverlayRow(
    string BalloonId,
    ulong SequenceId,
    string Category,
    string? SubjectId,
    string Text,
    float NormalizedX,
    float NormalizedY,
    bool HasMapAnchor,
    bool IsPauseTriggered,
    string CssClass);

public sealed record MapBalloonOverlayState(
    IReadOnlyList<MapBalloonOverlayRow> AnchoredBalloons,
    IReadOnlyList<MapBalloonOverlayRow> UnanchoredBalloons,
    bool IsInspectingHistory)
{
    public static MapBalloonOverlayState Empty { get; } = new(
        Array.Empty<MapBalloonOverlayRow>(),
        Array.Empty<MapBalloonOverlayRow>(),
        IsInspectingHistory: false);

    public static MapBalloonOverlayState InspectingHistory { get; } = new(
        Array.Empty<MapBalloonOverlayRow>(),
        Array.Empty<MapBalloonOverlayRow>(),
        IsInspectingHistory: true);
}
