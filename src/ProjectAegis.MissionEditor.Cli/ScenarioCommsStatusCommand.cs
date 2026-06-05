namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Sim.Scenario;

/// <summary>Headless comms policy snapshot for MCP (req 19).</summary>
public static class ScenarioCommsStatusCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string? policyId, TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            Console.Error.WriteLine("scenario_comms_status requires --policy <id>");
            return 2;
        }

        var profile = ScenarioPolicyRepository.TryGet(policyId);
        if (profile == null)
        {
            var missing = new { ok = false, policyId, error = "policy_not_found" };
            output.WriteLine(JsonSerializer.Serialize(missing, JsonOptions));
            return 1;
        }

        var display = profile.CommsDisplay;
        var payload = new
        {
            ok = true,
            policyId,
            commsDisplay = new
            {
                display.DegradedLagTicks,
                display.GhostOffsetX,
                display.GhostOffsetY,
                display.DegradedOrderDelayTicks,
                display.DegradedStaleThresholdDivisor,
            },
            commsTimeline = profile.CommsTransitions
                .OrderBy(c => c.AtTick)
                .Select(c => new { c.AtTick, state = c.NewState, c.NodeId, c.Reason })
                .ToArray(),
        };
        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}