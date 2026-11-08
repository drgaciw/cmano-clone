using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// UAT: scenario authoring places/changes platforms from domain catalog + SQLite catalog DB
/// across 10 scenario variations and 100 platform combinations.
/// </summary>
[TestFixture]
public sealed class ScenarioPlatformComboUatTests
{
    private const int ComboCount = 100;
    private const int VariationCount = 10;

    public static readonly string[] ScenarioVariationIds =
    [
        "empty-place",
        "seeded-add",
        "dup-id-reject",
        "platform-change-upsert",
        "opposing-pair-place",
        "multi-domain-pair",
        "save-reload-roundtrip",
        "catalog-db-place",
        "stale-gesture-then-place",
        "place-then-change-platform",
    ];

    [Test]
    public void Domain_catalog_and_sqlite_db_expose_platforms_for_authoring()
    {
        var domain = ScenarioPlatformDomainCatalog.AllPresets();
        Assert.That(domain.Count, Is.GreaterThanOrEqualTo(16));

        var dbPath = ResolveBalticDbPath();
        Assert.That(File.Exists(dbPath), Is.True, $"Missing catalog DB: {dbPath}");

        var dbPlatforms = LoadSqlitePlatformIds(dbPath);
        Assert.That(dbPlatforms.Count, Is.GreaterThanOrEqualTo(50), $"SQLite platform table too small: {dbPlatforms.Count}");
    }

    [Test]
    public void Builds_100_distinct_platform_combinations()
    {
        var combos = BuildPlatformCombinations(ComboCount);
        Assert.That(combos.Count, Is.EqualTo(ComboCount));
        Assert.That(combos.Select(c => c.Key).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(ComboCount));
        Assert.That(combos.All(c => !string.IsNullOrWhiteSpace(c.BluePlatformId) && !string.IsNullOrWhiteSpace(c.RedPlatformId)), Is.True);
    }

    [Test]
    public void Ten_scenario_variations_are_named_and_stable()
    {
        Assert.That(ScenarioVariationIds.Length, Is.EqualTo(VariationCount));
        Assert.That(ScenarioVariationIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(VariationCount));
    }

