namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One live-edit validation finding for UI binding (CMD-35).
/// Severity is a stable string ("Error" / "Warning" / "Info"); Path is an optional entity locator.
/// </summary>
public sealed record LiveEditFindingEntry(
    string Code,
    string Severity,
    string Message,
    string? Path);
