namespace ProjectAegis.Data.Catalog;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// DBI-7.3 canonical composite sort keys for Catalog* types. Single source of truth for
/// Propose* batch ordering, SQLite read ORDER BY, markdown import, and workbook export.
/// Immutable IDs (<see cref="CatalogPlatformBinding.PlatformId"/>, <see cref="CatalogWeaponRecord.WeaponId"/>,
/// <see cref="CatalogMount.MountId"/>, etc.) are never rewritten by display-name aliases.
/// </summary>
public static class CatalogSortKeyComparer
{
    public static IReadOnlyList<CatalogSensorBinding> SortSensors(IEnumerable<CatalogSensorBinding> rows) =>
        rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal)
            .ThenBy(r => r.SensorId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogPlatformBinding> SortPlatforms(IEnumerable<CatalogPlatformBinding> rows) =>
        rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogWeaponRecord> SortWeapons(IEnumerable<CatalogWeaponRecord> rows) =>
        rows.OrderBy(r => r.WeaponId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogMount> SortMounts(IEnumerable<CatalogMount> rows) =>
        rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal)
            .ThenBy(r => r.MountId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogLoadout> SortLoadouts(IEnumerable<CatalogLoadout> rows) =>
        rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal)
            .ThenBy(r => r.LoadoutId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogMagazineEntry> SortMagazines(IEnumerable<CatalogMagazineEntry> rows) =>
        rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal)
            .ThenBy(r => r.LoadoutId, StringComparer.Ordinal)
            .ThenBy(r => r.MountId, StringComparer.Ordinal)
            .ThenBy(r => r.WeaponId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogCommsBinding> SortComms(IEnumerable<CatalogCommsBinding> rows) =>
        rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal)
            .ThenBy(r => r.LinkId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogMobility> SortMobility(IEnumerable<CatalogMobility> rows) =>
        rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogSignature> SortSignatures(IEnumerable<CatalogSignature> rows) =>
        rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogEmcon> SortEmcon(IEnumerable<CatalogEmcon> rows) =>
        rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal)
            .ThenBy(r => r.Condition, StringComparer.Ordinal)
            .ThenBy(r => r.EmitterId, StringComparer.Ordinal)
            .ToArray();

    public static string FormatSensorKey(CatalogSensorBinding row) =>
        Join(row.PlatformId, row.SensorId);

    public static string FormatPlatformKey(CatalogPlatformBinding row) => row.PlatformId;

    public static string FormatWeaponKey(CatalogWeaponRecord row) => row.WeaponId;

    public static string FormatMountKey(CatalogMount row) => Join(row.PlatformId, row.MountId);

    public static string FormatLoadoutKey(CatalogLoadout row) => Join(row.PlatformId, row.LoadoutId);

    public static string FormatMagazineKey(CatalogMagazineEntry row) =>
        Join(row.PlatformId, row.LoadoutId, row.MountId, row.WeaponId);

    public static string FormatCommsKey(CatalogCommsBinding row) => Join(row.PlatformId, row.LinkId);

    public static string FormatMobilityKey(CatalogMobility row) => row.PlatformId;

    public static string FormatSignatureKey(CatalogSignature row) => row.PlatformId;

    public static string FormatEmconKey(CatalogEmcon row) => Join(row.PlatformId, row.Condition, row.EmitterId);

    /// <summary>
    /// Deterministic SHA-256 over canonical sort-key tuples for all Catalog* entity domains.
    /// Used by golden tests to pin stable ordering across propose, export, and read paths.
    /// </summary>
    public static string ComputeOrderingHash(CatalogSortKeyFixture fixture)
    {
        var sb = new StringBuilder(512);
        AppendSection(sb, "sensor", SortSensors(fixture.Sensors).Select(FormatSensorKey));
        AppendSection(sb, "platform", SortPlatforms(fixture.Platforms).Select(FormatPlatformKey));
        AppendSection(sb, "weapon", SortWeapons(fixture.Weapons).Select(FormatWeaponKey));
        AppendSection(sb, "mount", SortMounts(fixture.Mounts).Select(FormatMountKey));
        AppendSection(sb, "loadout", SortLoadouts(fixture.Loadouts).Select(FormatLoadoutKey));
        AppendSection(sb, "magazine", SortMagazines(fixture.Magazines).Select(FormatMagazineKey));
        AppendSection(sb, "comms", SortComms(fixture.Comms).Select(FormatCommsKey));

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return ToHexLower(bytes);
    }

    private static void AppendSection(StringBuilder sb, string section, IEnumerable<string> keys)
    {
        sb.Append(section);
        sb.Append('\n');
        foreach (var key in keys)
        {
            sb.Append(key);
            sb.Append('\n');
        }

        sb.Append('\n');
    }

    private static string Join(params string[] parts) => string.Join('\t', parts);

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

/// <summary>Fixture bundle for cross-entity sort-key golden hashing.</summary>
public sealed record CatalogSortKeyFixture(
    IReadOnlyList<CatalogSensorBinding> Sensors,
    IReadOnlyList<CatalogPlatformBinding> Platforms,
    IReadOnlyList<CatalogWeaponRecord> Weapons,
    IReadOnlyList<CatalogMount> Mounts,
    IReadOnlyList<CatalogLoadout> Loadouts,
    IReadOnlyList<CatalogMagazineEntry> Magazines,
    IReadOnlyList<CatalogCommsBinding> Comms)
{
    public static CatalogSortKeyFixture Empty { get; } = new([], [], [], [], [], [], []);
}