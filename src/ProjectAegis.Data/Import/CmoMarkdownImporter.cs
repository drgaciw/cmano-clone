namespace ProjectAegis.Data.Import;

using System.Globalization;
using System.Text.RegularExpressions;
using ProjectAegis.Data.Catalog;

/// <summary>
/// Parses CMO markdown (cmano-db.com export) into catalog bindings (DATA-5 P0 + S22-04 platform/weapon/mount).
/// Does not write SQLite — callers stage rows through <see cref="WriteGate.IWriteGate"/>.
/// </summary>
public static class CmoMarkdownImporter
{
    private static readonly Regex SectionHeading = new(@"^###\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex SensorPath = new(@"/sensor/(\d+)/", RegexOptions.Compiled);
    private static readonly Regex PlatformPath = new(@"/(?:ship|aircraft|submarine|facility)/(\d+)/", RegexOptions.Compiled);
    private static readonly Regex WeaponPath = new(@"/weapon/(\d+)/", RegexOptions.Compiled);
    private static readonly Regex RangeMaxRow = new(
        @"\|\s*Range\s+Max\s*\|\s*([\d.]+)\s*(km|m|nm)\s*\|",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ConfidenceRow = new(
        @"\|\s*Confidence\s*\|\s*([\d.]+)\s*\|",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TypeRow = new(
        @"\|\s*Type\s*\|\s*(.+?)\s*\|",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NationalityRow = new(
        @"\|\s*Nationality\s*\|\s*(.+?)\s*\|",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PlatformWeaponLine = new(
        @"^-\s+(.+?)\s+—\s+(.+?)\s+—",
        RegexOptions.Compiled);
    private static readonly Regex RangeKm = new(
        @"(?:Air|Surface|Land|Sub)\s+Max:\s*([\d.]+)\s*km",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, string> BalticPlatformIds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["patrol-frigate-u1"] = "u1",
            ["hostile-corvette-h1"] = "hostile-1",
            ["distant-hostile-frigate"] = "hostile-far",
        };

    public static string ResolveMiniFixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "sensor-mini.md"));

    public static string ResolveBalticPlatformFixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "baltic-platform-mini.md"));

    public static string ResolveMiniWeaponFixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "weapon-mini.md"));

    public static string ResolveWeaponSlice50FixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "weapon-slice-50.md"));

    public static string ResolveReferenceSensorMarkdownPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("docs", "reference", "cmano-db", "sensor.md"));

    public static IReadOnlyList<CatalogSensorBinding> ReadSensorBindings(
        string markdownPath,
        int? maxRecords = null)
    {
        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        var text = File.ReadAllText(markdownPath);
        var batchId = Path.GetFileNameWithoutExtension(markdownPath);
        return ReadSensorBindingsFromText(text, Path.GetFileName(markdownPath), batchId, maxRecords);
    }

    public static IReadOnlyList<CatalogSensorBinding> ReadSensorBindingsFromText(
        string markdown,
        string sourceFile,
        string importBatchId,
        int? maxRecords = null)
    {
        var bindings = new List<CatalogSensorBinding>();
        string? title = null;
        int? sensorNumericId = null;
        double? rangeMax = null;
        string? rangeUnit = null;
        double confidence = 0.85;

        void FlushSection()
        {
            if (title == null || sensorNumericId == null || rangeMax == null || rangeUnit == null)
            {
                return;
            }

            var platformId = SlugPlatformId(title);
            var sensorId = $"cmo-sensor-{sensorNumericId.Value}";
            var basePd = InferBasePd(rangeMax.Value, rangeUnit);
            bindings.Add(new CatalogSensorBinding(
                platformId,
                sensorId,
                basePd,
                $"cmano-db:sensor/{sensorNumericId.Value}",
                Confidence: confidence,
                ImportBatchId: importBatchId,
                SourceFile: sourceFile,
                ReviewState: CatalogReviewStates.Approved,
                TrlLevel: 9,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                ReviewerId: "cmo-markdown-import",
                CitationRef: $"/sensor/{sensorNumericId.Value}/"));
            if (!string.IsNullOrEmpty(title))
            {
                title = title.Trim();
            }

            title = null;
            sensorNumericId = null;
            rangeMax = null;
            rangeUnit = null;
            confidence = 0.85;
        }

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var heading = SectionHeading.Match(line);
            if (heading.Success)
            {
                FlushSection();
                if (maxRecords.HasValue && bindings.Count >= maxRecords.Value)
                {
                    break;
                }

                title = heading.Groups[1].Value.Trim();
                confidence = 0.85;
                continue;
            }

            if (title == null)
            {
                continue;
            }

            if (sensorNumericId == null)
            {
                var pathMatch = SensorPath.Match(line);
                if (pathMatch.Success)
                {
                    sensorNumericId = int.Parse(pathMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }
            }

            if (rangeMax == null)
            {
                var rangeMatch = RangeMaxRow.Match(line);
                if (rangeMatch.Success)
                {
                    rangeMax = double.Parse(rangeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    rangeUnit = rangeMatch.Groups[2].Value.ToLowerInvariant();
                }
            }

            var confidenceMatch = ConfidenceRow.Match(line);
            if (confidenceMatch.Success)
            {
                confidence = double.Parse(confidenceMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }
        }

        FlushSection();

        return bindings
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<CatalogPlatformBinding> ReadPlatformBindings(
        string markdownPath,
        int? maxRecords = null,
        bool mapBalticIds = false)
    {
        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        var text = File.ReadAllText(markdownPath);
        var batchId = Path.GetFileNameWithoutExtension(markdownPath);
        return ReadPlatformBindingsFromText(text, Path.GetFileName(markdownPath), batchId, maxRecords, mapBalticIds);
    }

    public static IReadOnlyList<CatalogPlatformBinding> ReadPlatformBindingsFromText(
        string markdown,
        string sourceFile,
        string importBatchId,
        int? maxRecords = null,
        bool mapBalticIds = false)
    {
        var bindings = new List<CatalogPlatformBinding>();
        string? title = null;
        int? platformNumericId = null;
        string platformClass = "";
        string nationality = "";
        string domain = "surface";

        void FlushSection()
        {
            if (title == null || platformNumericId == null)
            {
                return;
            }

            var slug = SlugPlatformId(title);
            var platformId = mapBalticIds && BalticPlatformIds.TryGetValue(slug, out var mapped)
                ? mapped
                : slug;
            bindings.Add(new CatalogPlatformBinding(
                platformId,
                DisplayName: title.Trim(),
                Domain: domain,
                PlatformClass: platformClass,
                Nationality: nationality,
                ReviewState: CatalogReviewStates.Provisional,
                TrlLevel: 9,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                CitationRef: $"/ship/{platformNumericId.Value}/",
                SourceFactId: $"cmano-db:ship/{platformNumericId.Value}",
                ImportBatchId: importBatchId,
                SourceFile: sourceFile));

            title = null;
            platformNumericId = null;
            platformClass = "";
            nationality = "";
            domain = "surface";
        }

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var heading = SectionHeading.Match(line);
            if (heading.Success)
            {
                FlushSection();
                if (maxRecords.HasValue && bindings.Count >= maxRecords.Value)
                {
                    break;
                }

                title = heading.Groups[1].Value.Trim();
                continue;
            }

            if (title == null)
            {
                continue;
            }

            if (platformNumericId == null)
            {
                var pathMatch = PlatformPath.Match(line);
                if (pathMatch.Success)
                {
                    platformNumericId = int.Parse(pathMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }
            }

            var typeMatch = TypeRow.Match(line);
            if (typeMatch.Success)
            {
                platformClass = typeMatch.Groups[1].Value.Trim();
                domain = InferDomain(platformClass);
            }

            var nationalityMatch = NationalityRow.Match(line);
            if (nationalityMatch.Success)
            {
                nationality = nationalityMatch.Groups[1].Value.Trim();
            }
        }

        FlushSection();

        return bindings
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<CatalogWeaponRecord> ReadWeaponBindings(
        string markdownPath,
        int? maxRecords = null)
    {
        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        var text = File.ReadAllText(markdownPath);
        var batchId = Path.GetFileNameWithoutExtension(markdownPath);
        return ReadWeaponBindingsFromText(text, Path.GetFileName(markdownPath), batchId, maxRecords);
    }

    public static IReadOnlyList<CatalogWeaponRecord> ReadWeaponBindingsFromText(
        string markdown,
        string sourceFile,
        string importBatchId,
        int? maxRecords = null)
    {
        var bindings = new List<CatalogWeaponRecord>();
        string? title = null;
        int? weaponNumericId = null;
        string weaponType = "";
        double maxRangeMeters = 0;
        bool inWeaponsSection;

        void FlushSection()
        {
            if (title == null || weaponNumericId == null)
            {
                return;
            }

            bindings.Add(new CatalogWeaponRecord(
                $"cmo-weapon-{weaponNumericId.Value}",
                DisplayName: title.Trim(),
                MinRangeMeters: 0,
                MaxRangeMeters: maxRangeMeters,
                WeaponType: weaponType,
                ReviewState: CatalogReviewStates.Provisional,
                SourceFactId: $"cmano-db:weapon/{weaponNumericId.Value}",
                ImportBatchId: importBatchId,
                SourceFile: sourceFile));

            title = null;
            weaponNumericId = null;
            weaponType = "";
            maxRangeMeters = 0;
        }

        inWeaponsSection = false;
        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var heading = SectionHeading.Match(line);
            if (heading.Success)
            {
                FlushSection();
                if (maxRecords.HasValue && bindings.Count >= maxRecords.Value)
                {
                    break;
                }

                title = heading.Groups[1].Value.Trim();
                inWeaponsSection = false;
                continue;
            }

            if (title == null)
            {
                continue;
            }

            if (line.StartsWith("**Weapons**", StringComparison.Ordinal))
            {
                inWeaponsSection = true;
                continue;
            }

            if (weaponNumericId == null)
            {
                var pathMatch = WeaponPath.Match(line);
                if (pathMatch.Success)
                {
                    weaponNumericId = int.Parse(pathMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }
            }

            var typeMatch = TypeRow.Match(line);
            if (typeMatch.Success)
            {
                weaponType = typeMatch.Groups[1].Value.Trim();
            }

            if (inWeaponsSection)
            {
                var rangeMatch = RangeKm.Match(line);
                if (rangeMatch.Success)
                {
                    var km = double.Parse(rangeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    maxRangeMeters = Math.Max(maxRangeMeters, km * 1000.0);
                }
            }
        }

        FlushSection();

        return bindings
            .OrderBy(w => w.WeaponId, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<CatalogMount> ReadPlatformMounts(
        string markdownPath,
        int? maxRecords = null,
        bool mapBalticIds = false)
    {
        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        var text = File.ReadAllText(markdownPath);
        return ReadPlatformMountsFromText(text, maxRecords, mapBalticIds);
    }

    public static IReadOnlyList<CatalogMount> ReadPlatformMountsFromText(
        string markdown,
        int? maxRecords = null,
        bool mapBalticIds = false)
    {
        var mounts = new List<CatalogMount>();
        string? title = null;
        string? platformId = null;
        bool inWeaponsSection;

        void FlushPlatform()
        {
            title = null;
            platformId = null;
            inWeaponsSection = false;
        }

        inWeaponsSection = false;
        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var heading = SectionHeading.Match(line);
            if (heading.Success)
            {
                FlushPlatform();
                title = heading.Groups[1].Value.Trim();
                var slug = SlugPlatformId(title);
                platformId = mapBalticIds && BalticPlatformIds.TryGetValue(slug, out var mapped)
                    ? mapped
                    : slug;
                continue;
            }

            if (platformId == null)
            {
                continue;
            }

            if (line.StartsWith("**Weapons**", StringComparison.Ordinal))
            {
                inWeaponsSection = true;
                continue;
            }

            if (!inWeaponsSection)
            {
                continue;
            }

            var weaponMatch = PlatformWeaponLine.Match(line);
            if (!weaponMatch.Success)
            {
                continue;
            }

            var weaponName = weaponMatch.Groups[1].Value.Trim();
            var weaponType = weaponMatch.Groups[2].Value.Trim();
            var mountId = SlugWeaponMountId(weaponName);
            mounts.Add(new CatalogMount(
                platformId,
                mountId,
                MountType: InferMountType(weaponType),
                ReviewState: CatalogReviewStates.Provisional));

            if (maxRecords.HasValue && mounts.Count >= maxRecords.Value)
            {
                break;
            }
        }

        return mounts
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ThenBy(m => m.MountId, StringComparer.Ordinal)
            .ToArray();
    }

    public static double InferBasePd(double rangeValue, string unit)
    {
        return unit switch
        {
            "nm" => Math.Clamp(rangeValue / 200.0, 0.05, 1.0),
            "km" => Math.Clamp(rangeValue / 300.0, 0.05, 1.0),
            "m" => Math.Clamp(rangeValue / 370_400.0, 0.05, 1.0),
            _ => 0.5,
        };
    }

    public static string SlugPlatformId(string title)
    {
        var head = title.Split(',')[0].Trim();
        var slug = Regex.Replace(head.ToLowerInvariant(), @"[^\w]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "unknown-platform" : slug[..Math.Min(slug.Length, 64)];
    }

    public static string SlugWeaponMountId(string weaponName)
    {
        var slug = Regex.Replace(weaponName.ToLowerInvariant(), @"[^\w]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "unknown-mount" : slug[..Math.Min(slug.Length, 64)];
    }

    public static string InferDomain(string platformClass)
    {
        if (platformClass.Contains("aircraft", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("helicopter", StringComparison.OrdinalIgnoreCase))
        {
            return "air";
        }

        if (platformClass.Contains("submarine", StringComparison.OrdinalIgnoreCase))
        {
            return "subsurface";
        }

        if (platformClass.Contains("facility", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("land", StringComparison.OrdinalIgnoreCase))
        {
            return "land";
        }

        return "surface";
    }

    public static string InferMountType(string weaponType)
    {
        if (weaponType.Contains("gun", StringComparison.OrdinalIgnoreCase))
        {
            return "gun";
        }

        if (weaponType.Contains("torpedo", StringComparison.OrdinalIgnoreCase) ||
            weaponType.Contains("tube", StringComparison.OrdinalIgnoreCase))
        {
            return "tube";
        }

        if (weaponType.Contains("vls", StringComparison.OrdinalIgnoreCase))
        {
            return "vls";
        }

        return "rail";
    }
}