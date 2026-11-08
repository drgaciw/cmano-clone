namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Where a displayed doctrine field value came from.
/// Precedence matches <c>ScenarioPolicyProfile.ResolveUnitPolicy</c>:
/// unit override, then mission, then the friendly or opposing side default.
/// </summary>
public enum DoctrineFieldSourceLevel
{
    /// <summary>Per-unit <c>EffectivePolicy</c> override.</summary>
    UnitOverride = 0,

    /// <summary>Mission policy for a unit assigned to that mission.</summary>
    Mission = 1,

    /// <summary>Friendly side default. The resolver's last step when <c>isFriendly</c> is true.</summary>
    FriendlyDefault = 2,

    /// <summary>Opposing side default. The resolver's last step when <c>isFriendly</c> is false.</summary>
    OpposingDefault = 3,

    /// <summary>A level the resolver does not implement. Group is this level.</summary>
    Unsupported = 4,

    /// <summary>A field the resolver does not model, or no policy was supplied.</summary>
    NotModeled = 5,
}

/// <summary>Whether a doctrine cascade level is implemented by the resolver.</summary>
public enum DoctrineLevelAvailability
{
    /// <summary>The resolver can select this level.</summary>
    Modeled = 0,

    /// <summary>The level is named so callers do not invent it. Group is unsupported.</summary>
    Unsupported = 1,
}

/// <summary>Stable ids for doctrine, ROE, and WRA fields on the inheritance read model.</summary>
public static class DoctrineInheritanceFields
{
    /// <summary>ROE level from the resolved <c>EffectivePolicy</c>.</summary>
    public const string Roe = "Roe";

    /// <summary>WRA quantity (<c>EffectivePolicy.MaxSalvo</c>).</summary>
    public const string WraMaxSalvo = "WraMaxSalvo";

    /// <summary>Doctrine auto-engage gate on the resolved policy.</summary>
    public const string AutoEngageAuthorized = "AutoEngageAuthorized";

    /// <summary>Doctrine expend gate on the resolved policy.</summary>
    public const string ExpendAuthorized = "ExpendAuthorized";

    /// <summary>WRA target type. Not modeled by the resolver.</summary>
    public const string WraTargetType = "WraTargetType";

    /// <summary>WRA range. Not modeled by the resolver.</summary>
    public const string WraRange = "WraRange";

    /// <summary>EMCON. The panel label is separate; the resolver does not inherit it.</summary>
    public const string Emcon = "Emcon";

    /// <summary>Fields whose effective value is <c>ResolveUnitPolicy</c>.</summary>
    public static readonly IReadOnlyList<string> Resolved =
    [
        Roe,
        WraMaxSalvo,
        AutoEngageAuthorized,
        ExpendAuthorized,
    ];

    /// <summary>Fields reported explicitly as not modeled.</summary>
    public static readonly IReadOnlyList<string> NotModeled =
    [
        WraTargetType,
        WraRange,
        Emcon,
    ];
}

/// <summary>Cascade level ids. Group is present so it stays explicit and unsupported.</summary>
public static class DoctrineInheritanceLevels
{
    /// <summary>Unit override level.</summary>
    public const string UnitOverride = "UnitOverride";

    /// <summary>Mission level.</summary>
    public const string Mission = "Mission";

    /// <summary>Friendly side default.</summary>
    public const string FriendlyDefault = "FriendlyDefault";

    /// <summary>Opposing side default.</summary>
    public const string OpposingDefault = "OpposingDefault";

    /// <summary>Group level. Not implemented by the resolver.</summary>
    public const string Group = "Group";
}

/// <summary>
/// One doctrine, ROE, or WRA field for a selected unit: effective value, source level,
/// whether a parent value was overridden, and that parent value when present.
/// </summary>
public sealed record DoctrineFieldProvenance(
    string UnitId,
    string FieldId,
    string EffectiveValue,
    DoctrineFieldSourceLevel SourceLevel,
    bool IsOverridden,
    string? OverriddenParentValue);

/// <summary>Support flag for one cascade level.</summary>
public sealed record DoctrineLevelSupport(string Level, DoctrineLevelAvailability Availability);

/// <summary>Per-unit doctrine inheritance read model. Presentation only.</summary>
public sealed record DoctrineInheritanceReadModel(
    string UnitId,
    IReadOnlyList<DoctrineFieldProvenance> Fields,
    IReadOnlyList<DoctrineLevelSupport> Levels)
{
    /// <summary>Returns the field row with this id.</summary>
    /// <param name="fieldId">A <see cref="DoctrineInheritanceFields"/> id.</param>
    public DoctrineFieldProvenance Field(string fieldId)
    {
        foreach (var field in Fields)
        {
            if (string.Equals(field.FieldId, fieldId, StringComparison.Ordinal))
            {
                return field;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(fieldId), fieldId, "Doctrine field is not in this read model.");
    }
}
