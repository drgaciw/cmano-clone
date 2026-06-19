namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Catalog;

/// <summary>S32-03 off-CI curator triage for mount/loadout quarantine child rows.</summary>
public static class MountLoadoutQuarantineTriageCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(
        string databasePath,
        bool dryRun,
        string? entityHint,
        string? proposeJsonPath,
        TextWriter output)
    {
        try
        {
            var result = MountLoadoutQuarantineTriage.Run(
                databasePath,
                dryRun,
                entityHint,
                proposeJsonPath);

            var payload = new
            {
                ok = result.Ok,
                dryRun = result.DryRun,
                databasePath = result.DatabasePath,
                repairEnvelope = MountLoadoutQuarantineRepairEnvelope.RepairRules,
                before = result.Before.Select(ToPayload),
                after = result.After.Select(ToPayload),
                remainingQuarantine = result.RemainingQuarantine.Select(row => new
                {
                    row.Domain,
                    row.ChildKind,
                    row.BatchId,
                    row.PlatformId,
                    row.ChildId,
                    row.Reason,
                    row.RepairRule,
                }),
                repairedBatchIds = result.RepairedBatchIds,
                advisoryNotes = result.AdvisoryNotes,
            };

            output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
            return result.Ok ? 0 : 1;
        }
        catch (Exception ex)
        {
            output.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOptions));
            return 1;
        }
    }

    private static object ToPayload(MountLoadoutDomainQuarantineCounts counts) => new
    {
        domain = counts.Domain,
        mountQuarantined = counts.MountQuarantined,
        loadoutQuarantined = counts.LoadoutQuarantined,
        fittingQuarantined = counts.FittingQuarantined,
        repairable = counts.Repairable,
        outOfEnvelope = counts.OutOfEnvelope,
    };
}