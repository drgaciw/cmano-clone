namespace ProjectAegis.Data.Validation.Rules;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;

internal static class ValidationRules
{
    public static void TlBranchRule(ScenarioDocumentDto scenario, ICatalogReader catalog, List<ValidationFinding> sink)
    {
        var tlBranch = scenario.Metadata.TlBranch;
        if (string.IsNullOrWhiteSpace(tlBranch))
        {
            sink.Add(new ValidationFinding(
                "TL_BRANCH_MISSING",
                ValidationSeverity.Error,
                "Scenario package metadata.tlBranch is required (TL-0…TL-5).",
                Data: new Dictionary<string, string> { ["field"] = "tlBranch" }));
            return;
        }

        var trimmed = tlBranch.Trim();
        if (!CatalogTlTier.IsValid(trimmed))
        {
            sink.Add(new ValidationFinding(
                "TL_BRANCH_INVALID",
                ValidationSeverity.Error,
                $"Scenario tlBranch '{trimmed}' is not a valid TL tier (TL-0…TL-5).",
                Data: new Dictionary<string, string> { ["tlBranch"] = trimmed }));
            return;
        }

        var normalized = CatalogTlTier.Normalize(trimmed);
        string snapshotId;
        if (ScenarioPackage.HasExplicitDbBinding(scenario.Metadata))
        {
            snapshotId = ResolveExplicitSnapshotId(scenario.Metadata, catalog);
        }
        else if (!catalog.TryResolveSnapshotForTlBranch(normalized, out snapshotId, out _))
        {
            return;
        }

        if (catalog.TryGetSnapshotBranch(snapshotId, out var snapshotBranch) &&
            !string.Equals(normalized, snapshotBranch, StringComparison.Ordinal))
        {
            sink.Add(new ValidationFinding(
                "TL_BRANCH_SNAPSHOT_MISMATCH",
                ValidationSeverity.Error,
                $"Scenario tlBranch '{normalized}' does not match catalog_snapshot.branch '{snapshotBranch}' for snapshot '{snapshotId}'.",
                Data: new Dictionary<string, string>
                {
                    ["tlBranch"] = normalized,
                    ["snapshotBranch"] = snapshotBranch,
                    ["snapshotId"] = snapshotId,
                }));
        }
    }

    public static void TlReleaseTrainRule(ScenarioDocumentDto scenario, ICatalogReader catalog, List<ValidationFinding> sink)
    {
        var tlBranch = scenario.Metadata.TlBranch;
        if (string.IsNullOrWhiteSpace(tlBranch))
        {
            return;
        }

        var trimmed = tlBranch.Trim();
        if (!CatalogTlTier.IsValid(trimmed))
        {
            return;
        }

        var normalized = CatalogTlTier.Normalize(trimmed);
        if (!catalog.TryResolveSnapshotForTlBranch(normalized, out var resolvedSnapshot, out _))
        {
            sink.Add(new ValidationFinding(
                "TL_RELEASE_TRAIN_NOT_FOUND",
                ValidationSeverity.Error,
                $"No catalog snapshot in release train for tlBranch '{normalized}'.",
                Data: new Dictionary<string, string> { ["tlBranch"] = normalized }));
            return;
        }

        if (!ScenarioPackage.HasExplicitDbBinding(scenario.Metadata))
        {
            return;
        }

        var explicitSnapshot = ResolveExplicitSnapshotId(scenario.Metadata, catalog);
        if (!string.Equals(explicitSnapshot, resolvedSnapshot, StringComparison.Ordinal))
        {
            var dbRef = scenario.Metadata.DbRef ?? scenario.Metadata.DbSnapshotId ?? "";
            sink.Add(new ValidationFinding(
                "TL_RELEASE_TRAIN_MISMATCH",
                ValidationSeverity.Error,
                $"Explicit database binding '{dbRef}' resolves to snapshot '{explicitSnapshot}' but tlBranch '{normalized}' release train expects '{resolvedSnapshot}'.",
                Data: new Dictionary<string, string>
                {
                    ["tlBranch"] = normalized,
                    ["explicitSnapshot"] = explicitSnapshot,
                    ["releaseTrainSnapshot"] = resolvedSnapshot,
                    ["dbRef"] = dbRef,
                }));
        }
    }

    private static string ResolveExplicitSnapshotId(ScenarioMetadataDto metadata, ICatalogReader catalog)
    {
        var dbRef = metadata.DbRef ?? metadata.DbSnapshotId;
        if (!string.IsNullOrWhiteSpace(dbRef) && catalog.TryResolveDbRef(dbRef, out var resolved))
        {
            return resolved;
        }

        return ScenarioPackage.ResolveDbSnapshotId(metadata);
    }

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

    // Model integrity extensions for continuous live validation (incompatible hosts, broken refs, terrain from research/11)
    public static void IncompatibleHostRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        // simplistic: air units require host capable platform (demo)
        foreach (var mission in scenario.Missions)
        {
            if (!string.Equals(mission.Type, "Strike", StringComparison.OrdinalIgnoreCase) && !string.Equals(mission.Type, "Patrol", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var uid in mission.AssignedUnitIds)
            {
                if (uid.Contains("air", StringComparison.OrdinalIgnoreCase) && scenario.Missions.Count(m => m.AssignedUnitIds.Contains("carrier")) == 0)
                {
                    sink.Add(new ValidationFinding("INCOMPATIBLE_HOST", ValidationSeverity.Error, $"Unit '{uid}' incompatible host relationship (no carrier).", MissionId: mission.Id, UnitId: uid));
                }
            }
        }
    }

    public static void BrokenRefRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        // detect broken mission refs demo
        var allUnits = scenario.Missions.SelectMany(m => m.AssignedUnitIds).ToHashSet(StringComparer.Ordinal);
        foreach (var m in scenario.Missions)
        {
            foreach (var t in m.TargetIds)
            {
                if (t.StartsWith("ref:") && !allUnits.Contains(t.Replace("ref:","")))
                {
                    sink.Add(new ValidationFinding("BROKEN_REF", ValidationSeverity.Error, $"Broken reference '{t}' in mission '{m.Id}'.", MissionId: m.Id));
                }
            }
        }
    }

    /// <summary>Resolves per-mission ROE from side default or mission override (AME-3.2 / AC-4).</summary>
    public static void DoctrineInheritanceRule(ScenarioDocumentDto scenario, List<ValidationFinding> sink)
    {
        const string defaultRoe = "WeaponsFree";
        var sideRoe = string.IsNullOrWhiteSpace(scenario.Metadata.SideRoe)
            ? defaultRoe
            : scenario.Metadata.SideRoe.Trim();

        foreach (var mission in scenario.Missions.OrderBy(m => m.Id, StringComparer.Ordinal))
        {
            string resolvedRoe;
            string inheritanceSource;
            if (!string.IsNullOrWhiteSpace(mission.RoeOverride))
            {
                resolvedRoe = mission.RoeOverride.Trim();
                inheritanceSource = "override";
            }
            else
            {
                resolvedRoe = sideRoe;
                inheritanceSource = "side";
            }

            sink.Add(new ValidationFinding(
                "DOCTRINE_RESOLVED",
                ValidationSeverity.Info,
                $"Mission '{mission.Id}' resolved ROE '{resolvedRoe}' from {inheritanceSource}.",
                MissionId: mission.Id,
                Data: new Dictionary<string, string>
                {
                    ["missionId"] = mission.Id,
                    ["resolvedRoe"] = resolvedRoe,
                    ["inheritanceSource"] = inheritanceSource,
                }));
        }
    }
}