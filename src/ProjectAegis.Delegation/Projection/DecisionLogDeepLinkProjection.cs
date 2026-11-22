namespace ProjectAegis.Delegation.Projection;

using System;
using System.Collections.Generic;
using System.Globalization;
using Decision;
using Orchestration;
using ProjectAegis.Sim.Scenario;

public sealed record DecisionLogDeepLinkDetail(
    bool Found,
    ulong SequenceId,
    ulong SimTick,
    double SimTime,
    OrderLogEntryKind? EntryKind,
    string Category,
    string SourceKind,
    string? ActorId,
    string? AgentId,
    string? UnitId,
    string Summary,
    string? Rationale,
    string? AutonomyLevel,
    double? AttentionLoad,
    double? AttentionBudget,
    double? RngDraw,
    ulong? PolicySnapshotId,
    IReadOnlyList<string> ScoredAlternatives,
    IReadOnlyList<KeyValuePair<string, string>> Fields,
    string? UnavailableReason)
{
    public static DecisionLogDeepLinkDetail NotFound(ulong sequenceId) => new(
        Found: false,
        SequenceId: sequenceId,
        SimTick: 0,
        SimTime: 0,
        EntryKind: null,
        Category: string.Empty,
        SourceKind: string.Empty,
        ActorId: null,
        AgentId: null,
        UnitId: null,
        Summary: "Decision log entry is not available.",
        Rationale: null,
        AutonomyLevel: null,
        AttentionLoad: null,
        AttentionBudget: null,
        RngDraw: null,
        PolicySnapshotId: null,
        ScoredAlternatives: Array.Empty<string>(),
        Fields: Array.Empty<KeyValuePair<string, string>>(),
        UnavailableReason: DecisionLogDeepLinkProjection.UnavailableReasonCode);
}

/// <summary>
/// Read-only deep-link projection from a message-log <c>SequenceId</c> into <see cref="DecisionLog"/>
/// (DRG-273). Live inspection queries only the already-filtered <c>GetLiveOrderLogView()</c> so
/// fogged <c>FullAutonomous</c> decisions return <see cref="UnavailableReasonCode"/> with zero
/// existence oracle.
/// </summary>
public static class DecisionLogDeepLinkProjection
{
    public const string UnavailableReasonCode = "ENTRY_NOT_AVAILABLE";

    public static DecisionLogDeepLinkDetail InspectLive(
        IReadOnlyList<OrderLogEntry> liveEntries,
        ulong sequenceId)
    {
        if (liveEntries is null)
        {
            throw new ArgumentNullException(nameof(liveEntries));
        }

        if (sequenceId == 0)
        {
            return DecisionLogDeepLinkDetail.NotFound(sequenceId);
        }

        for (var i = 0; i < liveEntries.Count; i++)
        {
            if (liveEntries[i].SequenceId == sequenceId)
            {
                return ProjectEntry(liveEntries[i]);
            }
        }

        return DecisionLogDeepLinkDetail.NotFound(sequenceId);
    }

