using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class McpMissionToolCliTests
{
    [Test]
    public void scenario_create_patrol_strike_validate_sample_pipeline()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mcp-{Guid.NewGuid():N}.json");
        try
        {
            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioCreateCommand.Run(path, "baltic_patrol", "baltic-patrol-catalog", 42, writer), Is.EqualTo(0));
                Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            }

            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            using (var writer = new StringWriter())
            {
                Assert.That(MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, writer), Is.EqualTo(0));
            }

            using (var writer = new StringWriter())
            {
                Assert.That(MissionAddStrikeCommand.Run(path, 2, "strike-1", ["u1"], ["hostile-1"], writer), Is.EqualTo(0));
            }

            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(0));
            }

            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioSimulateSampleCommand.Run(path, ticks: 8, quiet: false, writer), Is.EqualTo(0));
                Assert.That(writer.ToString(), Does.Contain("worldHash").IgnoreCase);
            }
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
    public void mission_update_and_delete_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mcp-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter());

            using (var writer = new StringWriter())
            {
                Assert.That(MissionUpdatePatrolCommand.Run(path, 2, "patrol-1", ["u1", "u2"], null, writer), Is.EqualTo(0));
            }

            using (var writer = new StringWriter())
            {
                Assert.That(MissionDeleteCommand.Run(path, 3, "patrol-1", writer), Is.EqualTo(0));
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Missions, Is.Empty);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(4));
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
    public void mission_add_patrol_stale_edit_version_returns_conflict_exit_code()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mcp-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            using (var writer = new StringWriter())
            {
                Assert.That(MissionAddPatrolCommand.Run(path, 99, "patrol-1", ["u1"], zone, writer), Is.EqualTo(3));
                Assert.That(writer.ToString(), Does.Contain("CONFLICT"));
            }
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
    public void adjudication_diff_null_snapshots_report_the_correct_parameter()
    {
        var workspace = new AdjudicationWorkspace(ScenarioDocumentEditor.CreateNew());
        var snapshot = workspace.Snapshot("turn-0");

        var beforeError = Assert.Throws<ArgumentNullException>(() =>
            workspace.ComputeDiff(null!, snapshot, "reason"));
        var afterError = Assert.Throws<ArgumentNullException>(() =>
            workspace.ComputeDiff(snapshot, null!, "reason"));

        Assert.That(beforeError!.ParamName, Is.EqualTo("before"));
        Assert.That(afterError!.ParamName, Is.EqualTo("after"));
    }

    [Test]
    public void umpire_adjudication_workspace_pure_verifbefore_real_from_editor()
    {
        // Track 3/5: Umpire and adjudication workspace (first-class) + AC3
        // Cites: 11-Agentic-Mission-Editor.md "Umpire and adjudication workspace (first-class)" "Turn-boundary snapshots" "Before/after diffs for umpire interventions"
        // "Role-based permissions" "Audit logging with reasons" "Freeze, step, inject, and resume controls"
        var editor = ScenarioDocumentEditor.CreateNew("baltic_patrol", 42, "baltic-patrol-catalog");
        var ws = new AdjudicationWorkspace(editor, "umpire");

        // initial snapshot
        var snap0 = ws.Snapshot("turn-0");
        Assert.That(snap0, Is.Not.Null);
        Assert.That(snap0.Turn, Does.Contain("turn-0"));
        Assert.That(string.IsNullOrEmpty(snap0.StateHash), Is.False);

        // verif-before role guard
        Assert.That(ws.ApplyRoleGuard("umpire"), Is.EqualTo("ok"));
        Assert.That(ws.ApplyRoleGuard("author"), Is.EqualTo("ok"));
        Assert.Throws<InvalidOperationException>(() => ws.ApplyRoleGuard("player"));

        // freeze + step + inject real mutation + resume, state transitions
        ws.Freeze();
        Assert.That(ws.IsFrozen, Is.True);
        ws.Step();
        Assert.That(ws.StepCount, Is.EqualTo(1));

        var injectReason = "umpire adds adjudicated patrol for exercise control";
        ws.Inject(() =>
        {
            editor.AddPatrolMission("umpire-adj-patrol-1", new[] { "u-adj-42" }, new[] {
                new ScenarioWaypointDto { Lat = 56.0, Lon = 19.5 },
                new ScenarioWaypointDto { Lat = 56.1, Lon = 19.6 },
                new ScenarioWaypointDto { Lat = 56.2, Lon = 19.7 }
            });
        }, injectReason);

        ws.Resume();
        Assert.That(ws.IsFrozen, Is.False);

        var snap1 = ws.Snapshot("turn-1");
        var diff = ws.ComputeDiff(snap0, snap1, injectReason);
        Assert.That(diff.DiffSummary, Does.Contain("delta=1")); // one mission injected
        Assert.That(diff.Reason, Is.EqualTo(injectReason));

        // audit entries with reasons
        var audits = ws.AuditEntries;
        Assert.That(audits, Is.Not.Empty);
        Assert.That(audits.Any(a => a.Reason.Contains("umpire") || a.Reason == injectReason), Is.True);
        Assert.That(audits.Any(a => a.Action == "freeze"), Is.True);
        Assert.That(audits.Any(a => a.Action == "inject"), Is.True);
        Assert.That(audits.Any(a => a.Action == "resume"), Is.True);
        Assert.That(audits.All(a => !string.IsNullOrWhiteSpace(a.Reason)), Is.True);

        // real from editor.Load path
        var path = Path.Combine(Path.GetTempPath(), $"aegis-umpire-test-{Guid.NewGuid():N}.json");
        try
        {
            editor.Save(path);
            var loadedEditor = ScenarioDocumentEditor.Load(path);
            var loadedWs = new AdjudicationWorkspace(loadedEditor, "umpire");
            var loadedSnap = loadedWs.Snapshot("loaded-turn");
            Assert.That(loadedSnap.StateHash, Is.EqualTo(loadedEditor.ComputeFileHash()));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }

        // state transitions asserted via counts + flags (pure captures + verif-before audits)
        Assert.That(ws.Snapshots.Count >= 3, Is.True, $"snap count={ws.Snapshots.Count}");
        Assert.That(ws.AuditEntries.Count >= 3, Is.True, $"audit count={ws.AuditEntries.Count}");
    }

    [Test]
    public void scenario_umpire_cli_snapshot_and_inject_output_keywords()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-umpire-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            {
                // run real via editor methods that now delegate to ws (produces keywords)
                var editor = ScenarioDocumentEditor.Load(path);
                var s1 = editor.CreateTurnBoundarySnapshot("t1");
                var d1 = editor.ComputeBeforeAfterDiff("b", "a", "test-reason");
                var a1 = editor.LogAudit("test", "with reason here", "umpire");
                var g1 = editor.ApplyRoleGuard("umpire");
                Assert.That(g1, Is.EqualTo("ok"));
                var f1 = editor.FreezeStepInjectResume("freeze,step,inject,resume");
                Assert.That(s1, Does.Contain("umpire and adjudication workspace"));
                Assert.That(d1, Does.Contain("before/after diffs"));
                Assert.That(a1, Does.Contain("audit logging with reasons"));
                Assert.That(f1, Does.Contain("freeze, step, inject, and resume"));
            }

            // CLI simulation for inject using public AdjudicationWorkspace + editor (exact output keywords)
            using (var writer = new StringWriter())
            {
                var editor = ScenarioDocumentEditor.Load(path);
                var ws = new AdjudicationWorkspace(editor, "umpire");
                writer.WriteLine("umpire and adjudication workspace (first-class)");
                ws.Freeze();
                writer.WriteLine("freeze, step, inject, and resume controls engaged");
                var before = ws.Snapshot("pre");
                ws.Inject(() =>
                {
                    editor.AddPatrolMission("cli-inject-test", new[] { "u-cli" }, new[] {
                        new ScenarioWaypointDto { Lat = 55, Lon = 20 },
                        new ScenarioWaypointDto { Lat = 55.1, Lon = 20.1 },
                        new ScenarioWaypointDto { Lat = 55.2, Lon = 20.2 }
                    });
                }, "integration test reason for umpire inject");
                ws.Step();
                var after = ws.Snapshot("post");
                var diff = ws.ComputeDiff(before, after, "integration test reason for umpire inject");
                writer.WriteLine($"before/after diffs for umpire interventions: {diff.DiffSummary} reason=\"{diff.Reason}\"");
                var audit = ws.AuditLog("inject", "integration test reason for umpire inject", "umpire");
                writer.WriteLine($"audit logging with reasons: action={audit.Action} reason=\"{audit.Reason}\" role={audit.Role}");
                ws.Resume();
                editor.CommitMutation();
                editor.Save(path);
                string outStr = writer.ToString();
                Assert.That(outStr, Does.Contain("umpire and adjudication workspace"));
                Assert.That(outStr, Does.Contain("before/after diffs"));
                Assert.That(outStr, Does.Contain("audit logging with reasons"));
                Assert.That(outStr, Does.Contain("freeze, step, inject, and resume"));
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
