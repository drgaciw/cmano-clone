using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.UnityAdapter.Experiments;

const string metadataNotice = "doctrineId and roeId are metadata only; no runtime policy overrides are applied.";
if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
{
    Console.WriteLine("Usage: experiment-runner <definition.json> <results.csv>");
    Console.WriteLine("Run from the repository root so scenario policy fixtures can be discovered.");
    Console.WriteLine("JSON fields: scenarioId, seeds (ordered nonempty int32 grid), ticks (positive int32), optional doctrineId/roeId.");
    Console.WriteLine("Seeds run in input order; duplicates are retained. " + metadataNotice);
    return 0;
}
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: experiment-runner <definition.json> <results.csv> (or --help)");
    return 2;
}

try
{
    var definition = ExperimentDefinitionJson.Parse(File.ReadAllText(args[0]));
    Console.Error.WriteLine(metadataNotice);
    if (definition.DoctrineId != null || definition.RoeId != null)
        Console.Error.WriteLine("Metadata: " + ExperimentDefinitionJson.Serialize(definition));
    var rows = SeedGridExperimentRunner.Run(definition);
    File.WriteAllText(args[1], SeedGridExperimentRunner.ExportCsv(rows), new UTF8Encoding(false));
    Console.WriteLine($"Wrote {rows.Count.ToString(CultureInfo.InvariantCulture)} runs to {args[1]}.");
    return 0;
}
catch (Exception exception) when (exception is FormatException or IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
{
    Console.Error.WriteLine("Experiment failed: " + exception.Message);
    return 1;
}
