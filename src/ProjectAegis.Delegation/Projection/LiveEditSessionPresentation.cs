namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless live-edit session presentation for UI hosts (CMD-35).
/// Surfaces mutation outcome + validation findings without inventing sim state.
/// </summary>
public sealed record LiveEditSessionPresentation(
    int EditVersion,
    int BlockingCount,
    int WarningCount,
    IReadOnlyList<LiveEditFindingEntry> Findings,
    bool LastMutationOk,
    string? LastMutationReason,
    string StatusLine)
{
    /// <summary>True when Commit may proceed (no mutation failure and no blocking findings).</summary>
    public bool CommitEnabled => LastMutationOk && BlockingCount == 0;

    /// <summary>Tooltip / accessible reason when <see cref="CommitEnabled"/> is false; null when enabled.</summary>
    public string? CommitDisabledReason
    {
        get
        {
            if (!LastMutationOk)
            {
                return string.IsNullOrWhiteSpace(LastMutationReason)
                    ? "Last mutation failed."
                    : LastMutationReason;
            }

            if (BlockingCount > 0)
            {
                var first = Findings.FirstOrDefault(f =>
                    string.Equals(f.Severity, "Error", StringComparison.Ordinal));
                var code = first?.Code;
                return string.IsNullOrEmpty(code)
                    ? $"Blocked by {BlockingCount} error finding{(BlockingCount == 1 ? string.Empty : "s")}."
                    : $"Blocked by {code} ({BlockingCount} error{(BlockingCount == 1 ? string.Empty : "s")}).";
            }

            return null;
        }
    }

    /// <summary>Empty session before any mutation or validation.</summary>
    public static LiveEditSessionPresentation Empty { get; } = new(
        EditVersion: 0,
        BlockingCount: 0,
        WarningCount: 0,
        Findings: Array.Empty<LiveEditFindingEntry>(),
        LastMutationOk: true,
        LastMutationReason: null,
        StatusLine: "LIVE EDIT v0 · 0 blocking · 0 warnings");
}
