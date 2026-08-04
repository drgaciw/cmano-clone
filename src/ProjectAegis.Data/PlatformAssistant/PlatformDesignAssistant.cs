namespace ProjectAegis.Data.PlatformAssistant;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// Platform Design Assistant (proposal agent). Grounds on <see cref="ICatalogReader"/>,
/// relative-scales archetype fields, stages extend-only batches via <see cref="CatalogWriteGate"/>.
/// Never mutates live catalog rows directly. Does not implement ProposeAsync — host API is synchronous
/// <see cref="Propose"/> matching <see cref="Platform.PlatformWorkbookWriteService.Propose"/>.
/// </summary>
public sealed class PlatformDesignAssistant
{
    public PlatformDesignProposal Draft(ICatalogReader catalog, PlatformDesignBrief brief)
    {
        if (catalog is null) throw new ArgumentNullException(nameof(catalog));
        if (brief is null) throw new ArgumentNullException(nameof(brief));

        var export = catalog.LoadExportData();
        return PlatformRelativeScaler.Scale(export, brief);
    }

    /// <summary>
    /// Stage a design proposal through CatalogWriteGate propose path only (no auto-approve).
    /// Order: platform metadata → damage → mobility (approve order should respect FK: platform first).
    /// </summary>
    public PlatformDesignProposeResult Propose(
        string databasePath,
        ICatalogReader catalog,
        PlatformDesignBrief brief,
        ICatalogClock clock,
        string actorType = "agent",
        string actorId = "platform-design-assistant",
        string? rationale = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        if (catalog is null) throw new ArgumentNullException(nameof(catalog));
        if (brief is null) throw new ArgumentNullException(nameof(brief));
        if (clock is null) throw new ArgumentNullException(nameof(clock));
        if (string.IsNullOrWhiteSpace(actorType))
        {
            throw new ArgumentException("Actor type is required.", nameof(actorType));
        }

        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException("Actor id is required.", nameof(actorId));
        }

        var proposal = Draft(catalog, brief);
        var effectiveRationale = string.IsNullOrWhiteSpace(rationale)
            ? $"platform_design_assistant:{proposal.Binding.PlatformId}"
            : rationale;

        var notes = new List<string>
        {
            proposal.Summary,
            $"skills:{string.Join(",", proposal.SkillsApplied)}",
            $"peers:{string.Join(",", proposal.Peers.Select(p => p.PlatformId))}",
        };

        foreach (var outlier in proposal.Outliers)
        {
            notes.Add($"outlier:{outlier}");
        }

        using var gate = new CatalogWriteGate(databasePath, clock);

        var platformBatchId = gate.ProposePlatformBatch(
            [proposal.Binding],
            actorType,
            actorId,
            effectiveRationale);
        notes.Add($"Proposed platform metadata batch '{platformBatchId}'.");

        var damageBatchId = gate.ProposePlatformDamageBatch(
            [proposal.Damage],
            actorType,
            actorId,
            effectiveRationale);
        notes.Add($"Proposed platform damage batch '{damageBatchId}'.");

        string? mobilityBatchId = null;
        if (proposal.Mobility.MaxSpeedKnots > 0
            || proposal.Mobility.CruiseSpeedKnots > 0
            || proposal.Mobility.RangeNm > 0)
        {
            mobilityBatchId = gate.ProposeMobilityBatch(
                [proposal.Mobility],
                actorType,
                actorId,
                effectiveRationale);
            notes.Add($"Proposed platform mobility batch '{mobilityBatchId}'.");
        }
        else
        {
            notes.Add("Skipped mobility batch (all speed/range zero).");
        }

        return new PlatformDesignProposeResult(
            Proposal: proposal,
            PlatformBatchId: platformBatchId,
            DamageBatchId: damageBatchId,
            MobilityBatchId: mobilityBatchId,
            Notes: notes);
    }
}
