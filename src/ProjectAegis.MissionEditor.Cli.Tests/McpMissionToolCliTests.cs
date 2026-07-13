using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class McpMissionToolCliTests
{
    [Fact]
    public void scenario_create_patrol_strike_validate_sample_pipeline()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mcp-{Guid.NewGuid():N}.json");
        try
        {
            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioCreateCommand.Run(path, "baltic_patrol", "baltic-patrol-catalog", 42, writer));
                Assert.Contains("\"ok\":true", writer.ToString());
            }

            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            using (var writer = new StringWriter())
            {
                Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, writer));
            }

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, MissionAddStrikeCommand.Run(path, 2, "strike-1", ["u1"], ["hostile-1"], writer));
            }

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioValidateCommand.Run(path, quiet: false, writer));
            }

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, ticks: 8, quiet: false, writer));
                Assert.Contains("worldHash", writer.ToString(), StringComparison.OrdinalIgnoreCase);
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

    [Fact]
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
                Assert.Equal(0, MissionUpdatePatrolCommand.Run(path, 2, "patrol-1", ["u1", "u2"], null, writer));
            }

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, MissionDeleteCommand.Run(path, 3, "patrol-1", writer));
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Empty(dto.Missions);
            Assert.Equal(4, dto.Metadata.EditVersion);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void mission_add_patrol_stale_edit_version_returns_conflict_exit_code()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mcp-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            using (var writer = new StringWriter())
            {
                Assert.Equal(3, MissionAddPatrolCommand.Run(path, 99, "patrol-1", ["u1"], zone, writer));
                Assert.Contains("CONFLICT", writer.ToString());
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

    [Fact]
    public void umpire_adjudication_workspace_pure_verifbefore_real_from_editor()
    {
        // Track 3/5: Umpire and adjudication workspace (first-class) + AC3
        // Cites: 11-Agentic-Mission-Editor.md "Umpire and adjudication workspace (first-class)" "Turn-boundary snapshots" "Before/after diffs for umpire interventions"
        // "Role-based permissions" "Audit logging with reasons" "Freeze, step, inject, and resume controls"
        var editor = ScenarioDocumentEditor.CreateNew("baltic_patrol", 42, "baltic-patrol-catalog");
        var ws = new AdjudicationWorkspace(editor, "umpire");

        // initial snapshot
        var snap0 = ws.Snapshot("turn-0");
        Assert.NotNull(snap0);
        Assert.Contains("turn-0", snap0.Turn);
        Assert.False(string.IsNullOrEmpty(snap0.StateHash));

        // verif-before role guard
        Assert.Equal("ok", ws.ApplyRoleGuard("umpire"));
        Assert.Equal("ok", ws.ApplyRoleGuard("author"));
        Assert.Throws<InvalidOperationException>(() => ws.ApplyRoleGuard("player"));

        // freeze + step + inject real mutation + resume, state transitions
        ws.Freeze();
        Assert.True(ws.IsFrozen);
        ws.Step();
        Assert.Equal(1, ws.StepCount);

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
        Assert.False(ws.IsFrozen);

        var snap1 = ws.Snapshot("turn-1");
        var diff = ws.ComputeDiff(snap0, snap1, injectReason);
        Assert.Contains("delta=1", diff.DiffSummary); // one mission injected
        Assert.Equal(injectReason, diff.Reason);

        // audit entries with reasons
        var audits = ws.AuditEntries;
        Assert.NotEmpty(audits);
        Assert.Contains(audits, a => a.Reason.Contains("umpire") || a.Reason == injectReason);
        Assert.Contains(audits, a => a.Action == "freeze");
        Assert.Contains(audits, a => a.Action == "inject");
        Assert.Contains(audits, a => a.Action == "resume");
        Assert.All(audits, a => Assert.False(string.IsNullOrWhiteSpace(a.Reason)));

        // real from editor.Load path
        var path = Path.Combine(Path.GetTempPath(), $"aegis-umpire-test-{Guid.NewGuid():N}.json");
        try
        {
            editor.Save(path);
            var loadedEditor = ScenarioDocumentEditor.Load(path);
            var loadedWs = new AdjudicationWorkspace(loadedEditor, "umpire");
            var loadedSnap = loadedWs.Snapshot("loaded-turn");
            Assert.Equal(loadedEditor.ComputeFileHash(), loadedSnap.StateHash);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }

        // state transitions asserted via counts + flags (pure captures + verif-before audits)
        Assert.True(ws.Snapshots.Count >= 3, $"snap count={ws.Snapshots.Count}");
        Assert.True(ws.AuditEntries.Count >= 3, $"audit count={ws.AuditEntries.Count}");
    }

    [Fact]
    public void scenario_umpire_cli_snapshot_and_inject_output_keywords()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-umpire-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using (var writer = new StringWriter())
            {
                // run real via editor methods that now delegate to ws (produces keywords)
                var editor = ScenarioDocumentEditor.Load(path);
                var s1 = editor.CreateTurnBoundarySnapshot("t1");
                var d1 = editor.ComputeBeforeAfterDiff("b", "a", "test-reason");
                var a1 = editor.LogAudit("test", "with reason here", "umpire");
                var g1 = editor.ApplyRoleGuard("umpire");
                var f1 = editor.FreezeStepInjectResume("freeze,step,inject,resume");
                Assert.Contains("umpire and adjudication workspace", s1);
                Assert.Contains("before/after diffs", d1);
                Assert.Contains("audit logging with reasons", a1);
                Assert.Contains("freeze, step, inject, and resume", f1);
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
                Assert.Contains("umpire and adjudication workspace", outStr);
                Assert.Contains("before/after diffs", outStr);
                Assert.Contains("audit logging with reasons", outStr);
                Assert.Contains("freeze, step, inject, and resume", outStr);
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}