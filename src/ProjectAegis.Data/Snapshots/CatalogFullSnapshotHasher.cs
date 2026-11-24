namespace ProjectAegis.Data.Snapshots;

using System.Security.Cryptography;
using System.Text;
using Catalog;

/// <summary>
/// H6-01 / DRG-349: deterministic, order-independent SHA-256 fingerprint across all 13 catalog domains
/// (platforms, sensors, weapons, mounts, loadouts, magazines, mobility, damage, comms, links,
/// signatures, emcon, swarms), plus strict non-empty hash validation.
/// Preserves sensor-only <see cref="CatalogSnapshotHasher.ComputeSha256Hex"/> untouched.
/// </summary>
public static class CatalogFullSnapshotHasher
{
    public const string DomainTag = "H6-CATALOG-V1";
    public const string EmptyCatalogContentHashCode = "EMPTY_CATALOG_CONTENT_HASH";

    public static string ComputeSha256Hex(ICatalogReader reader)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        using var stream = new MemoryStream();
        using (var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            WriteString(bw, DomainTag);

            // 1. Platforms
            var platforms = reader.GetSortedPlatforms()
                .OrderBy(p => p.PlatformId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "platforms", platforms.Length);
            foreach (var p in platforms)
            {
                var domain = !string.IsNullOrEmpty(p.Domain)
                    ? p.Domain
                    : (reader.TryGetPlatformDomain(p.PlatformId, out var resolvedDomain) ? resolvedDomain : string.Empty);
                WriteString(bw, p.PlatformId);
                WriteDouble(bw, p.LatDeg);
                WriteDouble(bw, p.LonDeg);
                WriteDouble(bw, p.CombatRadiusNm);
                WriteString(bw, domain);
            }

            // 2. Sensors
            var sensors = reader.GetSortedSensorBindings()
                .OrderBy(s => s.PlatformId, StringComparer.Ordinal)
                .ThenBy(s => s.SensorId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "sensors", sensors.Length);
            foreach (var s in sensors)
            {
                WriteString(bw, s.PlatformId);
                WriteString(bw, s.SensorId);
                WriteDouble(bw, s.BasePd);
                WriteString(bw, s.SourceFactId);
                WriteDouble(bw, s.Confidence);
                WriteString(bw, s.ImportBatchId);
                WriteString(bw, s.SourceFile);
                WriteString(bw, s.ReviewState);
                bw.Write(s.TrlLevel);
                WriteString(bw, s.ValueTier);
                WriteString(bw, s.ReviewerId);
                bw.Write(s.RevisedUtcTicks);
                WriteString(bw, s.CitationRef);
                WriteDouble(bw, s.JamStrength);
                WriteDouble(bw, s.EccmFactor);
                WriteString(bw, s.Modality);
                WriteString(bw, s.RadarScanType);
                bw.Write(s.FrequencyAgile);
                bw.Write(s.RadarTechGeneration);
            }

            // 3. Weapons
            var weapons = reader.GetSortedWeapons()
                .OrderBy(w => w.WeaponId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "weapons", weapons.Length);
            foreach (var w in weapons)
            {
                WriteString(bw, w.WeaponId);
                WriteString(bw, w.DisplayName);
                WriteDouble(bw, w.MinRangeMeters);
                WriteDouble(bw, w.MaxRangeMeters);
                WriteString(bw, w.WeaponType);
                WriteString(bw, w.Guidance);
                WriteString(bw, w.ReviewState);
                WriteString(bw, w.SourceFactId);
                WriteString(bw, w.ImportBatchId);
                WriteString(bw, w.SourceFile);
            }

            // 4. Mounts
            var mounts = reader.GetSortedMounts()
                .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
                .ThenBy(m => m.MountId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "mounts", mounts.Length);
            foreach (var m in mounts)
            {
                WriteString(bw, m.PlatformId);
                WriteString(bw, m.MountId);
                WriteString(bw, m.MountType);
                WriteDouble(bw, m.ArcDeg);
                bw.Write(m.Capacity);
                WriteString(bw, m.ReviewState);
            }

            // 5. Loadouts
            var loadouts = reader.GetSortedLoadouts()
                .OrderBy(l => l.PlatformId, StringComparer.Ordinal)
                .ThenBy(l => l.LoadoutId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "loadouts", loadouts.Length);
            foreach (var l in loadouts)
            {
                WriteString(bw, l.PlatformId);
                WriteString(bw, l.LoadoutId);
                WriteString(bw, l.LoadoutName);
                WriteString(bw, l.Role);
                bw.Write(l.IsDefault);
            }

            // 6. Magazines
            var magazines = reader.GetSortedMagazines()
                .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
                .ThenBy(m => m.LoadoutId, StringComparer.Ordinal)
                .ThenBy(m => m.MountId, StringComparer.Ordinal)
                .ThenBy(m => m.WeaponId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "magazines", magazines.Length);
            foreach (var m in magazines)
            {
                WriteString(bw, m.PlatformId);
                WriteString(bw, m.LoadoutId);
                WriteString(bw, m.MountId);
                WriteString(bw, m.WeaponId);
                bw.Write(m.Quantity);
                bw.Write(m.ReloadTimeSec);
                bw.Write(m.Depth);
            }

            // 7. Mobility
            var mobility = reader.GetSortedMobility()
                .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "mobility", mobility.Length);
            foreach (var m in mobility)
            {
                WriteString(bw, m.PlatformId);
                WriteDouble(bw, m.MaxSpeedKnots);
                WriteDouble(bw, m.CruiseSpeedKnots);
                WriteDouble(bw, m.MaxAltitudeFt);
                WriteDouble(bw, m.MaxDepthM);
                WriteDouble(bw, m.FuelCapacity);
                WriteDouble(bw, m.RangeNm);
                WriteDouble(bw, m.EnduranceHr);
                WriteString(bw, m.ReviewState);
                bw.Write(m.TrlLevel);
                WriteString(bw, m.ValueTier);
                WriteString(bw, m.CitationRef);
            }

