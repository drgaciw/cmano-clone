using System.Globalization;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Non-color USS cue tokens for fuel band chrome (text + border class; ADR-010 §2–3).</summary>
public static class FuelBandCueClasses
{
    public const string Unknown = "fuel-band-cue--unknown";
    public const string Nominal = "fuel-band-cue--nominal";
    public const string Joker = "fuel-band-cue--joker";
    public const string Bingo = "fuel-band-cue--bingo";

    /// <summary>All cue classes hosts must clear before applying the active band cue.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Unknown, Nominal, Joker, Bingo };
}

/// <summary>Headless declutter tokens for fuel band state — never color-only (accessibility).</summary>
public static class FuelBandDeclutterTokens
{
    public const string Joker = "[FUEL:JOKER]";
    public const string Bingo = "[FUEL:BINGO]";
}

/// <summary>Resolves NOMINAL / JOKER / BINGO labels from projected fuel readouts and log rows.</summary>
public static class FuelBandLabelResolver
{
    public const string NominalBand = "NOMINAL";
    public const string JokerBand = "JOKER";
    public const string BingoBand = "BINGO";

    /// <summary>Maps a resolved band label to its USS cue class token.</summary>
    public static string CueClassForBand(string? band) => NormalizeBand(band) switch
    {
        JokerBand => FuelBandCueClasses.Joker,
        BingoBand => FuelBandCueClasses.Bingo,
        NominalBand => FuelBandCueClasses.Nominal,
        _ => FuelBandCueClasses.Unknown,
    };

    /// <summary>Optional declutter suffix for warning bands (unit detail chrome).</summary>
    public static string? DeclutterTokenForBand(string? band) => NormalizeBand(band) switch
    {
        JokerBand => FuelBandDeclutterTokens.Joker,
        BingoBand => FuelBandDeclutterTokens.Bingo,
        _ => null,
    };

    /// <summary>Parses the active band from a unit-detail <c>FUEL:</c> projection line.</summary>
    public static string? ResolveFromUnitFuelLine(string? fuelLine)
    {
        if (string.IsNullOrWhiteSpace(fuelLine))
        {
            return null;
        }

        const string prefix = "FUEL:";
        if (!fuelLine.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return TryResolveBandToken(fuelLine);
        }

        var remainder = fuelLine.TrimStart().Substring(prefix.Length).TrimStart();
        if (remainder.Length == 0)
        {
            return null;
        }

        var firstToken = remainder.Split([' ', '('], StringSplitOptions.RemoveEmptyEntries)[0];
        return TryResolveBandToken(firstToken);
    }

    /// <summary>Consumes <see cref="FuelStateChangeRecord"/> positional props for band chrome.</summary>
    public static string ResolveFromFuelStateChange(FuelStateChangeRecord change) =>
        NormalizeBand(change.NewState) ?? UnknownBand;

    /// <summary>Parses the active band from a bound message-log display row.</summary>
    public static string? ResolveFromMessageLogRow(MessageLogDisplayRow row)
    {
        if (row is null)
        {
            throw new ArgumentNullException(nameof(row));
        }

        if (!string.Equals(row.Category, "FUEL", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return ResolveFromMessageLogDisplayLine(row.DisplayLine);
    }

    /// <summary>Parses the active band from a formatted message-log display line.</summary>
    public static string? ResolveFromMessageLogDisplayLine(string? displayLine)
    {
        if (string.IsNullOrWhiteSpace(displayLine))
        {
            return null;
        }

        var arrowIndex = displayLine.IndexOf('→', StringComparison.Ordinal);
        if (arrowIndex >= 0)
        {
            var afterArrow = displayLine[(arrowIndex + 1)..].TrimStart();
            var transitionToken = afterArrow.Split([' ', '('], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            var resolved = TryResolveBandToken(transitionToken);
            if (resolved != null)
            {
                return resolved;
            }
        }

        return TryResolveBandToken(displayLine);
    }

    private const string UnknownBand = "UNKNOWN";

    private static string? TryResolveBandToken(string? token)
    {
        var normalized = NormalizeBand(token);
        return normalized;
    }

    private static string? NormalizeBand(string? band)
    {
        if (string.IsNullOrWhiteSpace(band))
        {
            return null;
        }

        var trimmed = band.Trim();
        if (trimmed.Equals(JokerBand, StringComparison.OrdinalIgnoreCase))
        {
            return JokerBand;
        }

        if (trimmed.Equals(BingoBand, StringComparison.OrdinalIgnoreCase))
        {
            return BingoBand;
        }

        if (trimmed.Equals(NominalBand, StringComparison.OrdinalIgnoreCase))
        {
            return NominalBand;
        }

        return null;
    }
}

/// <summary>Unit-detail fuel row: projected line text, band label, cue class, optional declutter token.</summary>
public sealed record UnitDetailFuelBandPresentation(
    string FuelLineText,
    string? BandLabel,
    string CueClass,
    string? DeclutterToken);

/// <summary>Headless binder for right-panel fuel band chrome from <see cref="UnitDetailPresentation"/>.</summary>
public static class UnitDetailFuelBandBinder
{
    public static UnitDetailFuelBandPresentation Bind(UnitDetailPresentation presentation)
    {
        if (presentation is null)
        {
            throw new ArgumentNullException(nameof(presentation));
        }

        var band = FuelBandLabelResolver.ResolveFromUnitFuelLine(presentation.FuelLine);
        return new UnitDetailFuelBandPresentation(
            presentation.FuelLine,
            band,
            FuelBandLabelResolver.CueClassForBand(band),
            FuelBandLabelResolver.DeclutterTokenForBand(band));
    }
}

/// <summary>Message-log row enhancement: band cue class layered on category styling.</summary>
public sealed record MessageLogFuelBandPresentation(string? BandLabel, string? CueClass);

/// <summary>Headless binder for message-log fuel band chrome from panel rows or order-log records.</summary>
public static class MessageLogFuelBandBinder
{
    public static MessageLogFuelBandPresentation Bind(MessageLogDisplayRow row)
    {
        if (row is null)
        {
            throw new ArgumentNullException(nameof(row));
        }

        var band = FuelBandLabelResolver.ResolveFromMessageLogRow(row);
        return new MessageLogFuelBandPresentation(
            band,
            band == null ? null : FuelBandLabelResolver.CueClassForBand(band));
    }

    public static MessageLogFuelBandPresentation Bind(FuelStateChangeRecord change)
    {
        if (change is null)
        {
            throw new ArgumentNullException(nameof(change));
        }

        var band = FuelBandLabelResolver.ResolveFromFuelStateChange(change);
        return new MessageLogFuelBandPresentation(band, FuelBandLabelResolver.CueClassForBand(band));
    }

    public static string FormatFuelStateChangeDisplayLine(FuelStateChangeRecord change)
    {
        if (change is null)
        {
            throw new ArgumentNullException(nameof(change));
        }

        var body = string.Format(
            CultureInfo.InvariantCulture,
            "Fuel {0}: {1} → {2} ({3:F0} kg)",
            change.UnitId.Value,
            change.PreviousState,
            change.NewState,
            change.RemainingFuelKg);
        return $"[FUEL] {body}";
    }
}

/// <summary>Shared host helper: swap fuel-band USS cue classes on a label or list row element name.</summary>
public static class FuelBandCueClassList
{
    public static IReadOnlyList<string> All => FuelBandCueClasses.All;
}
