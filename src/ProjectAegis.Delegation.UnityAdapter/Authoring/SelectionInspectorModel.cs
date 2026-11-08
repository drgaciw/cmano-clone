namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless selection-inspector view model for map authoring (unit / reference-point summary).
/// Presentation only — does not mutate the scenario document.
/// </summary>
public sealed class SelectionInspectorModel
{
    /// <summary>Id of the currently selected ORBAT unit, if any.</summary>
    public string? SelectedUnitId { get; private set; }

    /// <summary>Id of the currently selected reference point, if any.</summary>
    public string? SelectedReferencePointId { get; private set; }

    /// <summary>One-line human-readable summary of the current selection.</summary>
    public string SummaryLine { get; private set; } = "";

    /// <summary>
    /// Selects a unit (clears reference-point selection). Pass <c>null</c> to clear.
    /// </summary>
    public void SetUnit(ScenarioOrbatUnitDto? u)
    {
        SelectedReferencePointId = null;
        if (u is null)
        {
            SelectedUnitId = null;
            SummaryLine = "";
            return;
        }

        SelectedUnitId = u.Id;
        SummaryLine = $"{u.Id} | {u.SideId} | {u.PlatformId} @ {u.Lat:F3},{u.Lon:F3}";
    }

    /// <summary>
    /// Selects a reference point (clears unit selection). Pass <c>null</c> to clear.
    /// </summary>
    public void SetReferencePoint(ScenarioReferencePointDto? rp)
    {
        SelectedUnitId = null;
        if (rp is null)
        {
            SelectedReferencePointId = null;
            SummaryLine = "";
            return;
        }

        SelectedReferencePointId = rp.Id;
        SummaryLine = $"{rp.Id} | {rp.Type} | verts={rp.Geometry.Count}";
    }
}
