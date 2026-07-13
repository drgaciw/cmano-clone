namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;

/// <summary>DBI-3.5 bounded detect-only kill-chain impossibility rules (R1–R4).</summary>
public static class KillChainRules
{
    public const double NauticalMilesToMeters = 1852.0;

    public const string OrphanEdgeCode = "KILL_CHAIN_ORPHAN_EDGE";
    public const string RangeExceedsSensorCode = "KILL_CHAIN_RANGE_EXCEEDS_SENSOR";
    public const string SpeedMismatchCode = "KILL_CHAIN_SPEED_MISMATCH";
    public const string WeaponExceedsReachCode = "KILL_CHAIN_WEAPON_EXCEEDS_PLATFORM_REACH";

    public static IReadOnlyList<DatabaseAgentFinding> Evaluate(ICatalogReader catalog)
    {
        var findings = new List<DatabaseAgentFinding>();
        var mountsByPlatform = BuildMountLookup(catalog.GetSortedMounts());
        var approvedSensors = catalog.GetSortedSensorBindings()
            .Where(IsApprovedBinding)
            .ToArray();

        foreach (var edge in catalog.GetSortedDependencyEdges())
        {
            EvaluateOrphanEdge(catalog, edge, mountsByPlatform, approvedSensors, findings);

            if (edge.Kind != CatalogDependencyEdgeKind.PlatformToMountToWeapon)
            {
                continue;
            }

            EvaluateRangeExceedsSensor(catalog, edge, approvedSensors, findings);
            EvaluateSpeedMismatch(catalog, edge, findings);
            EvaluateWeaponExceedsPlatformReach(catalog, edge, findings);
        }

        return SortFindings(findings);
    }

