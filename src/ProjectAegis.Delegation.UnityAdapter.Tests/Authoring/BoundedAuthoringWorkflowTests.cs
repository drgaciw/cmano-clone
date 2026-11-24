using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// DRG-346 ([H4-FIRST-SLICE] headless bounded pipeline):
/// OpenDraft -> SelectUnit -> PlaceOrEditUnit (with required field validation) -> Undo/Redo ->
/// Validate (bound catalog + DRG-345 + DRG-371) -> SaveDraft (ungated) -> ExportPackage ->
/// HandOffToPlay (PlayEntrySession) -> BuildShellState projection.
/// </summary>
[TestFixture]
public sealed class BoundedAuthoringWorkflowTests
{
    private string _tempDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aegis-drg346-bounded-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public void Supported_workflow_open_select_place_edit_undo_redo_save_export_and_handoff_to_play()
    {
        var draftPath = CopyFixture("bounded-authoring-supported.json");
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        using var workflow = new BoundedAuthoringWorkflow();

        var opened = workflow.OpenDraft(draftPath, catalog);
        Assert.That(opened.Succeeded, Is.True, opened.Message);
        Assert.That(opened.EditVersion, Is.GreaterThanOrEqualTo(0));
        Assert.That(opened.ValidationReport, Is.Not.Null);
        Assert.That(workflow.HasOpenSession, Is.True);
        Assert.That(workflow.CanUndo, Is.False);
        Assert.That(workflow.CanRedo, Is.False);
        Assert.That(workflow.Surface!.Units.Any(u => u.UnitId == "u1"), Is.True);

        // Select existing unit
        var selected = workflow.SelectUnit("u1");
        Assert.That(selected.Succeeded, Is.True, selected.Message);
        Assert.That(workflow.Surface.Selection.SelectedUnitId, Is.EqualTo("u1"));

        // Place new unit
        var placed = workflow.PlaceOrEditUnit(new BoundedUnitEditRequest
        {
            UnitId = "u2",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57.3,
            Lon = 20.4,
            AllowReplace = false,
        });
        Assert.That(placed.Succeeded, Is.True, placed.Message);
        Assert.That(workflow.Surface.Units.Any(u => u.UnitId == "u2"), Is.True);
        Assert.That(workflow.CanUndo, Is.True);
        Assert.That(workflow.CanRedo, Is.False);

        // Edit placed unit coordinates
        var edited = workflow.PlaceOrEditUnit(new BoundedUnitEditRequest
        {
            UnitId = "u2",
            SideId = "blue",
            PlatformId = "ddg",
            Lat = 57.5,
            Lon = 20.6,
            AllowReplace = true,
            SaveImmediately = true,
        });
        Assert.That(edited.Succeeded, Is.True, edited.Message);
        var u2 = workflow.Surface.Units.Single(u => u.UnitId == "u2");
        Assert.That(u2.PlatformId, Is.EqualTo("ddg"));
        Assert.That(u2.Lat, Is.EqualTo(57.5).Within(1e-6));

        // Undo reverts edit
        var undone = workflow.Undo();
        Assert.That(undone.Succeeded, Is.True, undone.Message);
        Assert.That(workflow.CanRedo, Is.True);
        u2 = workflow.Surface.Units.Single(u => u.UnitId == "u2");
        Assert.That(u2.PlatformId, Is.EqualTo("ffg"));
        Assert.That(u2.Lat, Is.EqualTo(57.3).Within(1e-6));

        // Redo reapplies edit
        var redone = workflow.Redo();
        Assert.That(redone.Succeeded, Is.True, redone.Message);
        Assert.That(workflow.CanRedo, Is.False);
        u2 = workflow.Surface.Units.Single(u => u.UnitId == "u2");
        Assert.That(u2.PlatformId, Is.EqualTo("ddg"));

        // Save draft & verify shell projection
        var saved = workflow.SaveDraft();
        Assert.That(saved.Saved, Is.True);
        Assert.That(saved.ExportArtifactWritten, Is.False);

        var shellSnapshot = workflow.BuildShellSnapshot();
        Assert.That(shellSnapshot.ShellState.SessionOpen, Is.True);
        Assert.That(shellSnapshot.SaveExportState, Is.Not.Null);
        Assert.That(shellSnapshot.Gate.CanExport, Is.True);
        Assert.That(shellSnapshot.Capabilities, Is.Not.Null);

        var shell = workflow.BuildShellState();
        Assert.That(shell.SessionOpen, Is.True);
        Assert.That(shell.SelectedEntityId, Is.EqualTo("u1"));
        Assert.That(shell.PlayEnabled, Is.True);
        Assert.That(shell.SaveEnabled, Is.True);
        Assert.That(shell.UndoEnabled, Is.True);
        Assert.That(shell.RedoEnabled, Is.False);

        // Export package writes .export.json
        var exported = workflow.ExportPackage();
        Assert.That(exported.Allowed, Is.True, exported.StatusText);
        Assert.That(exported.ArtifactPath, Is.Not.Null);
        Assert.That(File.Exists(exported.ArtifactPath!), Is.True);

        // HandOffToPlay loads exported package via PlayEntrySession, selects mode/side, enters Execution
        var playEntry = new PlayEntrySession(catalog: catalog);
        var handoff = workflow.HandOffToPlay(playEntry);
        Assert.That(handoff.Succeeded, Is.True, handoff.Message);
        Assert.That(handoff.PlayState, Is.Not.Null);
        Assert.That(handoff.ExportArtifactPath, Is.EqualTo(exported.ArtifactPath));
        Assert.That(playEntry.State.HasPackage, Is.True);
        Assert.That(playEntry.State.Mode, Is.EqualTo(SimulationModeKind.Mixed));
        Assert.That(playEntry.State.Side, Is.EqualTo(PlaySide.Friendly));
        Assert.That(playEntry.State.Phase, Is.EqualTo(SimulationPhase.Executing));
    }

