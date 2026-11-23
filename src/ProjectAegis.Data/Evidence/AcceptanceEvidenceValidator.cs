namespace ProjectAegis.Data.Evidence;

/// <summary>Validation verdict for a <see cref="RevisionBoundAcceptanceRecord"/>.</summary>
public sealed record AcceptanceEvidenceValidationResult(
    bool IsSchemaValid,
    bool IsOwnerAcceptedForCurrentRevision,
    bool RequiresReassessment,
    bool HistoricalRecordPreserved,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> ReassessmentReasons);

/// <summary>
/// Fail-closed validator for VER-07.3..6 and REC-VER-07.3..6 revision-bound acceptance records.
/// Rejects missing provenance, layer substitution, forged owner fields, and revision/input drift.
/// </summary>
public static class AcceptanceEvidenceValidator
{
    /// <summary>
    /// Validates <paramref name="record"/> schema completeness, layer non-substitution,
    /// owner-acceptance rules, and optional drift against <paramref name="currentRevision"/> / <paramref name="currentInputs"/>.
    /// </summary>
    public static AcceptanceEvidenceValidationResult Validate(
        RevisionBoundAcceptanceRecord record,
        RevisionIdentity? currentRevision = null,
        InputDigestManifest? currentInputs = null)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        var errors = new List<string>();
        var reassessmentReasons = new List<string>();

        ValidateRequiredFields(record, errors);
        ValidateCounts(record, errors);
        ValidateLayer(record, errors);
        ValidateOwnerAcceptance(record, errors);
        DetectDrift(record, currentRevision, currentInputs, reassessmentReasons);

        var isSchemaValid = errors.Count == 0;
        var requiresReassessment = reassessmentReasons.Count > 0;
        var isOwnerAccepted = isSchemaValid
            && !requiresReassessment
            && record.OwnerAcceptance is not null
            && record.OwnerAcceptance.Status == OwnerAcceptanceStatus.Accepted;

