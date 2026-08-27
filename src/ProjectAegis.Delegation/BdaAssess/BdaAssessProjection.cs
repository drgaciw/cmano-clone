namespace ProjectAegis.Delegation.BdaAssess;

using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;

/// <summary>
/// DRG-216: folds order-log BDA rows (via <see cref="OrderLogBdaProjection"/>) and optional
/// pending-assessment facts into a deterministic per-contact assess snapshot. Presentation-only —
/// does not enqueue orders, resolve combat, or read UI state.
/// </summary>
public static class BdaAssessProjection
{
  /// <summary>
  /// Projects BDA assess state for every contact in the sensor picture. Contacts without terminal
  /// BDA emit <see cref="BdaAssessStateKind.None"/> or <see cref="BdaAssessStateKind.Unknown"/>
  /// explicitly — never a silent omission.
  /// </summary>
  public static BdaAssessSnapshot Project(
    DecisionLog? log,
    ulong currentSimTick,
    IReadOnlyList<BdaAssessPendingTarget>? pendingTargets = null)
  {
    _ = currentSimTick;

    if (log is null)
    {
      return BdaAssessSnapshot.Empty;
    }

    var picture = ContactPictureProjection.Project(log);
    if (picture.Count == 0)
    {
      return BdaAssessSnapshot.Empty;
    }

    var contactsByTarget = BuildContactsByTarget(picture);
    var byTarget = BuildRepresentativeContactsByTarget(contactsByTarget);
    var bdaPerTarget = OrderLogBdaProjection.ProjectBdaContactChanges(log, byTarget);
    var bdaFanned = FanOutBdaContactChanges(bdaPerTarget, contactsByTarget);
    var terminalByContact = BuildTerminalAssessByContact(log, bdaFanned);
    var pendingByTarget = BuildPendingByTarget(pendingTargets);

    var rows = new List<BdaAssessContactState>(picture.Count);
    for (var i = 0; i < picture.Count; i++)
    {
      var contact = picture[i];
      if (terminalByContact.TryGetValue(contact.ContactId, out var terminal))
      {
        rows.Add(new BdaAssessContactState(
          contact.ContactId,
          contact.TargetId,
          contact.ObserverId,
          terminal.State,
          terminal.Source,
          terminal.SimTick,
          terminal.SimTime,
          terminal.CorrelationSequenceId));
        continue;
      }

      if (pendingByTarget.TryGetValue(contact.TargetId, out var pending))
      {
        rows.Add(new BdaAssessContactState(
          contact.ContactId,
          contact.TargetId,
          contact.ObserverId,
          BdaAssessStateKind.InProgress,
          BdaAssessSourceKind.PendingEngagement,
          pending.SimTick,
          pending.SimTime,
          pending.CorrelationSequenceId));
        continue;
      }

      if (IsUnknownLifecycle(contact.LifecycleState))
      {
        rows.Add(new BdaAssessContactState(
          contact.ContactId,
          contact.TargetId,
          contact.ObserverId,
          BdaAssessStateKind.Unknown,
          BdaAssessSourceKind.ContactLifecycle,
          contact.LastSimTick,
          contact.LastSimTime,
          0));
        continue;
      }

      rows.Add(new BdaAssessContactState(
        contact.ContactId,
        contact.TargetId,
        contact.ObserverId,
        BdaAssessStateKind.None,
        BdaAssessSourceKind.None,
        contact.LastSimTick,
        contact.LastSimTime,
        0));
    }

    rows.Sort(static (a, b) => string.Compare(a.ContactId, b.ContactId, StringComparison.Ordinal));
    return new BdaAssessSnapshot(rows);
  }

  /// <summary>
  /// Replay-stable canonical form: same log + pending facts yield the same string.
  /// Invariant culture; no wall clock.
  /// </summary>
  public static string ComputeFingerprint(BdaAssessSnapshot? snapshot)
  {
    if (snapshot is null || snapshot.Contacts.Count == 0)
    {
      return "bda:empty";
    }

    var builder = new StringBuilder();
    builder.Append("bda:c=");
    builder.Append(snapshot.Contacts.Count);
    for (var i = 0; i < snapshot.Contacts.Count; i++)
    {
      var row = snapshot.Contacts[i];
      builder.Append('|');
      builder.Append(row.ContactId);
      builder.Append(',');
      builder.Append(row.TargetId);
      builder.Append(',');
      builder.Append(row.ObserverId);
      builder.Append(',');
      builder.Append((int)row.State);
      builder.Append(',');
      builder.Append((int)row.Source);
      builder.Append(',');
      builder.Append(row.SimTick);
      builder.Append(',');
      builder.Append(row.SimTime.ToString("R", CultureInfo.InvariantCulture));
      builder.Append(',');
      builder.Append(row.CorrelationSequenceId);
    }

    return builder.ToString();
  }

  private static Dictionary<string, TerminalAssess> BuildTerminalAssessByContact(
    DecisionLog log,
    IReadOnlyList<ContactChangeRecord> bdaChanges)
  {
    var byContact = new Dictionary<string, TerminalAssess>(StringComparer.Ordinal);
    for (var i = 0; i < bdaChanges.Count; i++)
    {
      var change = bdaChanges[i];
      var state = MapDamageState(change.NewState);
      if (state is null)
      {
        continue;
      }

      byContact[change.ContactId] = new TerminalAssess(
        state.Value,
        ResolveSource(log, change),
        change.SimTick,
        change.SimTime,
        change.SequenceId);
    }

    return byContact;
  }

