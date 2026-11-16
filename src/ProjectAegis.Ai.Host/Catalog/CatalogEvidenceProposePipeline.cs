using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.PlatformAssistant;
using ProjectAegis.Data.WriteGate;

namespace ProjectAegis.Ai.Host.Catalog;

/// <summary>
/// DRG-748: feed retrieved evidence into <see cref="PlatformDesignAssistant"/> briefs
/// and stage through additive CatalogWriteGate Propose* overloads only.
/// Never auto-approves; retrieved docs are evidence, not instructions.
/// </summary>
public sealed class CatalogEvidenceProposePipeline : ICatalogEvidenceProposePipeline
{
    private readonly PlatformDesignAssistant _assistant;
    private readonly ICatalogClock _clock;

    /// <summary>Creates a pipeline with injectable assistant and clock.</summary>
    public CatalogEvidenceProposePipeline(
        PlatformDesignAssistant? assistant = null,
        ICatalogClock? clock = null)
    {
        _assistant = assistant ?? new PlatformDesignAssistant();
        _clock = clock ?? new FixedCatalogClock(74_800);
    }

    /// <inheritdoc />
    public CatalogEvidenceProposeResult Propose(CatalogEvidenceProposeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.DatabasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(request));
        }

        var diagnostics = CatalogEvidenceValidator.Validate(
            request.Evidence,
            request.ViewingSide,
            request.KnownSourceRevisions);

        if (diagnostics.Count > 0)
        {
            var invalidated = diagnostics.Any(d =>
                string.Equals(d.Code, "SOURCE_REVISION_INVALIDATED", StringComparison.Ordinal));
            return Fail(
                invalidated ? CatalogEvidenceOutcome.Invalidated : CatalogEvidenceOutcome.Diagnostics,
                diagnostics);
        }

        if (request.Evidence.Count == 0)
        {
            return Fail(
                CatalogEvidenceOutcome.Diagnostics,
                [new CatalogEvidenceDiagnostic("NO_EVIDENCE", "At least one retrieved evidence document is required.")]);
        }

        var (brief, citations) = CatalogEvidenceBriefBuilder.Build(request.SeedBrief, request.Evidence);

        using var catalog = new SqliteCatalogReader(request.DatabasePath, "catalog-evidence-propose");
        var proposal = _assistant.Draft(catalog, brief);

        // Stamp citation refs on proposed rows so live approve path retains resolvable provenance.
        var primaryCite = citations[0];
        var citeRef = $"evidence:{primaryCite.SourceId}@{primaryCite.Revision}#{primaryCite.SourceHash}";
        proposal = proposal with
        {
            Binding = proposal.Binding with { CitationRef = citeRef },
            Damage = proposal.Damage with { CitationRef = citeRef },
            Mobility = proposal.Mobility with { CitationRef = citeRef },
        };

        var rationale = string.IsNullOrWhiteSpace(request.Rationale)
            ? $"catalog_evidence_propose:{proposal.Binding.PlatformId}"
            : request.Rationale!;

        var bindingCites = citations.Where(c => c.Field is "concept" or "platform.binding").ToArray();
        var damageCites = citations.Where(c => c.Field is "concept" or "platform.damage").ToArray();
        var mobilityCites = citations.Where(c => c.Field is "concept" or "platform.mobility").ToArray();

        using var gate = new CatalogWriteGate(request.DatabasePath, _clock);

        var platformBatchId = gate.ProposePlatformBatch(
            [proposal.Binding],
            request.ActorType,
            request.ActorId,
            rationale,
            bindingCites);

        var damageBatchId = gate.ProposePlatformDamageBatch(
            [proposal.Damage],
            request.ActorType,
            request.ActorId,
            rationale,
            damageCites);

        string? mobilityBatchId = null;
        if (proposal.Mobility.MaxSpeedKnots > 0
            || proposal.Mobility.CruiseSpeedKnots > 0
            || proposal.Mobility.RangeNm > 0)
        {
            mobilityBatchId = gate.ProposeMobilityBatch(
                [proposal.Mobility],
                request.ActorType,
                request.ActorId,
                rationale,
                mobilityCites);
        }

        return new CatalogEvidenceProposeResult(
            Outcome: CatalogEvidenceOutcome.ValidDraft,
            Diagnostics: Array.Empty<CatalogEvidenceDiagnostic>(),
            Brief: brief,
            Proposal: proposal,
            PlatformBatchId: platformBatchId,
            DamageBatchId: damageBatchId,
            MobilityBatchId: mobilityBatchId,
            Citations: citations,
            SubmitAllowedWithoutHuman: false);
    }

    private static CatalogEvidenceProposeResult Fail(
        CatalogEvidenceOutcome outcome,
        IReadOnlyList<CatalogEvidenceDiagnostic> diagnostics) =>
        new(
            Outcome: outcome,
            Diagnostics: diagnostics,
            Brief: null,
            Proposal: null,
            PlatformBatchId: null,
            DamageBatchId: null,
            MobilityBatchId: null,
            Citations: Array.Empty<CatalogEvidenceCitation>(),
            SubmitAllowedWithoutHuman: false);
}