    public static string ComputeFindingsHash(IReadOnlyList<DatabaseAgentFinding> findings)
    {
        var sorted = SortFindings(findings);
        var sb = new System.Text.StringBuilder();
        foreach (var finding in sorted)
        {
            sb.Append(finding.Code).Append('|')
                .Append(finding.Severity).Append('|')
                .Append(finding.Message).Append('\n');
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        using var sha = System.Security.Cryptography.SHA256.Create();
        return ToHexLower(sha.ComputeHash(bytes));
    }

    private static void EvaluateOrphanEdge(
        ICatalogReader catalog,
        CatalogDependencyEdge edge,
        IReadOnlyDictionary<string, HashSet<string>> mountsByPlatform,
        IReadOnlyList<CatalogSensorBinding> approvedSensors,
        List<DatabaseAgentFinding> findings)
    {
        if (!PlatformExists(catalog, edge.PlatformId))
        {
            findings.Add(OrphanFinding(edge, $"platform '{edge.PlatformId}' missing"));
            return;
        }

        switch (edge.Kind)
        {
            case CatalogDependencyEdgeKind.PlatformToSensor:
                if (!approvedSensors.Any(s =>
                        string.Equals(s.PlatformId, edge.PlatformId, StringComparison.Ordinal) &&
                        string.Equals(s.SensorId, edge.SensorId, StringComparison.Ordinal)))
                {
                    findings.Add(OrphanFinding(edge, $"sensor '{edge.SensorId}' missing on platform"));
                }

                break;

            case CatalogDependencyEdgeKind.PlatformToMount:
                if (!MountExists(mountsByPlatform, edge.PlatformId, edge.MountId))
                {
                    findings.Add(OrphanFinding(edge, $"mount '{edge.MountId}' missing on platform"));
                }

                break;

            case CatalogDependencyEdgeKind.PlatformToMountToWeapon:
                if (!MountExists(mountsByPlatform, edge.PlatformId, edge.MountId))
                {
                    findings.Add(OrphanFinding(edge, $"mount '{edge.MountId}' missing on platform"));
                }

                if (!catalog.TryGetWeaponEnvelope(edge.WeaponId, out _))
                {
                    findings.Add(OrphanFinding(edge, $"weapon '{edge.WeaponId}' missing"));
                }

                break;
        }
    }

    private static void EvaluateRangeExceedsSensor(
        ICatalogReader catalog,
        CatalogDependencyEdge edge,
        IReadOnlyList<CatalogSensorBinding> approvedSensors,
        List<DatabaseAgentFinding> findings)
    {
        if (!catalog.TryGetWeaponEnvelope(edge.WeaponId, out var weaponEnvelope))
        {
            return;
        }

        if (!TryGetPlatformSensorEnvelopeMaxMeters(catalog, edge.PlatformId, approvedSensors, out var sensorEnvelopeMeters))
        {
            return;
        }

        if (weaponEnvelope.MaxRangeMeters > sensorEnvelopeMeters)
        {
            findings.Add(new DatabaseAgentFinding(
                RangeExceedsSensorCode,
                $"{edge.PlatformId}/{edge.MountId}/{edge.WeaponId}: weapon_max_m={weaponEnvelope.MaxRangeMeters:F0} exceeds sensor_envelope_m={sensorEnvelopeMeters:F0}",
                "error"));
        }
    }

    private static void EvaluateSpeedMismatch(
        ICatalogReader catalog,
        CatalogDependencyEdge edge,
        List<DatabaseAgentFinding> findings)
    {
        if (!catalog.TryGetWeaponEnvelope(edge.WeaponId, out var weaponEnvelope))
        {
            return;
        }

        if (!catalog.TryGetMobility(edge.PlatformId, out var mobility))
        {
            findings.Add(new DatabaseAgentFinding(
                SpeedMismatchCode,
                $"{edge.PlatformId}/{edge.MountId}/{edge.WeaponId}: mobility missing — speed check skipped",
                "warning"));
            return;
        }

        var weaponMinSpeedKnots = InferWeaponMinSpeedKnots(edge.WeaponId, weaponEnvelope);
        if (weaponMinSpeedKnots > mobility.MaxSpeedKnots)
        {
            findings.Add(new DatabaseAgentFinding(
                SpeedMismatchCode,
                $"{edge.PlatformId}/{edge.MountId}/{edge.WeaponId}: weapon_min_speed_kts={weaponMinSpeedKnots:F0} exceeds platform_max_speed_kts={mobility.MaxSpeedKnots:F0}",
                "error"));
        }
    }

    private static void EvaluateWeaponExceedsPlatformReach(
        ICatalogReader catalog,
        CatalogDependencyEdge edge,
        List<DatabaseAgentFinding> findings)
    {
        if (!catalog.TryGetWeaponEnvelope(edge.WeaponId, out var weaponEnvelope))
        {
            return;
        }

        if (!catalog.TryGetCombatRadiusNm(edge.PlatformId, out var combatRadiusNm))
        {
            return;
        }

        var reachMeters = combatRadiusNm * NauticalMilesToMeters;
        if (weaponEnvelope.MaxRangeMeters > reachMeters)
        {
            findings.Add(new DatabaseAgentFinding(
                WeaponExceedsReachCode,
                $"{edge.PlatformId}/{edge.MountId}/{edge.WeaponId}: weapon_max_m={weaponEnvelope.MaxRangeMeters:F0} exceeds platform_reach_m={reachMeters:F0} (radius_nm={combatRadiusNm:F1})",
                "error"));
        }
    }

    private static bool TryGetPlatformSensorEnvelopeMaxMeters(
        ICatalogReader catalog,
        string platformId,
        IReadOnlyList<CatalogSensorBinding> approvedSensors,
        out double envelopeMeters)
    {
        envelopeMeters = 0;
        if (!catalog.TryGetCombatRadiusNm(platformId, out var combatRadiusNm))
        {
            return false;
        }

        var platformRadiusMeters = combatRadiusNm * NauticalMilesToMeters;
        var any = false;
        foreach (var sensor in approvedSensors)
        {
            if (!string.Equals(sensor.PlatformId, platformId, StringComparison.Ordinal))
            {
                continue;
            }

            var scale = Math.Clamp(sensor.BasePd, 0.05, 1.0);
            envelopeMeters = Math.Max(envelopeMeters, platformRadiusMeters * scale);
            any = true;
        }

        return any;
    }

    private static double InferWeaponMinSpeedKnots(string weaponId, WeaponEnvelopeDto envelope)
    {
        if (weaponId.Contains("hypersonic", StringComparison.OrdinalIgnoreCase) ||
            weaponId.Contains("mach5", StringComparison.OrdinalIgnoreCase) ||
            weaponId.Contains("boost-glide", StringComparison.OrdinalIgnoreCase))
        {
            return 3500;
        }

        if (envelope.MaxRangeMeters >= 300_000)
        {
            return 2500;
        }

        return 450;
    }

    private static bool PlatformExists(ICatalogReader catalog, string platformId) =>
        catalog.TryGetCombatRadiusNm(platformId, out _);

    private static bool MountExists(
        IReadOnlyDictionary<string, HashSet<string>> mountsByPlatform,
        string platformId,
        string mountId) =>
        mountsByPlatform.TryGetValue(platformId, out var mounts) &&
        mounts.Contains(mountId);

    private static IReadOnlyDictionary<string, HashSet<string>> BuildMountLookup(IReadOnlyList<CatalogMount> mounts)
    {
        var lookup = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var mount in mounts.Where(IsApprovedBinding))
        {
            if (!lookup.TryGetValue(mount.PlatformId, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                lookup[mount.PlatformId] = set;
            }

            set.Add(mount.MountId);
        }

        return lookup;
    }

    private static bool IsApprovedBinding(CatalogMount mount) =>
        string.Equals(mount.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase);

    private static bool IsApprovedBinding(CatalogSensorBinding sensor) =>
        string.Equals(sensor.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase);

    private static DatabaseAgentFinding OrphanFinding(CatalogDependencyEdge edge, string detail) =>
        new(
            OrphanEdgeCode,
            $"{edge.PlatformId}/{edge.MountId}/{edge.WeaponId}/{edge.SensorId}: {detail}",
            "error");

    private static IReadOnlyList<DatabaseAgentFinding> SortFindings(IReadOnlyList<DatabaseAgentFinding> findings) =>
        findings
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToArray();

    private static string ToHexLower(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = GetHexNibble(bytes[i] >> 4);
            chars[i * 2 + 1] = GetHexNibble(bytes[i] & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) => (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}