  private static BdaAssessStateKind? MapDamageState(string lifecycleState)
  {
    if (string.Equals(lifecycleState, BdaContactDamageStates.Lost, StringComparison.Ordinal))
    {
      return BdaAssessStateKind.Destroyed;
    }

    if (string.Equals(lifecycleState, BdaContactDamageStates.DegradedL1, StringComparison.Ordinal)
      || string.Equals(lifecycleState, BdaContactDamageStates.DegradedL2, StringComparison.Ordinal))
    {
      return BdaAssessStateKind.Damaged;
    }

    return null;
  }

  private static BdaAssessSourceKind ResolveSource(DecisionLog log, ContactChangeRecord change)
  {
    for (var i = 0; i < log.EngagementOutcomes.Count; i++)
    {
      var outcome = log.EngagementOutcomes[i];
      if (outcome.SequenceId == change.SequenceId
        && string.Equals(outcome.VictimTargetId.Value, change.TargetId, StringComparison.Ordinal)
        && outcome.OutcomeCode == EngagementOutcomeCodes.Kill)
      {
        return BdaAssessSourceKind.EngagementOutcome;
      }
    }

    for (var i = 0; i < log.PlatformDamageChanges.Count; i++)
    {
      var damage = log.PlatformDamageChanges[i];
      if (damage.SequenceId == change.SequenceId
        && string.Equals(damage.UnitId.Value, change.TargetId, StringComparison.Ordinal))
      {
        return BdaAssessSourceKind.PlatformDamage;
      }
    }

    return BdaAssessSourceKind.PlatformDamage;
  }

  private static Dictionary<string, BdaAssessPendingTarget> BuildPendingByTarget(
    IReadOnlyList<BdaAssessPendingTarget>? pendingTargets)
  {
    var pendingByTarget = new Dictionary<string, BdaAssessPendingTarget>(StringComparer.Ordinal);
    if (pendingTargets is null || pendingTargets.Count == 0)
    {
      return pendingByTarget;
    }

    for (var i = 0; i < pendingTargets.Count; i++)
    {
      var pending = pendingTargets[i];
      if (string.IsNullOrEmpty(pending.TargetId))
      {
        continue;
      }

      if (!pendingByTarget.TryGetValue(pending.TargetId, out var existing)
        || pending.SimTick > existing.SimTick
        || (pending.SimTick == existing.SimTick && pending.CorrelationSequenceId > existing.CorrelationSequenceId))
      {
        pendingByTarget[pending.TargetId] = pending;
      }
    }

    return pendingByTarget;
  }

  private static bool IsUnknownLifecycle(string lifecycleState) =>
    string.Equals(lifecycleState, "Unknown", StringComparison.Ordinal);

  private static Dictionary<string, List<ContactPictureEntry>> BuildContactsByTarget(
    IReadOnlyList<ContactPictureEntry> picture)
  {
    var contactsByTarget = new Dictionary<string, List<ContactPictureEntry>>(picture.Count, StringComparer.Ordinal);
    for (var i = 0; i < picture.Count; i++)
    {
      var entry = picture[i];
      if (!contactsByTarget.TryGetValue(entry.TargetId, out var contacts))
      {
        contacts = new List<ContactPictureEntry>();
        contactsByTarget[entry.TargetId] = contacts;
      }

      contacts.Add(entry);
    }

    return contactsByTarget;
  }

  private static Dictionary<string, ContactPictureEntry> BuildRepresentativeContactsByTarget(
    IReadOnlyDictionary<string, List<ContactPictureEntry>> contactsByTarget)
  {
    var byTarget = new Dictionary<string, ContactPictureEntry>(contactsByTarget.Count, StringComparer.Ordinal);
    foreach (var (targetId, contacts) in contactsByTarget)
    {
      byTarget[targetId] = contacts[0];
    }

    return byTarget;
  }

  /// <summary>
  /// Mirrors <see cref="KillChainContactStateProjection"/> #575 multi-contact fan-out.
  /// </summary>
  private static List<ContactChangeRecord> FanOutBdaContactChanges(
    IReadOnlyList<ContactChangeRecord> perTargetChanges,
    IReadOnlyDictionary<string, List<ContactPictureEntry>> contactsByTarget)
  {
    if (perTargetChanges.Count == 0)
    {
      return new List<ContactChangeRecord>();
    }

    var expanded = new List<ContactChangeRecord>(perTargetChanges.Count);
    for (var i = 0; i < perTargetChanges.Count; i++)
    {
      var change = perTargetChanges[i];
      if (!contactsByTarget.TryGetValue(change.TargetId, out var contacts) || contacts.Count <= 1)
      {
        expanded.Add(change);
        continue;
      }

      for (var j = 0; j < contacts.Count; j++)
      {
        var contact = contacts[j];
        expanded.Add(new ContactChangeRecord(
          change.SequenceId,
          change.SimTime,
          change.SimTick,
          contact.ObserverId,
          contact.ContactId,
          contact.TargetId,
          change.PreviousState,
          change.NewState));
      }
    }

    return expanded;
  }

  private sealed record TerminalAssess(
    BdaAssessStateKind State,
    BdaAssessSourceKind Source,
    ulong SimTick,
    double SimTime,
    ulong CorrelationSequenceId);
}