            // 8. Damage
            var damage = reader.GetSortedPlatformDamage()
                .OrderBy(d => d.PlatformId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "damage", damage.Length);
            foreach (var d in damage)
            {
                WriteString(bw, d.PlatformId);
                WriteDouble(bw, d.MaxHp);
                WriteDouble(bw, d.WithdrawThresholdPct);
                bw.Write(d.CriticalFlags);
                WriteString(bw, d.ReviewState);
                bw.Write(d.TrlLevel);
                WriteString(bw, d.ValueTier);
                WriteString(bw, d.CitationRef);
                WriteDouble(bw, d.Resilience);
            }

            // 9. Comms
            var comms = reader.GetSortedComms()
                .OrderBy(c => c.PlatformId, StringComparer.Ordinal)
                .ThenBy(c => c.LinkId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "comms", comms.Length);
            foreach (var c in comms)
            {
                WriteString(bw, c.PlatformId);
                WriteString(bw, c.LinkId);
                WriteString(bw, c.Role);
                bw.Write(c.SatcomCapable);
                WriteString(bw, c.ReviewState);
                bw.Write(c.TrlLevel);
                WriteString(bw, c.ValueTier);
                WriteString(bw, c.CitationRef);
            }

            // 10. Links
            var links = reader.GetSortedLinks()
                .OrderBy(l => l.LinkId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "links", links.Length);
            foreach (var l in links)
            {
                WriteString(bw, l.LinkId);
                WriteString(bw, l.DisplayName);
                WriteString(bw, l.LinkType);
                bw.Write(l.LatencyMsNominal);
            }

            // 11. Signatures
            var signatures = reader.GetSortedSignatures()
                .OrderBy(s => s.PlatformId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "signatures", signatures.Length);
            foreach (var s in signatures)
            {
                WriteString(bw, s.PlatformId);
                WriteDouble(bw, s.RcsBandDbsm);
                WriteDouble(bw, s.IrSignature);
                WriteDouble(bw, s.AcousticSignatureDb);
                WriteDouble(bw, s.MagneticSignature);
                WriteString(bw, s.ReviewState);
                bw.Write(s.TrlLevel);
                WriteString(bw, s.ValueTier);
                WriteString(bw, s.CitationRef);
            }

