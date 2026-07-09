namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;

/// <summary>
/// Derives per-order <see cref="OrderLifecycleState"/> from order-log records (req 20 §Order lifecycle,
/// TR-c2-006). Presentation-only — the UI binds this projection and never mutates sim state (ADR-010).
/// </summary>
/// <remarks>
/// <para>
/// Correlation is keyed by <see cref="OrderKey"/> = (unit id, order-log <c>SequenceId</c> of the
/// originating <see cref="PlayerOrderRecord"/>) — the ADR-003 order-log schema has no explicit
/// "this record belongs to order X" foreign key. Evidence records (<see cref="PolicyDenialRecord"/>,
/// <see cref="EngagementRecord"/>, <see cref="EngagementOutcomeRecord"/>) are matched FIFO to the
/// OLDEST still-open (non-terminal) order for the same unit, filtered by matching <see cref="OrderKind"/>
/// where the evidence record carries one.
/// </para>
/// <para>
/// Mapping: <see cref="PlayerOrderRecord"/> logged with no comms delay (<c>ResolvedExecuteSimTick ==
/// SimTick</c>) → <see cref="OrderLifecycleState.Accepted"/>; logged with a resolved future execute
/// tick (comms delay already scheduled it) → <see cref="OrderLifecycleState.Queued"/>;
/// <see cref="EngagementRecord"/> with <c>Launched == true</c> → <see cref="OrderLifecycleState.Executing"/>;
/// matching <see cref="EngagementOutcomeRecord"/> → <see cref="OrderLifecycleState.Completed"/>;
/// <see cref="PolicyDenialRecord"/> → <see cref="OrderLifecycleState.Denied"/>;
/// <see cref="EngagementRecord"/> with <c>Launched == false</c> (pre-launch abort) →
/// <see cref="OrderLifecycleState.Aborted"/>.
/// </para>
/// <para>
/// A player cancel of a queued order (<see cref="OrderLogEntryKind.PlayerOrderCancelled"/>, Phase 2b via
/// <c>DelegationBridge.TryCancelHumanOrder</c>) maps to <see cref="OrderLifecycleState.Aborted"/>, matched
/// FIFO to the oldest still-open order for the unit with the same <see cref="OrderKind"/>.
/// </para>
/// <para>
/// Known MVP gap (Track T2 report, req 20 AC-8): non-Engage order kinds (Move, Hold, SetEwPosture,
/// ReturnToBase) have no "underway"/"outcome" log evidence in the current schema, so they remain
/// Accepted/Queued unless denied or cancelled.
/// </para>
/// </remarks>
public static class OrderLifecycleProjection
{
    /// <summary>Correlates order-log records to a single player order (unit id + originating sequence id).</summary>
    public readonly record struct OrderKey(string UnitId, ulong SequenceId);

    /// <summary>Latest-known state for a single tracked order, returned by <see cref="ProjectLatestForUnit"/>.</summary>
    public readonly record struct UnitOrderState(OrderKey Key, OrderKind Kind, OrderLifecycleState State);

    public static IReadOnlyDictionary<OrderKey, OrderLifecycleState> Project(DecisionLog log) =>
        Project(log.ChronologicalEntries());

