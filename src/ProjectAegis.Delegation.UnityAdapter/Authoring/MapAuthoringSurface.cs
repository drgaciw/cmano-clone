namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using System.Linq;
using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// ORBAT glyph view model for map presentation (lat/lon + identity fields).
/// </summary>
public sealed class OrbatGlyphView
{
    /// <summary>Unit id (ORBAT key).</summary>
    public string UnitId { get; init; } = "";

    /// <summary>Owning side id.</summary>
    public string SideId { get; init; } = "";

    /// <summary>Catalog / platform id.</summary>
    public string PlatformId { get; init; } = "";

    /// <summary>Latitude degrees.</summary>
    public double Lat { get; init; }

    /// <summary>Longitude degrees.</summary>
    public double Lon { get; init; }
}

/// <summary>
/// Reference-point geometry view model, including map-invalid marking via
/// <see cref="ScenarioGeometryValidity"/> (not a Validation Engine finding).
/// </summary>
public sealed class ReferencePointView
{
    /// <summary>Reference-point id.</summary>
    public string Id { get; init; } = "";

    /// <summary>Geometry type (point / line / corridor / polygon / circle).</summary>
    public string Type { get; init; } = "point";

    /// <summary>Vertex list.</summary>
    public IReadOnlyList<ScenarioWaypointDto> Geometry { get; init; } = Array.Empty<ScenarioWaypointDto>();

    /// <summary>Optional radius for circle types (nautical miles).</summary>
    public double? RadiusNm { get; init; }

    /// <summary>True when geometry satisfies type rules.</summary>
    public bool IsGeometryValid { get; init; }

    /// <summary>Human-readable invalid reason when <see cref="IsGeometryValid"/> is false.</summary>
    public string? InvalidReason { get; init; }
}

/// <summary>
/// Headless map presentation + gesture staging for Phase 2.1 authoring.
/// Commits only via <see cref="ScenarioAuthoringSession.Bus"/> — never mutates the document directly.
/// Invalid reference-point geometry still commits (visible + marked invalid on rebuild).
/// </summary>
public sealed class MapAuthoringSurface
{
    private readonly ScenarioAuthoringSession _session;
    private ScenarioOrbatUnitDto? _tentativeUnit;
    private ScenarioReferencePointDto? _tentativeRp;

    /// <summary>Creates a map surface bound to the given authoring session.</summary>
    public MapAuthoringSurface(ScenarioAuthoringSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>Committed ORBAT glyphs rebuilt from the document.</summary>
    public IReadOnlyList<OrbatGlyphView> Units { get; private set; } = Array.Empty<OrbatGlyphView>();

    /// <summary>Committed reference points with validity flags rebuilt from the document.</summary>
    public IReadOnlyList<ReferencePointView> ReferencePoints { get; private set; } = Array.Empty<ReferencePointView>();

    /// <summary>Selection inspector model (unit / RP summary).</summary>
    public SelectionInspectorModel Selection { get; } = new();

    /// <summary>Current place-unit gesture payload, if any (not yet committed).</summary>
    public ScenarioOrbatUnitDto? TentativeUnit => _tentativeUnit;

    /// <summary>Current draw-reference-point gesture payload, if any (not yet committed).</summary>
    public ScenarioReferencePointDto? TentativeReferencePoint => _tentativeRp;

    /// <summary>
    /// Rebuilds glyph and reference-point view models from <see cref="ScenarioDocumentEditor.ToDto"/>.
    /// Applies <see cref="ScenarioGeometryValidity"/> so invalid geometry remains visible and flagged.
    /// </summary>
    public void RebuildFromDocument()
    {
        var dto = _session.Editor.ToDto();
        Units = (dto.Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>())
            .OrderBy(u => u.Id, StringComparer.Ordinal)
            .Select(u => new OrbatGlyphView
            {
                UnitId = u.Id,
                SideId = u.SideId,
                PlatformId = u.PlatformId,
                Lat = u.Lat,
                Lon = u.Lon,
            })
            .ToArray();

        ReferencePoints = dto.ReferencePoints
            .OrderBy(p => p.Id, StringComparer.Ordinal)
            .Select(p =>
            {
                var valid = ScenarioGeometryValidity.IsValid(p, out var reason);
                return new ReferencePointView
                {
                    Id = p.Id,
                    Type = p.Type,
                    Geometry = p.Geometry,
                    RadiusNm = p.RadiusNm,
                    IsGeometryValid = valid,
                    InvalidReason = reason,
                };
            })
            .ToArray();
    }

    /// <summary>Starts a place-unit gesture with a tentative unit DTO (not committed).</summary>
    public void BeginPlaceUnit(ScenarioOrbatUnitDto tentative)
    {
        _tentativeUnit = tentative ?? throw new ArgumentNullException(nameof(tentative));
        _tentativeRp = null;
    }

    /// <summary>Cancels any in-progress place/draw gesture without mutating the document.</summary>
    public void CancelGesture()
    {
        _tentativeUnit = null;
        _tentativeRp = null;
    }

    /// <summary>
    /// Commits the current place-unit gesture via the session bus.
    /// Returns <c>null</c> when no tentative unit is staged.
    /// </summary>
    public ScenarioMutationResult? CommitPlaceUnit(bool save = true)
    {
        if (_tentativeUnit is null)
        {
            return null;
        }

        var result = _session.Bus.PlaceUnit(_session.EditVersion, _tentativeUnit, save);
        _tentativeUnit = null;
        if (result.Ok)
        {
            RebuildFromDocument();
        }

        return result;
    }

    /// <summary>Starts a draw-reference-point gesture with a tentative RP DTO (not committed).</summary>
    public void BeginDrawReferencePoint(ScenarioReferencePointDto tentative)
    {
        _tentativeRp = tentative ?? throw new ArgumentNullException(nameof(tentative));
        _tentativeUnit = null;
    }

    /// <summary>
    /// Commits the current reference-point gesture via the session bus.
    /// Invalid geometry still commits (visible + marked invalid on rebuild) per design.
    /// Returns <c>null</c> when no tentative RP is staged.
    /// </summary>
    public ScenarioMutationResult? CommitReferencePoint(bool save = true)
    {
        if (_tentativeRp is null)
        {
            return null;
        }

        // Invalid geometry still commits (visible + marked invalid) per design §10
        var result = _session.Bus.UpsertReferencePoint(_session.EditVersion, _tentativeRp, save);
        _tentativeRp = null;
        if (result.Ok)
        {
            RebuildFromDocument();
        }

        return result;
    }

    /// <summary>
    /// Selects an ORBAT unit by id and populates <see cref="Selection"/>.
    /// Unknown ids clear the unit selection.
    /// </summary>
    public void SelectUnit(string unitId)
    {
        var units = _session.Editor.ToDto().Orbat?.Units;
        var match = units?.FirstOrDefault(x =>
            string.Equals(x.Id, unitId, StringComparison.OrdinalIgnoreCase));
        Selection.SetUnit(match);
    }

    /// <summary>
    /// Selects a reference point by id and populates <see cref="Selection"/>.
    /// Unknown ids clear the reference-point selection.
    /// </summary>
    public void SelectReferencePoint(string referencePointId)
    {
        var match = _session.Editor.ToDto().ReferencePoints
            .FirstOrDefault(x => string.Equals(x.Id, referencePointId, StringComparison.OrdinalIgnoreCase));
        Selection.SetReferencePoint(match);
    }
}