        return new AcceptanceEvidenceValidationResult(
            IsSchemaValid: isSchemaValid,
            IsOwnerAcceptedForCurrentRevision: isOwnerAccepted,
            RequiresReassessment: requiresReassessment,
            HistoricalRecordPreserved: true,
            Errors: errors,
            ReassessmentReasons: reassessmentReasons);
    }

    private static void ValidateRequiredFields(RevisionBoundAcceptanceRecord record, List<string> errors)
    {
        Require(record.CriterionId, nameof(record.CriterionId), errors);
        Require(record.RequirementId, nameof(record.RequirementId), errors);
        Require(record.ExecutionCommandOrSteps, nameof(record.ExecutionCommandOrSteps), errors);
        Require(record.Result, nameof(record.Result), errors);
        Require(record.ArtifactLocation, nameof(record.ArtifactLocation), errors);
        Require(record.ArtifactSha256, nameof(record.ArtifactSha256), errors);
        Require(record.CollectedAtUtc, nameof(record.CollectedAtUtc), errors);

        if (record.Revision is null)
        {
            errors.Add("Revision identity is required.");
        }
        else
        {
            Require(record.Revision.CommitSha, "Revision.CommitSha", errors);
            Require(record.Revision.BuildIdentity, "Revision.BuildIdentity", errors);
            Require(record.Revision.LocalDeltaIdentity, "Revision.LocalDeltaIdentity", errors);
        }

        if (record.Inputs is null)
        {
            errors.Add("Input digest manifest is required.");
        }
        else
        {
            Require(record.Inputs.ScenarioId, "Inputs.ScenarioId", errors);
            Require(record.Inputs.ScenarioSha256, "Inputs.ScenarioSha256", errors);
            Require(record.Inputs.CatalogId, "Inputs.CatalogId", errors);
            Require(record.Inputs.CatalogSha256, "Inputs.CatalogSha256", errors);
            if (record.Inputs.Policies is null || record.Inputs.Policies.Count == 0)
            {
                errors.Add("Inputs.Policies must contain at least one policy digest.");
            }
            else
            {
                for (var i = 0; i < record.Inputs.Policies.Count; i++)
                {
                    var p = record.Inputs.Policies[i];
                    if (string.IsNullOrWhiteSpace(p.Path) || string.IsNullOrWhiteSpace(p.Sha256))
                    {
                        errors.Add($"Inputs.Policies[{i}] requires non-empty path and sha256.");
                    }
                }
            }
        }
    }

    private static void ValidateCounts(RevisionBoundAcceptanceRecord record, List<string> errors)
    {
        if (record.Counts is null)
        {
            errors.Add("FloorVsObservedCounts is required.");
            return;
        }

        if (record.Counts.DeclaredFloor < 0 || record.Counts.ObservedPassed < 0 || record.Counts.ObservedFailed < 0 || record.Counts.ObservedSkipped < 0)
        {
            errors.Add("Counts must be non-negative.");
            return;
        }

        if (string.Equals(record.Result, "PASS", StringComparison.OrdinalIgnoreCase))
        {
            if (record.Counts.ObservedFailed > 0)
            {
                errors.Add("Result cannot be PASS when ObservedFailed > 0.");
            }

            if (record.Counts.DeclaredFloor > 0 && record.Counts.ObservedPassed < record.Counts.DeclaredFloor)
            {
                errors.Add($"ObservedPassed ({record.Counts.ObservedPassed}) is below DeclaredFloor ({record.Counts.DeclaredFloor}).");
            }
        }
    }

    private static void ValidateLayer(RevisionBoundAcceptanceRecord record, List<string> errors)
    {
        if (!Enum.IsDefined(typeof(AcceptanceEvidenceLayer), record.Layer)
            || !Enum.IsDefined(typeof(AcceptanceEvidenceLayer), record.RequiredLayer))
        {
            errors.Add("Evidence layer and required layer must be valid enum values.");
            return;
        }

        if (record.Layer < record.RequiredLayer)
        {
            errors.Add($"Layer substitution rejected: evidence layer {record.Layer} cannot satisfy required layer {record.RequiredLayer}.");
        }
    }

    private static void ValidateOwnerAcceptance(RevisionBoundAcceptanceRecord record, List<string> errors)
    {
        if (record.OwnerAcceptance is null)
        {
            errors.Add("OwnerAcceptance is required.");
            return;
        }

        var oa = record.OwnerAcceptance;
        switch (oa.Status)
        {
            case OwnerAcceptanceStatus.Pending:
                if (!string.IsNullOrWhiteSpace(oa.Owner)
                    || !string.IsNullOrWhiteSpace(oa.DecisionDateUtc)
                    || !string.IsNullOrWhiteSpace(oa.DecisionReference)
                    || oa.AcceptedRevision is not null
                    || oa.AcceptedInputs is not null)
                {
                    errors.Add("Pending acceptance must not populate Owner, DecisionDateUtc, DecisionReference, AcceptedRevision, or AcceptedInputs.");
                }
                break;

            case OwnerAcceptanceStatus.Accepted:
                Require(oa.Owner, "OwnerAcceptance.Owner", errors);
                Require(oa.DecisionDateUtc, "OwnerAcceptance.DecisionDateUtc", errors);
                Require(oa.DecisionReference, "OwnerAcceptance.DecisionReference", errors);
                if (oa.AcceptedRevision is null)
                {
                    errors.Add("OwnerAcceptance.AcceptedRevision is required when Status is Accepted.");
                }
                else if (record.Revision is not null && !RevisionsEqual(record.Revision, oa.AcceptedRevision))
                {
                    errors.Add("OwnerAcceptance.AcceptedRevision does not match record Revision.");
                }

                if (oa.AcceptedInputs is null)
                {
                    errors.Add("OwnerAcceptance.AcceptedInputs is required when Status is Accepted.");
                }
                else if (record.Inputs is not null && !InputsEqual(record.Inputs, oa.AcceptedInputs))
                {
                    errors.Add("OwnerAcceptance.AcceptedInputs does not match record Inputs.");
                }

                if (!string.Equals(record.Result, "PASS", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("OwnerAcceptance cannot be Accepted when execution Result is not PASS.");
                }
                break;

            case OwnerAcceptanceStatus.Rejected:
                Require(oa.Owner, "OwnerAcceptance.Owner", errors);
                Require(oa.DecisionDateUtc, "OwnerAcceptance.DecisionDateUtc", errors);
                Require(oa.DecisionReference, "OwnerAcceptance.DecisionReference", errors);
                break;

            default:
                errors.Add($"Unsupported OwnerAcceptanceStatus: {oa.Status}.");
                break;
        }
    }

    private static void DetectDrift(
        RevisionBoundAcceptanceRecord record,
        RevisionIdentity? currentRevision,
        InputDigestManifest? currentInputs,
        List<string> reassessmentReasons)
    {
        if (currentRevision is not null && record.Revision is not null)
        {
            if (!string.Equals(record.Revision.CommitSha, currentRevision.CommitSha, StringComparison.Ordinal))
            {
                reassessmentReasons.Add($"CommitSha drifted: '{record.Revision.CommitSha}' -> '{currentRevision.CommitSha}'.");
            }

            if (!string.Equals(record.Revision.BuildIdentity, currentRevision.BuildIdentity, StringComparison.Ordinal))
            {
                reassessmentReasons.Add($"BuildIdentity drifted: '{record.Revision.BuildIdentity}' -> '{currentRevision.BuildIdentity}'.");
            }

            if (!string.Equals(record.Revision.LocalDeltaIdentity, currentRevision.LocalDeltaIdentity, StringComparison.Ordinal))
            {
                reassessmentReasons.Add($"LocalDeltaIdentity drifted: '{record.Revision.LocalDeltaIdentity}' -> '{currentRevision.LocalDeltaIdentity}'.");
            }

            if (!UntrackedEqual(record.Revision.UntrackedInputManifest, currentRevision.UntrackedInputManifest))
            {
                reassessmentReasons.Add("UntrackedInputManifest drifted.");
            }
        }

        if (currentInputs is not null && record.Inputs is not null)
        {
            if (!string.Equals(record.Inputs.ScenarioId, currentInputs.ScenarioId, StringComparison.Ordinal))
            {
                reassessmentReasons.Add($"ScenarioId drifted: '{record.Inputs.ScenarioId}' -> '{currentInputs.ScenarioId}'.");
            }

            if (!string.Equals(record.Inputs.ScenarioSha256, currentInputs.ScenarioSha256, StringComparison.OrdinalIgnoreCase))
            {
                reassessmentReasons.Add($"ScenarioSha256 drifted: '{record.Inputs.ScenarioSha256}' -> '{currentInputs.ScenarioSha256}'.");
            }

            if (record.Inputs.Seed != currentInputs.Seed)
            {
                reassessmentReasons.Add($"Seed drifted: {record.Inputs.Seed} -> {currentInputs.Seed}.");
            }

            if (!string.Equals(record.Inputs.CatalogId, currentInputs.CatalogId, StringComparison.Ordinal))
            {
                reassessmentReasons.Add($"CatalogId drifted: '{record.Inputs.CatalogId}' -> '{currentInputs.CatalogId}'.");
            }

            if (!string.Equals(record.Inputs.CatalogSha256, currentInputs.CatalogSha256, StringComparison.OrdinalIgnoreCase))
            {
                reassessmentReasons.Add($"CatalogSha256 drifted: '{record.Inputs.CatalogSha256}' -> '{currentInputs.CatalogSha256}'.");
            }

            if (!PoliciesEqual(record.Inputs.Policies, currentInputs.Policies))
            {
                reassessmentReasons.Add("Policy input digests drifted.");
            }
        }
    }

    private static bool RevisionsEqual(RevisionIdentity a, RevisionIdentity b) =>
        string.Equals(a.CommitSha, b.CommitSha, StringComparison.Ordinal)
        && string.Equals(a.BuildIdentity, b.BuildIdentity, StringComparison.Ordinal)
        && string.Equals(a.LocalDeltaIdentity, b.LocalDeltaIdentity, StringComparison.Ordinal)
        && UntrackedEqual(a.UntrackedInputManifest, b.UntrackedInputManifest);

    private static bool UntrackedEqual(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        var left = (a ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var right = (b ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        return left.SequenceEqual(right, StringComparer.Ordinal);
    }

    private static bool InputsEqual(InputDigestManifest a, InputDigestManifest b) =>
        string.Equals(a.ScenarioId, b.ScenarioId, StringComparison.Ordinal)
        && string.Equals(a.ScenarioSha256, b.ScenarioSha256, StringComparison.OrdinalIgnoreCase)
        && a.Seed == b.Seed
        && string.Equals(a.CatalogId, b.CatalogId, StringComparison.Ordinal)
        && string.Equals(a.CatalogSha256, b.CatalogSha256, StringComparison.OrdinalIgnoreCase)
        && PoliciesEqual(a.Policies, b.Policies);

    private static bool PoliciesEqual(IReadOnlyList<HashedInputFile>? a, IReadOnlyList<HashedInputFile>? b)
    {
        var left = (a ?? Array.Empty<HashedInputFile>())
            .OrderBy(p => p.Path, StringComparer.Ordinal)
            .ThenBy(p => p.Sha256, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var right = (b ?? Array.Empty<HashedInputFile>())
            .OrderBy(p => p.Path, StringComparer.Ordinal)
            .ThenBy(p => p.Sha256, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!string.Equals(left[i].Path, right[i].Path, StringComparison.Ordinal)
                || !string.Equals(left[i].Sha256, right[i].Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static void Require(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} is required.");
        }
    }
}
