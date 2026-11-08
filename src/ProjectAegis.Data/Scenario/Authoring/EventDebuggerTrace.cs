namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// AC-7 event debugger: projects structured JSON for a single event evaluation.
/// Filtered view aligned with order-log <c>EventFired</c> semantics (AME-5.5).
/// Full projection fields: <c>sim_tick</c>, <c>sequence_id</c>, <c>action_results</c>
/// plus existing <c>event_id</c>, <c>fired</c>, <c>last_evaluated_tick</c>, <c>unmet_conditions</c>.
/// </summary>
public static class EventDebuggerTrace
{
    public const int DefaultEvaluationHorizonTicks = 32;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes the AC-7 debugger projection for <paramref name="eventId"/> as compact JSON.
    /// </summary>
    public static string ToJson(ScenarioDocumentDto document, string eventId, int? evaluationHorizonTicks = null)
    {
        var trace = Evaluate(document, eventId, evaluationHorizonTicks);
        return JsonSerializer.Serialize(trace, JsonOptions);
    }

    /// <summary>
    /// Evaluates a single event and returns the full AC-7 debugger projection DTO.
    /// </summary>
    public static EventDebuggerTraceDto Evaluate(
        ScenarioDocumentDto document,
        string eventId,
        int? evaluationHorizonTicks = null)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            eventId = "evt";
        }

        var horizon = evaluationHorizonTicks ?? DefaultEvaluationHorizonTicks;
        var evt = FindEvent(document, eventId);
        if (evt == null)
        {
            return new EventDebuggerTraceDto
            {
                EventId = eventId,
                Fired = false,
                LastEvaluatedTick = 0,
                SimTick = 0,
                SequenceId = 0,
                UnmetConditions = Array.Empty<EventDebuggerUnmetConditionDto>(),
                ActionResults = Array.Empty<EventDebuggerActionResultDto>(),
            };
        }

        var unmet = new List<EventDebuggerUnmetConditionDto>();
        foreach (var condition in evt.Conditions)
        {
            if (!EvaluateCondition(document, condition, out var detail))
            {
                unmet.Add(detail);
            }
        }

        var fired = unmet.Count == 0 && evt.Conditions.Count > 0;
        if (evt.Conditions.Count == 0)
        {
            fired = string.Equals(evt.TriggerType, "Time", StringComparison.OrdinalIgnoreCase);
        }

        var lastEvaluatedTick = fired ? 0 : horizon;
        // When fired: sim_tick is 0 (fires at evaluation start). When not: mirrors last_evaluated_tick/horizon.
        var simTick = fired ? 0 : lastEvaluatedTick;

        return new EventDebuggerTraceDto
        {
            EventId = evt.Id,
            Fired = fired,
            LastEvaluatedTick = lastEvaluatedTick,
            SimTick = simTick,
            SequenceId = ResolveSequenceId(document, evt.Id),
            UnmetConditions = unmet,
            ActionResults = BuildActionResults(evt, fired),
        };
    }

    private static ScenarioEventDto? FindEvent(ScenarioDocumentDto document, string eventId)
    {
        if (document.Events == null || document.Events.Count == 0)
        {
            return null;
        }

        return document.Events.FirstOrDefault(e =>
            string.Equals(e.Id, eventId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Index of the event in <c>document.Events</c> ordered by id ordinal (stable), or 0 if missing.
    /// </summary>
    private static int ResolveSequenceId(ScenarioDocumentDto document, string eventId)
    {
        if (document.Events == null || document.Events.Count == 0)
        {
            return 0;
        }

        // Stable OrderBy (LINQ) on id ordinal — authoring path, not a hot loop.
        var ordered = document.Events
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            if (string.Equals(ordered[i].Id, eventId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }

    private static IReadOnlyList<EventDebuggerActionResultDto> BuildActionResults(
        ScenarioEventDto evt,
        bool fired)
    {
        if (evt.Actions == null || evt.Actions.Count == 0)
        {
            return Array.Empty<EventDebuggerActionResultDto>();
        }

        var results = new List<EventDebuggerActionResultDto>(evt.Actions.Count);
        foreach (var action in evt.Actions)
        {
            results.Add(new EventDebuggerActionResultDto
            {
                Type = action.Type ?? "",
                Applied = fired,
                Note = fired ? null : "not-fired",
            });
        }

        return results;
    }

    private static bool EvaluateCondition(
        ScenarioDocumentDto document,
        ScenarioEventConditionDto condition,
        out EventDebuggerUnmetConditionDto detail)
    {
        detail = new EventDebuggerUnmetConditionDto
        {
            Type = condition.Type,
            Result = false,
            UnitId = condition.UnitId,
            ZoneId = condition.ZoneId,
        };

        bool holds;
        if (condition.Result == false)
        {
            holds = false;
        }
        else if (string.Equals(condition.Type, "UnitEntersZone", StringComparison.OrdinalIgnoreCase))
        {
            // Authoring documents do not carry live unit positions; without sim state
            // the zone-entry predicate never holds (AC-7 event-no-fire case).
            holds = IsUnitInZone(document, condition.UnitId, condition.ZoneId);
        }
        else if (condition.Result == true)
        {
            holds = true;
        }
        else
        {
            // Default authoring evaluation: without live sim snapshot, most conditions
            // (ContactDetected, Variable, etc.) are treated unmet for debugger projection.
            // Matches order-log unmet_conditions semantics (AME-5.5, AC-7).
            holds = false;
            detail.Note = "no-sim-state";
        }

        if (!holds && detail.Note == null && string.Equals(condition.Type, "UnitEntersZone", StringComparison.OrdinalIgnoreCase))
        {
            detail.Note = "no-sim-state";
        }

        detail.Result = holds;
        return holds;
    }

    private static bool IsUnitInZone(ScenarioDocumentDto document, string? unitId, string? zoneId)
    {
        if (string.IsNullOrWhiteSpace(unitId) || string.IsNullOrWhiteSpace(zoneId))
        {
            return false;
        }

        if (document.EditorState != null &&
            document.EditorState.TryGetValue("unitZonePresence", out var presence) &&
            presence.ValueKind == JsonValueKind.Object &&
            presence.TryGetProperty(unitId, out var zoneValue) &&
            zoneValue.ValueKind == JsonValueKind.String)
        {
            return string.Equals(zoneValue.GetString(), zoneId, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>Full AC-7 event debugger projection (order-log-aligned fields).</summary>
    public sealed class EventDebuggerTraceDto
    {
        [JsonPropertyName("event_id")]
        public string EventId { get; init; } = "";

        [JsonPropertyName("fired")]
        public bool Fired { get; init; }

        [JsonPropertyName("last_evaluated_tick")]
        public int LastEvaluatedTick { get; init; }

        /// <summary>
        /// Sim tick of the evaluation: 0 when fired; otherwise equals
        /// <see cref="LastEvaluatedTick"/> (horizon).
        /// </summary>
        [JsonPropertyName("sim_tick")]
        public int SimTick { get; init; }

        /// <summary>
        /// Index of the event in <c>document.Events</c> ordered by id ordinal (stable),
        /// or 0 when the event is missing.
        /// </summary>
        [JsonPropertyName("sequence_id")]
        public int SequenceId { get; init; }

        [JsonPropertyName("unmet_conditions")]
        public IReadOnlyList<EventDebuggerUnmetConditionDto> UnmetConditions { get; init; } =
            Array.Empty<EventDebuggerUnmetConditionDto>();

        /// <summary>
        /// Per-action results. When fired, each action has <c>applied=true</c>;
        /// when not fired, <c>applied=false</c> with <c>note="not-fired"</c>.
        /// </summary>
        [JsonPropertyName("action_results")]
        public IReadOnlyList<EventDebuggerActionResultDto> ActionResults { get; init; } =
            Array.Empty<EventDebuggerActionResultDto>();
    }

    /// <summary>Per-action projection entry for AC-7 <c>action_results</c>.</summary>
    public sealed class EventDebuggerActionResultDto
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "";

        [JsonPropertyName("applied")]
        public bool Applied { get; init; }

        /// <summary>Optional note (e.g. <c>not-fired</c> when the parent event did not fire).</summary>
        [JsonPropertyName("note")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Note { get; init; }
    }

    public sealed class EventDebuggerUnmetConditionDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("result")]
        public bool Result { get; set; }

        [JsonPropertyName("unit_id")]
        public string? UnitId { get; set; }

        [JsonPropertyName("zone_id")]
        public string? ZoneId { get; set; }

        /// <summary>Optional detail for richer unmet reporting (e.g. "no-sim-state" for authoring debugger projection).</summary>
        [JsonPropertyName("note")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Note { get; set; }
    }
}
