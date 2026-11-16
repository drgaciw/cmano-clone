using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>
/// Optional per-sequence visibility metadata (fixture / side-tagged export).
/// Production order-log rows without annotations are treated as visible to every side.
/// </summary>
public sealed record EvidenceSourceAnnotation(
    ViewingSide Side,
    IReadOnlyList<ViewingSide> HiddenFrom);
