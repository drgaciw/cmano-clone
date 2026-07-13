using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// MissionBoardPresenter (ME-W1 / AME-3.4): list, filter, clone, template via session bus.
/// </summary>
public sealed class MissionBoardPresenterTests
{
    [Test]
    public void Refresh_lists_missions_from_session()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mb-refresh-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var presenter = new MissionBoardPresenter(session, findings);

            var add = presenter.AddFromTemplate("tpl-patrol-empty", "patrol-1", save: true);
            Assert.That(add.Ok, Is.True);

            presenter.Refresh();

            Assert.That(presenter.Rows.Count, Is.EqualTo(1));
            Assert.That(presenter.Rows[0].Id, Is.EqualTo("patrol-1"));
            Assert.That(presenter.Rows[0].Type, Is.EqualTo("Patrol"));
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
    public void Filter_type_patrol_only()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mb-filter-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var presenter = new MissionBoardPresenter(session, findings);

            Assert.That(presenter.AddFromTemplate("tpl-patrol-empty", "patrol-1", save: true).Ok, Is.True);
            Assert.That(presenter.AddFromTemplate("tpl-strike-empty", "strike-1", save: true).Ok, Is.True);

            presenter.TypeFilter = "Patrol";
            presenter.Refresh();

            Assert.That(presenter.Rows.Count, Is.EqualTo(1));
            Assert.That(presenter.Rows[0].Id, Is.EqualTo("patrol-1"));
            Assert.That(presenter.Rows[0].Type, Is.EqualTo("Patrol"));
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
    public void Clone_selected_updates_rows_and_findings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mb-clone-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var presenter = new MissionBoardPresenter(session, findings);

            Assert.That(presenter.AddFromTemplate("tpl-patrol-empty", "patrol-1", save: true).Ok, Is.True);
            Assert.That(findings.LastCodes, Does.Contain("MISSION_NO_UNITS"));

            presenter.Select("patrol-1");
            var clone = presenter.CloneSelected("patrol-1-copy", save: true);
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone!.Ok, Is.True);

            Assert.That(presenter.Rows.Count, Is.EqualTo(2));
            Assert.That(presenter.Rows.Select(r => r.Id), Is.EquivalentTo(new[] { "patrol-1", "patrol-1-copy" }));
            Assert.That(findings.LastCodes, Does.Contain("MISSION_NO_UNITS"));
            Assert.That(findings.LastReport, Is.Not.Null);
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
    public void Add_from_template_refresh_findings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mb-tpl-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var presenter = new MissionBoardPresenter(session, findings);

            Assert.That(findings.LastCodes, Is.Empty);

            var add = presenter.AddFromTemplate("tpl-patrol-empty", "p-empty", save: true);
            Assert.That(add.Ok, Is.True);
            Assert.That(presenter.Rows.Count, Is.EqualTo(1));
            Assert.That(findings.LastCodes, Is.Not.Null);
            Assert.That(findings.LastCodes, Does.Contain("MISSION_NO_UNITS"));
            Assert.That(findings.LastReport, Is.Not.Null);
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
