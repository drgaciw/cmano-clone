namespace ProjectAegis.Data.Agents;

using ProjectAegis.Data.Catalog;

/// <summary>Req-06 agent pipeline step (headless, deterministic — no LLM in tick path).</summary>
public interface IDatabaseIntelligenceAgent
{
    string AgentId { get; }

    DatabaseAgentReport Run(DatabaseAgentContext context);
}

public sealed record DatabaseAgentContext(
    ICatalogReader Catalog,
    string? DatabasePath = null);

public sealed record DatabaseAgentReport(
    string AgentId,
    bool Passed,
    IReadOnlyList<DatabaseAgentFinding> Findings);

public sealed record DatabaseAgentFinding(
    string Code,
    string Message,
    string Severity);