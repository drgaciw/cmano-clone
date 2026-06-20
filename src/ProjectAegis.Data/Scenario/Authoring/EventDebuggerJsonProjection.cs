namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Event debugger JSON projection (GDD AC-7).</summary>
public static class EventDebuggerJsonProjection
{
    public static string SerializeProjection(ScenarioEventDto evt, bool fired, int lastEvaluatedTick)
    {
        var dto = new EventDebuggerEntryDto
        {
            EventId = evt.Id,
            Fired = fired,
            LastEvaluatedTick = lastEvaluatedTick,
            UnmetConditions = fired
                ? Array.Empty<EventDebuggerConditionDto>()
                : evt.Conditions
                    .Select(c => new EventDebuggerConditionDto
                    {
                        Type = c.Type,
                        Result = false,
                        UnitId = c.UnitId,
                        ZoneId = c.ZoneId,
                    })
                    .ToArray(),
        };

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private sealed class EventDebuggerEntryDto
    {
        public string EventId { get; init; } = "";

        public bool Fired { get; init; }

        public int LastEvaluatedTick { get; init; }

        public IReadOnlyList<EventDebuggerConditionDto> UnmetConditions { get; init; } = Array.Empty<EventDebuggerConditionDto>();
    }

    private sealed class EventDebuggerConditionDto
    {
        public string Type { get; init; } = "";

        public bool Result { get; init; }

        public string? UnitId { get; init; }

        public string? ZoneId { get; init; }
    }
}
