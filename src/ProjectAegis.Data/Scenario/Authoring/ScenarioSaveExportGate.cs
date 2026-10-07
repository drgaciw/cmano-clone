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

    public static string ExportArtifactPathFor(string draftPath)
    {
        if (string.IsNullOrWhiteSpace(draftPath)) throw new ArgumentException("Draft path is required.", nameof(draftPath));

        var dir = Path.GetDirectoryName(draftPath) ?? string.Empty;
        return Path.Combine(dir, Path.GetFileNameWithoutExtension(draftPath) + ExportArtifactSuffix);
    }

    /// <summary>Persists the session draft; reports (but never enforces) blocking findings.</summary>
    public static ScenarioDraftSaveOutcome SaveDraft(
        ScenarioAuthoringSession session,
        ICatalogReader? catalog = null,
        ValidationConfig? config = null)
    {
        if (session is null) throw new ArgumentNullException(nameof(session));

        config ??= new ValidationConfig();
        session.Save();
        // Follow-up (not fixed here): LiveValidate always uses InMemoryCatalogReader.BalticPatrolFixture
        // when no catalog is passed. A non-Baltic dbRef can pick up fixture findings and a false
        // "Export stays blocked" count. See docs/engineering/editor-verb-honesty.md.
        var report = catalog is null
            ? session.Editor.LiveValidate()
            : new ScenarioValidationEngine().Validate(session.Editor.ToDto(), catalog, config);
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
        ValidationConfig? config = null)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (catalog is null) throw new ArgumentNullException(nameof(catalog));
        if (string.IsNullOrWhiteSpace(artifactPath)) throw new ArgumentException("Artifact path is required.", nameof(artifactPath));
        if (draftPath is not null && IsSameFilePath(artifactPath, draftPath))
        {
            throw new ArgumentException("Export artifact path must differ from the draft path.", nameof(artifactPath));
        }

        config ??= new ValidationConfig();
        var package = ScenarioExportCommand.Prepare(document, catalog, config);
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
    /// <see cref="Path.GetFullPath"/> normalization, using <see cref="ExportPathComparison"/>.
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
