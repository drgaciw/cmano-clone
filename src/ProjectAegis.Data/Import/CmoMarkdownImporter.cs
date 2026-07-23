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
    private static readonly Regex CountrySectionHeading = new(@"^##\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex SensorPath = new(@"/sensor/(\d+)/", RegexOptions.Compiled);
    private static readonly Regex PlatformPath = new(@"/(ship|aircraft|submarine|facility)/(\d+)/", RegexOptions.Compiled);
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
            ["ucav-blue"] = "ucav-blue",
            ["ucav-red"] = "ucav-red",
        };

    public static string ResolveMiniFixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "sensor-mini.md"));

    public static string ResolveBalticPlatformFixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "baltic-platform-mini.md"));

    public static string ResolveBalticV3UcavFixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "baltic-v3-ucav-mini.md"));

    public static string ResolveMiniWeaponFixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "weapon-mini.md"));

    public static string ResolveWeaponSlice50FixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "weapon-slice-50.md"));

    public static string ResolveShipSlice100FixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "ship-slice-100.md"));

    public static string ResolveAircraftSlice100FixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "aircraft-slice-100.md"));

    public static string ResolveSubmarineSlice100FixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "submarine-slice-100.md"));

    public static string ResolveFacilitySlice100FixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "facility-slice-100.md"));

    public static string ResolveReferenceSensorMarkdownPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("docs", "reference", "cmano-db", "sensor.md"));

    /// <summary>Full platform corpus for off-CI nightly propose→approve (S30-04; 4844 records).</summary>
    public static string ResolveReferenceShipMarkdownPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("docs", "reference", "cmano-db", "ship.md"));

    /// <summary>Full aircraft corpus for off-CI nightly propose→approve (S30-11; 7387 records).</summary>
    public static string ResolveReferenceAircraftMarkdownPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("docs", "reference", "cmano-db", "aircraft.md"));

    /// <summary>Full submarine corpus for off-CI nightly propose→approve (S30-11; 732 records).</summary>
    public static string ResolveReferenceSubmarineMarkdownPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("docs", "reference", "cmano-db", "submarine.md"));

    /// <summary>Full facility corpus for off-CI nightly propose→approve (S30-11; 4511 records).</summary>
    public static string ResolveReferenceFacilityMarkdownPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("docs", "reference", "cmano-db", "facility.md"));

    /// <summary>Full ground-unit corpus (derived from facility.md mobile-unit subset; 3289 records).</summary>
    public static string ResolveReferenceGroundUnitMarkdownPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("docs", "reference", "cmano-db", "ground-units.md"));

    /// <summary>Full weapon corpus for off-CI nightly propose→approve (S31-10; 4403 records).</summary>
    public static string ResolveReferenceWeaponMarkdownPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("docs", "reference", "cmano-db", "weapon.md"));

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

            var platformId = IsPublicCorpusImport()
                ? CatalogValidationDefaults.PublicCorpusSensorCatalogPlatformId
                : SlugPlatformId(title);
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
        bool mapBalticIds = false,
        string defaultDomain = "surface")
    {
        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        var text = File.ReadAllText(markdownPath);
        var batchId = Path.GetFileNameWithoutExtension(markdownPath);
        return ReadPlatformBindingsFromText(text, Path.GetFileName(markdownPath), batchId, maxRecords, mapBalticIds, defaultDomain);
    }

    public static IReadOnlyList<CatalogPlatformBinding> ReadPlatformBindingsFromText(
        string markdown,
        string sourceFile,
        string importBatchId,
        int? maxRecords = null,
        bool mapBalticIds = false,
        string defaultDomain = "surface")
    {
        var bindings = new List<CatalogPlatformBinding>();
        var bindingNumericIds = new List<int>();
        var bindingIsBalticMapped = new List<bool>();
        string? title = null;
        int? platformNumericId = null;
        string platformClass = "";
        string nationality = "";
        string domain = defaultDomain;
        string currentCountrySection = "";
        string? platformUrlSegment = null;

        void FlushSection()
        {
            if (title == null || platformNumericId == null)
            {
                return;
            }

            var slug = SlugPlatformId(title);
            string platformId;
            bool isBalticMapped;
            if (mapBalticIds && BalticPlatformIds.TryGetValue(slug, out var mapped))
            {
                platformId = mapped;
                isBalticMapped = true;
            }
            else
            {
                platformId = slug;
                isBalticMapped = false;
            }

            // S31-11 completeness fix: CitationRef/SourceFactId were hardcoded to "ship" for
            // every entity (aircraft/submarine/facility records pointed at a fabricated
            // /ship/{id}/ URL). Use the actual matched cmano-db path segment.
            var urlSegment = platformUrlSegment ?? "ship";
            bindings.Add(new CatalogPlatformBinding(
                platformId,
                DisplayName: title.Trim(),
                Domain: domain,
                PlatformClass: platformClass,
                Nationality: string.IsNullOrEmpty(nationality) ? currentCountrySection : nationality,
                ReviewState: CatalogReviewStates.Provisional,
                TrlLevel: 9,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                CitationRef: $"/{urlSegment}/{platformNumericId.Value}/",
                SourceFactId: $"cmano-db:{urlSegment}/{platformNumericId.Value}",
                ImportBatchId: importBatchId,
                SourceFile: sourceFile));
            bindingNumericIds.Add(platformNumericId.Value);
            bindingIsBalticMapped.Add(isBalticMapped);

            title = null;
            platformNumericId = null;
            platformClass = "";
            nationality = "";
            domain = defaultDomain;
            platformUrlSegment = null;
        }

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            // S31-11 completeness fix: cmano-db markdown groups records under H2 "## <Country>"
            // headers; per-record tables never carry an explicit Nationality row. Track the
            // enclosing country section so it can be used as the Nationality fallback.
            var countryHeading = CountrySectionHeading.Match(line);
            if (countryHeading.Success)
            {
                FlushSection();
                currentCountrySection = countryHeading.Groups[1].Value.Trim();
                continue;
            }

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
                    platformUrlSegment = pathMatch.Groups[1].Value;
                    platformNumericId = int.Parse(pathMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                }
            }

            var typeMatch = TypeRow.Match(line);
            if (typeMatch.Success)
            {
                platformClass = typeMatch.Groups[1].Value.Trim();
                domain = InferDomain(platformClass, defaultDomain);
            }

            var nationalityMatch = NationalityRow.Match(line);
            if (nationalityMatch.Success)
            {
                nationality = nationalityMatch.Groups[1].Value.Trim();
            }
        }

        FlushSection();

        // S31-11 fix: SlugPlatformId derives platform_id from display name (+ optional year),
        // not the unique cmano-db numeric id — different national operators of the same
        // airframe/hull can share a slug. CatalogWriteGate stages rows keyed on
        // (batch_id, platform_id) via INSERT OR REPLACE, so undetected slug collisions
        // silently drop records. Disambiguate only the colliding subset by appending the
        // numeric id, leaving all non-colliding (and Baltic-mapped) ids unchanged so
        // existing mount/loadout/magazine references (which independently recompute the
        // plain slug) keep matching for every platform that wasn't actually ambiguous.
        var collisionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var binding in bindings)
        {
            collisionCounts[binding.PlatformId] = collisionCounts.GetValueOrDefault(binding.PlatformId) + 1;
        }

        for (var i = 0; i < bindings.Count; i++)
        {
            if (bindingIsBalticMapped[i] || collisionCounts[bindings[i].PlatformId] <= 1)
            {
                continue;
            }

            bindings[i] = bindings[i] with { PlatformId = $"{bindings[i].PlatformId}-{bindingNumericIds[i]}" };
        }

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
                // S31-13 fix: a single "**Weapons**" bullet line can carry multiple
                // domain-qualified range clauses (e.g. "Surface Max: 22.2 km. Land Max: 157.4 km.").
                // Regex.Match only returns the first (leftmost) clause, silently discarding a
                // higher range reported later on the same line. Scan all clauses and keep the max.
                foreach (Match rangeMatch in RangeKm.Matches(line))
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

    internal static bool IsPublicCorpusImport() =>
        string.Equals(
            Environment.GetEnvironmentVariable("AEGIS_PUBLIC_CORPUS"),
            "1",
            StringComparison.Ordinal);

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

    public static string InferDomain(string platformClass, string defaultDomain = "surface")
    {
        // Hull-type override: aircraft/helicopter carriers and cruisers are surface combatants
        // that operate aircraft, not airborne platforms themselves — must win over the
        // "helicopter" keyword hit below (S31-11: was misclassifying ~250 ship.md carriers as air).
        //
        // Surface auxiliaries that mention "submarine" (rescue/tender/chaser) must also win
        // before the subsurface "submarine" substring match — Wave 2 corpus audit found
        // ASR/AS/PC ship.md rows mis-tagged subsurface (~44) for this reason. Aligns with
        // tools/cmo_verify_corpus_coverage.py _SUBSURFACE_FALSE_POSITIVE_RE.
        if (Regex.IsMatch(platformClass, @"\b(carrier|cruiser|destroyer)\b", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(platformClass, @"\b(rescue ship|tender|chaser)\b", RegexOptions.IgnoreCase))
        {
            return "surface";
        }

        // Air before subsurface: "Anti-Submarine Warfare" must not match "submarine".
        if (platformClass.Contains("anti-submarine", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("aircraft", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("helicopter", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("fixed wing", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("fighter", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("bomber", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("multirole", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("attack)", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("asw", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("nfh", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("tth", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("helix", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("maritime patrol", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("uav", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("ucav", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("unmanned aerial", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("trainer", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("tanker (air", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("airborne", StringComparison.OrdinalIgnoreCase) ||
            (platformClass.Contains("aerostat", StringComparison.OrdinalIgnoreCase) &&
                !platformClass.Contains("mooring", StringComparison.OrdinalIgnoreCase)) ||
            platformClass.Contains("target drone", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("wild weasel", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("elint", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("area surveillance", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("search and rescue", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("electronic warfare", StringComparison.OrdinalIgnoreCase))
        {
            return "air";
        }

        // Subsurface: SSK/SSN/SSBN and true submarine classes, plus unmanned underwater vehicles.
        if (platformClass.Contains("submarine", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains(" ssk", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("ssn", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("ssbn", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("hunter-killer", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("plarb", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("plark", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("rov", StringComparison.OrdinalIgnoreCase) ||
            platformClass.Contains("uuv", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(platformClass, @"\bsdv\b", RegexOptions.IgnoreCase) ||
            platformClass.Contains("underwater", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(platformClass, @"\bPLA-\d", RegexOptions.IgnoreCase))
        {
            return "subsurface";
        }

        // Explicit water-surface facility types stay surface even when the corpus default is land.
        if (platformClass.Contains("water (surface)", StringComparison.OrdinalIgnoreCase))
        {
            return "surface";
        }

        // Word-boundary match: "land" must not fire on "Landing" (amphibious ships/craft are
        // surface vessels, not land vehicles — S31-11: was misclassifying ~350 ship.md entries).
        if (platformClass.Contains("facility", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(platformClass, @"\bland\b", RegexOptions.IgnoreCase))
        {
            return "land";
        }

        return defaultDomain;
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

    public const string DefaultLoadoutId = "default";

    public static IReadOnlyDictionary<string, string> BuildWeaponNameLookup(
        IReadOnlyList<CatalogWeaponRecord> weapons)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var weapon in weapons)
        {
            var head = weapon.DisplayName.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(head))
            {
                lookup.TryAdd(head, weapon.WeaponId);
            }

            lookup.TryAdd(weapon.WeaponId, weapon.WeaponId);
        }

        return lookup;
    }

    public static string? ResolveWeaponId(
        string weaponLineName,
        IReadOnlyDictionary<string, string> weaponLookup)
    {
        var name = weaponLineName.Trim();
        if (weaponLookup.TryGetValue(name, out var direct))
        {
            return direct;
        }

        string? best = null;
        var bestLen = 0;
        foreach (var entry in weaponLookup)
        {
            if (name.StartsWith(entry.Key, StringComparison.OrdinalIgnoreCase) ||
                entry.Key.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                if (entry.Key.Length > bestLen)
                {
                    best = entry.Value;
                    bestLen = entry.Key.Length;
                }
            }
        }

        return best;
    }

    public static IReadOnlyList<CatalogLoadout> ReadPlatformLoadouts(
        string markdownPath,
        int? maxRecords = null,
        bool mapBalticIds = false)
    {
        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        return ReadPlatformLoadoutsFromText(File.ReadAllText(markdownPath), maxRecords, mapBalticIds);
    }

    public static IReadOnlyList<CatalogLoadout> ReadPlatformLoadoutsFromText(
        string markdown,
        int? maxRecords = null,
        bool mapBalticIds = false)
    {
        var loadouts = new List<CatalogLoadout>();
        string? title = null;
        string? platformId = null;

        void FlushPlatform()
        {
            title = null;
            platformId = null;
        }

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
                loadouts.Add(new CatalogLoadout(
                    platformId,
                    DefaultLoadoutId,
                    LoadoutName: "Default Loadout",
                    Role: "general",
                    IsDefault: true));

                if (maxRecords.HasValue && loadouts.Count >= maxRecords.Value)
                {
                    break;
                }

                continue;
            }
        }

        return loadouts
            .OrderBy(l => l.PlatformId, StringComparer.Ordinal)
            .ThenBy(l => l.LoadoutId, StringComparer.Ordinal)
            .ToArray();
    }

    public static (IReadOnlyList<CatalogMagazineEntry> Approved, IReadOnlyList<CmoMarkdownFittingQuarantineEntry> Quarantined)
        PartitionPlatformMagazines(
            string markdownPath,
            bool mapBalticIds,
            IReadOnlyDictionary<string, string> weaponLookup,
            string sourceFile,
            int? maxRecords = null)
    {
        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        return PartitionPlatformMagazinesFromText(
            File.ReadAllText(markdownPath),
            mapBalticIds,
            weaponLookup,
            sourceFile,
            maxRecords);
    }

    public static (IReadOnlyList<CatalogMagazineEntry> Approved, IReadOnlyList<CmoMarkdownFittingQuarantineEntry> Quarantined)
        PartitionPlatformMagazinesFromText(
            string markdown,
            bool mapBalticIds,
            IReadOnlyDictionary<string, string> weaponLookup,
            string sourceFile,
            int? maxRecords = null)
    {
        var approved = new List<CatalogMagazineEntry>();
        var quarantined = new List<CmoMarkdownFittingQuarantineEntry>();
        string? title = null;
        string? platformId = null;
        bool inWeaponsSection;
        var mountIds = new HashSet<string>(StringComparer.Ordinal);

        void FlushPlatform()
        {
            title = null;
            platformId = null;
            inWeaponsSection = false;
            mountIds.Clear();
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
            mountIds.Add(mountId);

            var weaponId = ResolveWeaponId(weaponName, weaponLookup);
            if (weaponId is null)
            {
                quarantined.Add(new CmoMarkdownFittingQuarantineEntry(
                    platformId,
                    DefaultLoadoutId,
                    mountId,
                    weaponName,
                    "orphan_weapon_id",
                    sourceFile));
                continue;
            }

            var quantity = weaponType.Contains("gun", StringComparison.OrdinalIgnoreCase) ? 200 : 16;
            approved.Add(new CatalogMagazineEntry(
                platformId,
                DefaultLoadoutId,
                mountId,
                weaponId,
                Quantity: quantity,
                ReloadTimeSec: 0,
                Depth: 0));

            if (maxRecords.HasValue && approved.Count >= maxRecords.Value)
            {
                break;
            }
        }

        return (
            approved
                .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
                .ThenBy(m => m.LoadoutId, StringComparer.Ordinal)
                .ThenBy(m => m.MountId, StringComparer.Ordinal)
                .ThenBy(m => m.WeaponId, StringComparer.Ordinal)
                .ToArray(),
            quarantined
                .OrderBy(q => q.PlatformId, StringComparer.Ordinal)
                .ThenBy(q => q.MountId, StringComparer.Ordinal)
                .ThenBy(q => q.WeaponRef, StringComparer.Ordinal)
                .ToArray());
    }
}