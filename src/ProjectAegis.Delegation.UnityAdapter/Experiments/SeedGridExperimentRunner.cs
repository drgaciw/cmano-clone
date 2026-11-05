using ProjectAegis.Delegation.UnityAdapter.Baltic;

namespace ProjectAegis.Delegation.UnityAdapter.Experiments;

/// <summary>Runs the seed-only experiment grid through the existing headless Baltic batch path.</summary>
public static class SeedGridExperimentRunner
{
    /// <summary>Runs once per seed in input order, preserving duplicates. Opaque metadata never changes runtime policy.</summary>
    public static IReadOnlyList<BalticBatchRunner.BatchRow> Run(ExperimentDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        return BalticBatchRunner.Run(new BalticBatchRunner.BatchRequest(
            new[] { definition.ScenarioId }, definition.Seeds, definition.Ticks));
    }

    /// <summary>Exports the existing stable score CSV, including each real order-log fingerprint.</summary>
    public static string ExportCsv(IReadOnlyList<BalticBatchRunner.BatchRow> rows)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        return BalticBatchRunner.ExportCsv(rows);
    }
}