    public static DecisionLogDeepLinkDetail InspectLive(
        DecisionLog log,
        PlayerInfoModel model,
        ulong sequenceId)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var filtered = PlayerInfoFilter.FilterLiveEntries(log.ChronologicalEntries(), model);
        return InspectLive(filtered, sequenceId);
    }

    public static DecisionLogDeepLinkDetail Inspect(DecisionLog log, ulong sequenceId)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (sequenceId == 0)
        {
            return DecisionLogDeepLinkDetail.NotFound(sequenceId);
        }

        var entries = log.ChronologicalEntries();
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].SequenceId == sequenceId)
            {
                return ProjectEntry(entries[i]);
            }
        }

        return DecisionLogDeepLinkDetail.NotFound(sequenceId);
    }

    private static DecisionLogDeepLinkDetail ProjectEntry(OrderLogEntry entry)
    {
        switch (entry.Kind)
        {
            case OrderLogEntryKind.AgentDecision when entry.Payload is AgentDecisionPayload p:
                {
                    var alternatives = FormatAlternatives(p.ScoredIntents);
                    return new DecisionLogDeepLinkDetail(
                        Found: true,
                        SequenceId: entry.SequenceId,
                        SimTick: p.SimTick,
                        SimTime: entry.SimTime,
                        EntryKind: entry.Kind,
                        Category: "AGENT_DECISION",
                        SourceKind: "Agent",
                        ActorId: p.AgentId.Value,
                        AgentId: p.AgentId.Value,
                        UnitId: p.TargetId.Value,
                        Summary: $"Agent {p.AgentId.Value} chose {p.ChosenOrderKind} for {p.TargetId.Value}",
                        Rationale: p.Rationale,
                        AutonomyLevel: p.AutonomyLevel.ToString(),
                        AttentionLoad: p.AttentionLoad,
                        AttentionBudget: p.AttentionBudget,
                        RngDraw: p.RngDraw,
                        PolicySnapshotId: null,
                        ScoredAlternatives: alternatives,
                        Fields: new[]
                        {
                        Kv("OrderKind", p.ChosenOrderKind.ToString()),
                        Kv("Autonomy", p.AutonomyLevel.ToString()),
                        Kv("Attention", string.Format(CultureInfo.InvariantCulture, "{0:F2}/{1:F2}", p.AttentionLoad, p.AttentionBudget)),
                        Kv("RngDraw", string.Format(CultureInfo.InvariantCulture, "{0:F6}", p.RngDraw)),
                        },
                        UnavailableReason: null);
                }

            case OrderLogEntryKind.AgentDecision when entry.Payload is DecisionRecord r:
                {
                    var alternatives = FormatAlternatives(r.Alternatives);
                    return new DecisionLogDeepLinkDetail(
                        Found: true,
                        SequenceId: entry.SequenceId,
                        SimTick: r.SimTick,
                        SimTime: entry.SimTime,
                        EntryKind: entry.Kind,
                        Category: "AGENT_DECISION",
                        SourceKind: "Agent",
                        ActorId: r.AgentId.Value,
                        AgentId: r.AgentId.Value,
                        UnitId: r.TargetId.Value,
                        Summary: $"Agent {r.AgentId.Value} chose {r.ChosenKind} for {r.TargetId.Value}",
                        Rationale: r.Rationale,
                        AutonomyLevel: r.AutonomyLevel.ToString(),
                        AttentionLoad: r.AttentionLoad,
                        AttentionBudget: r.AttentionBudget,
                        RngDraw: r.RngDraw,
                        PolicySnapshotId: null,
                        ScoredAlternatives: alternatives,
                        Fields: new[]
                        {
                        Kv("OrderKind", r.ChosenKind.ToString()),
                        Kv("Autonomy", r.AutonomyLevel.ToString()),
                        Kv("Attention", string.Format(CultureInfo.InvariantCulture, "{0:F2}/{1:F2}", r.AttentionLoad, r.AttentionBudget)),
                        Kv("RngDraw", string.Format(CultureInfo.InvariantCulture, "{0:F6}", r.RngDraw)),
                        },
                        UnavailableReason: null);
                }

            case OrderLogEntryKind.PolicyDenial when entry.Payload is PolicyDenialRecord d:
                return SimpleDetail(
                    entry, d.SimTick, "POLICY_DENIAL", "Policy", d.AgentId.Value, d.AgentId.Value, d.TargetId.Value,
                    $"Fire denied for {d.TargetId.Value}: {d.Reason} ({d.AttemptedKind})",
                    rationale: d.Reason.ToString(),
                    policySnapshotId: d.PolicySnapshotId,
                    fields: new[]
                    {
                        Kv("AttemptedKind", d.AttemptedKind.ToString()),
                        Kv("Reason", d.Reason.ToString()),
                        Kv("PolicySnapshotId", d.PolicySnapshotId.ToString(CultureInfo.InvariantCulture)),
                    });

            case OrderLogEntryKind.Engagement when entry.Payload is EngagementRecord e:
                return SimpleDetail(
                    entry, e.SimTick, e.Launched ? "WEAPON_LAUNCH" : "ENGAGE_ABORT", "Combat",
                    e.ShooterTargetId.Value, null, e.ShooterTargetId.Value,
                    e.Launched
                        ? $"Engagement {e.EngagementId} launched by {e.ShooterTargetId.Value}"
                        : $"Engagement {e.EngagementId} aborted: {e.AbortReasonCode}",
                    rationale: e.AbortReasonCode,
                    fields: new[]
                    {
                        Kv("EngagementId", e.EngagementId.ToString(CultureInfo.InvariantCulture)),
                        Kv("Launched", e.Launched ? "true" : "false"),
                        Kv("Victim", e.VictimTargetId?.Value ?? "—"),
                        Kv("WeaponFamily", e.WeaponFamilyId ?? "—"),
                        Kv("SalvoSize", e.SalvoSize.ToString(CultureInfo.InvariantCulture)),
                    });

            case OrderLogEntryKind.EngagementOutcome when entry.Payload is EngagementOutcomeRecord o:
                return SimpleDetail(
                    entry, o.SimTick, o.OutcomeCode.ToUpperInvariant(), "Combat",
                    o.ShooterTargetId.Value, null, o.ShooterTargetId.Value,
                    $"Engagement {o.EngagementId} outcome: {o.OutcomeCode} vs {o.VictimTargetId.Value}",
                    fields: new[]
                    {
                        Kv("EngagementId", o.EngagementId.ToString(CultureInfo.InvariantCulture)),
                        Kv("Victim", o.VictimTargetId.Value),
                        Kv("OutcomeCode", o.OutcomeCode),
                        Kv("PkDraw", string.Format(CultureInfo.InvariantCulture, "{0:F6}", o.PkDraw)),
                    });

            case OrderLogEntryKind.ControllerChange when entry.Payload is ControllerChangeRecord cc:
                return SimpleDetail(
                    entry, 0, "CONTROLLER", "Controller",
                    cc.AgentId?.Value ?? cc.NewKind, cc.AgentId?.Value, cc.TargetId.Value,
                    $"Controller {cc.TargetId.Value}: {cc.PreviousKind} → {cc.NewKind}",
                    fields: new[]
                    {
                        Kv("PreviousKind", cc.PreviousKind),
                        Kv("NewKind", cc.NewKind),
                    });

            case OrderLogEntryKind.GroupMemberDetach when entry.Payload is GroupMemberDetachRecord gd:
                return SimpleDetail(
                    entry, 0, "CONTROLLER", "Group", gd.GroupId.Value, null, gd.UnitId.Value,
                    $"Unit {gd.UnitId.Value} detached from {gd.GroupId.Value}",
                    fields: new[] { Kv("GroupId", gd.GroupId.Value) });

            case OrderLogEntryKind.GroupMemberRejoin when entry.Payload is GroupMemberRejoinRecord gr:
                return SimpleDetail(
                    entry, 0, "CONTROLLER", "Group", gr.GroupId.Value, null, gr.UnitId.Value,
                    $"Unit {gr.UnitId.Value} rejoined {gr.GroupId.Value}",
                    fields: new[] { Kv("GroupId", gr.GroupId.Value) });

            case OrderLogEntryKind.MagazineChange when entry.Payload is MagazineChangeRecord m:
                return SimpleDetail(
                    entry, m.SimTick, "MAGAZINE", "Logistics", m.ShooterTargetId.Value, null, m.ShooterTargetId.Value,
                    $"Magazine {m.ShooterTargetId.Value} mount {m.MountId}: {m.Delta} ({m.ReasonCode})",
                    rationale: m.ReasonCode,
                    fields: new[]
                    {
                        Kv("MountId", m.MountId.ToString(CultureInfo.InvariantCulture)),
                        Kv("Delta", m.Delta.ToString(CultureInfo.InvariantCulture)),
                        Kv("ReasonCode", m.ReasonCode),
                    });

            case OrderLogEntryKind.ContactChange when entry.Payload is ContactChangeRecord c:
                return SimpleDetail(
                    entry, c.SimTick, "CONTACT", "Sensor", c.ObserverId, null, c.ObserverId,
                    $"Contact {c.ContactId} {c.PreviousState} → {c.NewState} ({c.TargetId})",
                    fields: new[]
                    {
                        Kv("ContactId", c.ContactId),
                        Kv("TargetId", c.TargetId),
                        Kv("Transition", $"{c.PreviousState} → {c.NewState}"),
                    });

            case OrderLogEntryKind.MissionTransition when entry.Payload is MissionTransitionRecord mt:
                return SimpleDetail(
                    entry, mt.SimTick, "MISSION", "Mission", mt.EventId, null, null,
                    $"Mission {mt.EventId} → {mt.PhaseCode}",
                    fields: new[]
                    {
                        Kv("EventId", mt.EventId),
                        Kv("PhaseCode", mt.PhaseCode),
                    });

            case OrderLogEntryKind.EventFired when entry.Payload is EventFiredRecord ev:
                return SimpleDetail(
                    entry, ev.SimTick, "EVENT", "Event", ev.EventId, null, null,
                    $"Event {ev.EventId}: {ev.EventCode}",
                    fields: new[]
                    {
                        Kv("EventId", ev.EventId),
                        Kv("EventCode", ev.EventCode),
                    });

            case OrderLogEntryKind.PlayerOrder when entry.Payload is PlayerOrderRecord po:
                return SimpleDetail(
                    entry, po.SimTick, "PLAYER_ORDER", "Player", po.Source, null, po.UnitId.Value,
                    $"Player ordered {po.Kind} for {po.UnitId.Value}",
                    fields: new[]
                    {
                        Kv("OrderKind", po.Kind.ToString()),
                        Kv("Source", po.Source),
                        Kv("ExecuteSimTick", po.ResolvedExecuteSimTick.ToString(CultureInfo.InvariantCulture)),
                    });

            case OrderLogEntryKind.PolicyUpdate when entry.Payload is PolicyUpdateRecord pu:
                return SimpleDetail(
                    entry, pu.SimTick, "POLICY_UPDATE", "Policy", $"policy:{pu.PolicySnapshotId}", null, null,
                    $"Policy {pu.Field}: {pu.PreviousValue} → {pu.NewValue}",
                    policySnapshotId: pu.PolicySnapshotId,
                    fields: new[]
                    {
                        Kv("Field", pu.Field),
                        Kv("Transition", $"{pu.PreviousValue} → {pu.NewValue}"),
                        Kv("PolicySnapshotId", pu.PolicySnapshotId.ToString(CultureInfo.InvariantCulture)),
                    });

            case OrderLogEntryKind.ModeChange when entry.Payload is ModeChangeRecord mc:
                return SimpleDetail(
                    entry, mc.SimTick, "MODE", "Mode", mc.UnitId?.Value ?? "session", null, mc.UnitId?.Value,
                    $"Mode {mc.PreviousMode} → {mc.NewMode}",
                    fields: new[]
                    {
                        Kv("PreviousMode", mc.PreviousMode),
                        Kv("NewMode", mc.NewMode),
                    });

            case OrderLogEntryKind.CommsStateChange when entry.Payload is CommsStateChangeRecord cs:
                return SimpleDetail(
                    entry, cs.SimTick, "COMMS", "Comms", cs.NodeId, null, cs.NodeId,
                    $"Comms {cs.NodeId}: {cs.PreviousState} → {cs.NewState} ({cs.Reason})",
                    rationale: cs.Reason,
                    fields: new[]
                    {
                        Kv("NodeId", cs.NodeId),
                        Kv("Transition", $"{cs.PreviousState} → {cs.NewState}"),
                        Kv("Reason", cs.Reason),
                    });

            case OrderLogEntryKind.FuelStateChange when entry.Payload is FuelStateChangeRecord fs:
                return SimpleDetail(
                    entry, fs.SimTick, "FUEL", "Logistics", fs.UnitId.Value, null, fs.UnitId.Value,
                    $"Fuel {fs.UnitId.Value}: {fs.PreviousState} → {fs.NewState}",
                    fields: new[]
                    {
                        Kv("Transition", $"{fs.PreviousState} → {fs.NewState}"),
                        Kv("RemainingKg", string.Format(CultureInfo.InvariantCulture, "{0:F1}", fs.RemainingFuelKg)),
                    });

            case OrderLogEntryKind.FuelBurn when entry.Payload is FuelBurnRecord fb:
                return SimpleDetail(
                    entry, fb.SimTick, "FUEL", "Logistics", fb.UnitId.Value, null, fb.UnitId.Value,
                    $"Fuel burn {fb.UnitId.Value}: {fb.DeltaKg:F1} kg",
                    fields: new[]
                    {
                        Kv("DeltaKg", string.Format(CultureInfo.InvariantCulture, "{0:F1}", fb.DeltaKg)),
                        Kv("RemainingKg", string.Format(CultureInfo.InvariantCulture, "{0:F1}", fb.RemainingFuelKg)),
                    });

            case OrderLogEntryKind.PlatformDamageChange when entry.Payload is PlatformDamageChangeRecord dmg:
                return SimpleDetail(
                    entry, dmg.SimTick, "DAMAGE", "Damage", dmg.UnitId.Value, null, dmg.UnitId.Value,
                    $"Damage {dmg.UnitId.Value}: {dmg.PreviousHpPct:F0}% → {dmg.NewHpPct:F0}% ({dmg.ReasonCode})",
                    rationale: dmg.ReasonCode,
                    fields: new[]
                    {
                        Kv("HpPct", string.Format(CultureInfo.InvariantCulture, "{0:F0}% → {1:F0}%", dmg.PreviousHpPct, dmg.NewHpPct)),
                        Kv("ReasonCode", dmg.ReasonCode),
                        Kv("DamageLevel", dmg.DamageLevel.ToString(CultureInfo.InvariantCulture)),
                    });

            case OrderLogEntryKind.OrdnanceStateChange when entry.Payload is OrdnanceStateChangeRecord ord:
                return SimpleDetail(
                    entry, ord.SimTick, "ORDNANCE", "Logistics", ord.UnitId.Value, null, ord.UnitId.Value,
                    $"Ordnance {ord.UnitId.Value}: {ord.PreviousState} → {ord.NewState} (rem {ord.RoundsRemaining})",
                    fields: new[]
                    {
                        Kv("Transition", $"{ord.PreviousState} → {ord.NewState}"),
                        Kv("RoundsRemaining", ord.RoundsRemaining.ToString(CultureInfo.InvariantCulture)),
                    });

            case OrderLogEntryKind.ContactOverride when entry.Payload is ContactOverrideRecord co:
                return SimpleDetail(
                    entry, co.SimTick, "CONTACT", "Operator", "operator", null, null,
                    $"Contact {co.ContactId} {co.Action}: {co.PreviousValue} → {co.NewValue}",
                    fields: new[]
                    {
                        Kv("ContactId", co.ContactId),
                        Kv("Action", co.Action.ToString()),
                        Kv("PreviousValue", co.PreviousValue),
                        Kv("NewValue", co.NewValue),
                    });

            case OrderLogEntryKind.CommsGridRejoin when entry.Payload is CommsGridRejoinRecord cr:
                return SimpleDetail(
                    entry, cr.SimTick, "COMMS", "Comms", cr.UnitId, null, cr.UnitId,
                    $"Comms rejoin {cr.UnitId}: {cr.InformativeMerges.Count} contact merge(s)",
                    fields: new[]
                    {
                        KV("UnitId", cr.UnitId),
                        KV("OffGridSinceTick", cr.OffGridSinceTick.ToString(CultureInfo.InvariantCulture)),
                        KV("RejoinTick", cr.RejoinTick.ToString(CultureInfo.InvariantCulture)),
                        KV("InformativeMergeCount", cr.InformativeMerges.Count.ToString(CultureInfo.InvariantCulture)),
                        KV("SuppressedCount", cr.SuppressedCount.ToString(CultureInfo.InvariantCulture)),
                        KV("MergedTargets", string.Join(", ", cr.InformativeMerges.Select(m => $"{m.TargetId}:{m.PreviousState}->{m.NewState}"))),
                    });

            default:
                return SimpleDetail(
                    entry, 0, entry.Kind.ToString().ToUpperInvariant(), entry.Kind.ToString(),
                    null, null, null,
                    $"{entry.Kind} (seq {entry.SequenceId})",
                    fields: Array.Empty<KeyValuePair<string, string>>());
        }
    }

    private static DecisionLogDeepLinkDetail SimpleDetail(
        OrderLogEntry entry,
        ulong simTick,
        string category,
        string sourceKind,
        string? actorId,
        string? agentId,
        string? unitId,
        string summary,
        string? rationale = null,
        ulong? policySnapshotId = null,
        IReadOnlyList<KeyValuePair<string, string>>? fields = null) =>
        new(
            Found: true,
            SequenceId: entry.SequenceId,
            SimTick: simTick,
            SimTime: entry.SimTime,
            EntryKind: entry.Kind,
            Category: category,
            SourceKind: sourceKind,
            ActorId: actorId,
            AgentId: agentId,
            UnitId: unitId,
            Summary: summary,
            Rationale: rationale,
            AutonomyLevel: null,
            AttentionLoad: null,
            AttentionBudget: null,
            RngDraw: null,
            PolicySnapshotId: policySnapshotId,
            ScoredAlternatives: Array.Empty<string>(),
            Fields: fields ?? Array.Empty<KeyValuePair<string, string>>(),
            UnavailableReason: null);

    private static IReadOnlyList<string> FormatAlternatives(IReadOnlyList<ScoredIntent>? intents)
    {
        if (intents is null || intents.Count == 0)
        {
            return Array.Empty<string>();
        }

        var list = new string[intents.Count];
        for (var i = 0; i < intents.Count; i++)
        {
            var intent = intents[i];
            list[i] = string.Format(
                CultureInfo.InvariantCulture,
                "{0}: score {1:F3} ({2})",
                intent.Kind,
                intent.Score,
                intent.Risk);
        }

        return list;
    }

    private static KeyValuePair<string, string> Kv(string key, string value) => new(key, value);
}
