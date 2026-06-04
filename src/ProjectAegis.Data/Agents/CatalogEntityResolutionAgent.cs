namespace ProjectAegis.Data.Agents;

using ProjectAegis.Data.Catalog;

/// <summary>Req-06 entity resolution — canonical platform_id check (P0 alias table deferred).</summary>
public sealed class CatalogEntityResolutionAgent : IDatabaseIntelligenceAgent
{
    public string AgentId => "entity_resolution";

    public DatabaseAgentReport Run(DatabaseAgentContext context)
    {
        var findings = new List<DatabaseAgentFinding>();
        foreach (var binding in context.Catalog.GetSortedSensorBindings())
        {
            if (string.IsNullOrWhiteSpace(binding.PlatformId) || string.IsNullOrWhiteSpace(binding.SensorId))
            {
                findings.Add(new DatabaseAgentFinding(
                    "ENTITY_ID_EMPTY",
                    "platform_id or sensor_id is empty",
                    "error"));
                continue;
            }

            if (binding.PlatformId.Contains(' ', StringComparison.Ordinal))
            {
                findings.Add(new DatabaseAgentFinding(
                    "ENTITY_ID_ALIAS_REQUIRED",
                    $"{binding.PlatformId}: contains spaces — map alias before commit",
                    "warning"));
            }
        }

        return new DatabaseAgentReport(AgentId, findings.All(f => f.Severity != "error"), findings);
    }
}