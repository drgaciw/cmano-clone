namespace ProjectAegis.Data.Platform;

using System.Globalization;
using ProjectAegis.Data.Validation;

/// <summary>
/// Req-21 / ADR-011 PLE-4.*: deterministic fitting validation over an edited workbook. Cross-sheet
/// referential and capacity rules that the SQLite CHECK constraints cannot express at edit time.
/// Pure — operates on the in-memory <see cref="PlatformWorkbook"/>; findings sorted by (Code, Message)
/// for golden-hash stability.
/// </summary>
public static class PlatformWorkbookValidator
{
    public const string MagazineUnknownMount = "PLE-MAG-MOUNT";
    public const string MagazineUnknownLoadout = "PLE-MAG-LOADOUT";
    public const string MagazineOverCapacity = "PLE-MAG-CAPACITY";
    public const string MagazineNegativeQuantity = "PLE-MAG-QTY-NEGATIVE";
    public const string MountRangeInvalid = "PLE-MOUNT-RANGE";

    public const string MobilityHeaderMismatch = "PLE-MOB-HEADER";
    public const string SignaturesHeaderMismatch = "PLE-SIG-HEADER";
    public const string EmconHeaderMismatch = "PLE-EMC-HEADER";
    public const string PhaseBOrphanPlatform = "PLE-PHB-ORPHAN";
    public const string EmconInvalidCondition = "PLE-EMCON-CONDITION";
    public const string EmconInvalidPosture = "PLE-EMCON-POSTURE";
    public const string MobilityNegativeSpeed = "PLE-MOB-SPEED";
    public const string MobilityNegativeRange = "PLE-MOB-RANGE";

    public const string PlatformsHeaderMismatch = "PLE-PLT-HEADER";
    public const string DamageNonPositiveMaxHp = "PLE-DMG-HP";
    public const string DamageMaxHpExceedsCeiling = "PLE-DMG-HP-CEIL";
    public const string DamageWithdrawThresholdInvalid = "PLE-DMG-WITHDRAW";
    public const string DamageCriticalFlagsInvalid = "PLE-DMG-FLAGS";

    /// <summary>Gameplay abstraction ceiling for platform HP (DBI-2.2).</summary>
    public const double MaxHpCeiling = 100_000;

    private static readonly char KeySeparator = (char)31; // US — unit separator, absent from catalog IDs

    private static readonly string[] ExpectedMobilityHeader =
    [
        "PlatformId", "MaxSpeedKnots", "CruiseSpeedKnots", "MaxAltitudeFt", "MaxDepthM",
        "FuelCapacity", "RangeNm", "EnduranceHr",
    ];

    private static readonly string[] ExpectedSignaturesHeader =
    [
        "PlatformId", "RcsBandDbsm", "IrSignature", "AcousticSignatureDb", "MagneticSignature",
    ];

    private static readonly string[] ExpectedEmconHeader =
    [
        "PlatformId", "Condition", "EmitterId", "Posture",
    ];

    private static readonly string[] ExpectedPlatformsHeader =
    [
        "PlatformId", "LatDeg", "LonDeg", "CombatRadiusNm",
        "MaxHp", "WithdrawThresholdPct", "CriticalFlags",
    ];

    private static readonly HashSet<string> AllowedEmconConditions = new(StringComparer.OrdinalIgnoreCase)
    {
        "silent", "restricted", "free",
    };

    private static readonly HashSet<string> AllowedEmconPostures = new(StringComparer.OrdinalIgnoreCase)
    {
        "off", "standby", "active",
    };

