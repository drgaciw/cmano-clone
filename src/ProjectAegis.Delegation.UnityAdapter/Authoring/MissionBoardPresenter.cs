namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless Mission Board view model (AME-3.4 / ME-W1): list/filter missions,
/// select a row, clone, and add from built-in templates via
/// <see cref="ScenarioAuthoringSession.Bus"/>. Does not depend on UnityEngine.
/// </summary>
public sealed class MissionBoardPresenter
{
    private readonly ScenarioAuthoringSession _session;
    private readonly LiveFindingsPresenter _findings;

    /// <summary>
    /// Creates a Mission Board presenter bound to the given authoring session and findings façade.
    /// </summary>
    /// <param name="session">Open scenario authoring session.</param>
    /// <param name="findings">Live findings presenter refreshed after successful mutations.</param>
    public MissionBoardPresenter(ScenarioAuthoringSession session, LiveFindingsPresenter findings)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _findings = findings ?? throw new ArgumentNullException(nameof(findings));
    }

    /// <summary>Optional type filter (Patrol|Strike|Ferry|Support); null = all.</summary>
    public string? TypeFilter { get; set; }

    /// <summary>Optional side filter matching derived row side; null = all.</summary>
    public string? SideFilter { get; set; }

    /// <summary>Optional status filter (Assigned|Unassigned); null = all.</summary>
    public string? StatusFilter { get; set; }

    /// <summary>Currently selected mission id, or null when none selected.</summary>
    public string? SelectedMissionId { get; private set; }

    /// <summary>Last board rows from <see cref="Refresh"/> (filtered/sorted).</summary>
    public IReadOnlyList<MissionBoardRow> Rows { get; private set; } = Array.Empty<MissionBoardRow>();

    /// <summary>
    /// Rebuilds <see cref="Rows"/> from the session document via
    /// <see cref="MissionBoardQuery.List"/> using current filters.
    /// </summary>
    public void Refresh()
    {
        Rows = MissionBoardQuery.List(
            _session.Editor.ToDto(),
            TypeFilter,
            SideFilter,
            StatusFilter);
    }

    /// <summary>Sets <see cref="SelectedMissionId"/> (may be null to clear selection).</summary>
    public void Select(string? missionId) => SelectedMissionId = missionId;

    /// <summary>
    /// Clones the selected mission under <paramref name="newMissionId"/> via the bus.
    /// Returns null when no mission is selected. On success, refreshes rows and findings.
    /// </summary>
    /// <param name="newMissionId">Id for the cloned mission.</param>
    /// <param name="save">When true, persists after the mutation (default true).</param>
    public ScenarioMutationResult? CloneSelected(string newMissionId, bool save = true)
    {
        if (string.IsNullOrWhiteSpace(SelectedMissionId))
        {
            return null;
        }

        var result = _session.Bus.CloneMission(
            _session.EditVersion,
            SelectedMissionId,
            newMissionId,
            save);
        if (result.Ok)
        {
            Refresh();
            _findings.RefreshImmediate();
        }

        return result;
    }

    /// <summary>
    /// Adds a mission from a built-in template via the bus. On success, refreshes rows and findings.
    /// </summary>
    /// <param name="templateId">Built-in template id (e.g. <c>tpl-patrol-empty</c>).</param>
    /// <param name="newMissionId">Id for the new mission.</param>
    /// <param name="save">When true, persists after the mutation (default true).</param>
    public ScenarioMutationResult AddFromTemplate(string templateId, string newMissionId, bool save = true)
    {
        var result = _session.Bus.AddFromTemplate(
            _session.EditVersion,
            templateId,
            newMissionId,
            save);
        if (result.Ok)
        {
            Refresh();
            _findings.RefreshImmediate();
        }

        return result;
    }
}
