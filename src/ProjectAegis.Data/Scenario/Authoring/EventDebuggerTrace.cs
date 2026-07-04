namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// AC-7 event debugger: projects structured JSON for a single event evaluation.
/// Filtered view aligned with order-log <c>EventFired</c> semantics (AME-5.5).
/// </summary>
public static class EventDebuggerTrace
{
    public const int DefaultEvaluationHorizonTicks = 32;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string ToJson(ScenarioDocumentDto document, string eventId, int? evaluationHorizonTicks = null)
    {
        var trace = Evaluate(document, eventId, evaluationHorizonTicks);
        return JsonSerializer.Serialize(trace, JsonOptions);
    }

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
                UnmetConditions = Array.Empty<EventDebuggerUnmetConditionDto>(),
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

        return new EventDebuggerTraceDto
        {
            EventId = evt.Id,
            Fired = fired,
            LastEvaluatedTick = fired ? 0 : horizon,
            UnmetConditions = unmet,
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

    public sealed class EventDebuggerTraceDto
    {
        [JsonPropertyName("event_id")]
        public string EventId { get; init; } = "";

        [JsonPropertyName("fired")]
        public bool Fired { get; init; }

        [JsonPropertyName("last_evaluated_tick")]
        public int LastEvaluatedTick { get; init; }

        [JsonPropertyName("unmet_conditions")]
        public IReadOnlyList<EventDebuggerUnmetConditionDto> UnmetConditions { get; init; } =
            Array.Empty<EventDebuggerUnmetConditionDto>();
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