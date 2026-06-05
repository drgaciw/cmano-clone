namespace ProjectAegis.Delegation.Projection;

/// <summary>Right-panel unit detail for doc-20 C2 (read-only MVP).</summary>
public sealed record UnitDetailEntry(
    string UnitId,
    bool IsAlive,
    string StatusLabel,
    string MagazineLabel,
    string EmconLabel,
    string DoctrineLabel,
    string FuelLabel,
    string EngagePreviewLabel,
    string AttackOptionsLabel,
    IReadOnlyList<EngageAttackOptions.AttackOption> AttackMenu);