    [Test]
    public void PlaceOrEditUnit_rejects_missing_required_fields_without_mutating_document()
    {
        var draftPath = CopyFixture("bounded-authoring-supported.json");
        using var workflow = new BoundedAuthoringWorkflow();
        workflow.OpenDraft(draftPath, InMemoryCatalogReader.BalticPatrolFixture());
        var versionBefore = workflow.Session!.EditVersion;

        var missingId = workflow.PlaceOrEditUnit(new BoundedUnitEditRequest
        {
            UnitId = "",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57.0,
            Lon = 20.0,
        });
        Assert.That(missingId.Succeeded, Is.False);
        Assert.That(missingId.ErrorCode, Is.Not.Null.And.Not.Empty);

        var missingPlatform = workflow.PlaceOrEditUnit(new BoundedUnitEditRequest
        {
            UnitId = "u3",
            SideId = "blue",
            PlatformId = "  ",
            Lat = 57.0,
            Lon = 20.0,
        });
        Assert.That(missingPlatform.Succeeded, Is.False);

        var badLat = workflow.PlaceOrEditUnit(new BoundedUnitEditRequest
        {
            UnitId = "u3",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 95.0,
            Lon = 20.0,
        });
        Assert.That(badLat.Succeeded, Is.False);
        Assert.That(workflow.Session.EditVersion, Is.EqualTo(versionBefore));
    }

