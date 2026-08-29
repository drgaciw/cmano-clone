namespace ProjectAegis.Delegation.EscalationGate;

using ProjectAegis.Delegation.Skills;
using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-228: projects C2 authority / ROE facts into a deterministic advisory escalation gate ledger.
/// Presentation-only — never enqueues orders, resolves combat, or issues fire.
/// </summary>
public static class EscalationGateProjection
{
  /// <summary>
  /// Projects a single contact or order gate row when authority facts require escalation or
  /// approval. Returns <see cref="EscalationGateSnapshot.Empty"/> when no gate applies
  /// (e.g. weapons-free with permitted targeting).
  /// </summary>
  public static EscalationGateSnapshot Project(EscalationGateInput? input)
  {
    if (input is null || string.IsNullOrWhiteSpace(input.ContactOrOrderId))
    {
      return EscalationGateSnapshot.Empty;
    }

    var authorityContext = input.AuthorityContext;
    var authority = C2AuthorityProjector.Project(in authorityContext);
    var row = ResolveGateRow(input.ContactOrOrderId, authority);
    if (row is null)
    {
      return EscalationGateSnapshot.Empty;
    }

    return new EscalationGateSnapshot(new[] { row }, IsOrder: false);
  }

  /// <summary>
  /// Projects gate rows for each supplied input. Rows are sorted by contact or order id
  /// (ordinal). Every row and the snapshot carry <c>IsOrder=false</c>.
  /// </summary>
  public static EscalationGateSnapshot Project(IReadOnlyList<EscalationGateInput>? inputs)
  {
    if (inputs is null || inputs.Count == 0)
    {
      return EscalationGateSnapshot.Empty;
    }

    var rows = new List<EscalationGateRow>(inputs.Count);
    for (var i = 0; i < inputs.Count; i++)
    {
      var input = inputs[i];
      if (string.IsNullOrWhiteSpace(input.ContactOrOrderId))
      {
        continue;
      }

      var authorityContext = input.AuthorityContext;
      var authority = C2AuthorityProjector.Project(in authorityContext);
      var row = ResolveGateRow(input.ContactOrOrderId, authority);
      if (row is not null)
      {
        rows.Add(row);
      }
    }

    if (rows.Count == 0)
    {
      return EscalationGateSnapshot.Empty;
    }

    rows.Sort(static (a, b) =>
        string.Compare(a.ContactOrOrderId, b.ContactOrOrderId, StringComparison.Ordinal));

    return new EscalationGateSnapshot(rows, IsOrder: false);
  }

  private static EscalationGateRow? ResolveGateRow(
      string contactOrOrderId,
      C2AuthorityProjection authority)
  {
    if (authority.Roe.Roe == RoeLevel.HoldFire)
    {
      return BuildRow(
          contactOrOrderId,
          EscalationGateCode.HoldFire,
          RequiredApproval.None,
          authority.Roe.TargetingReasonCode ?? C2AuthorityProjector.ReasonRoeHoldFire);
    }

    if (authority.Roe.Roe == RoeLevel.WeaponsTight)
    {
      return BuildRow(
          contactOrOrderId,
          EscalationGateCode.WeaponsTight,
          RequiredApproval.None,
          authority.Roe.TargetingReasonCode ?? C2AuthorityProjector.ReasonWeaponsTight);
    }

    if (authority.Targeting.Disposition == C2AuthorityDisposition.ApprovalRequired)
    {
      var required = authority.Targeting.PendingApproval ?? RequiredApproval.Operator;
      var reason = authority.Targeting.ReasonCode
          ?? (required == RequiredApproval.WeaponsRelease
              ? C2AuthorityProjector.ReasonWeaponsReleaseRequired
              : C2AuthorityProjector.ReasonApprovalRequired);
      return BuildRow(contactOrOrderId, EscalationGateCode.HigherHq, required, reason);
    }

    return null;
  }

  private static EscalationGateRow BuildRow(
      string contactOrOrderId,
      string gateCode,
      RequiredApproval requiredAuthority,
      string reasonCode) =>
      new(
          contactOrOrderId,
          gateCode,
          requiredAuthority,
          reasonCode,
          IsOrder: false);
}
