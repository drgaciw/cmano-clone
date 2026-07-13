using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// EventGraphPresenter (ME-W2 Track W2-d): headless nodes/edges, static codes, explain.
/// </summary>
public sealed class EventGraphPresenterTests
{
    [Test]
    public void Refresh_builds_nodes_from_session_events()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-eg-nodes-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = "evt-b",
                TriggerType = "Time",
                Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            });
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = "evt-a",
                TriggerType = "Time",
                Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            });
            editor.Save(path);

            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var presenter = new EventGraphPresenter(session, findings);

            presenter.Refresh();

            Assert.That(presenter.Nodes.Count, Is.EqualTo(2));
            Assert.That(presenter.Nodes[0].EventId, Is.EqualTo("evt-a"));
            Assert.That(presenter.Nodes[0].SequenceId, Is.EqualTo(0));
            Assert.That(presenter.Nodes[0].TriggerType, Is.EqualTo("Time"));
            Assert.That(presenter.Nodes[1].EventId, Is.EqualTo("evt-b"));
            Assert.That(presenter.Nodes[1].SequenceId, Is.EqualTo(1));
            Assert.That(presenter.Nodes.All(n => n.LastFired), Is.True);
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
    public void Refresh_builds_fire_order_edges_by_id_ordinal()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-eg-fire-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = "evt-c",
                TriggerType = "Time",
                Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            });
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = "evt-a",
                TriggerType = "Time",
                Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            });
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = "evt-b",
                TriggerType = "Time",
                Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            });
            editor.Save(path);

            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var presenter = new EventGraphPresenter(session, findings);

            presenter.Refresh();

            var fire = presenter.Edges.Where(e => e.Kind == "FireOrder").ToArray();
            Assert.That(fire.Length, Is.EqualTo(2));
            Assert.That(fire[0].FromEventId, Is.EqualTo("evt-a"));
            Assert.That(fire[0].ToEventId, Is.EqualTo("evt-b"));
            Assert.That(fire[1].FromEventId, Is.EqualTo("evt-b"));
            Assert.That(fire[1].ToEventId, Is.EqualTo("evt-c"));
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
    public void Refresh_builds_activate_mission_edges()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-eg-act-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.AddPatrolMission(
                "m-patrol",
                ["u1"],
                [
                    new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ]);
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = "evt-activate",
                TriggerType = "Time",
                Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                Actions =
                [
                    new ScenarioEventActionDto { Type = "ActivateMission", UnitId = "m-patrol" },
                ],
            });
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = "evt-complete",
                TriggerType = "MissionComplete",
                Conditions =
                [
                    new ScenarioEventConditionDto
                    {
                        Type = "MissionComplete",
                        UnitId = "m-patrol",
                        Result = true,
                    },
                ],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            });
            editor.Save(path);

            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var presenter = new EventGraphPresenter(session, findings);

            presenter.Refresh();

            var act = presenter.Edges.Where(e => e.Kind == "ActivateMission").ToArray();
            Assert.That(act.Length, Is.EqualTo(1));
            Assert.That(act[0].FromEventId, Is.EqualTo("evt-activate"));
            Assert.That(act[0].ToEventId, Is.EqualTo("evt-complete"));
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
    public void Refresh_static_codes_after_dead_trigger()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-eg-dead-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = "evt-dead",
                TriggerType = "UnitDestroyed",
                Conditions = [],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            });
            editor.Save(path);

            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var presenter = new EventGraphPresenter(session, findings);

            presenter.Refresh();

            Assert.That(presenter.StaticAnalysisCodes, Does.Contain(EventStaticAnalyzer.DeadTriggerCode));
            Assert.That(presenter.Nodes.Count, Is.EqualTo(1));
            Assert.That(presenter.Nodes[0].EventId, Is.EqualTo("evt-dead"));
            Assert.That(presenter.Nodes[0].LastFired, Is.False);
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
    public void ExplainSelected_json_has_event_id()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-eg-explain-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = "evt-explain",
                TriggerType = "Time",
                Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            });
            editor.Save(path);

            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var presenter = new EventGraphPresenter(session, findings);

            presenter.Refresh();
            presenter.SelectEvent("evt-explain");

            Assert.That(presenter.SelectedEventId, Is.EqualTo("evt-explain"));
            var json = presenter.ExplainSelected();
            Assert.That(json, Does.Contain("event_id"));
            Assert.That(json, Does.Contain("evt-explain"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