    [Test]
    public void Invalid_draft_allows_save_blocks_export_and_play_and_recovers_when_fixed()
    {
        var draftPath = CopyFixture("bounded-authoring-invalid.json");
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        using var workflow = new BoundedAuthoringWorkflow();
        workflow.OpenDraft(draftPath, catalog);

        var initial = workflow.Validate();
        Assert.That(initial.BackendId, Is.Not.Empty);
        Assert.That(initial.Gate.CanExport, Is.False);
        Assert.That(initial.Gate.Codes, Does.Contain("MISSION_NO_UNITS"));

        // Save remains allowed on invalid draft
        var saveOutcome = workflow.SaveDraft();
        Assert.That(saveOutcome.Saved, Is.True);
        Assert.That(saveOutcome.BlockingFindingCount, Is.GreaterThanOrEqualTo(1));

        // Export and Play are blocked
        var blockedExport = workflow.ExportPackage();
        Assert.That(blockedExport.Allowed, Is.False);
        Assert.That(blockedExport.ArtifactPath, Is.Null);

        var playEntry = new PlayEntrySession(catalog: catalog);
        var blockedPlay = workflow.HandOffToPlay(playEntry);
        Assert.That(blockedPlay.Succeeded, Is.False);
        Assert.That(blockedPlay.ErrorCode, Is.Not.Null.And.Not.Empty);
        Assert.That(playEntry.State.HasPackage, Is.False);

        var blockedShell = workflow.BuildShellState();
        Assert.That(blockedShell.SaveEnabled, Is.True);
        Assert.That(blockedShell.PlayBlocked, Is.True);
        Assert.That(blockedShell.PlayEnabled, Is.False);
        Assert.That(blockedShell.ErrorFindingCount, Is.GreaterThanOrEqualTo(1));

        // Recover by fixing the missing mission unit assignment
        var recovered = workflow.AssignUnitToMission("patrol-1", "u1");
        Assert.That(recovered.Succeeded, Is.True, recovered.Message);

        var revalidated = workflow.Validate();
        Assert.That(revalidated.Gate.CanExport, Is.True);
        Assert.That(revalidated.Gate.ErrorCount, Is.Zero);

        var unblockedExport = workflow.ExportPackage();
        Assert.That(unblockedExport.Allowed, Is.True);
        Assert.That(File.Exists(unblockedExport.ArtifactPath!), Is.True);

        var unblockedPlay = workflow.HandOffToPlay(playEntry);
        Assert.That(unblockedPlay.Succeeded, Is.True, unblockedPlay.Message);
        Assert.That(playEntry.State.Phase, Is.EqualTo(SimulationPhase.Executing));
    }

    [Test]
    public void Unsupported_role_blocks_export_and_play_while_save_succeeds_and_generic_only_warning_does_not_block()
    {
        var unsupportedPath = CopyFixture("bounded-authoring-unsupported-role.json");
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        using var workflow = new BoundedAuthoringWorkflow();
        workflow.OpenDraft(unsupportedPath, catalog);

        var validation = workflow.Validate();
        Assert.That(validation.Gate.HasUnsupportedExecution, Is.True);
        Assert.That(validation.Gate.CanExport, Is.False);
        Assert.That(validation.Capabilities.Any(c => c.Execution == MissionRoleExecution.NotExecuted && c.BlocksExportAndPlay), Is.True);

        var save = workflow.SaveDraft();
        Assert.That(save.Saved, Is.True);

        var exportBlocked = workflow.ExportPackage();
        Assert.That(exportBlocked.Allowed, Is.False);

        var playEntry = new PlayEntrySession(catalog: catalog);
        var playBlocked = workflow.HandOffToPlay(playEntry, forceConfirmInvalid: true);
        Assert.That(playBlocked.Succeeded, Is.False);

        // GenericOnly backend on supported Patrol+Strike fixture emits Warning without blocking Export or Play
        var genericPath = CopyFixture("role-capability-generic-only.json");
        workflow.OpenDraft(genericPath, catalog, MissionRoleCapabilityManifest.CliSimulateBackendId);
        var genericValidation = workflow.Validate(MissionRoleCapabilityManifest.CliSimulateBackendId);
        Assert.That(genericValidation.Gate.HasUnsupportedExecution, Is.False);
        Assert.That(genericValidation.Gate.HasGenericExecutionWarnings, Is.True);
        Assert.That(genericValidation.Gate.CanExport, Is.True);
        Assert.That(genericValidation.Capabilities.All(c => c.Execution == MissionRoleExecution.GenericOnly && !c.BlocksExportAndPlay), Is.True);

        var genericExport = workflow.ExportPackage();
        Assert.That(genericExport.Allowed, Is.True);

        var genericPlay = workflow.HandOffToPlay(playEntry);
        Assert.That(genericPlay.Succeeded, Is.True, genericPlay.Message);
        Assert.That(playEntry.State.Phase, Is.EqualTo(SimulationPhase.Executing));
    }

