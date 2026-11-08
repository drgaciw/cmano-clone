using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>S125-04 / AUTH-04: Save draft and Export validated are visibly distinct verbs with distinct gates.</summary>
[TestFixture]
public sealed class EditorSaveExportProjectionTests
{
    [Test]
    public void Labels_tooltips_and_uss_classes_are_distinct()
    {
        var state = EditorSaveExportProjection.Bind(sessionOpen: true, isDirty: false, blockingFindingCount: 0);

        Assert.That(state.SaveLabel, Is.EqualTo("Save draft"));
        Assert.That(state.ExportLabel, Is.EqualTo("Export validated"));
        Assert.That(state.SaveLabel, Is.Not.EqualTo(state.ExportLabel));
        Assert.That(state.SaveTooltip, Is.Not.EqualTo(state.ExportTooltip));
        Assert.That(state.SaveUssClass, Does.Contain("editor-persist-action--save"));
        Assert.That(state.ExportUssClass, Does.Contain("editor-persist-action--export"));
    }

    [Test]
    public void Blocking_findings_keep_save_enabled_and_block_export()
    {
        var state = EditorSaveExportProjection.Bind(sessionOpen: true, isDirty: true, blockingFindingCount: 2);

        Assert.That(state.SaveEnabled, Is.True);
        Assert.That(state.ExportEnabled, Is.False);
        Assert.That(state.ExportBlocked, Is.True);
        Assert.That(state.ExportUssClass, Does.Contain("--blocked").And.Contain("--disabled"));
        Assert.That(state.ExportStatusText, Does.Contain("2 blocking findings").And.Contain("Save draft is still available"));
        Assert.That(state.SaveStatusText, Does.Contain("validation not required"));
    }

    [Test]
    public void No_session_disables_both()
    {
        var state = EditorSaveExportProjection.Bind(sessionOpen: false, isDirty: false, blockingFindingCount: 0);

        Assert.That(state.SaveEnabled, Is.False);
        Assert.That(state.ExportEnabled, Is.False);
        Assert.That(state.ExportBlocked, Is.False);
    }

    [Test]
    public void FromShell_uses_error_findings_as_the_export_gate()
    {
        var shell = ScenarioEditorShellProjection.Bind(sessionOpen: true, isDirty: true, errorFindingCount: 1, warningFindingCount: 3);

        var state = EditorSaveExportProjection.FromShell(shell);

        Assert.That(state.BlockingFindingCount, Is.EqualTo(1));
        Assert.That(state.SaveEnabled, Is.EqualTo(shell.SaveEnabled));
        Assert.That(state.ExportEnabled, Is.False);
    }

    [Test]
    public void Save_does_not_export_and_export_gate_reports_findings_end_to_end()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"aegis-s125-chrome-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var draftPath = Path.Combine(dir, "draft.json");
            var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
            editor.AddStrikeMission("strike-1", new[] { "u1" }, Array.Empty<string>());
            editor.Save(draftPath);
            var artifactPath = ScenarioSaveExportGate.ExportArtifactPathFor(draftPath);

            using var session = ScenarioAuthoringSession.Open(draftPath);
            var saved = ScenarioSaveExportGate.SaveDraft(session);
            var afterSave = EditorSaveExportProjection.Bind(true, session.IsDirty, saved.BlockingFindingCount, saved);

            Assert.That(saved.Saved, Is.True);
            Assert.That(saved.ExportArtifactWritten, Is.False);
            Assert.That(File.Exists(artifactPath), Is.False, "Save must not write the export artifact");
            Assert.That(afterSave.SaveStatusText, Does.Contain("not exported"));
            Assert.That(afterSave.ExportEnabled, Is.False);

            var exported = ScenarioSaveExportGate.Export(
                session.Editor.ToDto(),
                InMemoryCatalogReader.BalticPatrolFixture(),
                artifactPath,
                draftPath);
            var afterExport = EditorSaveExportProjection.Bind(true, false, exported.BlockingFindings.Count, saved, exported);

            Assert.That(exported.Allowed, Is.False);
            Assert.That(exported.BlockingFindings.Select(f => f.Code), Does.Contain("STRIKE_NO_TARGETS"));
            Assert.That(File.Exists(artifactPath), Is.False, "blocked export must not write an artifact");
            Assert.That(afterExport.LastExportArtifactPath, Is.Null);
            Assert.That(afterExport.ExportStatusText, Does.StartWith("Export blocked"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public void Successful_export_reports_artifact_path()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"aegis-s125-chrome-ok-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var draftPath = Path.Combine(dir, "ok.json");
            var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
            editor.Save(draftPath);
            var artifactPath = ScenarioSaveExportGate.ExportArtifactPathFor(draftPath);

            var exported = ScenarioSaveExportGate.Export(
                editor.ToDto(),
                InMemoryCatalogReader.BalticPatrolFixture(),
                artifactPath,
                draftPath);
            var state = EditorSaveExportProjection.Bind(true, false, 0, lastExport: exported);

            Assert.That(exported.Allowed, Is.True);
            Assert.That(state.LastExportArtifactPath, Is.EqualTo(artifactPath));
            Assert.That(state.ExportStatusText, Does.Contain(artifactPath));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
