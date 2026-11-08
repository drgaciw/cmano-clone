namespace ProjectAegis.Delegation.Projection;

using System.Globalization;
using Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Doctrine inheritance view model for UI binding (req 13 P0).</summary>
public sealed record DoctrineInheritanceEntry(
    string UnitId,
    string EffectiveRoeLabel,
    string EffectiveMaxSalvoLabel,
    string EffectiveEmconLabel,
    string InheritanceSource,
    bool IsInheritedFromMission,
    bool HasLocalOverride,
    string OverrideButtonLabel);

/// <summary>
/// Builds doctrine inheritance panel state from scenario policy (presentation only).
/// Field provenance uses <see cref="ScenarioPolicyProfile.ResolveUnitPolicy"/> and does not invent a group level.
/// </summary>
public static class DoctrineInheritanceProjection
{
    public static DoctrineInheritanceEntry ProjectUnit(
        TargetId unitId,
        ScenarioPolicyProfile? policy,
        bool isFriendly)
    {
        if (policy == null)
        {
            return new DoctrineInheritanceEntry(
                unitId.Value,
                "ROE: —",
                "SALVO: —",
                "EMCON: —",
                "SOURCE: —",
                false,
                false,
                "OVERRIDE: UNAVAILABLE");
        }

        var resolved = policy.ResolveUnitPolicy(unitId.Value, isFriendly);
        var effective = resolved.Effective;
        var source = ResolveInheritanceSource(resolved, unitId.Value, policy);
        var hasOverride = policy.UnitOverrides?.ContainsKey(unitId.Value) ?? false;

        return new DoctrineInheritanceEntry(
            unitId.Value,
            $"ROE: {effective.Roe}",
            $"SALVO: {effective.MaxSalvo}",
            FormatRadarEmconLabel(unitId.Value, policy),
            source,
            resolved.HasInheritedDoctrineFromMission,
            hasOverride,
            hasOverride ? "OVERRIDE: ACTIVE" : "OVERRIDE: NONE");
    }

    public static IReadOnlyList<DoctrineInheritanceEntry> ProjectAllUnits(
        IReadOnlyList<TargetId> unitIds,
        ScenarioPolicyProfile? policy,
        bool isFriendly)
    {
        var results = new List<DoctrineInheritanceEntry>();
        foreach (var unitId in unitIds.OrderBy(id => id.Value, StringComparer.Ordinal))
        {
            var entry = ProjectUnit(unitId, policy, isFriendly);
            results.Add(entry);
        }
        return results;
    }

    /// <summary>
    /// Effective value, source level, and overridden parent for each doctrine, ROE, and WRA field.
    /// Resolved fields match <see cref="ScenarioPolicyProfile.ResolveUnitPolicy"/>.
    /// WRA target type, WRA range, and EMCON inheritance are <see cref="DoctrineFieldSourceLevel.NotModeled"/>.
    /// Group is <see cref="DoctrineLevelAvailability.Unsupported"/>.
    /// </summary>
    public static DoctrineInheritanceReadModel ProjectFieldProvenance(
        TargetId unitId,
        ScenarioPolicyProfile? policy,
        bool isFriendly)
    {
        var fields = new List<DoctrineFieldProvenance>(
            DoctrineInheritanceFields.Resolved.Count + DoctrineInheritanceFields.NotModeled.Count);
        if (policy == null)
        {
            AddUnavailableFields(fields, unitId.Value);
        }
        else
        {
            AddResolvedFields(fields, unitId.Value, policy, isFriendly);
        }

        return new DoctrineInheritanceReadModel(unitId.Value, fields, LevelCatalog);
    }

    /// <summary>
    /// Field provenance for each selected unit, in ordinal unit-id order.
    /// </summary>
    public static IReadOnlyList<DoctrineInheritanceReadModel> ProjectFieldProvenance(
        IReadOnlyList<TargetId> unitIds,
        ScenarioPolicyProfile? policy,
        bool isFriendly)
    {
        var results = new List<DoctrineInheritanceReadModel>();
        foreach (var unitId in unitIds.OrderBy(id => id.Value, StringComparer.Ordinal))
        {
            results.Add(ProjectFieldProvenance(unitId, policy, isFriendly));
        }

        return results;
    }

    private static readonly DoctrineLevelSupport[] LevelCatalog =
    [
        new(DoctrineInheritanceLevels.UnitOverride, DoctrineLevelAvailability.Modeled),
        new(DoctrineInheritanceLevels.Mission, DoctrineLevelAvailability.Modeled),
        new(DoctrineInheritanceLevels.FriendlyDefault, DoctrineLevelAvailability.Modeled),
        new(DoctrineInheritanceLevels.OpposingDefault, DoctrineLevelAvailability.Modeled),
        new(DoctrineInheritanceLevels.Group, DoctrineLevelAvailability.Unsupported),
    ];

