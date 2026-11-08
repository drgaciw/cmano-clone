namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Camera altitude LOD bands for APP-6 map symbol clustering (REQ-20 Phase N / CMD-13/15).
/// Coarser bands (Overview) reduce symbol count via grid clustering; Close is identity.
/// </summary>
public enum MapLodBand
{
    /// <summary>High altitude overview — coarsest grid clustering.</summary>
    Overview = 0,

    /// <summary>Theater-scale altitude — coarse clustering.</summary>
    Theater = 1,

    /// <summary>Tactical altitude — finer grid clustering.</summary>
    Tactical = 2,

    /// <summary>Close-in altitude — 1:1 symbols, no clustering.</summary>
    Close = 3,
}
