namespace ProjectAegis.Delegation.UnityAdapter.Baltic;

using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;

/// <summary>Headless multi-scenario / multi-seed batch for agent-vs-agent CSV export (GDD agentic-infrastructure).</summary>
public static class BalticBatchRunner
{
    public sealed record BatchRequest(
        IReadOnlyList<string> ScenarioIds,
        IReadOnlyList<int> Seeds,
        int Ticks,
        bool MvpEngagement = true,
        string Side = "BLUE");

    public sealed record BatchRow(
        string ScenarioId,
        int Seed,
        string Side,
        BalticReplayHarness.Result Result);

    public static IReadOnlyList<BatchRow> Run(BatchRequest request)
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var rows = new List<BatchRow>(request.ScenarioIds.Count * request.Seeds.Count);
        foreach (var scenario in request.ScenarioIds)
        {
            if (ScenarioPolicyRepository.TryGet(scenario) == null)
            {
                throw new InvalidOperationException($"Unknown scenario policy: {scenario}");
            }

            foreach (var seed in request.Seeds)
            {
                var result = BalticReplayHarness.Run(
                    seed,
                    scenario,
                    request.Ticks,
                    mvpEngagement: request.MvpEngagement);
                rows.Add(new BatchRow(scenario, seed, request.Side, result));
            }
        }

        return rows;
    }

    public static string ExportCsv(IReadOnlyList<BatchRow> rows)
    {
        var lines = rows.Select(r => r.Result.ScoringCsvRow);
        return LossesScoringCsvExporter.FormatBatch(lines);
    }

    public static IReadOnlyList<string> DiscoverScenarioIds()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        return ScenarioPolicyRepository.AllIds()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }
}