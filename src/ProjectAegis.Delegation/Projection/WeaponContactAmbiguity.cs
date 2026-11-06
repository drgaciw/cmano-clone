namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Whether a nautical-mile quantity was supplied by a producer.
/// Absence is <see cref="Unknown"/>. Zero is a real measurement, not a stand-in for absence.
/// </summary>
public enum MeasuredNauticalMilesState
{
    /// <summary>No producer supplied a finite measurement.</summary>
    Unknown = 0,

    /// <summary>A finite measurement was supplied.</summary>
    Known = 1,
}

/// <summary>
/// A nautical-mile measurement. <see cref="NauticalMiles"/> is meaningful only when
/// <see cref="IsKnown"/> is true.
/// </summary>
public readonly record struct MeasuredNauticalMiles(
    MeasuredNauticalMilesState State,
    double? NauticalMiles)
{
    /// <summary>No measurement.</summary>
    public static MeasuredNauticalMiles Unknown { get; } =
        new(MeasuredNauticalMilesState.Unknown, null);

    /// <summary>True when <see cref="State"/> is known and the value is finite.</summary>
    public bool IsKnown => TryGetNauticalMiles(out _);

    /// <summary>
    /// Reads a finite known value. Returns false for <see cref="Unknown"/> and non-finite numbers.
    /// </summary>
    public bool TryGetNauticalMiles(out double value)
    {
        if (State == MeasuredNauticalMilesState.Known && NauticalMiles is { } known && double.IsFinite(known))
        {
            value = known;
            return true;
        }

        value = 0d;
        return false;
    }

    /// <summary>
    /// Maps a nullable producer value. Null and non-finite numbers stay <see cref="Unknown"/>.
    /// </summary>
    public static MeasuredNauticalMiles FromNullable(double? nauticalMiles)
    {
        if (nauticalMiles is { } value && double.IsFinite(value))
        {
            return new MeasuredNauticalMiles(MeasuredNauticalMilesState.Known, value);
        }

        return Unknown;
    }
}

/// <summary>
/// Ambiguity doctrine applied to weapon tolerance.
/// Contact provenance and weapon envelopes do not carry one (DRG-385). The only
/// level this projection can report from those sources is <see cref="Unknown"/>.
/// </summary>
public enum AmbiguityDoctrineLevel
{
    /// <summary>No ambiguity-doctrine producer supplied a level.</summary>
    Unknown = 0,
}

/// <summary>
/// Whether positional ambiguity is inside weapon tolerance.
/// <see cref="Unknown"/> when downrange ambiguity, cross-range ambiguity, or tolerance is unknown.
/// </summary>
public enum AmbiguityToleranceVerdict
{
    /// <summary>At least one compared measurement is unknown.</summary>
    Unknown = 0,

    /// <summary>Both ambiguities are known and are less than or equal to tolerance.</summary>
    Pass = 1,

    /// <summary>Both ambiguities and the tolerance are known, and at least one ambiguity is greater.</summary>
    Fail = 2,
}

/// <summary>
/// Explicit measurements for one weapon and one contact.
/// Null quantities mean the producer did not report them. Callers must not fill those
/// gaps from identification confidence or from weapon range.
/// </summary>
public sealed record WeaponContactAmbiguityFacts(
    string WeaponId,
    string ContactId,
    double? DownrangeAmbiguityNm,
    double? CrossRangeAmbiguityNm,
    double? WeaponToleranceNm,
    AmbiguityDoctrineLevel Doctrine);

/// <summary>
/// DRG-411: one (weapon, contact) comparison of positional ambiguity with weapon tolerance.
/// Headless DTO. It does not draw an area-of-uncertainty ellipse.
/// </summary>
public sealed record WeaponContactAmbiguityRow(
    string WeaponId,
    string ContactId,
    MeasuredNauticalMiles DownrangeAmbiguity,
    MeasuredNauticalMiles CrossRangeAmbiguity,
    MeasuredNauticalMiles WeaponTolerance,
    AmbiguityDoctrineLevel Doctrine,
    AmbiguityToleranceVerdict Verdict,
    string? RefusalText);

/// <summary>Replay-stable ambiguity picture for weapon and contact pairs.</summary>
public sealed record WeaponContactAmbiguitySnapshot(IReadOnlyList<WeaponContactAmbiguityRow> Rows)
{
    /// <summary>No pairs.</summary>
    public static WeaponContactAmbiguitySnapshot Empty { get; } =
        new(Array.Empty<WeaponContactAmbiguityRow>());
}
