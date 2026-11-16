using System.Globalization;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>
/// DRG-736: projects <see cref="IReadOnlyOrderLog"/> into <see cref="EvidenceDocument"/>.
/// Read-only; stable order by <see cref="OrderLogEntry.SequenceId"/>; side filter before emit.
/// </summary>
public sealed class EvidenceProjector : IEvidenceProjector
{
    /// <inheritdoc />
    public EvidenceProjectionResult Project(
        IReadOnlyOrderLog log,
        EvidenceProjectionRequest request,
        IReadOnlyDictionary<ulong, EvidenceSourceAnnotation>? annotations = null)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BuildIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ScenarioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CatalogRevision);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FingerprintPrefix);

        var chronological = log.ChronologicalEntries()
            .OrderBy(e => e.SequenceId)
            .ToArray();

        var quantitative = BuildQuantitative(log, chronological, request.AuthoritativeFacts);
        var aar = request.AarSection ?? AgentPerformanceAarProjection.Project(log);
        var aarAttributes = BuildAarAttributeMap(aar);

        var entries = new List<EvidenceEntry>(chronological.Length);
        var stampedFacts = false;
        foreach (var entry in chronological)
        {
            EvidenceSourceAnnotation? annotation = null;
            if (annotations is not null)
            {
                annotations.TryGetValue(entry.SequenceId, out annotation);
            }

            if (!IsVisible(request, annotation))
            {
                continue;
            }

            var mapped = MapEntry(entry, request, annotation, aarAttributes);
            if (!stampedFacts && request.AuthoritativeFacts is { Count: > 0 } facts)
            {
                mapped = StampFacts(mapped, facts);
                stampedFacts = true;
            }

            entries.Add(mapped);
        }

        var evidenceIds = entries.Select(e => e.EvidenceId).ToArray();
        var debrief = request.DebriefMode || request.ViewingSide == ViewingSide.Debrief;
        var header = new EvidenceHeader(
            SchemaVersion: AiContractSchema.Version,
            BuildIdentity: request.BuildIdentity,
            ScenarioId: request.ScenarioId,
            CatalogRevision: request.CatalogRevision,
            OrderLogFingerprint: log.ComputeFingerprint(),
            ViewingSide: request.ViewingSide,
            DebriefMode: debrief,
            EvidenceIds: evidenceIds);

        var document = new EvidenceDocument(header, entries);
        return new EvidenceProjectionResult(
            document,
            quantitative,
            aar,
            request.AuthoritativeFacts);
    }

    private static bool IsVisible(
        EvidenceProjectionRequest request,
        EvidenceSourceAnnotation? annotation)
    {
        if (request.DebriefMode || request.ViewingSide == ViewingSide.Debrief)
        {
            return true;
        }

        if (annotation is null || annotation.HiddenFrom.Count == 0)
        {
            return true;
        }

        foreach (var hidden in annotation.HiddenFrom)
        {
            if (hidden == request.ViewingSide)
            {
                return false;
            }
        }

        return true;
    }

    private static EvidenceEntry MapEntry(
        OrderLogEntry entry,
        EvidenceProjectionRequest request,
        EvidenceSourceAnnotation? annotation,
        IReadOnlyDictionary<string, string> aarAttributes)
    {
        var kind = MapKind(entry.Kind);
        var side = annotation?.Side ?? ViewingSide.Unknown;
        var (unitId, summary, attributes) = ExtractPayload(entry);

        if (aarAttributes.Count > 0)
        {
            attributes = Merge(attributes, aarAttributes);
        }

        var evidenceId = string.Create(
            CultureInfo.InvariantCulture,
            $"{request.FingerprintPrefix}:{entry.SequenceId}");

        return new EvidenceEntry(
            EvidenceId: evidenceId,
            SequenceId: entry.SequenceId,
            Kind: kind,
            SimTime: entry.SimTime,
            ViewingSide: side,
            UnitId: unitId,
            Summary: summary,
            Attributes: attributes);
    }

    private static EvidenceEntry StampFacts(
        EvidenceEntry entry,
        IReadOnlyDictionary<string, string> facts)
    {
        var prefixed = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in facts)
        {
            prefixed["fact." + pair.Key] = pair.Value;
        }

        return entry with { Attributes = Merge(entry.Attributes, prefixed) };
    }

    private static IReadOnlyDictionary<string, string>? Merge(
        IReadOnlyDictionary<string, string>? left,
        IReadOnlyDictionary<string, string> right)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (left is not null)
        {
            foreach (var pair in left)
            {
                map[pair.Key] = pair.Value;
            }
        }

        foreach (var pair in right)
        {
            map[pair.Key] = pair.Value;
        }

        return map.Count == 0 ? null : map;
    }

    private static EvidenceEntryKind MapKind(OrderLogEntryKind kind)
    {
        if (!Enum.TryParse(kind.ToString(), ignoreCase: false, out EvidenceEntryKind mapped))
        {
            throw new InvalidOperationException(
                $"Order-log kind '{kind}' has no EvidenceEntryKind mirror.");
        }

        return mapped;
    }

    private static (string? UnitId, string? Summary, IReadOnlyDictionary<string, string>? Attributes)
        ExtractPayload(OrderLogEntry entry)
    {
        return entry.Payload switch
        {
            ControllerChangeRecord c => (
                c.TargetId.Value,
                $"{c.PreviousKind}->{c.NewKind}",
                Dict(
                    ("previousKind", c.PreviousKind),
                    ("newKind", c.NewKind),
                    ("agentId", c.AgentId?.Value))),
            EngagementRecord e => (
                e.ShooterTargetId.Value,
                e.Launched ? "launched" : "aborted",
                Dict(
                    ("engagementId", e.EngagementId.ToString(CultureInfo.InvariantCulture)),
                    ("launched", e.Launched ? "true" : "false"),
                    ("victimUnitId", e.VictimTargetId?.Value),
                    ("weaponFamilyId", e.WeaponFamilyId),
                    ("salvoSize", e.SalvoSize.ToString(CultureInfo.InvariantCulture)),
                    ("abortReasonCode", e.AbortReasonCode))),
            EngagementOutcomeRecord o => (
                o.ShooterTargetId.Value,
                o.OutcomeCode,
                Dict(
                    ("engagementId", o.EngagementId.ToString(CultureInfo.InvariantCulture)),
                    ("outcomeCode", o.OutcomeCode),
                    ("victimUnitId", o.VictimTargetId.Value),
                    ("pkDraw", o.PkDraw.ToString(CultureInfo.InvariantCulture)))),
            PolicyDenialRecord d => (
                d.TargetId.Value,
                d.Reason.ToString(),
                Dict(
                    ("agentId", d.AgentId.Value),
                    ("reason", d.Reason.ToString()),
                    ("attemptedKind", d.AttemptedKind.ToString()),
                    ("policySnapshotId", d.PolicySnapshotId.ToString(CultureInfo.InvariantCulture)))),
            AgentDecisionPayload a => (
                null,
                a.ChosenOrderKind.ToString(),
                Dict(
                    ("agentId", a.AgentId.Value),
                    ("chosenOrderKind", a.ChosenOrderKind.ToString()),
                    ("simTick", a.SimTick.ToString(CultureInfo.InvariantCulture)))),
            DecisionRecord r => (
                null,
                r.ChosenKind.ToString(),
                Dict(
                    ("agentId", r.AgentId.Value),
                    ("chosenKind", r.ChosenKind.ToString()),
                    ("simTick", r.SimTick.ToString(CultureInfo.InvariantCulture)))),
            IReadOnlyDictionary<string, string> bag => (
                bag.TryGetValue("unitId", out var u) ? u
                    : bag.TryGetValue("shooterUnitId", out var s) ? s
                    : bag.TryGetValue("targetId", out var t) ? t
                    : null,
                bag.TryGetValue("summary", out var summary) ? summary : entry.Kind.ToString(),
                bag),
            _ => (null, entry.Kind.ToString(), null),
        };
    }

    private static IReadOnlyDictionary<string, string>? Dict(params (string Key, string? Value)[] pairs)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
        {
            if (!string.IsNullOrEmpty(value))
            {
                map[key] = value;
            }
        }

        return map.Count == 0 ? null : map;
    }

    private static EvidenceQuantitativeSummary BuildQuantitative(
        IReadOnlyOrderLog log,
        IReadOnlyList<OrderLogEntry> chronological,
        IReadOnlyDictionary<string, string>? facts)
    {
        var decisions = log.Records.Count;
        var denials = log.PolicyDenials.Count;
        var engagements = log.Engagements.Count;
        var controllers = log.ControllerChanges.Count;

        if (decisions == 0 && denials == 0 && engagements == 0 && controllers == 0)
        {
            foreach (var entry in chronological)
            {
                switch (entry.Kind)
                {
                    case OrderLogEntryKind.AgentDecision:
                        decisions++;
                        break;
                    case OrderLogEntryKind.PolicyDenial:
                        denials++;
                        break;
                    case OrderLogEntryKind.Engagement:
                        engagements++;
                        break;
                    case OrderLogEntryKind.ControllerChange:
                        controllers++;
                        break;
                }
            }
        }

        double? objectivesRatio = null;
        if (facts is not null
            && facts.TryGetValue("objectivesRatio", out var raw)
            && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            objectivesRatio = parsed;
        }

        return new EvidenceQuantitativeSummary(
            decisions,
            denials,
            engagements,
            controllers,
            objectivesRatio);
    }

    private static IReadOnlyDictionary<string, string> BuildAarAttributeMap(AgentPerformanceSection aar)
    {
        if (aar.Rows.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in aar.Rows)
        {
            var prefix = "aar." + row.AgentId + ".";
            map[prefix + "intentCount"] = row.IntentCount.ToString(CultureInfo.InvariantCulture);
            map[prefix + "policyDenialCount"] = row.PolicyDenialCount.ToString(CultureInfo.InvariantCulture);
            map[prefix + "overrideCount"] = row.OverrideCount.ToString(CultureInfo.InvariantCulture);
            map[prefix + "overrideRate"] = row.OverrideRate.ToString(CultureInfo.InvariantCulture);
        }

        return map;
    }
}
