namespace ProjectAegis.Data.Scenario;

using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

/// <summary>
/// Projects a scenario document / path into a <see cref="ScenarioLibraryEntry"/> with
/// pre-load feasibility (CMD-27.1–3/6/7/9 subset). Never throws for a single bad file.
/// </summary>
public static class ScenarioLibraryProjection
{
    public const string MissingLabel = "—";
    public const string DefaultProvenance = "authored";

    /// <summary>Project an already-loaded document (tests / in-memory packages).</summary>
    public static ScenarioLibraryEntry Project(
        string scenarioId,
        ScenarioDocumentDto document,
        string sourcePath,
        ICatalogReader? catalog = null,
        IScenarioValidationEngine? validation = null,
        ValidationConfig? validationConfig = null)
    {
        if (document is null)
        {
            return ProjectUnavailable(
                scenarioId,
                sourcePath,
                ScenarioLibraryReasons.SchemaError);
        }

        var meta = document.Metadata ?? new ScenarioMetadataDto();
        var title = PreferTitle(meta.Title, scenarioId);
        var policyId = string.IsNullOrWhiteSpace(meta.PolicyId) ? MissingLabel : meta.PolicyId.Trim();
        var tlBranch = string.IsNullOrWhiteSpace(meta.TlBranch)
            ? MissingLabel
            : CatalogTlTier.Normalize(meta.TlBranch);
        var seed = meta.Seed;
        var location = MissingLabel;
        var year = MissingLabel;
        var difficulty = MissingLabel;
        var complexity = MissingLabel;
        var provenance = ResolveProvenance(meta);

        var reason = EvaluateFeasibility(document, catalog, validation, validationConfig);
        return new ScenarioLibraryEntry(
            ScenarioId: scenarioId,
            Title: title,
            PolicyId: policyId,
            TlBranch: tlBranch,
            Seed: seed,
            Location: location,
            Year: year,
            ProvenanceLabel: provenance,
            Available: reason is null,
            UnavailableReason: reason,
            Difficulty: difficulty,
            Complexity: complexity,
            SourcePath: sourcePath ?? string.Empty);
    }

    /// <summary>
    /// Load from disk and project. Catches IO / parse failures as unavailable rows.
    /// </summary>
    public static ScenarioLibraryEntry ProjectFromPath(
        string path,
        ICatalogReader? catalog = null,
        IScenarioValidationEngine? validation = null,
        ValidationConfig? validationConfig = null)
    {
        var scenarioId = ScenarioIdFromPath(path);
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return ProjectUnavailable(scenarioId, path ?? string.Empty, ScenarioLibraryReasons.FileUnreadable);
            }

            ScenarioDocumentDto document;
            try
            {
                document = ScenarioDocumentJsonLoader.LoadFromFile(path);
            }
            catch (JsonException)
            {
                return ProjectUnavailable(scenarioId, path, ScenarioLibraryReasons.SchemaError);
            }
            catch (InvalidDataException)
            {
                return ProjectUnavailable(scenarioId, path, ScenarioLibraryReasons.SchemaError);
            }
            catch (IOException)
            {
                return ProjectUnavailable(scenarioId, path, ScenarioLibraryReasons.FileUnreadable);
            }
            catch (UnauthorizedAccessException)
            {
                return ProjectUnavailable(scenarioId, path, ScenarioLibraryReasons.FileUnreadable);
            }

            if (document is null)
            {
                return ProjectUnavailable(scenarioId, path, ScenarioLibraryReasons.SchemaError);
            }

