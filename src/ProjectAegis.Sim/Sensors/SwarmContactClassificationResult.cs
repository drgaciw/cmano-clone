namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// Result of pure swarm contact classification (SWARM-26 / DRG-96).
/// Confidence is clamped to [0, 1]. ReasonCode is a stable machine-readable tag.
/// </summary>
public sealed record SwarmContactClassificationResult(
    SwarmContactClass Class,
    double Confidence,
    string ReasonCode);
