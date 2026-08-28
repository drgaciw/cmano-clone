using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.BdaAssess;

[TestFixture]
public sealed class BdaAssessProjectionTests
{
  [Test]
  public void Terminal_kill_with_lost_transition_maps_destroyed_after_active_picture_drops_contact()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
    log.AppendEngagementOutcome(Kill(2, 1, "hostile-1", 10));
    log.AppendContactChange(Change(2, "c1", "hostile-1", "Identified", "Lost"));

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 2);

    Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
    var row = snapshot.Contacts[0];
    Assert.That(row.ContactId, Is.EqualTo("c1"));
    Assert.That(row.TargetId, Is.EqualTo("hostile-1"));
    Assert.That(row.State, Is.EqualTo(BdaAssessStateKind.Destroyed));
    Assert.That(row.Source, Is.EqualTo(BdaAssessSourceKind.EngagementOutcome));
    Assert.That(row.SimTick, Is.EqualTo(2UL));
    Assert.That(row.SimTime, Is.EqualTo(2.0));
    Assert.That(row.CorrelationSequenceId, Is.EqualTo(2UL));
  }

  [Test]
  public void Terminal_hit_maps_damaged_from_platform_damage()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
    log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 75, PlatformDamageChangeReasonCodes.Hit, 1));

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 2);

    Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
    var row = snapshot.Contacts[0];
    Assert.That(row.State, Is.EqualTo(BdaAssessStateKind.Damaged));
    Assert.That(row.Source, Is.EqualTo(BdaAssessSourceKind.PlatformDamage));
    Assert.That(row.SimTick, Is.EqualTo(2UL));
    Assert.That(row.CorrelationSequenceId, Is.EqualTo(2UL));
  }

  [Test]
  public void Hit_at_max_damage_level_with_remaining_hp_maps_damaged_not_destroyed()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
    log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 25, PlatformDamageChangeReasonCodes.Hit, 3));

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 2);

    Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
    var row = snapshot.Contacts[0];
    Assert.That(row.State, Is.EqualTo(BdaAssessStateKind.Damaged));
    Assert.That(row.Source, Is.EqualTo(BdaAssessSourceKind.PlatformDamage));
    Assert.That(row.SimTick, Is.EqualTo(2UL));
  }

  [Test]
  public void Contact_without_bda_emits_none_explicitly()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 1);

    Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
    var row = snapshot.Contacts[0];
    Assert.That(row.State, Is.EqualTo(BdaAssessStateKind.None));
    Assert.That(row.Source, Is.EqualTo(BdaAssessSourceKind.None));
    Assert.That(row.ContactId, Is.EqualTo("c1"));
  }

  [Test]
  public void Unknown_lifecycle_emits_unknown_not_silent_empty()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Unknown"));

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 1);

    Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
    var row = snapshot.Contacts[0];
    Assert.That(row.State, Is.EqualTo(BdaAssessStateKind.Unknown));
    Assert.That(row.Source, Is.EqualTo(BdaAssessSourceKind.ContactLifecycle));
    Assert.That(row.ContactId, Is.EqualTo("c1"));
  }

  [Test]
  public void Pending_target_emits_in_progress_for_all_contacts_on_target()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
    log.AppendContactChange(Change(1, "c2", "hostile-1", "Unknown", "Classified"));

    var pending = new[]
    {
      new BdaAssessPendingTarget("hostile-1", 3, 3.0, 42),
    };

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 3, pending);

    Assert.That(snapshot.Contacts, Has.Count.EqualTo(2));
    Assert.That(snapshot.Contacts.All(c => c.State == BdaAssessStateKind.InProgress), Is.True);
    Assert.That(snapshot.Contacts.All(c => c.Source == BdaAssessSourceKind.PendingEngagement), Is.True);
    Assert.That(snapshot.Contacts.All(c => c.CorrelationSequenceId == 42UL), Is.True);
  }

  [Test]
  public void Terminal_bda_overrides_pending_in_progress()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
    log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 75, PlatformDamageChangeReasonCodes.Hit, 1));

    var pending = new[]
    {
      new BdaAssessPendingTarget("hostile-1", 3, 3.0, 42),
    };

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 3, pending);

    Assert.That(snapshot.Contacts[0].State, Is.EqualTo(BdaAssessStateKind.Damaged));
    Assert.That(snapshot.Contacts[0].Source, Is.EqualTo(BdaAssessSourceKind.PlatformDamage));
  }

  [Test]
  public void Bda_kill_fans_out_to_all_contacts_on_same_target()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c-ucav-1", "hostile-1", "Unknown", "Classified"));
    log.AppendContactChange(Change(1, "c-ucav-2", "hostile-1", "Unknown", "Classified"));
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
    log.AppendEngagementOutcome(Kill(2, 1, "hostile-1", 10));
    log.AppendContactChange(Change(2, "c-ucav-1", "hostile-1", "Classified", "Lost"));
    log.AppendContactChange(Change(2, "c-ucav-2", "hostile-1", "Classified", "Lost"));
    log.AppendContactChange(Change(2, "c1", "hostile-1", "Identified", "Lost"));

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 2);

    Assert.That(snapshot.Contacts, Has.Count.EqualTo(3));
    Assert.That(snapshot.Contacts.Select(c => c.ContactId), Is.EqualTo(new[] { "c-ucav-1", "c-ucav-2", "c1" }));
    Assert.That(snapshot.Contacts.All(c => c.State == BdaAssessStateKind.Destroyed), Is.True);
    Assert.That(snapshot.Contacts.All(c => c.Source == BdaAssessSourceKind.EngagementOutcome), Is.True);
    Assert.That(snapshot.Contacts.All(c => c.SimTick == 2UL), Is.True);
  }

  [Test]
  public void Escalating_damage_promotes_to_destroyed_only_when_hp_reaches_zero()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
    log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 75, PlatformDamageChangeReasonCodes.Hit, 1));
    log.AppendPlatformDamageChange(DamageChange(3, 2, "hostile-1", 75, 0, PlatformDamageChangeReasonCodes.Hit, 3));

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 3);

    Assert.That(snapshot.Contacts[0].State, Is.EqualTo(BdaAssessStateKind.Destroyed));
    Assert.That(snapshot.Contacts[0].Source, Is.EqualTo(BdaAssessSourceKind.PlatformDamage));
    Assert.That(snapshot.Contacts[0].SimTick, Is.EqualTo(3UL));
    Assert.That(snapshot.Contacts[0].CorrelationSequenceId, Is.EqualTo(3UL));
  }

  [Test]
  public void Catalog_kill_resolves_engagement_outcome_by_target_and_tick()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
    log.AppendEngagementOutcome(Kill(2, 1, "hostile-1", 10));
    log.AppendPlatformDamageChange(DamageChange(2, 2, "hostile-1", 100, 0, PlatformDamageChangeReasonCodes.Kill, 3));
    log.AppendContactChange(Change(2, "c1", "hostile-1", "Identified", "Lost"));

    var snapshot = BdaAssessProjection.Project(log, currentSimTick: 2);

    Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
    Assert.That(snapshot.Contacts[0].State, Is.EqualTo(BdaAssessStateKind.Destroyed));
    Assert.That(snapshot.Contacts[0].Source, Is.EqualTo(BdaAssessSourceKind.EngagementOutcome));
    Assert.That(snapshot.Contacts[0].CorrelationSequenceId, Is.EqualTo(2UL));
  }

  [Test]
  public void Fingerprint_is_identical_for_identical_inputs()
  {
    var log = new DecisionLog();
    log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Detected"));
    log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
    log.AppendPlatformDamageChange(DamageChange(2, 1, "hostile-1", 100, 50, PlatformDamageChangeReasonCodes.Hit, 2));

    var pending = new[]
    {
      new BdaAssessPendingTarget("hostile-2", 4, 4.0, 99),
    };

    var a = BdaAssessProjection.Project(log, currentSimTick: 4, pending);
    var b = BdaAssessProjection.Project(log, currentSimTick: 4, pending);

    Assert.That(BdaAssessProjection.ComputeFingerprint(a), Is.EqualTo(BdaAssessProjection.ComputeFingerprint(b)));
    Assert.That(a.Contacts.Select(c => c.ContactId), Is.EqualTo(new[] { "c1", "c2" }));
  }

  [Test]
  public void Dtos_omit_ui_derived_truth_fields()
  {
    var types = new[]
    {
      typeof(BdaAssessContactState),
      typeof(BdaAssessSnapshot),
      typeof(BdaAssessPendingTarget),
    };
    string[] forbidden = ["selection", "hover", "camera", "visible", "visibility", "selected"];

    foreach (var type in types)
    {
      foreach (var property in type.GetProperties())
      {
        var name = property.Name.ToLowerInvariant();
        Assert.That(
          forbidden.Any(token => name.Contains(token, StringComparison.Ordinal)),
          Is.False,
          $"{type.Name}.{property.Name} is UI-derived truth");
      }
    }
  }

  private static ContactChangeRecord Change(
    ulong tick,
    string contactId,
    string targetId,
    string previous,
    string next) =>
    new(0, tick, tick, "u1", contactId, targetId, previous, next);

  private static PlatformDamageChangeRecord DamageChange(
    ulong tick,
    ulong sequenceId,
    string targetId,
    double previousHp,
    double newHp,
    string reasonCode,
    int damageLevel) =>
    new(sequenceId, tick, tick, new TargetId(targetId), previousHp, newHp, reasonCode, damageLevel);

  private static EngagementOutcomeRecord Kill(
    ulong tick,
    ulong sequenceId,
    string victimTargetId,
    ulong engagementId) =>
    new(
      sequenceId,
      tick,
      tick,
      new TargetId("shooter-1"),
      new TargetId(victimTargetId),
      engagementId,
      EngagementOutcomeCodes.Kill,
      0.42);
}
