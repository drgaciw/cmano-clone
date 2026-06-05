namespace ProjectAegis.Delegation.Projection;

public sealed record UnitDetailPanelState(
    string UnitIdLine,
    string StatusLine,
    string MagazineLine,
    string EmconLine,
    string DoctrineLine,
    string FuelLine,
    string EngagePreviewLine,
    string AttackOptionsLine,
    string ContactLine,
    IReadOnlyList<EngageAttackOptions.AttackOption> AttackMenu);