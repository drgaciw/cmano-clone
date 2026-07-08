namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless event-graph view model (ME-W2 / AME-5.5/5.7): nodes, fire-order and
/// ActivateMission edges, static-analysis codes, and selected-event debugger JSON.
/// Consumes <see cref="EventStaticAnalyzer"/> and <see cref="EventDebuggerTrace"/> only;
/// does not touch DelegationBridge. Does not depend on UnityEngine.
/// </summary>
public sealed class EventGraphNodeView
{
    /// <summary>Scenario event id.</summary>
    public string EventId { get; init; } = "";

    /// <summary>Event trigger type (e.g. Time, MissionComplete, UnitDestroyed).</summary>
    public string TriggerType { get; init; } = "";

    /// <summary>
    /// Stable sequence index among events ordered by id ordinal
    /// (matches <see cref="EventDebuggerTrace"/> sequence_id).
    /// </summary>
    public int SequenceId { get; init; }

    /// <summary>
    /// True when the AC-7 debugger evaluation reports the event would fire under
    /// authoring-time (no live sim) rules.
    /// </summary>
    public bool LastFired { get; init; }
}

/// <summary>Directed edge in the headless event graph.</summary>
public sealed class EventGraphEdgeView
{
    /// <summary>Source event id.</summary>
    public string FromEventId { get; init; } = "";

    /// <summary>Target event id.</summary>
    public string ToEventId { get; init; } = "";

    /// <summary>
    /// Edge kind: <c>FireOrder</c> (consecutive id-ordinal chain) or
    /// <c>ActivateMission</c> (ActivateMission → MissionComplete of same mission).
    /// </summary>
    public string Kind { get; init; } = "";
}

/// <summary>
/// Headless Event Graph presenter bound to a <see cref="ScenarioAuthoringSession"/>
/// and <see cref="LiveFindingsPresenter"/> (composition parity with Mission Board).
/// </summary>
public sealed class EventGraphPresenter
{
    private const string FireOrderKind = "FireOrder";
    private const string ActivateMissionKind = "ActivateMission";

    private readonly ScenarioAuthoringSession _session;
    private readonly LiveFindingsPresenter _findings;