    private static void AddUnavailableFields(List<DoctrineFieldProvenance> fields, string unitId)
    {
        foreach (var fieldId in DoctrineInheritanceFields.Resolved)
        {
            fields.Add(UnavailableField(unitId, fieldId));
        }

        foreach (var fieldId in DoctrineInheritanceFields.NotModeled)
        {
            fields.Add(UnavailableField(unitId, fieldId));
        }
    }

    private static void AddResolvedFields(
        List<DoctrineFieldProvenance> fields,
        string unitId,
        ScenarioPolicyProfile policy,
        bool isFriendly)
    {
        var resolved = policy.ResolveUnitPolicy(unitId, isFriendly);
        var source = ClassifySource(policy, unitId, resolved, isFriendly);
        var parent = OverriddenParent(policy, unitId, isFriendly, source);
        foreach (var fieldId in DoctrineInheritanceFields.Resolved)
        {
            fields.Add(new DoctrineFieldProvenance(
                unitId,
                fieldId,
                FormatResolvedField(resolved.Effective, fieldId),
                source,
                parent.HasValue,
                parent.HasValue ? FormatResolvedField(parent.Value, fieldId) : null));
        }

        foreach (var fieldId in DoctrineInheritanceFields.NotModeled)
        {
            fields.Add(new DoctrineFieldProvenance(
                unitId,
                fieldId,
                nameof(DoctrineFieldSourceLevel.NotModeled),
                DoctrineFieldSourceLevel.NotModeled,
                false,
                null));
        }
    }

    private static DoctrineFieldProvenance UnavailableField(string unitId, string fieldId) =>
        new(unitId, fieldId, "\u2014", DoctrineFieldSourceLevel.NotModeled, false, null);

    private static DoctrineFieldSourceLevel ClassifySource(
        ScenarioPolicyProfile policy,
        string unitId,
        ResolvedUnitPolicy resolved,
        bool isFriendly)
    {
        if (policy.UnitOverrides.TryGetValue(unitId, out _))
        {
            return DoctrineFieldSourceLevel.UnitOverride;
        }

        if (resolved.HasInheritedDoctrineFromMission)
        {
            return DoctrineFieldSourceLevel.Mission;
        }

        return isFriendly
            ? DoctrineFieldSourceLevel.FriendlyDefault
            : DoctrineFieldSourceLevel.OpposingDefault;
    }

    private static EffectivePolicy? OverriddenParent(
        ScenarioPolicyProfile policy,
        string unitId,
        bool isFriendly,
        DoctrineFieldSourceLevel source)
    {
        if (source == DoctrineFieldSourceLevel.UnitOverride)
        {
            return IsMissionMember(policy, unitId)
                ? policy.MissionRoe
                : SideDefault(policy, isFriendly);
        }

        if (source == DoctrineFieldSourceLevel.Mission)
        {
            return SideDefault(policy, isFriendly);
        }

        return null;
    }

    private static bool IsMissionMember(ScenarioPolicyProfile policy, string unitId)
    {
        if (policy.MissionRoe == null || policy.MissionUnitIds.Count == 0)
        {
            return false;
        }

        foreach (var missionUnitId in policy.MissionUnitIds)
        {
            if (string.Equals(missionUnitId, unitId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static EffectivePolicy SideDefault(ScenarioPolicyProfile policy, bool isFriendly) =>
        isFriendly ? policy.FriendlyDefault : policy.OpposingDefault;

    private static string FormatResolvedField(EffectivePolicy policy, string fieldId) =>
        fieldId switch
        {
            DoctrineInheritanceFields.Roe => policy.Roe.ToString(),
            DoctrineInheritanceFields.WraMaxSalvo => policy.MaxSalvo.ToString(CultureInfo.InvariantCulture),
            DoctrineInheritanceFields.AutoEngageAuthorized => policy.AutoEngageAuthorized.ToString(),
            DoctrineInheritanceFields.ExpendAuthorized => policy.ExpendAuthorized.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldId), fieldId, "Not a resolver field."),
        };

    private static string ResolveInheritanceSource(
        ResolvedUnitPolicy resolved,
        string unitId,
        ScenarioPolicyProfile policy)
    {
        if (resolved.HasInheritedDoctrineFromMission)
        {
            return "SOURCE: Mission";
        }

        if (policy.UnitOverrides?.ContainsKey(unitId) ?? false)
        {
            return "SOURCE: Unit Override";
        }

        return "SOURCE: Scenario Default";
    }

    private static string FormatRadarEmconLabel(string unitId, ScenarioPolicyProfile policy)
    {
        var state = ScenarioEmconResolver.ResolveRadar(unitId, policy.UnitRadarEmcon);
        return state switch
        {
            EmconState.Active => "EMCON: ACTIVE",
            EmconState.Passive => "EMCON: PASSIVE",
            _ => "EMCON: OFF",
        };
    }
}
