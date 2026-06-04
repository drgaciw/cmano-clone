namespace ProjectAegis.Data.Agents;

using ProjectAegis.Data.Catalog;

/// <summary>Req-06 diff agent — surfaces staged write-gate batches (propose-only output).</summary>
public sealed class CatalogDiffProposalAgent : IDatabaseIntelligenceAgent
{
    public string AgentId => "diff_proposal";

    public DatabaseAgentReport Run(DatabaseAgentContext context)
    {
        if (string.IsNullOrWhiteSpace(context.DatabasePath) || !File.Exists(context.DatabasePath))
        {
            return new DatabaseAgentReport(AgentId, true,
            [
                new DatabaseAgentFinding(
                    "DIFF_SKIPPED",
                    "No database path — diff agent requires SQLite catalog for staging inspection",
                    "info"),
            ]);
        }

        using var gate = new WriteGate.CatalogWriteGate(context.DatabasePath);
        var pending = gate.ListPendingBatches();
        var findings = pending
            .Select(b => new DatabaseAgentFinding(
                "STAGED_BATCH_PENDING",
                $"{b.BatchId} records={b.RecordCount} actor={b.ActorType}:{b.ActorId}",
                "info"))
            .ToList();

        if (findings.Count == 0)
        {
            findings.Add(new DatabaseAgentFinding("DIFF_CLEAN", "No pending staging batches", "info"));
        }

        return new DatabaseAgentReport(AgentId, true, findings);
    }
}