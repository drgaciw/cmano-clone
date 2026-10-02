using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Glossary;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-265 / REQ-18 DOM-02: domain abort code chrome for the Engage Explain panel.
/// Reads existing explain, preview, and combat-detail DTOs only (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record DomainAbortExplainState(
    string StateLine,
    string? Code,
    bool IsBlocked,
    string DeclutterToken,
    string CueClass,
    string Fingerprint)
{
    /// <summary>No explain, preview, or combat-detail evidence.</summary>
    public static DomainAbortExplainState Empty { get; } = new(
        "DOMAIN: — [DOM:UNKNOWN]",
        null,
        false,
        DomainAbortDeclutterTokens.Unknown,
        DomainAbortCueClasses.Unknown,
        "dom:unknown");
}

/// <summary>Non-color USS cue tokens for domain-abort rows (text + border class).</summary>
public static class DomainAbortCueClasses
{
    public const string Unknown = "domain-abort-cue--unknown";
    public const string Clear = "domain-abort-cue--clear";
    public const string Blocked = "domain-abort-cue--blocked";

    /// <summary>All cue classes hosts must clear before applying the active row cue.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Unknown, Clear, Blocked };
}

/// <summary>Headless text declutter tokens paired with cue classes — never color-only state.</summary>
public static class DomainAbortDeclutterTokens
{
    public const string Unknown = "[DOM:UNKNOWN]";
    public const string Clear = "[DOM:CLEAR]";
    public const string Blocked = "[DOM:BLOCKED]";
}

/// <summary>One Engage Explain domain-abort label row with its USS cue class token.</summary>
public sealed record DomainAbortExplainRow(string ElementName, string Text, string CueClass);

/// <summary>
/// Maps immutable engage-explain DTOs onto a domain-abort label and cue class.
/// Does not evaluate combat-domain validators or write orders.
/// </summary>
public static class DomainAbortExplainBinder
{
    /// <summary>UXML element name for the domain-abort line.</summary>
    public const string ElementName = "engage-explain-domain-abort";

    private static readonly string[] DomainCodes =
    [
        AbortReasonCatalog.Engage.AIR_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.SURFACE_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.SUBSURFACE_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.LAND_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.MINE_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.FACILITY_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.DOMAIN_NO_SOLUTION,
    ];

    /// <summary>
    /// Binds domain abort chrome from an explain projection, engage preview, and combat detail card.
    /// Explicit reason and preview codes win over prose that merely mentions a code.
    /// </summary>
    public static DomainAbortExplainState Bind(
        EngageExplain? explain,
        EngagePreview? preview = null,
        CombatDetailPresentation? detail = null)
    {
        if (!HasEvidence(explain, preview, detail))
        {
            return DomainAbortExplainState.Empty;
        }

        var code = ResolveCode(explain, preview, detail);
        if (code is null)
        {
            return new DomainAbortExplainState(
                $"DOMAIN: CLEAR {DomainAbortDeclutterTokens.Clear}",
                null,
                false,
                DomainAbortDeclutterTokens.Clear,
                DomainAbortCueClasses.Clear,
                "dom:clear");
        }

        return new DomainAbortExplainState(
            $"DOMAIN: BLOCKED — {code} {DomainAbortDeclutterTokens.Blocked}",
            code,
            true,
            DomainAbortDeclutterTokens.Blocked,
            DomainAbortCueClasses.Blocked,
            $"dom:blocked:{code}");
    }

    /// <summary>Maps bound state into the Engage Explain element name, text, and cue class.</summary>
    public static IReadOnlyList<DomainAbortExplainRow> BindRows(DomainAbortExplainState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        return
        [
            new DomainAbortExplainRow(ElementName, state.StateLine, state.CueClass),
        ];
    }

    private static bool HasEvidence(
        EngageExplain? explain,
        EngagePreview? preview,
        CombatDetailPresentation? detail) =>
        preview is not null
        || (explain is not null && explain != EngageExplain.Empty)
        || (detail is not null && detail != CombatDetailPresentation.Empty);

    private static string? ResolveCode(
        EngageExplain? explain,
        EngagePreview? preview,
        CombatDetailPresentation? detail)
    {
        var fromReason = FindCode(explain?.ReasonCode);
        if (fromReason is not null)
        {
            return fromReason;
        }

        var fromPreview = FindCode(preview?.AbortPreviewCode);
        if (fromPreview is not null)
        {
            return fromPreview;
        }

        var fromStatus = FindCode(explain?.StatusLine);
        if (fromStatus is not null)
        {
            return fromStatus;
        }

        var fromPlain = FindCode(explain?.ReasonPlain);
        if (fromPlain is not null)
        {
            return fromPlain;
        }

        if (detail is null || detail == CombatDetailPresentation.Empty)
        {
            return null;
        }

        return FindCode(detail.StatusLine)
            ?? FindCode(detail.WeaponLine)
            ?? FindCode(detail.HardConstraintsLine)
            ?? FindCode(detail.PolicyLine)
            ?? FindCode(detail.ConfidenceLine)
            ?? FindCode(detail.FiringSolutionLine)
            ?? FindCode(detail.NextActionLine)
            ?? FindCode(detail.BdaLine)
            ?? FindCode(detail.PostureLine)
            ?? FindCode(detail.CorrelationLine);
    }

    private static string? FindCode(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var bestIndex = int.MaxValue;
        string? best = null;
        foreach (var code in DomainCodes)
        {
            var index = IndexOfToken(text, code);
            if (index >= 0 && index < bestIndex)
            {
                bestIndex = index;
                best = code;
            }
        }

        return best;
    }

    private static int IndexOfToken(string text, string code)
    {
        var start = 0;
        while (start <= text.Length - code.Length)
        {
            var index = text.IndexOf(code, start, StringComparison.Ordinal);
            if (index < 0)
            {
                return -1;
            }

            var end = index + code.Length;
            var leftOk = index == 0 || !IsCodeChar(text[index - 1]);
            var rightOk = end == text.Length || !IsCodeChar(text[end]);
            if (leftOk && rightOk)
            {
                return index;
            }

            start = index + 1;
        }

        return -1;
    }

    // netstandard2.1: char.IsAsciiLetterOrDigit is .NET 5+ only.
    private static bool IsCodeChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_';
}
