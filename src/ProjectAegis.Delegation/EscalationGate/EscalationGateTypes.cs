namespace ProjectAegis.Delegation.EscalationGate;

using ProjectAegis.Delegation.Skills;

/// <summary>
/// DRG-228: one contact or order subject to an escalation / approval gate.
/// Advisory only — <see cref="IsOrder"/> is always false.
/// </summary>
public sealed record EscalationGateInput(
    string ContactOrOrderId,
    C2AuthorityProjectionContext AuthorityContext);

/// <summary>
/// One presentation-facing escalation / approval gate row. Sim-clock only — no UI state.
/// Advisory only — never an order side effect.
/// </summary>
public sealed record EscalationGateRow(
    string ContactOrOrderId,
    string GateCode,
    RequiredApproval RequiredAuthority,
    string ReasonCode,
    bool IsOrder);

/// <summary>Ordered, replay-stable escalation gate ledger for contacts or orders.</summary>
public sealed record EscalationGateSnapshot(
    IReadOnlyList<EscalationGateRow> Rows,
    bool IsOrder)
{
  /// <summary>Empty ledger — advisory only, never an order.</summary>
  public static EscalationGateSnapshot Empty { get; } =
      new(Array.Empty<EscalationGateRow>(), IsOrder: false);
}
