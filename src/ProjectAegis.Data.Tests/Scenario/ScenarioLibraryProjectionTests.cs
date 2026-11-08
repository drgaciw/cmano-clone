namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

public sealed class ScenarioLibraryProjectionTests
{
    [Fact]
    public void ListFromDirectory_temp_dir_with_two_fixtures_is_sorted_by_scenario_id()
    {
        var temp = CreateTempDir();
        try
        {
            WriteScenario(temp, "zulu.scenario.json", """
                {
                  "metadata": {
                    "title": "Zulu Patrol",
                    "dbRef": "baltic_patrol",
                    "policyId": "baltic-patrol",
                    "tlBranch": "TL-2",
                    "seed": 7
                  },
                  "missions": []
                }
                """);
            WriteScenario(temp, "alpha.scenario.json", """
                {
                  "metadata": {
                    "title": "Alpha Strike",
                    "dbRef": "baltic_patrol",
                    "policyId": "baltic-patrol-catalog",
                    "tlBranch": "TL-0",
                    "seed": 42
                  },
                  "missions": []
                }
                """);

            var entries = ScenarioLibraryLister.ListFromDirectory(temp);

            Assert.Equal(2, entries.Count);
            Assert.Equal("alpha.scenario", entries[0].ScenarioId);
            Assert.Equal("zulu.scenario", entries[1].ScenarioId);
            Assert.Equal("Alpha Strike", entries[0].Title);
            Assert.Equal("Zulu Patrol", entries[1].Title);
            Assert.Equal(42ul, entries[0].Seed);
            Assert.Equal("TL-0", entries[0].TlBranch);
            Assert.Equal("baltic-patrol-catalog", entries[0].PolicyId);
            Assert.Equal(ScenarioLibraryProjection.DefaultProvenance, entries[0].ProvenanceLabel);
            Assert.Equal(ScenarioLibraryProjection.MissingLabel, entries[0].Location);
            Assert.Equal(ScenarioLibraryProjection.MissingLabel, entries[0].Year);
            Assert.True(entries[0].Available);
            Assert.Null(entries[0].UnavailableReason);
            Assert.Contains("alpha.scenario.json", entries[0].SourcePath, StringComparison.Ordinal);
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Fact]
    public void ProjectFromPath_schema_error_marks_unavailable()
    {
        var temp = CreateTempDir();
        try
        {
            var badPath = Path.Combine(temp, "broken.scenario.json");
            File.WriteAllText(badPath, "{ not-valid-json ");

            var entry = ScenarioLibraryProjection.ProjectFromPath(badPath);

            Assert.False(entry.Available);
            Assert.Equal(ScenarioLibraryReasons.SchemaError, entry.UnavailableReason);
            Assert.Equal("broken.scenario", entry.ScenarioId);
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Fact]
    public void ListFromDirectory_one_bad_file_does_not_throw_and_marks_row()
    {
        var temp = CreateTempDir();
        try
        {
            WriteScenario(temp, "good.scenario.json", """
                {
                  "metadata": { "title": "Good", "dbRef": "baltic_patrol", "policyId": "p1", "seed": 1 },
                  "missions": []
                }
                """);
            File.WriteAllText(Path.Combine(temp, "bad.scenario.json"), "[[[");

            var entries = ScenarioLibraryLister.ListFromDirectory(temp);

            Assert.Equal(2, entries.Count);
            var bad = Assert.Single(entries, e => e.ScenarioId == "bad.scenario");
            Assert.False(bad.Available);
            Assert.Equal(ScenarioLibraryReasons.SchemaError, bad.UnavailableReason);

            var good = Assert.Single(entries, e => e.ScenarioId == "good.scenario");
            Assert.True(good.Available);
            Assert.Equal("Good", good.Title);
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Fact]
    public void Project_db_mismatch_when_catalog_cannot_resolve_dbRef()
    {
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                Title = "Mismatch Scenario",
                DbRef = "does-not-exist-anywhere-xyz",
                PolicyId = "baltic-patrol",
                TlBranch = "TL-2",
                Seed = 99,
            },
            Missions = Array.Empty<ScenarioMissionDto>(),
        };

        var entry = ScenarioLibraryProjection.Project(
            "mismatch",
            doc,
            sourcePath: "/tmp/mismatch.scenario.json",
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.False(entry.Available);
        Assert.Equal(ScenarioLibraryReasons.DbMismatch, entry.UnavailableReason);
        Assert.Equal("Mismatch Scenario", entry.Title);
        Assert.Equal(99ul, entry.Seed);
    }

    [Fact]
    public void Project_available_when_baltic_dbRef_resolves()
    {
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                Title = "Baltic Clean",
                DbRef = "baltic_patrol",
                PolicyId = "baltic-patrol-catalog",
                TlBranch = "TL-0",
                Seed = 42,
            },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "patrol-1",
                    Type = "Patrol",
                    AssignedUnitIds = ["u1"],
                    PatrolZone =
                    [
                        new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                        new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                        new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                    ],
                },
            ],
        };

        var entry = ScenarioLibraryProjection.Project(
            "golden_clean",
            doc,
            sourcePath: "golden_clean.json",
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.True(entry.Available);
        Assert.Null(entry.UnavailableReason);
        Assert.Equal("Baltic Clean", entry.Title);
        Assert.Equal("baltic-patrol-catalog", entry.PolicyId);
    }

    [Fact]
    public void Project_validation_blocked_when_engine_reports_errors()
    {
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                Title = "Strike Empty",
                DbRef = "baltic_patrol",
                PolicyId = "baltic-patrol",
            },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["u1"],
                    TargetIds = [],
                },
            ],
        };

        var entry = ScenarioLibraryProjection.Project(
            "strike-empty",
            doc,
            sourcePath: "strike-empty.scenario.json",
            catalog: InMemoryCatalogReader.BalticPatrolFixture(),
            validation: new ScenarioValidationEngine());

        Assert.False(entry.Available);
        Assert.Equal(ScenarioLibraryReasons.ValidationBlocked, entry.UnavailableReason);
    }

    [Fact]
    public void ListFromDirectory_repo_examples_via_ScenarioDataPaths_when_present()
    {
        var dir = ScenarioDataPaths.TryResolveScenariosDirectory();
        if (dir == null)
        {
            return;
        }

        var entries = ScenarioLibraryLister.ListFromDirectory(dir);
        // Repo ships examples/*.scenario.json under data/scenarios.
        if (entries.Count == 0)
        {
            return;
        }

        Assert.True(entries.Count >= 1);
        // Deterministic order.
        for (var i = 1; i < entries.Count; i++)
        {
            Assert.True(
                string.Compare(entries[i - 1].ScenarioId, entries[i].ScenarioId, StringComparison.Ordinal) <= 0);
        }
    }

    [Fact]
    public void ProjectFromPath_missing_file_is_file_unreadable()
    {
        var entry = ScenarioLibraryProjection.ProjectFromPath(
            Path.Combine(Path.GetTempPath(), "aegis-missing-" + Guid.NewGuid().ToString("N") + ".scenario.json"));

        Assert.False(entry.Available);
        Assert.Equal(ScenarioLibraryReasons.FileUnreadable, entry.UnavailableReason);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "aegis-scenariolib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void WriteScenario(string dir, string fileName, string json) =>
        File.WriteAllText(Path.Combine(dir, fileName), json);

    private static void TryDeleteDir(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
