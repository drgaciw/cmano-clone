namespace ProjectAegis.Data.Scenario.Authoring;

using System.Runtime.InteropServices;
using Catalog;
using Validation;

/// <summary>Result of persisting a draft. A draft save never produces an export artifact.</summary>
public sealed record ScenarioDraftSaveOutcome(
    bool Saved,
    string DraftPath,
    bool ExportArtifactWritten,
    int BlockingFindingCount,
    string StatusText);

/// <summary>Result of a validated export. <see cref="ArtifactPath"/> is <c>null</c> whenever the gate blocks.</summary>
public sealed record ScenarioValidatedExportOutcome(
    bool Allowed,
    string? ArtifactPath,
    ValidationReport Report,
    IReadOnlyList<ValidationFinding> BlockingFindings,
    int TransformCount,
    string StatusText);

/// <summary>
/// S125-04 / AUTH-04 (AME-6.5 / AC-12): the two distinct persistence verbs of the Mission Editor.
/// <b>Save</b> writes the work-in-progress draft to its own path and is allowed while invalid.
/// <b>Export</b> runs <see cref="ScenarioExportCommand.Prepare"/> (TeleportUnit transform + validation
/// export gate) and writes a separate artifact only when the gate allows it.
/// </summary>
public static class ScenarioSaveExportGate
{
    /// <summary>Suffix appended to the draft file name for the validated export artifact.</summary>
    public const string ExportArtifactSuffix = ".export.json";

    /// <summary>
    /// Stable finding when SaveDraft / <see cref="LiveValidate"/> has no bound catalog.
    /// Replaces the former silent <c>InMemoryCatalogReader.BalticPatrolFixture</c> fallback (DRG-372).
    /// </summary>
    public const string CatalogUnboundFindingCode = "CATALOG_UNBOUND";

    public static string ExportArtifactPathFor(string draftPath)
    {
        if (string.IsNullOrWhiteSpace(draftPath)) throw new ArgumentException("Draft path is required.", nameof(draftPath));

        var dir = Path.GetDirectoryName(draftPath) ?? string.Empty;
        return Path.Combine(dir, Path.GetFileNameWithoutExtension(draftPath) + ExportArtifactSuffix);
    }

    /// <summary>
    /// DRG-372 additive overload: validate a draft document against its bound catalog.
    /// When <paramref name="boundCatalog"/> is null, returns a single stable
    /// <see cref="CatalogUnboundFindingCode"/> finding — never the Baltic fixture.
    /// </summary>
    public static ValidationReport LiveValidate(
        ScenarioDocumentDto document,
        ICatalogReader? boundCatalog,
        ValidationConfig? config = null) =>
        LiveValidate(document, boundCatalog, config, MissionRoleCapabilityManifest.DefaultBackendId);

    /// <summary>
    /// DRG-372 + DRG-371 additive overload: validate a draft document against its bound catalog and
    /// target execution backend. When <paramref name="boundCatalog"/> is null, returns a single stable
    /// <see cref="CatalogUnboundFindingCode"/> finding — never the Baltic fixture.
    /// </summary>
    public static ValidationReport LiveValidate(
        ScenarioDocumentDto document,
        ICatalogReader? boundCatalog,
        ValidationConfig? config,
        string backendId)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        config ??= new ValidationConfig();
        if (boundCatalog is null)
        {
            return ValidationReport.FromFindings(
            [
                new ValidationFinding(
                    CatalogUnboundFindingCode,
                    ValidationSeverity.Error,
                    "No catalog is bound for this draft. Bind a catalog (dbRef/binding) before treating save findings as export authority. Save draft remains allowed; Export stays gated.",
                    Data: new Dictionary<string, string> { ["field"] = "catalog" }),
            ]);
        }

