namespace ProjectAegis.Delegation.EscalationGate;

/// <summary>
/// DRG-228: stable named escalation / approval gate codes. Every gated row carries one —
/// never silent.
/// </summary>
public static class EscalationGateCode
{
  public const string HoldFire = "HOLD_FIRE";
  public const string WeaponsTight = "WEAPONS_TIGHT";
  public const string HigherHq = "HIGHER_HQ";
}
