namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Scenario;

/// <summary>Headless cyber/comms abort catalog + policy hooks (req 19).</summary>
public static class ScenarioCyberStatusCommand
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
            Console.Error.WriteLine("scenario_cyber_status requires --policy <id>");
            return 2;
        }

        var profile = ScenarioPolicyRepository.TryGet(policyId);
        if (profile == null)
        {
            output.WriteLine(JsonSerializer.Serialize(new { ok = false, policyId, error = "policy_not_found" }, JsonOptions));
            return 1;
        }

        var payload = new
        {
            ok = true,
            policyId,
            cyberAbortCodes = new[]
            {
                AbortReasonCatalog.Cyber.CYBER_LINK_DEGRADED,
                AbortReasonCatalog.Cyber.CYBER_LINK_DOWN,
                AbortReasonCatalog.Cyber.CYBER_ORDER_DELAY,
                AbortReasonCatalog.Cyber.CYBER_SPOOF_TRACK,
            },
            commsOrderDelayTicks = profile.CommsDisplay.DegradedOrderDelayTicks,
            commsTransitions = profile.CommsTransitions.Count,
            mcpTools = new[] { "scenario_comms_status", "scenario_validate", "mission_plan_suggest" },
        };
        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}