namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// CMD-18: domain-appropriate discrete altitude/depth and throttle presets.
/// Catalog-backed later; Phase A uses fixed doctrinal tables per domain.
/// </summary>
public static class DomainPresetProjection
{
    public static IReadOnlyList<DomainPreset> AltitudePresets(PlatformDomain domain) =>
        domain switch
        {
            PlatformDomain.Subsurface => new[]
            {
                new DomainPreset("surface", "Surface", DomainPresetKind.Altitude),
                new DomainPreset("periscope", "Periscope depth", DomainPresetKind.Altitude),
                new DomainPreset("shallow", "Shallow", DomainPresetKind.Altitude),
                new DomainPreset("over_layer", "Just over the layer", DomainPresetKind.Altitude),
                new DomainPreset("under_layer", "Just under the layer", DomainPresetKind.Altitude),
                new DomainPreset("max_depth", "As deep as possible", DomainPresetKind.Altitude),
            },
            PlatformDomain.Air => new[]
            {
                new DomainPreset("deck", "Deck / low", DomainPresetKind.Altitude),
                new DomainPreset("low", "Low", DomainPresetKind.Altitude),
                new DomainPreset("medium", "Medium", DomainPresetKind.Altitude),
                new DomainPreset("high", "High", DomainPresetKind.Altitude),
                new DomainPreset("ceiling", "Ceiling", DomainPresetKind.Altitude),
            },
            PlatformDomain.Surface => new[]
            {
                new DomainPreset("surface", "Surface", DomainPresetKind.Altitude),
            },
            PlatformDomain.Ground => new[]
            {
                new DomainPreset("ground", "Ground", DomainPresetKind.Altitude),
            },
            _ => Array.Empty<DomainPreset>(),
        };

    public static IReadOnlyList<DomainPreset> ThrottlePresets(PlatformDomain domain) =>
        domain switch
        {
            PlatformDomain.Subsurface or PlatformDomain.Surface or PlatformDomain.Air => new[]
            {
                new DomainPreset("stop", "Stop", DomainPresetKind.Throttle),
                new DomainPreset("creep", "Creep", DomainPresetKind.Throttle),
                new DomainPreset("cruise", "Cruise", DomainPresetKind.Throttle),
                new DomainPreset("full", "Full", DomainPresetKind.Throttle),
                new DomainPreset("flank", "Flank", DomainPresetKind.Throttle),
            },
            PlatformDomain.Ground => new[]
            {
                new DomainPreset("halt", "Halt", DomainPresetKind.Throttle),
                new DomainPreset("march", "March", DomainPresetKind.Throttle),
                new DomainPreset("tactical", "Tactical", DomainPresetKind.Throttle),
            },
            _ => Array.Empty<DomainPreset>(),
        };

    public static DomainPresetPanel Project(string unitId, PlatformDomain domain) =>
        new(
            UnitId: unitId ?? string.Empty,
            Domain: domain,
            AltitudePresets: AltitudePresets(domain),
            ThrottlePresets: ThrottlePresets(domain));
}

public enum PlatformDomain
{
    Unknown = 0,
    Air,
    Surface,
    Subsurface,
    Ground,
}

public enum DomainPresetKind
{
    Altitude,
    Throttle,
}

public sealed record DomainPreset(string Id, string Label, DomainPresetKind Kind);

public sealed record DomainPresetPanel(
    string UnitId,
    PlatformDomain Domain,
    IReadOnlyList<DomainPreset> AltitudePresets,
    IReadOnlyList<DomainPreset> ThrottlePresets);
