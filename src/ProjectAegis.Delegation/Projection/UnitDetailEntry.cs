namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Right-panel unit detail for doc-20 C2 (read-only MVP).
/// CMD-17: optional <see cref="CommsLabel"/>.
/// S122-07 / DRG-182: optional <see cref="AuthorityLabel"/>; <see cref="DoctrineLabel"/> feeds ROE chrome.
/// </summary>
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
    IReadOnlyList<EngageAttackOptions.AttackOption> AttackMenu,
    string CommsLabel = "COMMS: —",
    string AuthorityLabel = "AUTH: —");