    public static IReadOnlyList<ValidationFinding> Validate(PlatformWorkbook workbook)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));

        var findings = new List<ValidationFinding>();
        var mounts = SheetView.For(workbook, "Mounts");
        var loadouts = SheetView.For(workbook, "Loadouts");
        var magazines = SheetView.For(workbook, "Magazines");

        // Index mount capacity by (platformId, mountId).
        var mountCapacity = new Dictionary<string, int>(StringComparer.Ordinal);
        if (mounts is not null)
        {
            foreach (var row in mounts.Rows)
            {
                var platformId = mounts.Cell(row, "PlatformId");
                var mountId = mounts.Cell(row, "MountId");
                mountCapacity[Key(platformId, mountId)] = ParseInt(mounts.Cell(row, "Capacity"));

                var arc = ParseDouble(mounts.Cell(row, "ArcDeg"));
                var cap = ParseInt(mounts.Cell(row, "Capacity"));
                if (arc < 0 || arc > 360 || cap < 0)
                {
                    findings.Add(new ValidationFinding(
                        MountRangeInvalid,
                        ValidationSeverity.Error,
                        $"Mount '{mountId}' on '{platformId}' has invalid arc/capacity (arc={arc}, capacity={cap}).",
                        UnitId: platformId,
                        TargetId: mountId));
                }
            }
        }

        var loadoutKeys = new HashSet<string>(StringComparer.Ordinal);
        if (loadouts is not null)
        {
            foreach (var row in loadouts.Rows)
            {
                loadoutKeys.Add(Key(loadouts.Cell(row, "PlatformId"), loadouts.Cell(row, "LoadoutId")));
            }
        }

        if (magazines is not null)
        {
            // Accumulate loaded quantity per (platformId, loadoutId, mountId): a single mount can carry
            // multiple weapon types under one loadout (e.g. a mixed VLS cell loaded with both ESSM and
            // Tomahawk rounds), so capacity must be checked against the *sum* of quantities sharing a
            // mount+loadout, not against each magazine row in isolation.
            var loadedQuantity = new Dictionary<string, int>(StringComparer.Ordinal);
            var loadGroupInfo = new Dictionary<string, (string PlatformId, string LoadoutId, string MountId)>(StringComparer.Ordinal);

            foreach (var row in magazines.Rows)
            {
                var platformId = magazines.Cell(row, "PlatformId");
                var loadoutId = magazines.Cell(row, "LoadoutId");
                var mountId = magazines.Cell(row, "MountId");
                var quantity = ParseInt(magazines.Cell(row, "Quantity"));

                if (quantity < 0)
                {
                    // A negative Quantity is nonsensical data on its own, but it is also load-bearing for
                    // the cumulative capacity check just below: if it were summed as-is, a negative row
                    // could silently discount a genuinely over-capacity mount (e.g. +50 rounds offset by
                    // -20 reads as "30 of 32 used", masking that the +50 weapon type alone busts capacity).
                    // Flag it explicitly and never let it contribute a negative amount to the group total.
                    findings.Add(new ValidationFinding(
                        MagazineNegativeQuantity,
                        ValidationSeverity.Error,
                        $"Magazine '{magazines.Cell(row, "WeaponId")}' on '{platformId}' has negative Quantity ({quantity}).",
                        UnitId: platformId,
                        TargetId: mountId));
                }

                if (!loadoutKeys.Contains(Key(platformId, loadoutId)))
                {
                    findings.Add(new ValidationFinding(
                        MagazineUnknownLoadout,
                        ValidationSeverity.Error,
                        $"Magazine on '{platformId}' references unknown loadout '{loadoutId}'.",
                        UnitId: platformId,
                        TargetId: loadoutId));
                }

                if (!mountCapacity.ContainsKey(Key(platformId, mountId)))
                {
                    findings.Add(new ValidationFinding(
                        MagazineUnknownMount,
                        ValidationSeverity.Error,
                        $"Magazine on '{platformId}' references unknown mount '{mountId}'.",
                        UnitId: platformId,
                        TargetId: mountId));
                    continue;
                }

                var loadGroupKey = Key(Key(platformId, loadoutId), mountId);
                loadedQuantity[loadGroupKey] = loadedQuantity.GetValueOrDefault(loadGroupKey) + Math.Max(0, quantity);
                loadGroupInfo[loadGroupKey] = (platformId, loadoutId, mountId);
            }

            foreach (var (loadGroupKey, totalQuantity) in loadedQuantity)
            {
                var (platformId, loadoutId, mountId) = loadGroupInfo[loadGroupKey];
                var capacity = mountCapacity[Key(platformId, mountId)];
                if (totalQuantity > capacity)
                {
                    findings.Add(new ValidationFinding(
                        MagazineOverCapacity,
                        ValidationSeverity.Error,
                        $"Loadout '{loadoutId}' loads {totalQuantity} total round(s) into mount '{mountId}' (capacity {capacity}) on '{platformId}'.",
                        UnitId: platformId,
                        TargetId: mountId));
                }
            }
        }

        ValidatePhaseB(workbook, findings);

        return findings
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidatePhaseB(PlatformWorkbook workbook, List<ValidationFinding> findings)
    {
        ValidateHeader(workbook, "Platforms", ExpectedPlatformsHeader, PlatformsHeaderMismatch, findings);
        ValidateHeader(workbook, "Mobility", ExpectedMobilityHeader, MobilityHeaderMismatch, findings);
        ValidateHeader(workbook, "Signatures", ExpectedSignaturesHeader, SignaturesHeaderMismatch, findings);
        ValidateHeader(workbook, "Emcon", ExpectedEmconHeader, EmconHeaderMismatch, findings);

        var platformIds = CollectPlatformIds(workbook);

        ValidateDamageRows(workbook, findings);
        ValidateMobilityRows(workbook, platformIds, findings);
        ValidateSignatureRows(workbook, platformIds, findings);
        ValidateEmconRows(workbook, platformIds, findings);
    }

    private static HashSet<string> CollectPlatformIds(PlatformWorkbook workbook)
    {
        var platformIds = new HashSet<string>(StringComparer.Ordinal);
        var platforms = SheetView.For(workbook, "Platforms");
        if (platforms is null)
        {
            return platformIds;
        }

        foreach (var row in platforms.Rows)
        {
            var platformId = platforms.Cell(row, "PlatformId");
            if (!string.IsNullOrEmpty(platformId))
            {
                platformIds.Add(platformId);
            }
        }

        return platformIds;
    }

    private static void ValidateHeader(
        PlatformWorkbook workbook,
        string sheetName,
        IReadOnlyList<string> expectedHeader,
        string code,
        List<ValidationFinding> findings)
    {
        var sheet = workbook.FindSheet(sheetName);
        if (sheet is null)
        {
            findings.Add(new ValidationFinding(
                code,
                ValidationSeverity.Error,
                $"Sheet '{sheetName}' is missing; expected Req-21 Phase B header parity."));
            return;
        }

        if (!HeadersEqual(sheet.Header, expectedHeader))
        {
            findings.Add(new ValidationFinding(
                code,
                ValidationSeverity.Error,
                $"Sheet '{sheetName}' header does not match Req-21 Phase B export contract."));
        }
    }

    private static void ValidateDamageRows(PlatformWorkbook workbook, List<ValidationFinding> findings)
    {
        var platforms = SheetView.For(workbook, "Platforms");
        if (platforms is null)
        {
            return;
        }

        foreach (var row in platforms.Rows)
        {
            var platformId = platforms.Cell(row, "PlatformId");
            if (string.IsNullOrEmpty(platformId))
            {
                continue;
            }

            var maxHp = ParseDouble(platforms.Cell(row, "MaxHp"));
            if (maxHp <= 0)
            {
                findings.Add(new ValidationFinding(
                    DamageNonPositiveMaxHp,
                    ValidationSeverity.Error,
                    $"Platform '{platformId}' has non-positive MaxHp ({maxHp}).",
                    UnitId: platformId));
            }
            else if (maxHp > MaxHpCeiling)
            {
                findings.Add(new ValidationFinding(
                    DamageMaxHpExceedsCeiling,
                    ValidationSeverity.Error,
                    $"Platform '{platformId}' MaxHp ({maxHp}) exceeds ceiling ({MaxHpCeiling}).",
                    UnitId: platformId));
            }

            var withdraw = ParseDouble(platforms.Cell(row, "WithdrawThresholdPct"));
            if (withdraw < 0 || withdraw > maxHp)
            {
                findings.Add(new ValidationFinding(
                    DamageWithdrawThresholdInvalid,
                    ValidationSeverity.Error,
                    $"Platform '{platformId}' has invalid WithdrawThresholdPct ({withdraw}); expected 0..MaxHp ({maxHp}).",
                    UnitId: platformId));
            }

            var flags = ParseInt(platforms.Cell(row, "CriticalFlags"));
            if (flags < 0)
            {
                findings.Add(new ValidationFinding(
                    DamageCriticalFlagsInvalid,
                    ValidationSeverity.Error,
                    $"Platform '{platformId}' has invalid CriticalFlags ({flags}); expected non-negative bitmask.",
                    UnitId: platformId));
            }
        }
    }

    private static void ValidateMobilityRows(
        PlatformWorkbook workbook,
        HashSet<string> platformIds,
        List<ValidationFinding> findings)
    {
        var mobility = SheetView.For(workbook, "Mobility");
        if (mobility is null)
        {
            return;
        }

        foreach (var row in mobility.Rows)
        {
            var platformId = mobility.Cell(row, "PlatformId");
            if (string.IsNullOrEmpty(platformId))
            {
                continue;
            }

            if (!platformIds.Contains(platformId))
            {
                findings.Add(new ValidationFinding(
                    PhaseBOrphanPlatform,
                    ValidationSeverity.Error,
                    $"Mobility row references unknown platform '{platformId}'.",
                    UnitId: platformId));
            }

            var maxSpeed = ParseDouble(mobility.Cell(row, "MaxSpeedKnots"));
            if (maxSpeed < 0)
            {
                findings.Add(new ValidationFinding(
                    MobilityNegativeSpeed,
                    ValidationSeverity.Error,
                    $"Mobility on '{platformId}' has negative MaxSpeedKnots ({maxSpeed}).",
                    UnitId: platformId));
            }

            var range = ParseDouble(mobility.Cell(row, "RangeNm"));
            if (range < 0)
            {
                findings.Add(new ValidationFinding(
                    MobilityNegativeRange,
                    ValidationSeverity.Error,
                    $"Mobility on '{platformId}' has negative RangeNm ({range}).",
                    UnitId: platformId));
            }
        }
    }

    private static void ValidateSignatureRows(
        PlatformWorkbook workbook,
        HashSet<string> platformIds,
        List<ValidationFinding> findings)
    {
        var signatures = SheetView.For(workbook, "Signatures");
        if (signatures is null)
        {
            return;
        }

        foreach (var row in signatures.Rows)
        {
            var platformId = signatures.Cell(row, "PlatformId");
            if (string.IsNullOrEmpty(platformId))
            {
                continue;
            }

            if (!platformIds.Contains(platformId))
            {
                findings.Add(new ValidationFinding(
                    PhaseBOrphanPlatform,
                    ValidationSeverity.Error,
                    $"Signatures row references unknown platform '{platformId}'.",
                    UnitId: platformId));
            }
        }
    }

    private static void ValidateEmconRows(
        PlatformWorkbook workbook,
        HashSet<string> platformIds,
        List<ValidationFinding> findings)
    {
        var emcon = SheetView.For(workbook, "Emcon");
        if (emcon is null)
        {
            return;
        }

        foreach (var row in emcon.Rows)
        {
            var platformId = emcon.Cell(row, "PlatformId");
            if (string.IsNullOrEmpty(platformId))
            {
                continue;
            }

            if (!platformIds.Contains(platformId))
            {
                findings.Add(new ValidationFinding(
                    PhaseBOrphanPlatform,
                    ValidationSeverity.Error,
                    $"Emcon row references unknown platform '{platformId}'.",
                    UnitId: platformId));
            }

            var condition = emcon.Cell(row, "Condition");
            if (!string.IsNullOrEmpty(condition) && !AllowedEmconConditions.Contains(condition))
            {
                findings.Add(new ValidationFinding(
                    EmconInvalidCondition,
                    ValidationSeverity.Error,
                    $"Emcon on '{platformId}' has invalid Condition '{condition}' (expected silent, restricted, or free).",
                    UnitId: platformId,
                    TargetId: emcon.Cell(row, "EmitterId")));
            }

            var posture = emcon.Cell(row, "Posture");
            if (!string.IsNullOrEmpty(posture) && !AllowedEmconPostures.Contains(posture))
            {
                findings.Add(new ValidationFinding(
                    EmconInvalidPosture,
                    ValidationSeverity.Error,
                    $"Emcon on '{platformId}' has invalid Posture '{posture}' (expected off, standby, or active).",
                    UnitId: platformId,
                    TargetId: emcon.Cell(row, "EmitterId")));
            }
        }
    }

    private static bool HeadersEqual(IReadOnlyList<string> actual, IReadOnlyList<string> expected)
    {
        if (actual.Count != expected.Count)
        {
            return false;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (!string.Equals(actual[i], expected[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string Key(string a, string b) => a + KeySeparator + b;

    private static int ParseInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static double ParseDouble(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    /// <summary>Header-indexed read access over a single sheet.</summary>
    private sealed class SheetView
    {
        private readonly Dictionary<string, int> _columns;

        private SheetView(PlatformWorkbookSheet sheet)
        {
            Rows = sheet.Rows;
            _columns = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < sheet.Header.Count; i++)
            {
                _columns[sheet.Header[i]] = i;
            }
        }

        public IReadOnlyList<IReadOnlyList<string>> Rows { get; }

        public static SheetView? For(PlatformWorkbook workbook, string name)
        {
            var sheet = workbook.FindSheet(name);
            return sheet is null ? null : new SheetView(sheet);
        }

        public string Cell(IReadOnlyList<string> row, string column) =>
            _columns.TryGetValue(column, out var i) && i < row.Count ? row[i] : string.Empty;
    }
}