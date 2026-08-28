namespace ProjectAegis.Delegation.EmconPosture;

/// <summary>One emitter currently radiating (Active EMCON) in the advisory posture snapshot.</summary>
public sealed record RadiatingSensor(string EmitterId, string Label);
