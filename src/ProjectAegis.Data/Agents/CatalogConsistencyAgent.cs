namespace ProjectAegis.Data.Agents;

using ProjectAegis.Data.Catalog;

/// <summary>Req-06 consistency agent — flags base_pd outliers vs catalog median (P0 heuristic).</summary>
public sealed class CatalogConsistencyAgent : IDatabaseIntelligenceAgent
{
    public const double OutlierDeltaThreshold = 0.35;

    public string AgentId => "consistency_normalization";

    public DatabaseAgentReport Run(DatabaseAgentContext context)
    {
        var bindings = context.Catalog.GetSortedSensorBindings();
        if (bindings.Count == 0)
        {
            return new DatabaseAgentReport(AgentId, true, []);
        }

        var median = bindings.Select(b => b.BasePd).OrderBy(v => v).ElementAt(bindings.Count / 2);
        var findings = new List<DatabaseAgentFinding>();
        foreach (var binding in bindings)
        {
            var delta = Math.Abs(binding.BasePd - median);
            if (delta > OutlierDeltaThreshold)
            {
                findings.Add(new DatabaseAgentFinding(
                    "BASE_PD_OUTLIER",
                    $"{binding.PlatformId}/{binding.SensorId} base_pd={binding.BasePd:F3} median={median:F3} delta={delta:F3}",
                    "warning"));
            }
        }

        return new DatabaseAgentReport(AgentId, findings.All(f => f.Severity != "error"), findings);
    }
}