    [Test]
    public void Validate_without_bound_catalog_surfaces_CATALOG_UNBOUND_instead_of_baltic_fallback()
    {
        var draftPath = CopyFixture("bounded-authoring-supported.json");
        using var workflow = new BoundedAuthoringWorkflow();
        workflow.OpenDraft(draftPath, catalog: null, autoResolveCatalogIfNull: false);

        var validation = workflow.Validate();

        Assert.That(validation.Gate.CanExport, Is.False);
        Assert.That(validation.Gate.Codes, Does.Contain(ScenarioSaveExportGate.CatalogUnboundFindingCode));
    }

    [Test]
    public void PlaceOrEditUnit_with_unknown_AssignToMissionId_does_not_leak_unit_bump_version_or_corrupt_undo_redo()
    {
        var draftPath = CopyFixture("bounded-authoring-supported.json");
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        using var workflow = new BoundedAuthoringWorkflow();
        workflow.OpenDraft(draftPath, catalog);

        // Place u2 and undo it so CanRedo is true before the failing call
        var placedU2 = workflow.PlaceOrEditUnit(new BoundedUnitEditRequest
        {
            UnitId = "u2",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57.3,
            Lon = 20.4,
            AllowReplace = false,
        });
        Assert.That(placedU2.Succeeded, Is.True);
        Assert.That(workflow.Undo().Succeeded, Is.True);
        Assert.That(workflow.CanRedo, Is.True);

        var versionBefore = workflow.Session!.EditVersion;
        var unitIdsBefore = workflow.Surface!.Units.Select(u => u.UnitId).ToArray();
        var undoCountBefore = ScenarioUndoStackStore.Count(draftPath);

        var failed = workflow.PlaceOrEditUnit(new BoundedUnitEditRequest
        {
            UnitId = "u3",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57.4,
            Lon = 20.5,
            AllowReplace = false,
            AssignToMissionId = "nonexistent-mission",
        });

        Assert.That(failed.Succeeded, Is.False);
        Assert.That(workflow.Session.EditVersion, Is.EqualTo(versionBefore));
        Assert.That(workflow.Surface.Units.Select(u => u.UnitId).ToArray(), Is.EqualTo(unitIdsBefore));
        Assert.That(ScenarioUndoStackStore.Count(draftPath), Is.EqualTo(undoCountBefore));
        Assert.That(workflow.CanRedo, Is.True);

        // Now commit a valid PlaceOrEditUnit and verify _redoStack clears immediately
        var committed = workflow.PlaceOrEditUnit(new BoundedUnitEditRequest
        {
            UnitId = "u3",
            SideId = "blue",
            PlatformId = "ffg",
            Lat = 57.4,
            Lon = 20.5,
            AllowReplace = false,
            AssignToMissionId = "patrol-1",
        });
        Assert.That(committed.Succeeded, Is.True, committed.Message);
        Assert.That(workflow.CanRedo, Is.False);
        Assert.That(workflow.Redo().ErrorCode, Is.EqualTo("NO_REDO"));
    }

