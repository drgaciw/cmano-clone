namespace ProjectAegis.Data.Catalog;

public sealed record MountLoadoutQuarantineRow(
    string Domain,
    string ChildKind,
    string BatchId,
    string PlatformId,
    string ChildId,
    string Reason,
    string? RepairRule);

public sealed record MountLoadoutDomainQuarantineCounts(
    string Domain,
    int MountQuarantined,
    int LoadoutQuarantined,
    int FittingQuarantined,
    int Repairable,
    int OutOfEnvelope);

public sealed record MountLoadoutQuarantineTriageResult(
    bool Ok,
    bool DryRun,
    string DatabasePath,
    IReadOnlyList<MountLoadoutDomainQuarantineCounts> Before,
    IReadOnlyList<MountLoadoutDomainQuarantineCounts> After,
    IReadOnlyList<MountLoadoutQuarantineRow> RemainingQuarantine,
    IReadOnlyList<string> RepairedBatchIds,
    IReadOnlyList<string> AdvisoryNotes);