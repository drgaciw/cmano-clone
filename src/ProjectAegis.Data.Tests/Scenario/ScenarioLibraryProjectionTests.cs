namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

public sealed class ScenarioLibraryProjectionTests
{
    [Test]
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

            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].ScenarioId, Is.EqualTo("alpha.scenario"));
            Assert.That(entries[1].ScenarioId, Is.EqualTo("zulu.scenario"));
            Assert.That(entries[0].Title, Is.EqualTo("Alpha Strike"));
            Assert.That(entries[1].Title, Is.EqualTo("Zulu Patrol"));
            Assert.That(entries[0].Seed, Is.EqualTo(42ul));
            Assert.That(entries[0].TlBranch, Is.EqualTo("TL-0"));
            Assert.That(entries[0].PolicyId, Is.EqualTo("baltic-patrol-catalog"));
            Assert.That(entries[0].ProvenanceLabel, Is.EqualTo(ScenarioLibraryProjection.DefaultProvenance));
            Assert.That(entries[0].Location, Is.EqualTo(ScenarioLibraryProjection.MissingLabel));
            Assert.That(entries[0].Year, Is.EqualTo(ScenarioLibraryProjection.MissingLabel));
            Assert.That(entries[0].Available, Is.True);
            Assert.That(entries[0].UnavailableReason, Is.Null);
            Assert.That(entries[0].SourcePath, Does.Contain("alpha.scenario.json"));
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Test]
    public void ProjectFromPath_schema_error_marks_unavailable()
    {
        var temp = CreateTempDir();
        try
        {
            var badPath = Path.Combine(temp, "broken.scenario.json");
            File.WriteAllText(badPath, "{ not-valid-json ");

            var entry = ScenarioLibraryProjection.ProjectFromPath(badPath);

            Assert.That(entry.Available, Is.False);
            Assert.That(entry.UnavailableReason, Is.EqualTo(ScenarioLibraryReasons.SchemaError));
            Assert.That(entry.ScenarioId, Is.EqualTo("broken.scenario"));
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Test]
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

            Assert.That(entries.Count, Is.EqualTo(2));
            var bad = ProjectAegis.Data.Tests.AssertEx.Single(entries, e => e.ScenarioId == "bad.scenario");
            Assert.That(bad.Available, Is.False);
            Assert.That(bad.UnavailableReason, Is.EqualTo(ScenarioLibraryReasons.SchemaError));

            var good = ProjectAegis.Data.Tests.AssertEx.Single(entries, e => e.ScenarioId == "good.scenario");
            Assert.That(good.Available, Is.True);
            Assert.That(good.Title, Is.EqualTo("Good"));
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Test]
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

        Assert.That(entry.Available, Is.False);
        Assert.That(entry.UnavailableReason, Is.EqualTo(ScenarioLibraryReasons.DbMismatch));
        Assert.That(entry.Title, Is.EqualTo("Mismatch Scenario"));
        Assert.That(entry.Seed, Is.EqualTo(99ul));
    }

    [Test]
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

        Assert.That(entry.Available, Is.True);
        Assert.That(entry.UnavailableReason, Is.Null);
        Assert.That(entry.Title, Is.EqualTo("Baltic Clean"));
        Assert.That(entry.PolicyId, Is.EqualTo("baltic-patrol-catalog"));
    }

    [Test]
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

        Assert.That(entry.Available, Is.False);
        Assert.That(entry.UnavailableReason, Is.EqualTo(ScenarioLibraryReasons.ValidationBlocked));
    }

    [Test]
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

        Assert.That(entries.Count >= 1, Is.True);
        // Deterministic order.
        for (var i = 1; i < entries.Count; i++)
        {
            Assert.That(                string.Compare(entries[i - 1].ScenarioId, entries[i].ScenarioId, StringComparison.Ordinal) <= 0, Is.True);
        }
    }

    [Test]
    public void ProjectFromPath_missing_file_is_file_unreadable()
    {
        var entry = ScenarioLibraryProjection.ProjectFromPath(
            Path.Combine(Path.GetTempPath(), "aegis-missing-" + Guid.NewGuid().ToString("N") + ".scenario.json"));

        Assert.That(entry.Available, Is.False);
        Assert.That(entry.UnavailableReason, Is.EqualTo(ScenarioLibraryReasons.FileUnreadable));
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
