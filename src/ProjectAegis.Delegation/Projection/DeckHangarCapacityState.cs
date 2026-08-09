namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Deck / hangar spot counts for an aviation host (CMD-24 Air Facilities presentation).
/// Simple occupancy model — does not invent LOG-08 deck-cycle timers.
/// </summary>
public sealed record DeckHangarCapacityState(
    string HostId,
    int DeckSpotsTotal,
    int DeckSpotsOccupied,
    int HangarSpotsTotal,
    int HangarSpotsOccupied,
    int ReadyOnDeck,
    int? SortieRatePerHour = null);
