namespace ProjectAegis.MissionEditor.Cli.Tests;

using Cli;
using NUnit.Framework;

[TestFixture]
public sealed class ScenarioValidateCliTests
{
    /// <summary>
    /// Adversarial CLI process contract: missing file → exit 2 + JSON error (not crash / exit 0).
    /// </summary>
    [Test]
    public void scenario_validate_missing_file_returns_exit_2_json_error()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"aegis-nope-{Guid.NewGuid():N}.json");
        using var writer = new StringWriter();
        Assert.That(ScenarioValidateCommand.Run(missing, quiet: false, writer), Is.EqualTo(2));
        var text = writer.ToString();
        Assert.That(text, Does.Contain("file not found").IgnoreCase);
        Assert.That(text, Does.Contain("error").IgnoreCase);
    }

    [Test]
    public void scenario_validate_clean_fixture_returns_exit_0()
    {
        var path = ResolveFixture("golden_clean.json");
        if (path == null)
        {
            return;
        }

        using var writer = new StringWriter();
        Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(0));
        Assert.That(writer.ToString(), Does.Contain("passed").IgnoreCase);
    }

    [Test]
    public void scenario_validate_missing_tlBranch_returns_exit_1_with_finding()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-missing-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "metadata": { "dbRef": "baltic_patrol" },
                  "missions": [
                    {
                      "id": "patrol-1",
                      "type": "Patrol",
                      "assignedUnitIds": ["u1"],
                      "patrolZone": [
                        { "lat": 57.0, "lon": 20.0 },
                        { "lat": 57.1, "lon": 20.1 },
                        { "lat": 57.2, "lon": 20.2 }
                      ]
                    }
                  ]
                }
                """);

            using var writer = new StringWriter();
            Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("TL_BRANCH_MISSING"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void scenario_validate_missing_release_train_returns_exit_1_with_finding()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-not-found-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "metadata": { "tlBranch": "TL-2" },
                  "missions": [
                    {
                      "id": "patrol-1",
                      "type": "Patrol",
                      "assignedUnitIds": ["u1"],
                      "patrolZone": [
                        { "lat": 57.0, "lon": 20.0 },
                        { "lat": 57.1, "lon": 20.1 },
                        { "lat": 57.2, "lon": 20.2 }
                      ]
                    }
                  ]
                }
                """);

            using var writer = new StringWriter();
            Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("TL_RELEASE_TRAIN_NOT_FOUND"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void scenario_validate_unknown_manifest_dbRef_returns_exit_1_with_DB_MISMATCH()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-unified-db-mismatch-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "metadata": {
                    "dbRef": "unified-corpus-TL-0-not-published",
                    "tlBranch": "TL-0"
                  },
                  "missions": [
                    {
                      "id": "patrol-1",
                      "type": "Patrol",
                      "assignedUnitIds": ["u1"],
                      "patrolZone": [
                        { "lat": 57.0, "lon": 20.0 },
                        { "lat": 57.1, "lon": 20.1 },
                        { "lat": 57.2, "lon": 20.2 }
                      ]
                    }
                  ]
                }
                """);

            using var writer = new StringWriter();
            Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("DB_MISMATCH"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void scenario_validate_unreachable_fixture_returns_exit_1()
    {
        var path = ResolveFixture("golden_strike_unreachable.json");
        if (path == null)
        {
            return;
        }

        using var writer = new StringWriter();
        Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(1));
        Assert.That(writer.ToString(), Does.Contain("STRIKE_UNREACHABLE"));
    }

    private static string? ResolveFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(dir.FullName, "assets", "data", "scenarios", "validation", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}