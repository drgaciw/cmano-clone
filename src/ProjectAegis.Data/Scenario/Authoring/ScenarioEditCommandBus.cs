namespace ProjectAegis.Data.Scenario.Authoring;

using ProjectAegis.Data.Validation;

/// <summary>
/// Serializes scenario mutations through optimistic concurrency, undo capture, commit, and optional save.
/// Returns live validation findings after each successful mutation (Phase 2.1 / map-first authoring).
/// </summary>
public sealed class ScenarioEditCommandBus
{
    private readonly ScenarioAuthoringSession _session;

    /// <summary>Creates a command bus bound to the given authoring session.</summary>
    public ScenarioEditCommandBus(ScenarioAuthoringSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>Most recent live validation report from a successful mutation or <see cref="RefreshFindings"/>.</summary>
    public ValidationReport? LastReport { get; private set; }

    /// <summary>Places or replaces an ORBAT unit (map place / inspector apply).</summary>
    public ScenarioMutationResult PlaceUnit(int expectedEditVersion, ScenarioOrbatUnitDto unit, bool save)
        => Mutate(expectedEditVersion, save, e => e.UpsertOrbatUnit(unit));

    /// <summary>Moves an existing ORBAT unit to a new lat/lon.</summary>
    public ScenarioMutationResult MoveUnit(int expectedEditVersion, string unitId, double lat, double lon, bool save)
        => Mutate(expectedEditVersion, save, e => e.MoveOrbatUnit(unitId, lat, lon));

    /// <summary>Clones an existing unit under a new id at the given position.</summary>
    public ScenarioMutationResult CloneUnit(
        int expectedEditVersion,
        string sourceId,
        string newId,
        double lat,
        double lon,
        bool save)
        => Mutate(expectedEditVersion, save, e => e.CloneOrbatUnit(sourceId, newId, lat, lon));

    /// <summary>Inserts or replaces a reference point (map draw gesture-end).</summary>
    public ScenarioMutationResult UpsertReferencePoint(
        int expectedEditVersion,
        ScenarioReferencePointDto rp,
        bool save)
        => Mutate(expectedEditVersion, save, e => e.UpsertReferencePoint(rp));

    /// <summary>Attaches a Patrol mission from current map selection (units + zone vertices).</summary>
    public ScenarioMutationResult AttachPatrolFromSelection(
        int expectedEditVersion,
        string missionId,
        IReadOnlyList<string> unitIds,
        IReadOnlyList<ScenarioWaypointDto> zone,
        bool save)
        => Mutate(expectedEditVersion, save, e => e.AddPatrolMission(missionId, unitIds, zone));

    /// <summary>Re-runs live validation without mutating the document.</summary>
    public ValidationReport RefreshFindings()
    {
        LastReport = _session.Editor.LiveValidate();
        return LastReport;
    }

    /// <summary>
    /// Shared mutate pipeline: require editVersion → capture undo → action → persist undo →
    /// commit → dirty/save → live validate.
    /// On conflict, no file write and no document mutation occurs (guard throws first).
    /// </summary>
    private ScenarioMutationResult Mutate(
        int expectedEditVersion,
        bool save,
        Action<ScenarioDocumentEditor> action)
    {
        var editor = _session.Editor;
        try
        {
            editor.RequireEditVersion(expectedEditVersion, _session.Path);
            var snap = editor.CaptureUndoSnapshot();
            action(editor);
            editor.PersistUndoSnapshot(_session.Path, snap);
            editor.CommitMutation();
            _session.IsDirty = true;
            if (save)
            {
                _session.Save();
            }

            LastReport = editor.LiveValidate();
            return new ScenarioMutationResult
            {
                Ok = true,
                EditVersion = editor.Metadata.EditVersion,
                FileHash = editor.ComputeFileHash(),
                Report = LastReport,
            };
        }
        catch (ScenarioEditConflictException ex)
        {
            return new ScenarioMutationResult
            {
                Ok = false,
                ErrorCode = ex.Code,
                ErrorMessage = ex.Message,
                EditVersion = ex.CurrentEditVersion,
                FileHash = ex.FileHash,
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ScenarioMutationResult
            {
                Ok = false,
                ErrorCode = "INVALID_OPERATION",
                ErrorMessage = ex.Message,
                EditVersion = editor.Metadata.EditVersion,
            };
        }
    }
}
