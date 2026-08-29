using ProjectAegis.Delegation.EscalationGate;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.EscalationGate;

[TestFixture]
public sealed class EscalationGateProjectionTests
{
  [Test]
  public void Weapons_free_organic_targeting_emits_no_gate_rows()
  {
    var snapshot = EscalationGateProjection.Project(new EscalationGateInput(
        ContactOrOrderId: "c1",
        AuthorityContext: OrganicContext(
            roe: RoeLevel.WeaponsFree,
            lane: SkillLane.Read,
            commandId: null)));

    Assert.That(snapshot.IsOrder, Is.False);
    Assert.That(snapshot.Rows, Is.Empty);
    Assert.That(EscalationGateFingerprint.Compute(snapshot), Is.EqualTo("eg:empty"));
  }

  [Test]
  public void Hold_fire_emits_named_hold_fire_gate_with_roe_reason()
  {
    var snapshot = EscalationGateProjection.Project(new EscalationGateInput(
        ContactOrOrderId: "c-hold",
        AuthorityContext: OrganicContext(
            roe: RoeLevel.HoldFire,
            lane: SkillLane.Read,
            commandId: "engage")));

    Assert.That(snapshot.IsOrder, Is.False);
    Assert.That(snapshot.Rows, Has.Count.EqualTo(1));

    var row = snapshot.Rows[0];
    Assert.That(row.ContactOrOrderId, Is.EqualTo("c-hold"));
    Assert.That(row.GateCode, Is.EqualTo(EscalationGateCode.HoldFire));
    Assert.That(row.RequiredAuthority, Is.EqualTo(RequiredApproval.None));
    Assert.That(row.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonRoeHoldFire));
    Assert.That(row.IsOrder, Is.False);
    Assert.That(string.IsNullOrWhiteSpace(row.ReasonCode), Is.False);
    Assert.That(string.IsNullOrWhiteSpace(row.GateCode), Is.False);
  }

  [Test]
  public void Higher_hq_propose_engage_requires_weapons_release_gate()
  {
    var basis = OrganicAuthority(roeLabel: "WEAPONS_FREE");
    var snapshot = EscalationGateProjection.Project(new EscalationGateInput(
        ContactOrOrderId: "order-engage-1",
        AuthorityContext: C2AuthorityProjectionContext.FromEnvelope(
            basis,
            SkillLane.Propose,
            RequiredApproval.WeaponsRelease,
            commandId: "engage")));

    Assert.That(snapshot.IsOrder, Is.False);
    Assert.That(snapshot.Rows, Has.Count.EqualTo(1));

    var row = snapshot.Rows[0];
    Assert.That(row.ContactOrOrderId, Is.EqualTo("order-engage-1"));
    Assert.That(row.GateCode, Is.EqualTo(EscalationGateCode.HigherHq));
    Assert.That(row.RequiredAuthority, Is.EqualTo(RequiredApproval.WeaponsRelease));
    Assert.That(row.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonWeaponsReleaseRequired));
    Assert.That(row.IsOrder, Is.False);
    Assert.That(string.IsNullOrWhiteSpace(row.ReasonCode), Is.False);
  }

  [Test]
  public void Weapons_tight_emits_named_weapons_tight_gate()
  {
    var snapshot = EscalationGateProjection.Project(new EscalationGateInput(
        ContactOrOrderId: "c-tight",
        AuthorityContext: OrganicContext(
            roe: RoeLevel.WeaponsTight,
            lane: SkillLane.Read,
            commandId: null)));

    Assert.That(snapshot.IsOrder, Is.False);
    Assert.That(snapshot.Rows, Has.Count.EqualTo(1));

    var row = snapshot.Rows[0];
    Assert.That(row.GateCode, Is.EqualTo(EscalationGateCode.WeaponsTight));
    Assert.That(row.RequiredAuthority, Is.EqualTo(RequiredApproval.None));
    Assert.That(row.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonWeaponsTight));
    Assert.That(row.IsOrder, Is.False);
  }

  [Test]
  public void Higher_hq_operator_approval_emits_named_gate_for_hold_order()
  {
    var snapshot = EscalationGateProjection.Project(new EscalationGateInput(
        ContactOrOrderId: "order-hold-1",
        AuthorityContext: OrganicContext(
            roe: RoeLevel.WeaponsFree,
            lane: SkillLane.Propose,
            commandId: "hold",
            requiredApproval: RequiredApproval.Operator)));

    Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
    Assert.That(snapshot.Rows[0].GateCode, Is.EqualTo(EscalationGateCode.HigherHq));
    Assert.That(snapshot.Rows[0].RequiredAuthority, Is.EqualTo(RequiredApproval.Operator));
    Assert.That(snapshot.Rows[0].ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonApprovalRequired));
    Assert.That(snapshot.IsOrder, Is.False);
  }

  [Test]
  public void Multiple_inputs_sort_by_contact_or_order_id()
  {
    var snapshot = EscalationGateProjection.Project(new[]
    {
      new EscalationGateInput("c-z", OrganicContext(roe: RoeLevel.HoldFire)),
      new EscalationGateInput("c-a", OrganicContext(roe: RoeLevel.WeaponsTight)),
    });

    Assert.That(snapshot.Rows.Select(r => r.ContactOrOrderId), Is.EqualTo(new[] { "c-a", "c-z" }));
    Assert.That(snapshot.Rows.All(r => !r.IsOrder), Is.True);
    Assert.That(snapshot.IsOrder, Is.False);
  }

  [Test]
  public void Fingerprint_is_identical_for_identical_inputs()
  {
    var inputs = new[]
    {
      new EscalationGateInput("c1", OrganicContext(roe: RoeLevel.HoldFire)),
      new EscalationGateInput("c2", OrganicContext(roe: RoeLevel.WeaponsTight)),
    };

    var a = EscalationGateProjection.Project(inputs);
    var b = EscalationGateProjection.Project(inputs);

    Assert.That(EscalationGateFingerprint.Compute(a), Is.EqualTo(EscalationGateFingerprint.Compute(b)));
  }

  [Test]
  public void Null_or_empty_input_returns_empty_snapshot()
  {
    Assert.That(EscalationGateProjection.Project((EscalationGateInput?)null).Rows, Is.Empty);
    Assert.That(EscalationGateProjection.Project(Array.Empty<EscalationGateInput>()).Rows, Is.Empty);
    Assert.That(EscalationGateFingerprint.Compute(EscalationGateSnapshot.Empty), Is.EqualTo("eg:empty"));
  }

  private static C2AuthorityProjectionContext OrganicContext(
      RoeLevel roe = RoeLevel.WeaponsFree,
      SkillLane lane = SkillLane.Read,
      string? commandId = null,
      RequiredApproval requiredApproval = RequiredApproval.None) =>
      new(
          roe,
          lane,
          requiredApproval,
          TrackSource.Organic,
          FireControlSatisfied: true,
          commandId,
          HumanControlled: true);

  private static AuthorityBasis OrganicAuthority(string roeLabel = "WEAPONS_FREE") =>
      new(
          PolicySnapshotId: "policy-baltic-default",
          PolicyUnavailable: false,
          Roe: roeLabel,
          Emcon: "radar-active",
          TrackSource: TrackSource.Organic,
          FireControlSatisfied: true,
          EngagementAuthorizationImplied: false);
}
