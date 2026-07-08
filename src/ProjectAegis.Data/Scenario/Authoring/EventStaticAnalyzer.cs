namespace ProjectAegis.Data.Scenario.Authoring;

using ProjectAegis.Data.Validation;

/// <summary>
/// Pure static analysis of scenario events (ME-W2 / AME event graph).
/// Emits <see cref="ValidationFinding"/> codes for dead triggers, unreachable
/// ActivateMission actions, contradictory Result flags, and ActivateMission→
/// MissionComplete cycles. Deterministic given the same document order.
/// </summary>
public static class EventStaticAnalyzer
{
    /// <summary>Event has zero conditions and TriggerType is not Time.</summary>
    public const string DeadTriggerCode = "EVENT_DEAD_TRIGGER";

    /// <summary>ActivateMission action missing mission id or targets unknown mission.</summary>
    public const string UnreachableActionCode = "EVENT_UNREACHABLE_ACTION";

    /// <summary>Same event has conditions with Result=true and Result=false.</summary>
    public const string ContradictoryCode = "EVENT_CONTRADICTORY";

    /// <summary>Event participates in an ActivateMission→MissionComplete cycle.</summary>
    public const string CircularCode = "EVENT_CIRCULAR";

    /// <summary>
    /// Analyze <paramref name="scenario"/> events and return static findings
    /// (severity Warning — does not block export at the default Error floor).
    /// </summary>
    /// <param name="scenario">Canonical scenario document (events + missions).</param>
    /// <returns>Deterministic findings ordered by code then event id then message.</returns>
    public static IReadOnlyList<ValidationFinding> Analyze(ScenarioDocumentDto scenario)
    {
        if (scenario is null)
        {
            throw new ArgumentNullException(nameof(scenario));
        }

        var events = scenario.Events ?? Array.Empty<ScenarioEventDto>();
        if (events.Count == 0)
        {
            return Array.Empty<ValidationFinding>();
        }

        var missionIds = new HashSet<string>(
            (scenario.Missions ?? Array.Empty<ScenarioMissionDto>()).Select(m => m.Id),
            StringComparer.OrdinalIgnoreCase);

        var findings = new List<ValidationFinding>();

        foreach (var evt in events)
        {
            AnalyzeDeadTrigger(evt, findings);
            AnalyzeContradictory(evt, findings);
            AnalyzeUnreachableActions(evt, missionIds, findings);
        }

        AnalyzeCircular(events, findings);

        return findings
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => EventIdOf(f), StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AnalyzeDeadTrigger(ScenarioEventDto evt, List<ValidationFinding> sink)
    {
        var conditions = evt.Conditions ?? Array.Empty<ScenarioEventConditionDto>();
        if (conditions.Count > 0)
        {
            return;
        }

        if (string.Equals(evt.TriggerType, "Time", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        sink.Add(new ValidationFinding(
            DeadTriggerCode,
            ValidationSeverity.Warning,
            $"Event '{evt.Id}' has zero conditions and TriggerType '{evt.TriggerType}' is not Time (dead trigger).",
            Data: EventData(evt.Id)));
    }

    private static void AnalyzeContradictory(ScenarioEventDto evt, List<ValidationFinding> sink)
    {
        var conditions = evt.Conditions ?? Array.Empty<ScenarioEventConditionDto>();
        var hasTrue = false;
        var hasFalse = false;
        foreach (var c in conditions)
        {
            if (c.Result == true)
            {
                hasTrue = true;
            }
            else if (c.Result == false)
            {
                hasFalse = true;
            }

            if (hasTrue && hasFalse)
            {
                break;
            }
        }

        if (!hasTrue || !hasFalse)
        {
            return;
        }

        sink.Add(new ValidationFinding(
            ContradictoryCode,
            ValidationSeverity.Warning,
            $"Event '{evt.Id}' has contradictory conditions (Result=true and Result=false).",
            Data: EventData(evt.Id)));
    }

    private static void AnalyzeUnreachableActions(
        ScenarioEventDto evt,
        HashSet<string> missionIds,
        List<ValidationFinding> sink)
    {
        var actions = evt.Actions ?? Array.Empty<ScenarioEventActionDto>();
        foreach (var action in actions)
        {
            if (!string.Equals(action.Type, "ActivateMission", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(action.UnitId))
            {
                sink.Add(new ValidationFinding(
                    UnreachableActionCode,
                    ValidationSeverity.Warning,
                    $"Event '{evt.Id}' ActivateMission action is missing mission id.",
                    Data: EventData(evt.Id)));
                continue;
            }

            var missionId = action.UnitId.Trim();
            if (missionIds.Contains(missionId))
            {
                continue;
            }

            sink.Add(new ValidationFinding(
                UnreachableActionCode,
                ValidationSeverity.Warning,
                $"Event '{evt.Id}' ActivateMission targets unknown mission '{missionId}'.",
                MissionId: missionId,
                Data: EventData(evt.Id)));
        }
    }

    /// <summary>
    /// Directed graph: edge A→B when A has ActivateMission(UnitId=M) and B has
    /// TriggerType MissionComplete or a MissionComplete condition with UnitId=M.
    /// Emit <see cref="CircularCode"/> for every event that participates in a cycle.
    /// </summary>
    private static void AnalyzeCircular(
        IReadOnlyList<ScenarioEventDto> events,
        List<ValidationFinding> sink)
    {
        var nodes = events.Select(e => e.Id).ToArray();
        var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var id in nodes)
        {
            adj[id] = new List<string>();
        }

        // missionId → events that complete / listen for that mission
        var listenersByMission = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var evt in events)
        {
            if (string.Equals(evt.TriggerType, "MissionComplete", StringComparison.OrdinalIgnoreCase))
            {
                // Trigger-level MissionComplete: UnitId on first condition is conventional target.
                var mid = FirstMissionCompleteUnitId(evt) ?? ExtractMissionIdFromTriggerContext(evt);
                if (!string.IsNullOrWhiteSpace(mid))
                {
                    AddListener(listenersByMission, mid!, evt.Id);
                }
            }

            foreach (var c in evt.Conditions ?? Array.Empty<ScenarioEventConditionDto>())
            {
                if (!string.Equals(c.Type, "MissionComplete", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(c.UnitId))
                {
                    continue;
                }

                AddListener(listenersByMission, c.UnitId.Trim(), evt.Id);
            }
        }

        foreach (var evt in events)
        {
            foreach (var action in evt.Actions ?? Array.Empty<ScenarioEventActionDto>())
            {
                if (!string.Equals(action.Type, "ActivateMission", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(action.UnitId))
                {
                    continue;
                }

                if (!listenersByMission.TryGetValue(action.UnitId.Trim(), out var listeners))
                {
                    continue;
                }

                foreach (var target in listeners)
                {
                    if (!adj.ContainsKey(evt.Id) || !adj.ContainsKey(target))
                    {
                        continue;
                    }

                    if (!adj[evt.Id].Contains(target, StringComparer.Ordinal))
                    {
                        adj[evt.Id].Add(target);
                    }
                }
            }
        }

        var inCycle = FindNodesInCycles(adj, nodes);
        foreach (var id in inCycle.OrderBy(x => x, StringComparer.Ordinal))
        {
            sink.Add(new ValidationFinding(
                CircularCode,
                ValidationSeverity.Warning,
                $"Event '{id}' participates in an ActivateMission→MissionComplete cycle.",
                Data: EventData(id)));
        }
    }

    private static string? FirstMissionCompleteUnitId(ScenarioEventDto evt)
    {
        foreach (var c in evt.Conditions ?? Array.Empty<ScenarioEventConditionDto>())
        {
            if (string.Equals(c.Type, "MissionComplete", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(c.UnitId))
            {
                return c.UnitId.Trim();
            }
        }

        // Fallback: any condition UnitId when trigger is MissionComplete.
        foreach (var c in evt.Conditions ?? Array.Empty<ScenarioEventConditionDto>())
        {
            if (!string.IsNullOrWhiteSpace(c.UnitId))
            {
                return c.UnitId.Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// When TriggerType is MissionComplete with no usable condition UnitId,
    /// no mission edge is formed (cannot resolve target).
    /// </summary>
    private static string? ExtractMissionIdFromTriggerContext(ScenarioEventDto evt) => null;

    private static void AddListener(
        Dictionary<string, List<string>> map,
        string missionId,
        string eventId)
    {
        if (!map.TryGetValue(missionId, out var list))
        {
            list = new List<string>();
            map[missionId] = list;
        }

        if (!list.Contains(eventId, StringComparer.Ordinal))
        {
            list.Add(eventId);
        }
    }

    /// <summary>
    /// Tarjan SCC: nodes in components of size &gt; 1, or a singleton with a self-loop,
    /// are on a cycle.
    /// </summary>
    private static HashSet<string> FindNodesInCycles(
        Dictionary<string, List<string>> adj,
        IReadOnlyList<string> nodes)
    {
        var index = 0;
        var stack = new Stack<string>();
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var lowlink = new Dictionary<string, int>(StringComparer.Ordinal);
        var inCycle = new HashSet<string>(StringComparer.Ordinal);

        void StrongConnect(string v)
        {
            indices[v] = index;
            lowlink[v] = index;
            index++;
            stack.Push(v);
            onStack.Add(v);

            if (adj.TryGetValue(v, out var neighbors))
            {
                foreach (var w in neighbors)
                {
                    if (!indices.ContainsKey(w))
                    {
                        StrongConnect(w);
                        lowlink[v] = Math.Min(lowlink[v], lowlink[w]);
                    }
                    else if (onStack.Contains(w))
                    {
                        lowlink[v] = Math.Min(lowlink[v], indices[w]);
                    }
                }
            }

            if (lowlink[v] != indices[v])
            {
                return;
            }

            var component = new List<string>();
            string w2;
            do
            {
                w2 = stack.Pop();
                onStack.Remove(w2);
                component.Add(w2);
            }
            while (!string.Equals(w2, v, StringComparison.Ordinal));

            var selfLoop = component.Count == 1
                && adj.TryGetValue(component[0], out var outs)
                && outs.Contains(component[0], StringComparer.Ordinal);

            if (component.Count > 1 || selfLoop)
            {
                foreach (var n in component)
                {
                    inCycle.Add(n);
                }
            }
        }

        foreach (var n in nodes)
        {
            if (!indices.ContainsKey(n))
            {
                StrongConnect(n);
            }
        }

        return inCycle;
    }

    private static Dictionary<string, string> EventData(string eventId) =>
        new() { ["eventId"] = eventId };

    /// <summary>Resolve event id from finding Data (or empty).</summary>
    public static string EventIdOf(ValidationFinding finding)
    {
        if (finding.Data != null
            && finding.Data.TryGetValue("eventId", out var id)
            && !string.IsNullOrEmpty(id))
        {
            return id;
        }

        return string.Empty;
    }
}
