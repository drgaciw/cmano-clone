using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.EscalationGate;
using ProjectAegis.Delegation.Skills;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>One authority verb row for DRG-182 chrome (observe/recommend/approve/engage/abort/retask).</summary>
public sealed record C2AuthorityVerbRow(
    string VerbLabel,
    string DispositionLabel,
    string? ReasonCode);

/// <summary>
/// Immutable authority / ROE / escalation presentation for Slice A command chrome.
/// Advisory only — never implies <c>IsOrder=true</c>.
/// </summary>
public sealed record C2AuthorityPresentation(
    string HeaderLine,
    string AdvisoryBadge,
    string RoeLine,
    string TargetingLine,
    IReadOnlyList<C2AuthorityVerbRow> VerbRows,
    IReadOnlyList<string> GateLines,
    string NextActionLine)
{
    /// <summary>Cleared presentation when no contact or authority projection is available.</summary>
    public static C2AuthorityPresentation Empty { get; } = new(
        C2AuthorityPresenter.HeaderLine,
        C2AuthorityPresenter.AdvisoryBadge,
        "ROE: —",
        "Targeting: —",
        Array.Empty<C2AuthorityVerbRow>(),
        Array.Empty<string>(),
        "Select a contact to inspect authority, ROE, and escalation gates.");
}

/// <summary>
/// Formats <see cref="C2AuthorityProjection"/> and <see cref="EscalationGateSnapshot"/> for UI hosts.
/// Build at tick/selection boundaries — not every render frame (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class C2AuthorityPresenter
{
    public const string HeaderLine = "COMMAND AUTHORITY";
    public const string AdvisoryBadge = "ADVISORY ONLY · IsOrder=false";

    /// <summary>Builds authority chrome for the selected contact.</summary>
    public static C2AuthorityPresentation Build(string? contactId, C2AuthorityProjection? authority)
    {
        if (string.IsNullOrWhiteSpace(contactId) || authority is null)
        {
            return C2AuthorityPresentation.Empty;
        }

        var gateSnapshot = EscalationGateProjection.ProjectFromAuthority(contactId, authority);
        var gateLines = gateSnapshot.Rows.Count == 0
            ? new[] { "Escalation gate: none active (weapons-free / permitted targeting)." }
            : gateSnapshot.Rows.Select(FormatGateRow).ToArray();

        var verbRows = authority.Actions
            .OrderBy(static action => (int)action.Action)
            .Select(FormatVerbRow)
            .ToArray();

        return new C2AuthorityPresentation(
            HeaderLine,
            AdvisoryBadge,
            FormatRoeLine(authority.Roe),
            FormatTargetingLine(authority.Targeting),
            verbRows,
            gateLines,
            NextAction(authority));
    }

    /// <summary>Single verb row label for AuthorityRoe list binders.</summary>
    public static string FormatVerbLine(C2AuthorityVerbRow row) =>
        string.IsNullOrEmpty(row.ReasonCode)
            ? $"{row.VerbLabel}: {row.DispositionLabel}"
            : $"{row.VerbLabel}: {row.DispositionLabel} ({row.ReasonCode})";

    /// <summary>Compact single-line summary for PendingApproval / EngageExplain hosts.</summary>
    public static string FormatSummaryLine(C2AuthorityPresentation presentation)
    {
        if (ReferenceEquals(presentation, C2AuthorityPresentation.Empty))
        {
            return "Authority: UNKNOWN — no projection bound";
        }

        var engage = presentation.VerbRows.FirstOrDefault(row => row.VerbLabel == "ENGAGE");
        var engageReasonSuffix = engage is null || string.IsNullOrEmpty(engage.ReasonCode)
            ? string.Empty
            : $" · Engage: {engage.ReasonCode}";
        return $"{presentation.RoeLine} · {presentation.TargetingLine}{engageReasonSuffix}";
    }

    /// <summary>Projects then formats a summary line from authority facts.</summary>
    public static string FormatSummaryLine(C2AuthorityProjection? authority) =>
        authority is null
            ? FormatSummaryLine(C2AuthorityPresentation.Empty)
            : FormatSummaryLine(Build("_summary", authority));

    private static string FormatRoeLine(RoeProjection roe) =>
        $"ROE: {roe.RoeLabel} · {FormatDispositionLabel(roe.TargetingDisposition)}"
        + (string.IsNullOrEmpty(roe.TargetingReasonCode) ? string.Empty : $" · {roe.TargetingReasonCode}");

    private static string FormatTargetingLine(C2TargetingAuthority targeting)
    {
        var text = new StringBuilder("Targeting: ")
            .Append(FormatDispositionLabel(targeting.Disposition));
        if (!string.IsNullOrEmpty(targeting.ReasonCode))
        {
            text.Append(" · ").Append(targeting.ReasonCode);
        }

        if (targeting.PendingApproval is not null)
        {
            text.Append(" · requires ").Append(targeting.PendingApproval);
        }

        return text.ToString();
    }

    private static C2AuthorityVerbRow FormatVerbRow(C2AuthorityActionState action) =>
        new(
            VerbLabel: action.Action.ToString().ToUpperInvariant(),
            DispositionLabel: FormatDispositionLabel(action.Disposition),
            ReasonCode: action.ReasonCode);

    private static string FormatGateRow(EscalationGateRow row) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "Gate {0} · {1} · requires {2} · IsOrder=false",
            row.GateCode,
            row.ReasonCode,
            row.RequiredAuthority);

    private static string FormatDispositionLabel(C2AuthorityDisposition disposition) =>
        disposition switch
        {
            C2AuthorityDisposition.Permitted => "PERMITTED",
            C2AuthorityDisposition.ApprovalRequired => "APPROVAL REQUIRED",
            _ => "WITHHELD",
        };

    private static string NextAction(C2AuthorityProjection authority)
    {
        if (authority.Targeting.Disposition == C2AuthorityDisposition.ApprovalRequired)
        {
            var pending = authority.Targeting.PendingApproval?.ToString() ?? "operator";
            return $"Request {pending} approval through the command workflow; this panel does not clear the gate.";
        }

        if (authority.Targeting.Disposition == C2AuthorityDisposition.Withheld)
        {
            return $"Review the authority restriction ({authority.Targeting.ReasonCode ?? "unspecified"}) before engaging.";
        }

        var engage = authority.Actions.FirstOrDefault(action => action.Action == C2AuthorityActionKind.Engage);
        if (engage.Disposition != C2AuthorityDisposition.Permitted)
        {
            return $"Engage is {FormatDispositionLabel(engage.Disposition).ToLowerInvariant()}"
                + (string.IsNullOrEmpty(engage.ReasonCode) ? "." : $" ({engage.ReasonCode}).");
        }

        return "Authority projection permits engage intent; execution still revalidates eligibility and ROE.";
    }
}
