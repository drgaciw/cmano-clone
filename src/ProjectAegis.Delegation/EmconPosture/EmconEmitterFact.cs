namespace ProjectAegis.Delegation.EmconPosture;

using ProjectAegis.Sim.Policy;

/// <summary>Read-only per-emitter EMCON fact for posture projection input.</summary>
public sealed record EmconEmitterFact(string EmitterId, EmconState State);
