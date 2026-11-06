namespace ProjectAegis.Delegation.EmconPosture;

using Comms;
using ProjectAegis.Sim.Policy;

/// <summary>Read-only facts for headless EMCON/emissions posture projection (DRG-221).</summary>
public sealed record EmconPostureInput(
    string UnitId,
    EmconState EmconLevel,
    CommsState CommsState = CommsState.Nominal,
    IReadOnlyList<EmconEmitterFact>? Emitters = null);
