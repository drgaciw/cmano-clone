namespace ProjectAegis.Data.Platform;

/// <summary>
/// Req-21 / ADR-011: selects the platform workbook I/O adapter without pulling ClosedXML into the
/// engine-free <c>ProjectAegis.Data</c> core (ADR-006). <see cref="CanonicalTextWorkbookIo"/> remains the
/// deterministic fallback; production <c>.xlsx</c> is supplied by <c>ProjectAegis.Data.Excel</c> via an
/// injected factory at the CLI/test edge.
/// </summary>
public static class PlatformWorkbookIoSelection
{
    public const string EnvVar = "PLATFORM_WORKBOOK_IO";
    public const string CanonicalFlag = "canonical";
    public const string ClosedXmlFlag = "closedxml";

    /// <summary>
    /// Resolves the workbook I/O adapter from an explicit flag, environment override, or path extension.
    /// <paramref name="closedXmlFactory"/> must be supplied when ClosedXML is selected.
    /// </summary>
    public static IPlatformWorkbookIo Resolve(
        string? path,
        string? ioFlag = null,
        Func<IPlatformWorkbookIo>? closedXmlFactory = null)
    {
        if (PrefersCanonical(path, ioFlag))
        {
            return new CanonicalTextWorkbookIo();
        }

        if (closedXmlFactory is null)
        {
            throw new InvalidOperationException(
                "ClosedXML workbook I/O was requested but no adapter factory was supplied. " +
                $"Use --io {CanonicalFlag} or set {EnvVar}={CanonicalFlag} for headless canonical text.");
        }

        return closedXmlFactory();
    }

    /// <summary>True when canonical text I/O should be used instead of binary <c>.xlsx</c>.</summary>
    public static bool PrefersCanonical(string? path, string? ioFlag = null)
    {
        var preference = Normalize(ioFlag) ?? Normalize(Environment.GetEnvironmentVariable(EnvVar));
        if (string.Equals(preference, CanonicalFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(preference, "text", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(preference, ClosedXmlFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(preference, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".platform.txt", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Default production path: binary .xlsx when a ClosedXML factory is available at the edge.
        return false;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}