    /// <summary>
    /// Creates an event-graph presenter bound to the given authoring session and findings façade.
    /// </summary>
    /// <param name="session">Open scenario authoring session.</param>
    /// <param name="findings">
    /// Live findings presenter retained for host composition (jump-from-finding);
    /// static EVENT_* codes are sourced from <see cref="EventStaticAnalyzer"/> on
    /// <see cref="Refresh"/>.
    /// </param>
    public EventGraphPresenter(ScenarioAuthoringSession session, LiveFindingsPresenter findings)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _findings = findings ?? throw new ArgumentNullException(nameof(findings));
    }

    /// <summary>Last graph nodes from <see cref="Refresh"/> (ordered by id ordinal).</summary>
    public IReadOnlyList<EventGraphNodeView> Nodes { get; private set; } = Array.Empty<EventGraphNodeView>();

    /// <summary>Last graph edges from <see cref="Refresh"/> (deterministic order).</summary>
    public IReadOnlyList<EventGraphEdgeView> Edges { get; private set; } = Array.Empty<EventGraphEdgeView>();

    /// <summary>
    /// EVENT_* codes from <see cref="EventStaticAnalyzer.Analyze"/> on the current document,
    /// ordered by ordinal string comparison.
    /// </summary>
    public IReadOnlyList<string> StaticAnalysisCodes { get; private set; } = Array.Empty<string>();

    /// <summary>Currently selected event id, or null when none selected.</summary>
    public string? SelectedEventId { get; private set; }

    /// <summary>
    /// Live findings façade supplied at construction (host may use for jump-to wiring).
    /// </summary>
    public LiveFindingsPresenter Findings => _findings;

    /// <summary>
    /// Rebuilds <see cref="Nodes"/>, <see cref="Edges"/>, and
    /// <see cref="StaticAnalysisCodes"/> from the session document.
    /// </summary>
    public void Refresh()
    {
        var dto = _session.Editor.ToDto();
        var events = dto.Events ?? Array.Empty<ScenarioEventDto>();
        var ordered = events
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .ToArray();

        var nodes = new EventGraphNodeView[ordered.Length];
        for (var i = 0; i < ordered.Length; i++)
        {
            var evt = ordered[i];
            var fired = EventDebuggerTrace.Evaluate(dto, evt.Id).Fired;
            nodes[i] = new EventGraphNodeView
            {
                EventId = evt.Id,
                TriggerType = evt.TriggerType ?? "",
                SequenceId = i,
                LastFired = fired,
            };
        }

        Nodes = nodes;
        Edges = BuildEdges(ordered);
        StaticAnalysisCodes = EventStaticAnalyzer.Analyze(dto)
            .Select(f => f.Code)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Sets <see cref="SelectedEventId"/> (may be null to clear selection).</summary>
    public void SelectEvent(string? eventId) => SelectedEventId = eventId;

    /// <summary>
    /// Returns AC-7 debugger JSON for the selected event via
    /// <see cref="ScenarioDocumentEditor.ExplainEventTrace"/>. Empty string when none selected.
    /// </summary>
    public string ExplainSelected()
    {
        if (string.IsNullOrWhiteSpace(SelectedEventId))
        {
            return string.Empty;
        }

        return _session.Editor.ExplainEventTrace(SelectedEventId);
    }

    /// <summary>
    /// Builds FireOrder (consecutive id-ordinal) and ActivateMission edges.
    /// </summary>
    private static IReadOnlyList<EventGraphEdgeView> BuildEdges(IReadOnlyList<ScenarioEventDto> orderedById)
    {
        var edges = new List<EventGraphEdgeView>();

        // 1) FireOrder: consecutive events ordered by Id ordinal.
        for (var i = 0; i + 1 < orderedById.Count; i++)
        {
            edges.Add(new EventGraphEdgeView
            {
                FromEventId = orderedById[i].Id,
                ToEventId = orderedById[i + 1].Id,
                Kind = FireOrderKind,
            });
        }

        // 2) ActivateMission: A → B when A ActivateMission UnitId=M and B listens for MissionComplete M.
        var listenersByMission = BuildMissionCompleteListeners(orderedById);
        foreach (var evt in orderedById)
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

                var missionId = action.UnitId.Trim();
                if (!listenersByMission.TryGetValue(missionId, out var listeners))
                {
                    continue;
                }

                foreach (var targetId in listeners)
                {
                    if (string.Equals(targetId, evt.Id, StringComparison.Ordinal))
                    {
                        // Self-loop still valid for circular visualization; include it.
                    }

                    // Dedup same A→B ActivateMission edge.
                    if (edges.Exists(e =>
                            string.Equals(e.Kind, ActivateMissionKind, StringComparison.Ordinal)
                            && string.Equals(e.FromEventId, evt.Id, StringComparison.Ordinal)
                            && string.Equals(e.ToEventId, targetId, StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    edges.Add(new EventGraphEdgeView
                    {
                        FromEventId = evt.Id,
                        ToEventId = targetId,
                        Kind = ActivateMissionKind,
                    });
                }
            }
        }

        return edges
            .OrderBy(e => e.Kind, StringComparer.Ordinal)
            .ThenBy(e => e.FromEventId, StringComparer.Ordinal)
            .ThenBy(e => e.ToEventId, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// missionId → event ids that complete / listen for that mission
    /// (TriggerType MissionComplete with UnitId, or MissionComplete condition).
    /// </summary>
    private static Dictionary<string, List<string>> BuildMissionCompleteListeners(
        IReadOnlyList<ScenarioEventDto> events)
    {
        var listenersByMission = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var evt in events)
        {
            if (string.Equals(evt.TriggerType, "MissionComplete", StringComparison.OrdinalIgnoreCase))
            {
                var mid = FirstMissionUnitId(evt);
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

        return listenersByMission;
    }

    private static string? FirstMissionUnitId(ScenarioEventDto evt)
    {
        foreach (var c in evt.Conditions ?? Array.Empty<ScenarioEventConditionDto>())
        {
            if (string.Equals(c.Type, "MissionComplete", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(c.UnitId))
            {
                return c.UnitId.Trim();
            }
        }

        foreach (var c in evt.Conditions ?? Array.Empty<ScenarioEventConditionDto>())
        {
            if (!string.IsNullOrWhiteSpace(c.UnitId))
            {
                return c.UnitId.Trim();
            }
        }

        return null;
    }

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
}