    [Test]
    public void Uat_100_platform_combos_across_10_scenario_variations()
    {
        var combos = BuildPlatformCombinations(ComboCount);
        var dbPath = ResolveBalticDbPath();
        var dbIds = File.Exists(dbPath)
            ? new HashSet<string>(LoadSqlitePlatformIds(dbPath), StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var failures = new List<string>();
        var placeOk = 0;
        var changeOk = 0;
        var rejectOk = 0;
        var dbHits = 0;

        for (var v = 0; v < VariationCount; v++)
        {
            var variation = ScenarioVariationIds[v];
            for (var i = 0; i < 10; i++)
            {
                var comboIndex = v * 10 + i;
                var combo = combos[comboIndex];
                try
                {
                    RunVariation(variation, combo, comboIndex, dbIds, ref placeOk, ref changeOk, ref rejectOk, ref dbHits);
                }
                catch (Exception ex)
                {
                    failures.Add($"[{variation}#{comboIndex} {combo.Key}] {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        Assert.That(
            failures,
            Is.Empty,
            "UAT failures:\n" + string.Join("\n", failures.Take(20))
            + (failures.Count > 20 ? $"\n... +{failures.Count - 20} more" : string.Empty));

        Assert.That(placeOk, Is.GreaterThanOrEqualTo(60), $"Expected many successful places, got {placeOk}");
        Assert.That(changeOk, Is.GreaterThanOrEqualTo(15), $"Expected platform-change upserts, got {changeOk}");
        Assert.That(rejectOk, Is.GreaterThanOrEqualTo(5), $"Expected duplicate-id rejects, got {rejectOk}");
        if (dbIds.Count > 0)
        {
            Assert.That(dbHits, Is.GreaterThanOrEqualTo(10), $"Expected SQLite platform hits, got {dbHits}");
        }
    }

    [Test]
    public void Uat_100_combos_place_opposing_pairs_into_empty_scenarios()
    {
        var combos = BuildPlatformCombinations(ComboCount);
        var failures = new List<string>();
        for (var i = 0; i < combos.Count; i++)
        {
            var combo = combos[i];
            var path = TempPath($"opp-{i}");
            try
            {
                ScenarioDocumentEditor.CreateNew().Save(path);
                using var session = ScenarioAuthoringSession.Open(path);
                var blue = session.Bus.PlaceUnit(
                    session.EditVersion,
                    Unit($"u-blue-{i}", "blue", combo.BluePlatformId, 57.0 + (i % 50) * 0.01, 20.0),
                    save: true);
                var red = session.Bus.PlaceUnit(
                    session.EditVersion,
                    Unit($"u-red-{i}", "red", combo.RedPlatformId, 58.0 + (i % 50) * 0.01, 21.0),
                    save: true);
                if (!blue.Ok || !red.Ok)
                {
                    failures.Add($"combo {i} {combo.Key}: blue={blue.ErrorMessage} red={red.ErrorMessage}");
                    continue;
                }

                var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
                Assert.That(dto.Orbat!.Units.Count, Is.EqualTo(2));
                Assert.That(dto.Orbat.Units.Any(u => u.PlatformId == combo.BluePlatformId && u.SideId == "blue"), Is.True);
                Assert.That(dto.Orbat.Units.Any(u => u.PlatformId == combo.RedPlatformId && u.SideId == "red"), Is.True);
            }
            catch (Exception ex)
            {
                failures.Add($"combo {i} {combo.Key}: {ex.Message}");
            }
            finally
            {
                TryDelete(path);
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(15)));
    }

    private static void RunVariation(
        string variation,
        PlatformCombo combo,
        int comboIndex,
        HashSet<string> dbIds,
        ref int placeOk,
        ref int changeOk,
        ref int rejectOk,
        ref int dbHits)
    {
        if (dbIds.Contains(combo.BluePlatformId) || dbIds.Contains(combo.RedPlatformId))
        {
            dbHits++;
        }

        switch (variation)
        {
            case "empty-place":
            {
                var path = TempPath($"v-empty-{comboIndex}");
                try
                {
                    ScenarioDocumentEditor.CreateNew().Save(path);
                    using var session = ScenarioAuthoringSession.Open(path);
                    var r = session.Bus.PlaceUnit(
                        session.EditVersion,
                        Unit($"u-{comboIndex}", "blue", combo.BluePlatformId, 57, 20),
                        save: true);
                    Assert.That(r.Ok, Is.True, r.ErrorMessage);
                    placeOk++;
                    Assert.That(
                        ScenarioDocumentJsonLoader.LoadFromFile(path).Orbat!.Units[0].PlatformId,
                        Is.EqualTo(combo.BluePlatformId));
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            case "seeded-add":
            {
                var path = TempPath($"v-seed-{comboIndex}");
                try
                {
                    var editor = ScenarioDocumentEditor.CreateNew();
                    editor.UpsertOrbatUnit(Unit("u-seed", "blue", "ffg", 57, 20));
                    editor.CommitMutation();
                    editor.Save(path);
                    using var session = ScenarioAuthoringSession.Open(path);
                    var r = session.Bus.PlaceUnit(
                        session.EditVersion,
                        Unit($"u-add-{comboIndex}", "blue", combo.BluePlatformId, 57.2, 20.2),
                        save: true);
                    Assert.That(r.Ok, Is.True, r.ErrorMessage);
                    placeOk++;
                    Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path).Orbat!.Units.Count, Is.EqualTo(2));
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            case "dup-id-reject":
            {
                var path = TempPath($"v-dup-{comboIndex}");
                try
                {
                    var editor = ScenarioDocumentEditor.CreateNew();
                    editor.UpsertOrbatUnit(Unit("u1", "blue", combo.BluePlatformId, 57, 20));
                    editor.CommitMutation();
                    editor.Save(path);
                    using var session = ScenarioAuthoringSession.Open(path);
                    var r = session.Bus.PlaceUnit(
                        session.EditVersion,
                        Unit("u1", "red", combo.RedPlatformId, 10, 10),
                        save: true);
                    Assert.That(r.Ok, Is.False);
                    rejectOk++;
                    var u = ScenarioDocumentJsonLoader.LoadFromFile(path).Orbat!.Units.Single();
                    Assert.That(u.SideId, Is.EqualTo("blue"));
                    Assert.That(u.PlatformId, Is.EqualTo(combo.BluePlatformId));
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            case "platform-change-upsert":
            {
                var path = TempPath($"v-chg-{comboIndex}");
                try
                {
                    var editor = ScenarioDocumentEditor.CreateNew();
                    editor.UpsertOrbatUnit(Unit("u1", "blue", combo.BluePlatformId, 57, 20));
                    editor.CommitMutation();
                    editor.Save(path);
                    using var session = ScenarioAuthoringSession.Open(path);
                    var r = session.Bus.UpsertUnit(
                        session.EditVersion,
                        Unit("u1", "blue", combo.RedPlatformId, 57, 20),
                        save: true);
                    Assert.That(r.Ok, Is.True, r.ErrorMessage);
                    changeOk++;
                    var u = ScenarioDocumentJsonLoader.LoadFromFile(path).Orbat!.Units.Single();
                    Assert.That(u.PlatformId, Is.EqualTo(combo.RedPlatformId));
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            case "opposing-pair-place":
            {
                var path = TempPath($"v-opp-{comboIndex}");
                try
                {
                    ScenarioDocumentEditor.CreateNew().Save(path);
                    using var session = ScenarioAuthoringSession.Open(path);
                    Assert.That(session.Bus.PlaceUnit(
                        session.EditVersion,
                        Unit($"b-{comboIndex}", "blue", combo.BluePlatformId, 57, 20),
                        save: true).Ok, Is.True);
                    Assert.That(session.Bus.PlaceUnit(
                        session.EditVersion,
                        Unit($"r-{comboIndex}", "red", combo.RedPlatformId, 58.5, 21),
                        save: true).Ok, Is.True);
                    placeOk += 2;
                    Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path).Orbat!.Units.Count, Is.EqualTo(2));
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            case "multi-domain-pair":
            {
                var path = TempPath($"v-md-{comboIndex}");
                try
                {
                    ScenarioDocumentEditor.CreateNew().Save(path);
                    using var session = ScenarioAuthoringSession.Open(path);
                    Assert.That(session.Bus.PlaceUnit(
                        session.EditVersion,
                        Unit($"md-a-{comboIndex}", "blue", combo.BluePlatformId, 57, 20),
                        save: true).Ok, Is.True);
                    Assert.That(session.Bus.PlaceUnit(
                        session.EditVersion,
                        Unit($"md-b-{comboIndex}", "blue", combo.RedPlatformId, 57.3, 20.4),
                        save: true).Ok, Is.True);
                    placeOk += 2;
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            case "save-reload-roundtrip":
            {
                var path = TempPath($"v-rt-{comboIndex}");
                try
                {
                    ScenarioDocumentEditor.CreateNew().Save(path);
                    using (var session = ScenarioAuthoringSession.Open(path))
                    {
                        Assert.That(session.Bus.PlaceUnit(
                            session.EditVersion,
                            Unit($"rt-{comboIndex}", "blue", combo.BluePlatformId, 57.1, 20.1),
                            save: true).Ok, Is.True);
                        placeOk++;
                    }

                    using var reopened = ScenarioAuthoringSession.Open(path);
                    var unit = reopened.Editor.ToDto().Orbat!.Units.Single();
                    Assert.That(unit.PlatformId, Is.EqualTo(combo.BluePlatformId));
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            case "catalog-db-place":
            {
                var path = TempPath($"v-db-{comboIndex}");
                try
                {
                    var platformId = combo.BluePlatformId;
                    if (dbIds.Count > 0)
                    {
                        var ordered = dbIds.OrderBy(x => x, StringComparer.Ordinal).ToArray();
                        platformId = ordered[comboIndex % ordered.Length];
                        dbHits++;
                    }

                    ScenarioDocumentEditor.CreateNew().Save(path);
                    using var session = ScenarioAuthoringSession.Open(path);
                    var r = session.Bus.PlaceUnit(
                        session.EditVersion,
                        Unit($"db-{comboIndex}", "blue", platformId, 56.5 + (comboIndex % 20) * 0.05, 19.5),
                        save: true);
                    Assert.That(r.Ok, Is.True, r.ErrorMessage);
                    placeOk++;
                    Assert.That(
                        ScenarioDocumentJsonLoader.LoadFromFile(path).Orbat!.Units[0].PlatformId,
                        Is.EqualTo(platformId));
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            case "stale-gesture-then-place":
            {
                var path = TempPath($"v-stale-{comboIndex}");
                try
                {
                    ScenarioDocumentEditor.CreateNew().Save(path);
                    using var session = ScenarioAuthoringSession.Open(path);
                    var surface = new MapAuthoringSurface(session);
                    surface.BeginPlaceUnit(Unit("u-stale", "blue", combo.BluePlatformId, 57, 20));
                    ScenarioMapAuthoringHostPolicy.InvalidateStagedGesturesForFormOrDomainChange(surface);
                    Assert.That(surface.TentativeUnit, Is.Null);
                    Assert.That(surface.CommitPlaceUnit(save: true), Is.Null);

                    surface.BeginPlaceUnit(Unit($"u-final-{comboIndex}", "blue", combo.RedPlatformId, 57.4, 20.4));
                    var r = surface.CommitPlaceUnit(save: true);
                    Assert.That(r, Is.Not.Null);
                    Assert.That(r!.Ok, Is.True, r.ErrorMessage);
                    placeOk++;
                    var u = ScenarioDocumentJsonLoader.LoadFromFile(path).Orbat!.Units.Single();
                    Assert.That(u.PlatformId, Is.EqualTo(combo.RedPlatformId));
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            case "place-then-change-platform":
            {
                var path = TempPath($"v-ptc-{comboIndex}");
                try
                {
                    ScenarioDocumentEditor.CreateNew().Save(path);
                    using var session = ScenarioAuthoringSession.Open(path);
                    Assert.That(session.Bus.PlaceUnit(
                        session.EditVersion,
                        Unit("u1", "blue", combo.BluePlatformId, 57, 20),
                        save: true).Ok, Is.True);
                    placeOk++;
                    var chg = session.Bus.UpsertUnit(
                        session.EditVersion,
                        Unit("u1", "blue", combo.RedPlatformId, 57.05, 20.05),
                        save: true);
                    Assert.That(chg.Ok, Is.True, chg.ErrorMessage);
                    changeOk++;
                    var u = ScenarioDocumentJsonLoader.LoadFromFile(path).Orbat!.Units.Single();
                    Assert.That(u.PlatformId, Is.EqualTo(combo.RedPlatformId));
                    Assert.That(u.Lat, Is.EqualTo(57.05).Within(1e-6));
                }
                finally
                {
                    TryDelete(path);
                }

                break;
            }
            default:
                throw new InvalidOperationException($"Unknown variation '{variation}'");
        }
    }

    public static IReadOnlyList<PlatformCombo> BuildPlatformCombinations(int count)
    {
        var blues = ScenarioPlatformDomainCatalog.AllPresets()
            .Where(p => string.Equals(p.DefaultSideId, "blue", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.PlatformId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        var reds = ScenarioPlatformDomainCatalog.AllPresets()
            .Where(p => string.Equals(p.DefaultSideId, "red", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.PlatformId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var dbPath = ResolveBalticDbPath();
        if (File.Exists(dbPath))
        {
            var db = LoadSqlitePlatformIds(dbPath);
            var extra = db.Where(id => !blues.Contains(id, StringComparer.OrdinalIgnoreCase)
                                       && !reds.Contains(id, StringComparer.OrdinalIgnoreCase))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            for (var i = 0; i < extra.Count; i++)
            {
                if (i % 2 == 0)
                {
                    blues.Add(extra[i]);
                }
                else
                {
                    reds.Add(extra[i]);
                }
            }
        }

        Assert.That(blues.Count, Is.GreaterThan(0));
        Assert.That(reds.Count, Is.GreaterThan(0));

        var combos = new List<PlatformCombo>(count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var bi = 0;
        var ri = 0;
        while (combos.Count < count)
        {
            var blue = blues[bi % blues.Count];
            var red = reds[ri % reds.Count];
            bi++;
            if (bi % blues.Count == 0)
            {
                ri++;
            }

            var key = $"{blue}|{red}";
            if (!seen.Add(key))
            {
                key = $"{blue}|{red}|{combos.Count}";
                if (!seen.Add(key))
                {
                    continue;
                }
            }

            combos.Add(new PlatformCombo(key, blue, red));
        }

        return combos;
    }

    public static string ResolveBalticDbPath()
    {
        var root = FindRepoRoot();
        return Path.Combine(root, "assets", "data", "catalog", "baltic_patrol.db");
    }

    public static IReadOnlyList<string> LoadSqlitePlatformIds(string databasePath)
    {
        var ids = new List<string>();
        SqliteConnection.ClearAllPools();
        using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Pooling=false");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT platform_id FROM platform ORDER BY platform_id ASC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    private static ScenarioOrbatUnitDto Unit(string id, string side, string platform, double lat, double lon) =>
        new()
        {
            Id = id,
            SideId = side,
            PlatformId = platform,
            Lat = lat,
            Lon = lon,
        };

    private static string TempPath(string tag) =>
        Path.Combine(Path.GetTempPath(), $"aegis-uat-combo-{tag}-{Guid.NewGuid():N}.json");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignore
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "assets", "data", "catalog", "baltic_patrol.db"))
                || File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                if (File.Exists(Path.Combine(dir.FullName, "assets", "data", "catalog", "baltic_patrol.db")))
                {
                    return dir.FullName;
                }
            }

            dir = dir.Parent;
        }

        var cwd = Directory.GetCurrentDirectory();
        foreach (var candidate in new[]
                 {
                     cwd,
                     Path.Combine(cwd, "cmano-clone"),
                     Path.GetFullPath(Path.Combine(cwd, "..")),
                     Path.GetFullPath(Path.Combine(cwd, "..", "..")),
                 })
        {
            var db = Path.Combine(candidate, "assets", "data", "catalog", "baltic_patrol.db");
            if (File.Exists(db))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not locate repo root with assets/data/catalog/baltic_patrol.db");
    }

    public sealed record PlatformCombo(string Key, string BluePlatformId, string RedPlatformId);
}
