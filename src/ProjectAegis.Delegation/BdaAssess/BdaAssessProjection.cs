namespace ProjectAegis.Delegation.BdaAssess;

using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Catalog;
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

    var contactsById = BuildLastKnownContactsFromLog(log);
    if (contactsById.Count == 0)
    {
      return BdaAssessSnapshot.Empty;
    }

    var picture = contactsById.Values
      .OrderBy(c => c.ContactId, StringComparer.Ordinal)
      .ToArray();

    var contactsByTarget = BuildContactsByTarget(picture);
    var byTarget = BuildRepresentativeContactsByTarget(contactsByTarget);
    var bdaPerTarget = OrderLogBdaProjection.ProjectBdaContactChanges(log, byTarget);
    var bdaFanned = FanOutBdaContactChanges(bdaPerTarget, contactsByTarget);
    var terminalByContact = BuildTerminalAssessByContact(log, bdaFanned);
    var pendingByTarget = BuildPendingByTarget(pendingTargets);

    var rows = new List<BdaAssessContactState>(picture.Length);
    for (var i = 0; i < picture.Length; i++)
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

      if (TryResolveKillDestroyed(log, contact, out var killTerminal))
      {
        rows.Add(new BdaAssessContactState(
          contact.ContactId,
          contact.TargetId,
          contact.ObserverId,
          killTerminal.State,
          killTerminal.Source,
          killTerminal.SimTick,
          killTerminal.SimTime,
          killTerminal.CorrelationSequenceId));
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

  /// <summary>
  /// Retains last-known contact rows from the order log, including <c>Lost</c> transitions that
  /// <see cref="ContactPictureProjection"/> drops from the active picture.
  /// </summary>
  private static Dictionary<string, ContactPictureEntry> BuildLastKnownContactsFromLog(DecisionLog log)
  {
    var tracks = new Dictionary<string, ContactPictureEntry>(StringComparer.Ordinal);
    var ordered = log.ContactChanges
      .OrderBy(c => c.SimTick)
      .ThenBy(c => c.SequenceId)
      .ThenBy(c => c.ContactId, StringComparer.Ordinal)
      .ToArray();

    for (var i = 0; i < ordered.Length; i++)
    {
      var change = ordered[i];
      if (string.IsNullOrEmpty(change.ContactId))
      {
        continue;
      }

      tracks[change.ContactId] = new ContactPictureEntry(
        change.ContactId,
        change.TargetId,
        change.ObserverId,
        change.NewState,
        change.SimTick,
        change.SimTime);
    }

    return tracks;
  }

  private static Dictionary<string, TerminalAssess> BuildTerminalAssessByContact(
    DecisionLog log,
    IReadOnlyList<ContactChangeRecord> bdaChanges)
  {
    var byContact = new Dictionary<string, TerminalAssess>(StringComparer.Ordinal);
    for (var i = 0; i < bdaChanges.Count; i++)
    {
      var change = bdaChanges[i];
      if (!TryResolveTerminalFromBdaChange(log, change, out var terminal))
      {
        continue;
      }

      byContact[change.ContactId] = terminal;
    }

    return byContact;
  }

  private static bool TryResolveTerminalFromBdaChange(
    DecisionLog log,
    ContactChangeRecord change,
    out TerminalAssess terminal)
  {
    terminal = default!;

    if (string.Equals(change.NewState, BdaContactDamageStates.DegradedL1, StringComparison.Ordinal)
      || string.Equals(change.NewState, BdaContactDamageStates.DegradedL2, StringComparison.Ordinal))
    {
      terminal = new TerminalAssess(
        BdaAssessStateKind.Damaged,
        BdaAssessSourceKind.PlatformDamage,
        change.SimTick,
        change.SimTime,
        change.SequenceId);
      return true;
    }

    if (!string.Equals(change.NewState, BdaContactDamageStates.Lost, StringComparison.Ordinal))
    {
      return false;
    }

    if (TryFindKillOutcome(log, change.TargetId, change.SimTick, out var kill))
    {
      terminal = new TerminalAssess(
        BdaAssessStateKind.Destroyed,
        BdaAssessSourceKind.EngagementOutcome,
        change.SimTick,
        change.SimTime,
        kill.SequenceId);
      return true;
    }

    var damage = FindPlatformDamageForChange(log, change);
    if (damage is not null)
    {
      if (damage.NewHpPct <= 0
        || string.Equals(damage.ReasonCode, PlatformDamageChangeReasonCodes.Kill, StringComparison.Ordinal))
      {
        terminal = new TerminalAssess(
          BdaAssessStateKind.Destroyed,
          BdaAssessSourceKind.PlatformDamage,
          change.SimTick,
          change.SimTime,
          change.SequenceId);
        return true;
      }

      terminal = new TerminalAssess(
        BdaAssessStateKind.Damaged,
        BdaAssessSourceKind.PlatformDamage,
        change.SimTick,
        change.SimTime,
        change.SequenceId);
      return true;
    }

  // Overloaded BDA Lost lifecycle (DamageLevel >= 3 with remaining HP) — damaged, not destroyed.
    terminal = new TerminalAssess(
      BdaAssessStateKind.Damaged,
      BdaAssessSourceKind.PlatformDamage,
      change.SimTick,
      change.SimTime,
      change.SequenceId);
    return true;
  }

  private static bool TryResolveKillDestroyed(
    DecisionLog log,
    ContactPictureEntry contact,
    out TerminalAssess terminal)
  {
    terminal = default!;
    if (!IsSensorLost(contact.LifecycleState))
    {
      return false;
    }

    if (!TryFindKillOutcome(log, contact.TargetId, contact.LastSimTick, out var kill))
    {
      return false;
    }

    terminal = new TerminalAssess(
      BdaAssessStateKind.Destroyed,
      BdaAssessSourceKind.EngagementOutcome,
      kill.SimTick,
      kill.SimTime,
      kill.SequenceId);
    return true;
  }

  private static bool TryFindKillOutcome(
    DecisionLog log,
    string targetId,
    ulong simTick,
    out EngagementOutcomeRecord kill)
  {
    kill = null!;
    EngagementOutcomeRecord? latest = null;
    for (var i = 0; i < log.EngagementOutcomes.Count; i++)
    {
      var outcome = log.EngagementOutcomes[i];
      if (!string.Equals(outcome.VictimTargetId.Value, targetId, StringComparison.Ordinal)
        || outcome.OutcomeCode != EngagementOutcomeCodes.Kill
        || outcome.SimTick != simTick)
      {
        continue;
      }

      if (latest is null
        || outcome.SequenceId > latest.SequenceId
        || (outcome.SequenceId == latest.SequenceId && outcome.EngagementId >= latest.EngagementId))
      {
        latest = outcome;
      }
    }

    if (latest is null)
    {
      return false;
    }

    kill = latest;
    return true;
  }

  private static PlatformDamageChangeRecord? FindPlatformDamageForChange(
    DecisionLog log,
    ContactChangeRecord change)
  {
    PlatformDamageChangeRecord? latest = null;
    for (var i = 0; i < log.PlatformDamageChanges.Count; i++)
    {
      var damage = log.PlatformDamageChanges[i];
      if (!string.Equals(damage.UnitId.Value, change.TargetId, StringComparison.Ordinal)
        || damage.SimTick != change.SimTick)
      {
        continue;
      }

      if (latest is null || damage.SequenceId >= latest.SequenceId)
      {
        latest = damage;
      }
    }

    return latest;
  }

  private static bool IsSensorLost(string lifecycleState) =>
    string.Equals(lifecycleState, "Lost", StringComparison.Ordinal);

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
