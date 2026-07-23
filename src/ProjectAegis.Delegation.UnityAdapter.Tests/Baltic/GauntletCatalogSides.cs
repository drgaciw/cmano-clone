using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>
/// Catalog ORBAT side classification for gauntlet policies: Russia = red; other catalog
/// nationalities (NATO / Nordic / partner) = blue. Loaded once from baltic_patrol.db so
/// expanded ORBATs do not require hard-coded platform id lists in each test fixture.
/// </summary>
internal static class GauntletCatalogSides
{
    private static readonly Lazy<(HashSet<string> Blue, HashSet<string> Red)> Sides = new(Load);

    public static bool IsBlue(string id) => Sides.Value.Blue.Contains(id);

    public static bool IsRed(string id) => Sides.Value.Red.Contains(id);

    private static (HashSet<string> Blue, HashSet<string> Red) Load()
    {
        var blue = new HashSet<string>(StringComparer.Ordinal);
        var red = new HashSet<string>(StringComparer.Ordinal);
        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, nationality
            FROM platform
            WHERE platform_id NOT IN ('u1', 'hostile-1', 'hostile-far')
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetString(0);
            var nationality = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            if (string.Equals(nationality, "Russia", StringComparison.OrdinalIgnoreCase))
            {
                red.Add(id);
            }
            else if (!string.IsNullOrWhiteSpace(nationality)
                     && !string.Equals(nationality, "Algeria", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(nationality, "Iraq", StringComparison.OrdinalIgnoreCase))
            {
                blue.Add(id);
            }
        }

        return (blue, red);
    }
}
