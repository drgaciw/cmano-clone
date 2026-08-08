namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Stable basemap layer identifiers (CMD-28.2).
/// UI-local presentation only — not sim state, not DecisionLog (ADR-010 exception).
/// </summary>
public enum MapLayerId
{
    Satellite = 0,
    Relief = 1,
    Borders = 2,
    Terrain = 3,
    Roads = 4,
    LandCover = 5,
    Placenames = 6,
    DayNight = 7,
}