    public static IReadOnlyDictionary<OrderKey, OrderLifecycleState> Project(
        IReadOnlyList<OrderLogEntry> entries)
    {
        var states = new Dictionary<OrderKey, OrderLifecycleState>();
        var kindByKey = new Dictionary<OrderKey, OrderKind>();
        var openByUnit = new Dictionary<string, List<OrderKey>>(StringComparer.Ordinal);
        var keyByEngagementId = new Dictionary<ulong, OrderKey>();

        foreach (var entry in entries)
        {
            switch (entry.Kind)
            {
                case OrderLogEntryKind.PlayerOrder when entry.Payload is PlayerOrderRecord playerOrder:
                {
                    var key = new OrderKey(playerOrder.UnitId.Value, entry.SequenceId);
                    states[key] = playerOrder.ResolvedExecuteSimTick > playerOrder.SimTick
                        ? OrderLifecycleState.Queued
                        : OrderLifecycleState.Accepted;
                    kindByKey[key] = playerOrder.Kind;
                    Open(openByUnit, playerOrder.UnitId.Value, key);
                    break;
                }

                case OrderLogEntryKind.PolicyDenial when entry.Payload is PolicyDenialRecord denial:
                {
                    if (TryTakeOldestOpen(
                            openByUnit, kindByKey, denial.TargetId.Value, denial.AttemptedKind,
                            remove: true, out var key))
                    {
                        states[key] = OrderLifecycleState.Denied;
                    }

                    break;
                }

                case OrderLogEntryKind.Engagement when entry.Payload is EngagementRecord engagement:
                {
                    if (engagement.Launched)
                    {
                        if (TryTakeOldestOpen(
                                openByUnit, kindByKey, engagement.ShooterTargetId.Value, OrderKind.Engage,
                                remove: false, out var key))
                        {
                            states[key] = OrderLifecycleState.Executing;
                            keyByEngagementId[engagement.EngagementId] = key;
                        }
                    }
                    else if (TryTakeOldestOpen(
                                 openByUnit, kindByKey, engagement.ShooterTargetId.Value, OrderKind.Engage,
                                 remove: true, out var abortedKey))
                    {
                        states[abortedKey] = OrderLifecycleState.Aborted;
                    }

                    break;
                }

                case OrderLogEntryKind.EngagementOutcome when entry.Payload is EngagementOutcomeRecord outcome:
                {
                    if (keyByEngagementId.TryGetValue(outcome.EngagementId, out var key))
                    {
                        states[key] = OrderLifecycleState.Completed;
                        RemoveOpen(openByUnit, key);
                        keyByEngagementId.Remove(outcome.EngagementId);
                    }

                    break;
                }

                case OrderLogEntryKind.PlayerOrderCancelled when entry.Payload is PlayerOrderCancelledRecord cancelled:
                {
                    // Phase 2b (req 20 AC-8): a player cancel of a queued order → Aborted. Matched FIFO to
                    // the oldest still-open order for the unit with the same OrderKind. An order that has
                    // already launched (Executing) stays open in openByUnit until its EngagementOutcome
                    // arrives, so the predicate below excludes it — the real bridge path
                    // (PlayerOrderExecutionQueue.TryRemove) can never produce a cancel for an
                    // already-launched order, but this guards the pure projection against a
                    // stray/out-of-order cancel record retroactively downgrading Executing to Aborted.
                    if (TryTakeOldestOpen(
                            openByUnit, kindByKey, cancelled.UnitId.Value, cancelled.Kind,
                            remove: true, out var key,
                            predicate: candidate =>
                                !states.TryGetValue(candidate, out var candidateState) ||
                                candidateState is OrderLifecycleState.Accepted or OrderLifecycleState.Queued))
                    {
                        states[key] = OrderLifecycleState.Aborted;
                    }

                    break;
                }
            }
        }

        return states;
    }

    /// <summary>
    /// Convenience for single-unit chip binding (RightUnitPanelHost): the most recently logged order
    /// for <paramref name="unitId"/> and its currently resolved lifecycle state, or <c>null</c> if the
    /// unit has never had a player order logged.
    /// </summary>
    public static UnitOrderState? ProjectLatestForUnit(DecisionLog log, string unitId)
    {
        PlayerOrderRecord? latest = null;
        ulong latestKeySequence = 0;
        foreach (var entry in log.ChronologicalEntries())
        {
            if (entry.Kind == OrderLogEntryKind.PlayerOrder &&
                entry.Payload is PlayerOrderRecord playerOrder &&
                string.Equals(playerOrder.UnitId.Value, unitId, StringComparison.Ordinal))
            {
                latest = playerOrder;
                latestKeySequence = entry.SequenceId;
            }
        }

        if (latest == null)
        {
            return null;
        }

        var key = new OrderKey(unitId, latestKeySequence);
        var states = Project(log);
        var state = states.TryGetValue(key, out var resolved) ? resolved : OrderLifecycleState.Accepted;
        return new UnitOrderState(key, latest.Kind, state);
    }

    private static void Open(Dictionary<string, List<OrderKey>> openByUnit, string unitId, OrderKey key)
    {
        if (!openByUnit.TryGetValue(unitId, out var list))
        {
            list = new List<OrderKey>();
            openByUnit[unitId] = list;
        }

        list.Add(key);
    }

    private static void RemoveOpen(Dictionary<string, List<OrderKey>> openByUnit, OrderKey key)
    {
        if (openByUnit.TryGetValue(key.UnitId, out var list))
        {
            list.Remove(key);
        }
    }

    private static bool TryTakeOldestOpen(
        Dictionary<string, List<OrderKey>> openByUnit,
        Dictionary<OrderKey, OrderKind> kindByKey,
        string unitId,
        OrderKind kind,
        bool remove,
        out OrderKey key,
        Func<OrderKey, bool>? predicate = null)
    {
        key = default;
        if (!openByUnit.TryGetValue(unitId, out var list) || list.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var candidate = list[i];
            if (kindByKey.TryGetValue(candidate, out var candidateKind) && candidateKind == kind &&
                (predicate is null || predicate(candidate)))
            {
                key = candidate;
                if (remove)
                {
                    list.RemoveAt(i);
                }

                return true;
            }
        }

        return false;
    }
}
