namespace ProjectAegis.Data.Validation.Rules;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;

internal static class ValidationRules
{
    public static void DbRefRule(ScenarioDocumentDto scenario, ICatalogReader catalog, List<ValidationFinding> sink)
    {
        var dbRef = scenario.Metadata.DbRef ?? scenario.Metadata.DbSnapshotId;
        if (string.IsNullOrWhiteSpace(dbRef))
        {
            return;
        }

        if (!catalog.TryResolveDbRef(dbRef, out _))
        {
            sink.Add(new ValidationFinding(
                "DB_MISMATCH",
                ValidationSeverity.Error,
                $"Database reference '{dbRef}' does not resolve to an available catalog snapshot.",
                Data: new Dictionary<string, string> { ["dbRef"] = dbRef }));
        }
    }

    public static void MissionNoUnitsRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        foreach (var mission in scenario.Missions)
        {
            if (mission.AssignedUnitIds.Count == 0)
            {
                sink.Add(new ValidationFinding(
                    "MISSION_NO_UNITS",
                    ValidationSeverity.Error,
                    $"Mission '{mission.Id}' has no assigned units.",
                    MissionId: mission.Id));
            }
        }
    }

    public static void PatrolZoneRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        foreach (var mission in scenario.Missions)
        {
            if (!string.Equals(mission.Type, "Patrol", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (mission.PatrolZone.Count < 3)
            {
                sink.Add(new ValidationFinding(
                    "PATROL_ZONE_DEGENERATE",
                    ValidationSeverity.Error,
                    $"Patrol mission '{mission.Id}' requires at least 3 waypoints.",
                    MissionId: mission.Id));
            }
        }
    }

    public static void StrikeNoTargetsRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        foreach (var mission in scenario.Missions)
        {
            if (!string.Equals(mission.Type, "Strike", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (mission.TargetIds.Count == 0)
            {
                sink.Add(new ValidationFinding(
                    "STRIKE_NO_TARGETS",
                    ValidationSeverity.Error,
                    $"Strike mission '{mission.Id}' has no targets.",
                    MissionId: mission.Id));
            }
        }
    }

    public static void FerryDestinationRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        foreach (var mission in scenario.Missions)
        {
            if (!string.Equals(mission.Type, "Ferry", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(mission.FerryDestinationBaseId))
            {
                sink.Add(new ValidationFinding(
                    "FERRY_NO_DESTINATION",
                    ValidationSeverity.Error,
                    $"Ferry mission '{mission.Id}' is missing a destination base.",
                    MissionId: mission.Id));
            }
        }
    }

    public static void AirReadyLaunchRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        var readiness = scenario.Metadata.UnitReadiness;
        if (readiness == null || readiness.Count == 0)
        {
            return;
        }

        foreach (var mission in scenario.Missions)
        {
            if (!string.Equals(mission.Type, "Strike", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var unitId in mission.AssignedUnitIds.OrderBy(u => u, StringComparer.Ordinal))
            {
                if (readiness.TryGetValue(unitId, out var state) && !state.ReadyForLaunch)
                {
                    sink.Add(new ValidationFinding(
                        "AIR_NOT_READY",
                        ValidationSeverity.Error,
                        $"Unit '{unitId}' is not ready for launch (mission '{mission.Id}').",
                        MissionId: mission.Id,
                        UnitId: unitId));
                }
            }
        }
    }

    public static void FerryReachabilityRule(
        ScenarioDocumentDto scenario,
        ICatalogReader catalog,
        ValidationConfig config,
        List<ValidationFinding> sink)
    {
        foreach (var mission in scenario.Missions)
        {
            if (!string.Equals(mission.Type, "Ferry", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(mission.FerryDestinationBaseId))
            {
                continue;
            }

            if (mission.AssignedUnitIds.Count == 0)
            {
                continue;
            }

            var unitId = mission.AssignedUnitIds[0];
            if (!catalog.TryGetPlatformPosition(unitId, out var unitLat, out var unitLon) ||
                !catalog.TryGetPlatformPosition(mission.FerryDestinationBaseId, out var baseLat, out var baseLon))
            {
                continue;
            }

            if (!catalog.TryGetCombatRadiusNm(unitId, out var combatRadiusNm))
            {
                continue;
            }

            var distanceNm = ReachabilityCalculator.HaversineNm(unitLat, unitLon, baseLat, baseLon);
            if (ReachabilityCalculator.TryClassifyStrikeUnreachable(
                    distanceNm,
                    combatRadiusNm,
                    config.IngressEgressPadNm,
                    config.FuelFraction,
                    out var excessNm,
                    out var code))
            {
                var rounded = Math.Round(excessNm, 1);
                var ferryCode = string.Equals(code, "STRIKE_UNREACHABLE_FUEL", StringComparison.Ordinal)
                    ? "FERRY_UNREACHABLE_FUEL"
                    : "FERRY_UNREACHABLE";
                var message = string.Equals(ferryCode, "FERRY_UNREACHABLE_FUEL", StringComparison.Ordinal)
                    ? $"Ferry mission '{mission.Id}' destination exceeds fuel range by {rounded} nm."
                    : $"Ferry mission '{mission.Id}' destination is out of combat radius by {rounded} nm.";
                sink.Add(new ValidationFinding(
                    ferryCode,
                    ValidationSeverity.Error,
                    message,
                    MissionId: mission.Id,
                    UnitId: unitId,
                    TargetId: mission.FerryDestinationBaseId,
                    Data: new Dictionary<string, string> { ["excess_nm"] = rounded.ToString("F1") }));
            }
        }
    }

    public static void StrikeReachabilityRule(
        ScenarioDocumentDto scenario,
        ICatalogReader catalog,
        ValidationConfig config,
        List<ValidationFinding> sink)
    {
        foreach (var mission in scenario.Missions)
        {
            if (!string.Equals(mission.Type, "Strike", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (mission.AssignedUnitIds.Count == 0 || mission.TargetIds.Count == 0)
            {
                continue;
            }

            var unitId = mission.AssignedUnitIds[0];
            if (!catalog.TryGetPlatformPosition(unitId, out var unitLat, out var unitLon))
            {
                continue;
            }

            if (!catalog.TryGetCombatRadiusNm(unitId, out var combatRadiusNm))
            {
                sink.Add(new ValidationFinding(
                    "STRIKE_INVALID_PLATFORM",
                    ValidationSeverity.Error,
                    $"Strike mission '{mission.Id}' assigned unit '{unitId}' has invalid combat_radius_nm.",
                    MissionId: mission.Id,
                    UnitId: unitId));
                continue;
            }

            foreach (var targetId in mission.TargetIds.OrderBy(t => t, StringComparer.Ordinal))
            {
                if (!catalog.TryGetPlatformPosition(targetId, out var tgtLat, out var tgtLon))
                {
                    continue;
                }

                var distanceNm = ReachabilityCalculator.HaversineNm(unitLat, unitLon, tgtLat, tgtLon);
                if (ReachabilityCalculator.TryClassifyStrikeUnreachable(
                        distanceNm,
                        combatRadiusNm,
                        config.IngressEgressPadNm,
                        config.FuelFraction,
                        out var excessNm,
                        out var code))
                {
                    var rounded = Math.Round(excessNm, 1);
                    var message = string.Equals(code, "STRIKE_UNREACHABLE_FUEL", StringComparison.Ordinal)
                        ? $"Strike mission '{mission.Id}' target '{targetId}' exceeds fuel range by {rounded} nm."
                        : $"Strike mission '{mission.Id}' target '{targetId}' is out of combat radius by {rounded} nm.";
                    sink.Add(new ValidationFinding(
                        code,
                        ValidationSeverity.Error,
                        message,
                        MissionId: mission.Id,
                        UnitId: unitId,
                        TargetId: targetId,
                        Data: new Dictionary<string, string> { ["excess_nm"] = rounded.ToString("F1") }));
                }
            }
        }
    }

    public static void SupportMissionRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        foreach (var mission in scenario.Missions)
        {
            if (!string.Equals(mission.Type, "Support", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(mission.SupportRole))
            {
                sink.Add(new ValidationFinding(
                    "SUPPORT_NO_ROLE",
                    ValidationSeverity.Error,
                    $"Support mission '{mission.Id}' requires a role (Tanker/AEW/EW).",
                    MissionId: mission.Id));
            }

            if (mission.StationGeometry is not { Count: >= 1 })
            {
                sink.Add(new ValidationFinding(
                    "SUPPORT_NO_STATION",
                    ValidationSeverity.Error,
                    $"Support mission '{mission.Id}' requires station geometry.",
                    MissionId: mission.Id));
            }
        }
    }

    public static void DoctrineResolutionRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        if (scenario.Sides.Count == 0)
        {
            return;
        }

        foreach (var mission in scenario.Missions.OrderBy(m => m.Id, StringComparer.Ordinal))
        {
            var resolved = ScenarioDoctrineResolver.ResolveMissionDoctrine(scenario, mission);
            sink.Add(new ValidationFinding(
                "DOCTRINE_RESOLVED",
                ValidationSeverity.Info,
                $"Mission '{mission.Id}' resolved ROE={resolved.Roe} EMCON={resolved.Emcon}.",
                MissionId: mission.Id,
                Data: new Dictionary<string, string>
                {
                    ["roe"] = resolved.Roe,
                    ["emcon"] = resolved.Emcon,
                    ["inherited"] = (!resolved.HasRoeOverride).ToString(),
                }));
        }
    }
}