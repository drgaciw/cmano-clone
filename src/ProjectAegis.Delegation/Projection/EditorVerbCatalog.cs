namespace ProjectAegis.Delegation.Projection;

/// <summary>Which editor surface exposes a verb.</summary>
public enum EditorSurface
{
    PlatformEditor,
    MissionEditor,
    CatalogTools,
}

/// <summary>The <c>IWriteGate</c> operation class a verb performs (if any).</summary>
public enum EditorWriteGateOperation
{
    None,
    Propose,
    Approve,
    Reject,
    ListPending,
}

/// <summary>Observable effect of a verb on persistent state.</summary>
public enum EditorVerbEffect
{
    ReadOnly,
    DraftFile,
    ValidatedArtifact,
    StagedBatch,
    CommittedBatch,
    DiscardedBatch,
}

/// <summary>One UI/CLI verb and the write-gate behavior it actually has.</summary>
public sealed record EditorVerbEntry(
    string VerbId,
    EditorSurface Surface,
    string UiLabel,
    string? UxmlButtonName,
    string? CliVerb,
    EditorWriteGateOperation GateOperation,
    IReadOnlyList<string> GateMethods,
    EditorVerbEffect Effect,
    string Description);

/// <summary>
/// S125-05 / AUTH-03 (Doc 11 / Doc 21 verb honesty): the verb table for Platform/Mission Editor chrome and
/// CLI. Labels name what happens: <b>Propose</b> only stages batches, <b>Approve</b> is the sole commit,
/// <b>Reject</b> discards staging, <b>Save</b> never touches the write gate, and <b>Export</b> is either a
/// read-only workbook export (PE) or the validated scenario artifact (ME).
/// Mirrored in <c>docs/engineering/editor-verb-honesty.md</c>.
/// </summary>
public static class EditorVerbCatalog
{
    private static readonly string[] WorkbookProposeMethods =
    [
        "ProposeSensorBatch", "ProposeMountBatch", "ProposeLoadoutBatch", "ProposeMagazineBatch",
        "ProposeCommsBatch", "ProposeLinkCatalogBatch", "ProposeMobilityBatch", "ProposeSignatureBatch",
        "ProposeEmconBatch", "ProposeSwarmBatch", "ProposePlatformDamageBatch",
    ];

    private static readonly string[] MarkdownProposeMethods =
    [
        "ProposeSensorBatch", "ProposePlatformBatch", "ProposeWeaponBatch",
        "ProposeMountBatch", "ProposeLoadoutBatch", "ProposeMagazineBatch",
    ];

    public static IReadOnlyList<EditorVerbEntry> Entries { get; } =
    [
        new("pe.export_workbook", EditorSurface.PlatformEditor, "Export", "platform-catalog-export", "platform_export_xlsx",
            EditorWriteGateOperation.None, [], EditorVerbEffect.ReadOnly,
            "Writes the bound snapshot to a workbook; no catalog write."),
        new("pe.diff_workbook", EditorSurface.PlatformEditor, "Diff", "platform-catalog-diff", "platform_diff_xlsx",
            EditorWriteGateOperation.None, [], EditorVerbEffect.ReadOnly,
            "Diffs an edited workbook against its source snapshot; no catalog write."),
        new("pe.propose", EditorSurface.PlatformEditor, "Propose", "platform-import-propose", "platform_import_xlsx",
            EditorWriteGateOperation.Propose, WorkbookProposeMethods, EditorVerbEffect.StagedBatch,
            "Stages workbook changes as pending batches; nothing reaches live tables until Approve."),
        new("pe.approve", EditorSurface.PlatformEditor, "Approve", "platform-import-approve", "catalog_write_approve",
            EditorWriteGateOperation.Approve, ["ApproveBatch"], EditorVerbEffect.CommittedBatch,
            "Commits a pending batch to live tables (human approval)."),
        new("pe.reject", EditorSurface.PlatformEditor, "Reject", "platform-import-reject", null,
            EditorWriteGateOperation.Reject, ["RejectBatch"], EditorVerbEffect.DiscardedBatch,
            "Discards pending staging; live tables unchanged. No CLI verb today."),
        new("catalog.write_propose", EditorSurface.CatalogTools, "Propose", null, "catalog_write_propose",
            EditorWriteGateOperation.Propose, ["ProposeSensorBatch"], EditorVerbEffect.StagedBatch,
            "Stages a single sensor binding edit."),
        new("catalog.import_markdown", EditorSurface.CatalogTools, "Propose", null, "catalog_import_markdown",
            EditorWriteGateOperation.Propose, MarkdownProposeMethods, EditorVerbEffect.StagedBatch,
            "Stages rows parsed from markdown fixtures; never approves."),
        new("catalog.osint_pending", EditorSurface.CatalogTools, "List pending", null, "osint_staging_review",
            EditorWriteGateOperation.ListPending, ["ListPendingBatches"], EditorVerbEffect.ReadOnly,
            "Lists pending batches (no --approve flag)."),
        new("catalog.osint_approve", EditorSurface.CatalogTools, "Approve", null, "osint_staging_review",
            EditorWriteGateOperation.Approve, ["ApproveBatch"], EditorVerbEffect.CommittedBatch,
            "Commits the batch named by --approve."),
        new("me.save", EditorSurface.MissionEditor, EditorSaveExportProjection.SaveLabel, "scenario-editor-shell-btn-save", null,
            EditorWriteGateOperation.None, [], EditorVerbEffect.DraftFile,
            "Persists the scenario draft; allowed with blocking findings; no export, no write gate."),
        new("me.export", EditorSurface.MissionEditor, EditorSaveExportProjection.ExportLabel, null, "scenario_export",
            EditorWriteGateOperation.None, [], EditorVerbEffect.ValidatedArtifact,
            "Runs the validation export gate; blocked on error findings."),
        new("me.publish", EditorSurface.MissionEditor, "Publish", null, "scenario_publish",
            EditorWriteGateOperation.None, [], EditorVerbEffect.ValidatedArtifact,
            "Emits a scenario manifest only when the export gate passes."),
        new("me.sample", EditorSurface.MissionEditor, "Sample", "scenario-editor-shell-btn-sample", "scenario_simulate_sample",
            EditorWriteGateOperation.None, [], EditorVerbEffect.ReadOnly,
            "Headless sample run; rejected while export is blocked."),
    ];

    public static EditorVerbEntry Get(string verbId) =>
        Entries.FirstOrDefault(e => string.Equals(e.VerbId, verbId, StringComparison.Ordinal))
        ?? throw new KeyNotFoundException($"Unknown editor verb '{verbId}'.");

    /// <summary>First word of a label — the verb a user reads.</summary>
    public static string VerbWord(string label) =>
        (label ?? string.Empty).Trim().Split(' ', 2)[0];
}