        return new ScenarioValidationEngine().Validate(document, boundCatalog, config, backendId);
    }

    /// <summary>Persists the session draft; reports (but never enforces) blocking findings.</summary>
    public static ScenarioDraftSaveOutcome SaveDraft(
        ScenarioAuthoringSession session,
        ICatalogReader? catalog = null,
        ValidationConfig? config = null) =>
        SaveDraft(session, catalog, config, MissionRoleCapabilityManifest.DefaultBackendId);

    /// <summary>Persists the session draft and evaluates blocking findings against <paramref name="backendId"/>.</summary>
    public static ScenarioDraftSaveOutcome SaveDraft(
        ScenarioAuthoringSession session,
        ICatalogReader? catalog,
        ValidationConfig? config,
        string backendId)
    {
        if (session is null) throw new ArgumentNullException(nameof(session));

        config ??= new ValidationConfig();
        session.Save();
        var report = LiveValidate(session.Editor.ToDto(), catalog, config, backendId);
        var blocking = BlockingFindings(report, config).Count;
        var status = blocking == 0
            ? "Draft saved — not exported. Export to produce the validated artifact."
            : $"Draft saved with {Pluralize(blocking, "blocking finding")} — not exported. Export stays blocked until resolved.";
        return new ScenarioDraftSaveOutcome(true, session.Path, ExportArtifactWritten: false, blocking, status);
    }

    /// <summary>
    /// Runs the export gate and writes <paramref name="artifactPath"/> only when allowed. A blocked export
    /// writes nothing. <paramref name="artifactPath"/> may never name the same file as the draft
    /// (<see cref="IsSameFilePath(string, string)"/>: normalized full path, case-insensitive on Windows and macOS).
    /// </summary>
    public static ScenarioValidatedExportOutcome Export(
        ScenarioDocumentDto document,
        ICatalogReader catalog,
        string artifactPath,
        string? draftPath = null,
        ValidationConfig? config = null) =>
        Export(document, catalog, artifactPath, draftPath, config, MissionRoleCapabilityManifest.DefaultBackendId);

    /// <summary>Runs the export gate for a specific execution backend and writes <paramref name="artifactPath"/> when allowed.</summary>
    public static ScenarioValidatedExportOutcome Export(
        ScenarioDocumentDto document,
        ICatalogReader catalog,
        string artifactPath,
        string? draftPath,
        ValidationConfig? config,
        string backendId)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (catalog is null) throw new ArgumentNullException(nameof(catalog));
        if (string.IsNullOrWhiteSpace(artifactPath)) throw new ArgumentException("Artifact path is required.", nameof(artifactPath));
        if (draftPath is not null && IsSameFilePath(artifactPath, draftPath))
        {
            throw new ArgumentException("Export artifact path must differ from the draft path.", nameof(artifactPath));
        }

        config ??= new ValidationConfig();
        var package = ScenarioExportCommand.Prepare(document, catalog, config, backendId);
        var blocking = BlockingFindings(package.ValidationReport, config);
        if (!package.Allowed)
        {
            return new ScenarioValidatedExportOutcome(
                false,
                null,
                package.ValidationReport,
                blocking,
                package.TransformManifest.Count,
                $"Export blocked — {Pluralize(blocking.Count, "blocking finding")} must be resolved. Save draft is still available.");
        }

        ScenarioDocumentJsonWriter.WriteToFile(package.ExportDocument, artifactPath);
        return new ScenarioValidatedExportOutcome(
            true,
            artifactPath,
            package.ValidationReport,
            blocking,
            package.TransformManifest.Count,
            $"Exported validated artifact ({Pluralize(package.TransformManifest.Count, "logged transform")}).");
    }

    private static IReadOnlyList<ValidationFinding> BlockingFindings(ValidationReport report, ValidationConfig config) =>
        report.Findings.Where(f => f.Severity >= config.ExportBlockSeverityFloor).ToArray();

    /// <summary>
    /// Case rule for <see cref="IsSameFilePath(string, string)"/>. Windows and macOS editor hosts
    /// use case-insensitive volumes, so a case-variant of the draft must not pass the overwrite guard.
    /// Other platforms compare ordinally (case-sensitive).
    /// </summary>
    public static StringComparison ExportPathComparison =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <summary>
    /// True when <paramref name="left"/> and <paramref name="right"/> name the same file after
    /// <see cref="Path.GetFullPath(string)"/> normalization, using <see cref="ExportPathComparison"/>.
    /// </summary>
    public static bool IsSameFilePath(string left, string right) =>
        IsSameFilePath(left, right, ExportPathComparison);

    /// <summary>
    /// True when the normalized full paths are equal under <paramref name="comparison"/>.
    /// Trailing directory separators are ignored. Callers that need the editor-host rule use
    /// <see cref="IsSameFilePath(string, string)"/>.
    /// </summary>
    public static bool IsSameFilePath(string left, string right, StringComparison comparison)
    {
        if (string.IsNullOrWhiteSpace(left)) throw new ArgumentException("Path is required.", nameof(left));
        if (string.IsNullOrWhiteSpace(right)) throw new ArgumentException("Path is required.", nameof(right));

        return string.Equals(NormalizeFullPath(left), NormalizeFullPath(right), comparison);
    }

    private static string NormalizeFullPath(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string Pluralize(int count, string noun) =>
        count == 1 ? $"{count} {noun}" : $"{count} {noun}s";
}
