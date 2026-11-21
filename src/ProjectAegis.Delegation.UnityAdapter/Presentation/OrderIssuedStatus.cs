namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>UAT D25: confirmation text shown by the order toolbar after a successful command.</summary>
public static class OrderIssuedStatus
{
    /// <summary>Formats <c>ISSUED: &lt;command&gt; → &lt;unitId&gt;</c> (unit omitted when unknown).</summary>
    public static string FormatIssued(string commandId, string? unitId) =>
        string.IsNullOrEmpty(unitId)
            ? "ISSUED: " + commandId
            : "ISSUED: " + commandId + " → " + unitId;
}
