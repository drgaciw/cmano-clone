using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// DRG-346 / DRG-371: ADR-010 §2-3, ADR-007, ADR-001, ADR-017 source-fence &amp; host wiring
/// contract tests for <c>ScenarioEditorShellHost.cs</c> and <c>ScenarioMapAuthoringWindow.cs</c>.
/// </summary>
[TestFixture]
public sealed class ScenarioEditorShellHostContractTests
{
    [Test]
    public void ScenarioEditorShellHost_wires_bounded_authoring_workflow_and_play_entry_under_adr_010_017_fences()
    {
        var source = UiIaSourceReader.ReadRuntime("ScenarioEditorShellHost.cs");

        // Bounded H4 authoring & PlayEntry wiring
        Assert.That(source, Does.Contain("BoundedAuthoringWorkflow"));
        Assert.That(source, Does.Contain("PlayEntrySession"));
        Assert.That(source, Does.Contain("AttachAuthoringWorkflow"));
        Assert.That(source, Does.Contain("DetachAuthoringWorkflow"));
        Assert.That(source, Does.Contain("DelegationBridgeHost"));
        Assert.That(source, Does.Contain("BindExternalSession"));
        Assert.That(source, Does.Contain("LoadRequested"));
        Assert.That(source, Does.Contain("SaveRequested"));
        Assert.That(source, Does.Contain("UndoRequested"));
        Assert.That(source, Does.Contain("RedoRequested"));
        Assert.That(source, Does.Contain("PlayRequested"));
        Assert.That(source, Does.Contain("SampleRequested"));
        Assert.That(source, Does.Contain("ScenarioEditorActionAvailability"));
        Assert.That(source, Does.Contain("scenario-editor-shell-map-summary"));

        // ADR-010 / ADR-017 / UnitySimImportForbidTests fences
        Assert.That(source, Does.Not.Contain("ProjectAegis.Sim"));
        Assert.That(source, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(source, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(source, Does.Not.Contain("DecisionLog"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("FindObjectOfType"));
        Assert.That(source, Does.Not.Contain("FindObjectsByType"));
        Assert.That(source, Does.Not.Contain("ADR-018"));
    }

    [Test]
    public void ScenarioMapAuthoringWindow_surfaces_generic_only_warnings_without_blocking_save_or_export()
    {
        var source = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "Editor", "ScenarioMapAuthoringWindow.cs");

        // DRG-371 GenericOnly capability disclosure in chrome + findings list
        Assert.That(source, Does.Contain("HasGenericExecutionWarnings"));
        Assert.That(source, Does.Contain("GenericExecutionFindings"));
        Assert.That(source, Does.Contain("CodeRoleGenericOnly"));
        Assert.That(source, Does.Contain("Export and Play remain allowed"));

        // Preserve ADR-010/017 and ScenarioMapAuthoringPanelUiTests invariants
        Assert.That(source, Does.Not.Contain("ValidationSeverity.Error"));
        Assert.That(source, Does.Not.Contain("saveButton.SetEnabled(gate"));
        Assert.That(source, Does.Not.Contain("DelegationBridge."));
        Assert.That(source, Does.Not.Contain("private void OnGUI()"));
        Assert.That(source, Does.Not.Contain("ADR-018"));
    }

    [Test]
    public void ScenarioMapAuthoringWindow_does_not_claim_export_allowed_when_gate_is_blocked()
    {
        var source = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "Editor", "ScenarioMapAuthoringWindow.cs");

        Assert.That(source, Does.Contain("!_findings.Gate.CanExport"));
        Assert.That(source, Does.Contain("non-blocking warning; export blocked by other errors"));
        Assert.That(source, Does.Not.Contain("lines.Add($\"[WARN] {f.Code} — {f.Message} (Export and Play remain allowed)\")"));
    }
}