            // 12. EMCON
            var emcon = reader.GetSortedEmcon()
                .OrderBy(e => e.PlatformId, StringComparer.Ordinal)
                .ThenBy(e => e.Condition, StringComparer.Ordinal)
                .ThenBy(e => e.EmitterId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "emcon", emcon.Length);
            foreach (var e in emcon)
            {
                WriteString(bw, e.PlatformId);
                WriteString(bw, e.Condition);
                WriteString(bw, e.EmitterId);
                WriteString(bw, e.Posture);
                WriteString(bw, e.ReviewState);
            }

            // 13. Swarms
            var swarms = reader.GetSortedSwarmPlatforms()
                .OrderBy(s => s.PlatformId, StringComparer.Ordinal)
                .ToArray();
            WriteSectionHeader(bw, "swarms", swarms.Length);
            foreach (var s in swarms)
            {
                WriteString(bw, s.PlatformId);
                bw.Write(s.MaxDrones);
                bw.Write(s.IsSwarm);
                WriteString(bw, s.ArmorClass);
                WriteString(bw, s.DefaultSensorId);
                WriteString(bw, s.DefaultWeaponId);
                WriteString(bw, s.ReviewState);
                bw.Write(s.TrlLevel);
                WriteString(bw, s.ValueTier);
                WriteString(bw, s.CitationRef);
                WriteString(bw, s.DefaultMode);
                bw.Write(s.RequiresHost);
                WriteString(bw, s.AllowedHostClasses);
                bw.Write(s.CecCapable);
            }

            bw.Flush();
        }

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream.ToArray());
        return ToHexLower(hash);
    }

    public static bool IsValidNonEmptySha256Hex(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        var trimmed = hash.Trim();
        if (trimmed.Length != 64)
        {
            return false;
        }

        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            var isHex = (c >= '0' && c <= '9')
                || (c >= 'a' && c <= 'f')
                || (c >= 'A' && c <= 'F');
            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }

    public static bool AreCompatibleHashes(string? savedHash, string? liveHash)
    {
        if (!IsValidNonEmptySha256Hex(savedHash) || !IsValidNonEmptySha256Hex(liveHash))
        {
            return false;
        }

        return string.Equals(savedHash!.Trim(), liveHash!.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryValidateBoundSnapshot(DbSnapshotStore store, string snapshotId, out string? failureReason)
    {
        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            failureReason = $"{EmptyCatalogContentHashCode}: snapshotId is empty.";
            return false;
        }

        if (!store.TryGetContentHash(snapshotId, out var contentHash) || string.IsNullOrWhiteSpace(contentHash))
        {
            failureReason = $"{EmptyCatalogContentHashCode}: catalog_snapshot '{snapshotId}' has empty or missing content_hash_sha256.";
            return false;
        }

        failureReason = null;
        return true;
    }

    private static void WriteSectionHeader(BinaryWriter bw, string name, int count)
    {
        WriteString(bw, name);
        bw.Write(count);
    }

    private static void WriteString(BinaryWriter bw, string? value)
    {
        if (value is null)
        {
            bw.Write(-1);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        bw.Write(bytes.Length);
        bw.Write(bytes);
    }

    private static void WriteDouble(BinaryWriter bw, double value)
    {
        var normalized = double.IsNaN(value)
            ? double.NaN
            : (value == 0.0 ? 0.0 : value);
        bw.Write(BitConverter.DoubleToInt64Bits(normalized));
    }

    private static string ToHexLower(byte[] hash)
    {
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            var b = hash[i];
            chars[i * 2] = GetHexNibble(b >> 4);
            chars[i * 2 + 1] = GetHexNibble(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) =>
        (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}
