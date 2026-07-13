namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectAegis.Data.Scenario.Authoring;

/// <summary>AC-5 integration record emitted after a successful headless sample run.</summary>
public static class SampleCompleteRecorder
{
    public const int FifteenMinuteSampleTicks = 900;

    public static string Format(
        string scenarioPath,
        ScenarioDocumentDto scenario,
        int ticks,
        string worldHash,
        int seed)
    {
        var missionTypes = scenario.Missions
            .Select(m => m.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();

        var record = new SampleCompleteRecordDto
        {
            RecordType = "sample-complete",
            ScenarioPath = scenarioPath,
            Ticks = ticks,
            Seed = seed,
            WorldHash = worldHash,
            MissionTypes = missionTypes,
            MissionCount = scenario.Missions.Count,
        };

        return JsonSerializer.Serialize(record, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private sealed class SampleCompleteRecordDto
    {
        public string RecordType { get; init; } = "";

        public string ScenarioPath { get; init; } = "";

        public int Ticks { get; init; }

        public int Seed { get; init; }

        public string WorldHash { get; init; } = "";

        public string[] MissionTypes { get; init; } = Array.Empty<string>();

        public int MissionCount { get; init; }
    }
}