namespace ProjectAegis.Data.Agents;

using ProjectAegis.Data.Catalog;

/// <summary>Req-06 rules agent — TRL/review/confidence gates (wraps CatalogImportGate).</summary>
public sealed class CatalogRulesValidationAgent : IDatabaseIntelligenceAgent
{
    public string AgentId => "rules_validation";

    public DatabaseAgentReport Run(DatabaseAgentContext context)
    {
        var findings = new List<DatabaseAgentFinding>();
        var (_, quarantined) = CatalogImportGate.PartitionForImport(context.Catalog.GetSortedSensorBindings());
        foreach (var row in quarantined.OrderBy(q => q.Binding.PlatformId, StringComparer.Ordinal)
                     .ThenBy(q => q.Binding.SensorId, StringComparer.Ordinal))
        {
            findings.Add(new DatabaseAgentFinding(
                "RULE_GATE_REJECT",
                $"{row.Binding.PlatformId}/{row.Binding.SensorId}: {row.RejectionReason}",
                "error"));
        }

        return new DatabaseAgentReport(AgentId, findings.Count == 0, findings);
    }
}