    [TestCase(null)]
    [TestCase("   ")]
    [TestCase("unknown-catalog-ref")]
    public void OpenDraft_with_missing_blank_or_unknown_dbRef_surfaces_CATALOG_UNBOUND_instead_of_baltic_fallback(string? dbRef)
    {
        var source = ScenarioDocumentJsonLoader.LoadFromFile(FixturePath("bounded-authoring-supported.json"));
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                Title = source.Metadata.Title,
                DbRef = dbRef,
                DbSnapshotId = null,
                TlBranch = source.Metadata.TlBranch,
                Seed = source.Metadata.Seed,
                PolicyId = source.Metadata.PolicyId,
                EditVersion = source.Metadata.EditVersion,
            },
            Sides = source.Sides,
            Orbat = source.Orbat,
            Missions = source.Missions,
        };
        var draftPath = Path.Combine(_tempDir, $"no-dbref-{Guid.NewGuid():N}.json");
        ScenarioDocumentJsonWriter.WriteToFile(doc, draftPath);

        using var workflow = new BoundedAuthoringWorkflow();
        var opened = workflow.OpenDraft(draftPath, catalog: null, autoResolveCatalogIfNull: true);

        Assert.That(opened.Succeeded, Is.True, opened.Message);
        Assert.That(workflow.BoundCatalog, Is.Null);
        Assert.That(opened.Gate!.CanExport, Is.False);
        Assert.That(opened.Gate.Codes, Does.Contain(ScenarioSaveExportGate.CatalogUnboundFindingCode));
    }

    [Test]
    public void OpenDraft_with_explicit_baltic_dbRef_resolves_baltic_catalog_when_autoResolve_is_enabled()
    {
        var draftPath = CopyFixture("bounded-authoring-supported.json");
        using var workflow = new BoundedAuthoringWorkflow();

        var opened = workflow.OpenDraft(draftPath, catalog: null, autoResolveCatalogIfNull: true);

        Assert.That(opened.Succeeded, Is.True, opened.Message);
        Assert.That(workflow.BoundCatalog, Is.Not.Null);
        Assert.That(opened.Gate!.Codes, Does.Not.Contain(ScenarioSaveExportGate.CatalogUnboundFindingCode));
    }

    [Test]
    public void OpenDraft_with_bad_path_or_malformed_json_preserves_currently_open_session()
    {
        var validPath = CopyFixture("bounded-authoring-supported.json");
        var malformedPath = Path.Combine(_tempDir, "malformed.json");
        File.WriteAllText(malformedPath, "{ not valid json");

        using var workflow = new BoundedAuthoringWorkflow();
        var opened = workflow.OpenDraft(validPath, InMemoryCatalogReader.BalticPatrolFixture());
        Assert.That(opened.Succeeded, Is.True);
        workflow.SelectUnit("u1");
        var sessionBefore = workflow.Session;
        var versionBefore = workflow.Session!.EditVersion;

        var badJsonResult = workflow.OpenDraft(malformedPath);
        Assert.That(badJsonResult.Succeeded, Is.False);
        Assert.That(badJsonResult.ErrorCode, Is.EqualTo("OPEN_FAILED"));
        Assert.That(workflow.HasOpenSession, Is.True);
        Assert.That(workflow.Session, Is.SameAs(sessionBefore));
        Assert.That(workflow.Session!.EditVersion, Is.EqualTo(versionBefore));
        Assert.That(workflow.Surface!.Selection.SelectedUnitId, Is.EqualTo("u1"));
        Assert.That(workflow.BoundCatalog, Is.Not.Null);

        var missingResult = workflow.OpenDraft(Path.Combine(_tempDir, "does-not-exist.json"));
        Assert.That(missingResult.Succeeded, Is.False);
        Assert.That(missingResult.ErrorCode, Is.EqualTo("FILE_NOT_FOUND"));
        Assert.That(workflow.HasOpenSession, Is.True);
        Assert.That(workflow.Session, Is.SameAs(sessionBefore));
    }

    private string CopyFixture(string fileName)
    {
        var src = FixturePath(fileName);
        var dest = Path.Combine(_tempDir, $"{Guid.NewGuid():N}-{fileName}");
        File.Copy(src, dest, overwrite: true);
        return dest;
    }

    private static string FixturePath(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "data", "scenarios", "validation", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}