            return Project(scenarioId, document, path, catalog, validation, validationConfig);
        }
        catch
        {
            // Never throw on one bad file — mark unavailable.
            return ProjectUnavailable(scenarioId, path ?? string.Empty, ScenarioLibraryReasons.FileUnreadable);
        }
    }

    public static ScenarioLibraryEntry ProjectUnavailable(
        string scenarioId,
        string sourcePath,
        string reason)
    {
        var id = string.IsNullOrWhiteSpace(scenarioId) ? Path.GetFileName(sourcePath) ?? "unknown" : scenarioId;
        return new ScenarioLibraryEntry(
            ScenarioId: id,
            Title: id,
            PolicyId: MissingLabel,
            TlBranch: MissingLabel,
            Seed: 0,
            Location: MissingLabel,
            Year: MissingLabel,
            ProvenanceLabel: DefaultProvenance,
            Available: false,
            UnavailableReason: reason,
            Difficulty: MissingLabel,
            Complexity: MissingLabel,
            SourcePath: sourcePath ?? string.Empty);
    }

    public static string ScenarioIdFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "unknown";
        }

        // Match ScenarioPackageLoader: first extension strip only.
        return Path.GetFileNameWithoutExtension(path);
    }

    public static string PreferTitle(string? metadataTitle, string scenarioId) =>
        string.IsNullOrWhiteSpace(metadataTitle) ? scenarioId : metadataTitle.Trim();

    private static string ResolveProvenance(ScenarioMetadataDto meta)
    {
        // Provenance is not yet a first-class metadata field; author can hint source.
        // Default to "authored" (CMD-27.2 subset) when absent.
        if (!string.IsNullOrWhiteSpace(meta.Author))
        {
            var author = meta.Author.Trim();
            if (author.Equals("user", StringComparison.OrdinalIgnoreCase) ||
                author.Equals("ai", StringComparison.OrdinalIgnoreCase) ||
                author.Equals("import", StringComparison.OrdinalIgnoreCase))
            {
                return author.ToLowerInvariant();
            }
        }

        return DefaultProvenance;
    }

    /// <summary>
    /// Pre-load feasibility: catalog db binding + optional light validation (CMD-27.1).
    /// Returns null when available.
    /// </summary>
    public static string? EvaluateFeasibility(
        ScenarioDocumentDto document,
        ICatalogReader? catalog = null,
        IScenarioValidationEngine? validation = null,
        ValidationConfig? validationConfig = null)
    {
        if (document is null)
        {
            return ScenarioLibraryReasons.SchemaError;
        }

        var meta = document.Metadata ?? new ScenarioMetadataDto();

        // Catalog binding check (DB_MISMATCH) when a catalog is installed.
        if (catalog is not null)
        {
            var dbRef = meta.DbRef ?? meta.DbSnapshotId;
            if (!string.IsNullOrWhiteSpace(dbRef) && !catalog.TryResolveDbRef(dbRef, out _))
            {
                return ScenarioLibraryReasons.DbMismatch;
            }
        }

        // Optional light-touch validation (errors → unavailable).
        if (catalog is not null && validation is not null)
        {
            try
            {
                var config = validationConfig ?? new ValidationConfig();
                var report = validation.Validate(document, catalog, config);
                if (!report.Passed)
                {
                    // Prefer specific codes when present.
                    foreach (var finding in report.Findings)
                    {
                        if (finding.Severity < ValidationSeverity.Error)
                        {
                            continue;
                        }

                        if (string.Equals(finding.Code, ScenarioLibraryReasons.DbMismatch, StringComparison.Ordinal) ||
                            string.Equals(finding.Code, "DB_MISMATCH", StringComparison.Ordinal))
                        {
                            return ScenarioLibraryReasons.DbMismatch;
                        }

                        if (string.Equals(finding.Code, ScenarioLibraryReasons.BrokenRef, StringComparison.Ordinal) ||
                            string.Equals(finding.Code, "BROKEN_REF", StringComparison.Ordinal))
                        {
                            return ScenarioLibraryReasons.BrokenRef;
                        }
                    }

                    return ScenarioLibraryReasons.ValidationBlocked;
                }
            }
            catch
            {
                // Validation failure itself should not crash the library row.
                return ScenarioLibraryReasons.ValidationBlocked;
            }
        }
        else if (catalog is null)
        {
            // Without catalog, still surface obvious broken refs (no engine needed).
            if (HasBrokenRef(document))
            {
                return ScenarioLibraryReasons.BrokenRef;
            }
        }

        return null;
    }

    private static bool HasBrokenRef(ScenarioDocumentDto document)
    {
        var missions = document.Missions;
        if (missions is null || missions.Count == 0)
        {
            return false;
        }

        var allUnits = missions
            .SelectMany(m => m.AssignedUnitIds ?? Array.Empty<string>())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var m in missions)
        {
            foreach (var t in m.TargetIds ?? Array.Empty<string>())
            {
                if (t.StartsWith("ref:", StringComparison.Ordinal) &&
                    !allUnits.Contains(t.Replace("ref:", "")))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
