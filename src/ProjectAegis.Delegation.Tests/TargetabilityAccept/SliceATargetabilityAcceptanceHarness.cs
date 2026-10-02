using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.TargetabilityAccept;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.Tests.TargetabilityAccept;

/// <summary>
/// DRG-183: one Find → Fix → Track → Target path and its acceptance verdict.
/// Read-only. The harness never appends an engagement, player order, magazine change, or ordnance row.
/// </summary>
public sealed record SliceATargetabilityPathEvidence(
    string PathId,
    string Kind,
    string ContactId,
    string TargetId,
    KillChainPhase Phase,
    string Loss,
    string PhasesVisited,
    bool TechnicallyTargetable,
    bool ProvenancePresent,
    ContactProvenanceFreshness Freshness,
    ulong AgeTicks,
    string QualityState,
    string Confidence,
    string SourceRef,
    string Roe,
    string RoeLabel,
    bool EngageAllowedByRoe,
    string AuthorityDisposition,
    string AuthorityReasonCode,
    string PendingApproval,
    string EngageVerbDisposition,
    string EngageVerbReason,
    TargetabilityAcceptDisposition Disposition,
    string WithheldCauseCode,
    bool ChainComplete,
    string ChainCauseLabel,
    int ContactChangeCount,
    int EngagementCount,
    int EngagementOutcomeCount,
    int PlayerOrderCount,
    int MagazineChangeCount,
    int OrdnanceStateChangeCount,
    string Explanation,
    string AcceptFingerprint,
    string KillChainFingerprint);

/// <summary>
/// DRG-183: replay-stable Slice A exit evidence for a scenario that stops at targetability.
/// </summary>
public sealed record SliceATargetabilityAcceptanceEvidence(
    string ScenarioId,
    string StoryId,
    string ReplayScope,
    ulong EvaluationSimTick,
    int EngagementCount,
    int EngagementOutcomeCount,
    int PlayerOrderCount,
    int MagazineChangeCount,
    int OrdnanceStateChangeCount,
    int ContactChangeCount,
    IReadOnlyList<SliceATargetabilityPathEvidence> Paths,
    string AcceptanceFingerprint);

/// <summary>
/// DRG-183: executed evidence plus the fingerprint pinned in the scenario fixture.
/// </summary>
public sealed record SliceATargetabilityAcceptanceRun(
    SliceATargetabilityAcceptanceEvidence Evidence,
    string PinnedAcceptanceFingerprint);

/// <summary>
/// DRG-183: loads the Slice A targetability scenario fixture and projects Find through Target
/// without firing a weapon. Headless only: contact rows in, acceptance snapshot out.
/// No engagement, magazine, ordnance, or player-order rows are appended.
/// </summary>
public static class SliceATargetabilityAcceptanceHarness
{
    public const string FixtureRelativePath =
        "src/ProjectAegis.Delegation.Tests/TargetabilityAccept/Fixtures/slice-a-targetability-scenario.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        NumberHandling = JsonNumberHandling.Strict,
    };

    /// <summary>Run the checked-in Slice A scenario from the repository root.</summary>
    public static SliceATargetabilityAcceptanceRun Run()
    {
        var root = FindRepoRoot()
            ?? throw new InvalidOperationException("ProjectAegis.sln was not found above the test base directory.");
        return Run(Path.Combine(root, FixtureRelativePath));
    }

    /// <summary>Run the scenario fixture at <paramref name="fixturePath"/>.</summary>
    public static SliceATargetabilityAcceptanceRun Run(string fixturePath)
    {
        if (string.IsNullOrWhiteSpace(fixturePath))
        {
            throw new ArgumentException("A scenario fixture path is required.", nameof(fixturePath));
        }

        var document = Load(fixturePath);
        var evidence = Execute(document);
        return new SliceATargetabilityAcceptanceRun(evidence, document.AcceptanceFingerprint);
    }

    private static SliceAScenarioFile Load(string fixturePath)
    {
        var json = File.ReadAllText(fixturePath);
        var document = JsonSerializer.Deserialize<SliceAScenarioFile>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Scenario fixture '{fixturePath}' deserialized to null.");
        Validate(document, fixturePath);
        return document;
    }

    private static void Validate(SliceAScenarioFile document, string fixturePath)
    {
        Require(document.ScenarioId, nameof(document.ScenarioId), fixturePath);
        if (!string.Equals(document.StoryId, "DRG-183", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Scenario fixture '{fixturePath}' storyId must be DRG-183.");
        }

        if (!string.Equals(document.ReplayScope, "slice-a-exit-gate-off-baltic-v2-hash", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Scenario fixture '{fixturePath}' replayScope must stay off the Baltic v2 hash.");
        }

        if (!string.Equals(document.Catalog, "baltic-patrol", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Scenario fixture '{fixturePath}' catalog must be baltic-patrol.");
        }

        Require(document.ObserverId, nameof(document.ObserverId), fixturePath);
        Require(document.ShooterUnitId, nameof(document.ShooterUnitId), fixturePath);
        if (document.WeaponRounds < 1)
        {
            throw new InvalidOperationException(
                $"Scenario fixture '{fixturePath}' weaponRounds must be a positive read-only candidate count.");
        }

        if (string.IsNullOrWhiteSpace(document.AcceptanceFingerprint))
        {
            throw new InvalidOperationException(
                $"Scenario fixture '{fixturePath}' is missing acceptanceFingerprint.");
        }

        if (document.Paths is null || document.Paths.Count < 2)
        {
            throw new InvalidOperationException(
                $"Scenario fixture '{fixturePath}' must include a valid path and an approval-required path.");
        }
    }

    private static SliceATargetabilityAcceptanceEvidence Execute(SliceAScenarioFile document)
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var paths = new SliceATargetabilityPathEvidence[document.Paths.Count];
        var engagements = 0;
        var outcomes = 0;
        var playerOrders = 0;
        var magazines = 0;
        var ordnance = 0;
        var contacts = 0;
        for (var i = 0; i < document.Paths.Count; i++)
        {
            paths[i] = ProjectPath(document, document.Paths[i], catalog);
            engagements += paths[i].EngagementCount;
            outcomes += paths[i].EngagementOutcomeCount;
            playerOrders += paths[i].PlayerOrderCount;
            magazines += paths[i].MagazineChangeCount;
            ordnance += paths[i].OrdnanceStateChangeCount;
            contacts += paths[i].ContactChangeCount;
        }

        var fingerprint = ComposeAcceptanceFingerprint(
            document,
            engagements,
            outcomes,
            playerOrders,
            magazines,
            ordnance,
            contacts,
            paths);
        return new SliceATargetabilityAcceptanceEvidence(
            document.ScenarioId,
            document.StoryId,
            document.ReplayScope,
            document.EvaluationSimTick,
            engagements,
            outcomes,
            playerOrders,
            magazines,
            ordnance,
            contacts,
            paths,
            fingerprint);
    }

    private static SliceATargetabilityPathEvidence ProjectPath(
        SliceAScenarioFile document,
        SliceAScenarioPathFile path,
        ICatalogReader catalog)
    {
        Require(path.PathId, nameof(path.PathId), path.PathId);
        if (path.Kind is not ("Valid" or "ApprovalRequired"))
        {
            throw new InvalidOperationException(
                $"Path '{path.PathId}' kind '{path.Kind}' must be Valid or ApprovalRequired.");
        }

        Require(path.ContactId, nameof(path.ContactId), path.PathId);
        Require(path.TargetId, nameof(path.TargetId), path.PathId);
        if (path.Steps is null || path.Steps.Count == 0)
        {
            throw new InvalidOperationException($"Path '{path.PathId}' has no contact steps.");
        }

        if (path.Authority is null)
        {
            throw new InvalidOperationException($"Path '{path.PathId}' has no authority block.");
        }

        var log = new DecisionLog();
        for (var i = 0; i < path.Steps.Count; i++)
        {
            var step = path.Steps[i];
            if (step.SequenceId == 0)
            {
                throw new InvalidOperationException(
                    $"Path '{path.PathId}' step {i} must set a non-zero sequenceId.");
            }

            Require(step.PreviousState, nameof(step.PreviousState), path.PathId);
            Require(step.NewState, nameof(step.NewState), path.PathId);
            var change = new ContactChangeRecord(
                step.SequenceId,
                step.SimTime,
                step.SimTick,
                document.ObserverId,
                path.ContactId,
                path.TargetId,
                step.PreviousState,
                step.NewState);
            log.Append(OrderLogEntryFactories.FromContactChange(change, step.SequenceId));
        }

        var fireControl = new FixtureFireControl(path.TargetId, path.FireControlContactIds);
        var shooters = new FixtureShooterSource(
            path.TargetId,
            new SensorToShooterShooterCandidate(
                document.ShooterUnitId,
                ScenarioEngageDefaults.MvpFallback,
                document.WeaponRounds));
        var authorityContext = BuildAuthority(path);
        var accept = TargetabilityAcceptProjection.Project(
            log,
            document.EvaluationSimTick,
            authorityContext,
            fireControl,
            shooters,
            catalog);
        var killChain = KillChainContactStateProjection.Project(
            log,
            document.EvaluationSimTick,
            fireControl);

        if (accept.Contacts.Count != 1)
        {
            throw new InvalidOperationException(
                $"Path '{path.PathId}' projected {accept.Contacts.Count} acceptance rows.");
        }

        if (killChain.Contacts.Count != 1)
        {
            throw new InvalidOperationException(
                $"Path '{path.PathId}' projected {killChain.Contacts.Count} kill-chain rows.");
        }

        var row = accept.Contacts[0];
        var contact = killChain.Contacts[0];
        var provenance = row.Provenance;
        var engage = FindEngage(row.Authority);
        var phases = FormatPhases(killChain);
        var explanation = FormatExplanation(contact, phases, row, engage);
        return new SliceATargetabilityPathEvidence(
            path.PathId,
            path.Kind,
            row.ContactId,
            row.TargetId,
            contact.Phase,
            contact.Loss.ToString(),
            phases,
            contact.Targetable,
            provenance is not null,
            provenance?.Freshness ?? ContactProvenanceFreshness.Stale,
            provenance?.AgeTicks ?? ulong.MaxValue,
            provenance is null ? "missing" : provenance.QualityState.ToString(),
            provenance is null ? "missing" : provenance.Confidence.ToString(),
            provenance?.Source.SourceRef ?? string.Empty,
            row.Authority.Roe.Roe.ToString(),
            row.Authority.Roe.RoeLabel,
            row.Authority.Roe.EngageAllowedByRoe,
            row.Authority.Targeting.Disposition.ToString(),
            row.Authority.Targeting.ReasonCode ?? string.Empty,
            row.Authority.Targeting.PendingApproval?.ToString() ?? string.Empty,
            engage.Disposition.ToString(),
            engage.ReasonCode ?? string.Empty,
            row.Disposition,
            row.WithheldCauseCode,
            row.SensorToShooter is { IsComplete: true },
            row.SensorToShooter?.PrimaryCauseLabel ?? "missing",
            log.ContactChanges.Count,
            log.Engagements.Count,
            log.EngagementOutcomes.Count,
            log.PlayerOrders.Count,
            log.MagazineChanges.Count,
            log.OrdnanceStateChanges.Count,
            explanation,
            TargetabilityAcceptFingerprint.Compute(accept),
            KillChainContactStateProjection.ComputeFingerprint(killChain));
    }

    private static C2AuthorityProjectionContext BuildAuthority(SliceAScenarioPathFile path)
    {
        var authority = path.Authority!;
        var commandId = string.IsNullOrEmpty(authority.CommandId) ? null : authority.CommandId;
        return new C2AuthorityProjectionContext(
            ParseEnum<RoeLevel>(authority.Roe, path.PathId, nameof(authority.Roe)),
            ParseEnum<SkillLane>(authority.Lane, path.PathId, nameof(authority.Lane)),
            ParseEnum<RequiredApproval>(authority.RequiredApproval, path.PathId, nameof(authority.RequiredApproval)),
            ParseEnum<TrackSource>(authority.TrackSource, path.PathId, nameof(authority.TrackSource)),
            authority.FireControlSatisfied,
            commandId,
            authority.HumanControlled);
    }

    private static C2AuthorityActionState FindEngage(C2AuthorityProjection authority)
    {
        for (var i = 0; i < authority.Actions.Count; i++)
        {
            var action = authority.Actions[i];
            if (action.Action == C2AuthorityActionKind.Engage)
            {
                return action;
            }
        }

        throw new InvalidOperationException("Authority projection did not include an Engage verb.");
    }

    private static string FormatPhases(KillChainContactSnapshot snapshot)
    {
        var seen = new List<string>(4);
        for (var i = 0; i < snapshot.Transitions.Count; i++)
        {
            var kind = snapshot.Transitions[i].Kind;
            if (kind is not (
                KillChainTransitionKind.Find
                or KillChainTransitionKind.Fix
                or KillChainTransitionKind.Track
                or KillChainTransitionKind.Target))
            {
                continue;
            }

            var label = kind.ToString();
            if (!seen.Contains(label))
            {
                seen.Add(label);
            }
        }

        return string.Join('>', seen);
    }

    private static string FormatExplanation(
        KillChainContactState contact,
        string phases,
        TargetabilityAcceptContactRow row,
        C2AuthorityActionState engage)
    {
        var provenance = row.Provenance;
        var builder = new StringBuilder();
        builder.Append("phase=").Append(contact.Phase.ToString());
        builder.Append(";loss=").Append(contact.Loss.ToString());
        builder.Append(";phases=").Append(phases);
        builder.Append(";targetable=").Append(contact.Targetable ? '1' : '0');
        if (provenance is null)
        {
            builder.Append(";provenance=missing");
        }
        else
        {
            builder.Append(";provenance=").Append(provenance.Source.SourceRef);
            builder.Append(";observer=").Append(provenance.Source.ObserverId);
            builder.Append(";confidence=").Append(provenance.Confidence.ToString());
            builder.Append(";freshness=").Append(provenance.Freshness.ToString());
            builder.Append(";ageTicks=").Append(provenance.AgeTicks.ToString(CultureInfo.InvariantCulture));
            builder.Append(";quality=").Append(provenance.QualityState.ToString());
            builder.Append(";lifecycle=").Append(provenance.LastKnown.LifecycleState);
        }

        builder.Append(";roe=").Append(row.Authority.Roe.Roe.ToString());
        builder.Append(";roeLabel=").Append(row.Authority.Roe.RoeLabel);
        builder.Append(";engageAllowed=").Append(row.Authority.Roe.EngageAllowedByRoe ? '1' : '0');
        builder.Append(";authority=").Append(row.Authority.Targeting.Disposition.ToString());
        builder.Append(";authorityReason=").Append(row.Authority.Targeting.ReasonCode ?? string.Empty);
        builder.Append(";pending=").Append(row.Authority.Targeting.PendingApproval?.ToString() ?? string.Empty);
        builder.Append(";engageVerb=").Append(engage.Disposition.ToString());
        builder.Append(";engageVerbReason=").Append(engage.ReasonCode ?? string.Empty);
        builder.Append(";disposition=").Append(row.Disposition.ToString());
        builder.Append(";cause=").Append(row.WithheldCauseCode);
        builder.Append(";chain=").Append(row.SensorToShooter is { IsComplete: true } ? "complete" : "broken");
        builder.Append(";chainCause=").Append(row.SensorToShooter?.PrimaryCauseLabel ?? "missing");
        return builder.ToString();
    }

    private static string ComposeAcceptanceFingerprint(
        SliceAScenarioFile document,
        int engagements,
        int outcomes,
        int playerOrders,
        int magazines,
        int ordnance,
        int contacts,
        IReadOnlyList<SliceATargetabilityPathEvidence> paths)
    {
        var builder = new StringBuilder();
        builder.Append("sla:id=").Append(document.ScenarioId);
        builder.Append("|story=").Append(document.StoryId);
        builder.Append("|scope=").Append(document.ReplayScope);
        builder.Append("|tick=").Append(document.EvaluationSimTick.ToString(CultureInfo.InvariantCulture));
        builder.Append("|engage=").Append(engagements.ToString(CultureInfo.InvariantCulture));
        builder.Append("|outcomes=").Append(outcomes.ToString(CultureInfo.InvariantCulture));
        builder.Append("|orders=").Append(playerOrders.ToString(CultureInfo.InvariantCulture));
        builder.Append("|mag=").Append(magazines.ToString(CultureInfo.InvariantCulture));
        builder.Append("|ordnance=").Append(ordnance.ToString(CultureInfo.InvariantCulture));
        builder.Append("|contacts=").Append(contacts.ToString(CultureInfo.InvariantCulture));
        builder.Append("|n=").Append(paths.Count.ToString(CultureInfo.InvariantCulture));
        for (var i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            builder.Append("|path=").Append(path.PathId);
            builder.Append(";kind=").Append(path.Kind);
            builder.Append(";contact=").Append(path.ContactId);
            builder.Append(";target=").Append(path.TargetId);
            builder.Append(";phase=").Append(path.Phase.ToString());
            builder.Append(";loss=").Append(path.Loss);
            builder.Append(";visited=").Append(path.PhasesVisited);
            builder.Append(";tech=").Append(path.TechnicallyTargetable ? '1' : '0');
            builder.Append(";prov=").Append(path.ProvenancePresent ? '1' : '0');
            builder.Append(";fresh=").Append(path.Freshness.ToString());
            builder.Append(";age=").Append(path.AgeTicks.ToString(CultureInfo.InvariantCulture));
            builder.Append(";quality=").Append(path.QualityState);
            builder.Append(";confidence=").Append(path.Confidence);
            builder.Append(";source=").Append(path.SourceRef);
            builder.Append(";roe=").Append(path.Roe);
            builder.Append(";roeLabel=").Append(path.RoeLabel);
            builder.Append(";roeEngage=").Append(path.EngageAllowedByRoe ? '1' : '0');
            builder.Append(";authority=").Append(path.AuthorityDisposition);
            builder.Append(";authorityReason=").Append(path.AuthorityReasonCode);
            builder.Append(";pending=").Append(path.PendingApproval);
            builder.Append(";engageVerb=").Append(path.EngageVerbDisposition);
            builder.Append(";engageVerbReason=").Append(path.EngageVerbReason);
            builder.Append(";disposition=").Append(path.Disposition.ToString());
            builder.Append(";cause=").Append(path.WithheldCauseCode);
            builder.Append(";chain=").Append(path.ChainComplete ? '1' : '0');
            builder.Append(";chainCause=").Append(path.ChainCauseLabel);
            builder.Append(";log=").Append(path.ContactChangeCount.ToString(CultureInfo.InvariantCulture));
            builder.Append('/').Append(path.EngagementCount.ToString(CultureInfo.InvariantCulture));
            builder.Append('/').Append(path.EngagementOutcomeCount.ToString(CultureInfo.InvariantCulture));
            builder.Append('/').Append(path.PlayerOrderCount.ToString(CultureInfo.InvariantCulture));
            builder.Append('/').Append(path.MagazineChangeCount.ToString(CultureInfo.InvariantCulture));
            builder.Append('/').Append(path.OrdnanceStateChangeCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(";explain=").Append(path.Explanation);
            builder.Append(";tac=").Append(path.AcceptFingerprint);
            builder.Append(";kc=").Append(path.KillChainFingerprint);
        }

        return builder.ToString();
    }

    private static TEnum ParseEnum<TEnum>(string? value, string pathId, string field)
        where TEnum : struct, Enum
    {
        if (value is not null && Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Path '{pathId}' field '{field}' value '{value}' is not a {typeof(TEnum).Name}.");
    }

    private static void Require(string? value, string field, string owner)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Scenario field '{field}' is required ({owner}).");
        }
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }

    private sealed class FixtureFireControl : IKillChainFireControlSource
    {
        private readonly string _targetId;
        private readonly HashSet<string> _contactIds;

        public FixtureFireControl(string targetId, IReadOnlyList<string>? contactIds)
        {
            _targetId = targetId;
            _contactIds = new HashSet<string>(contactIds ?? [], StringComparer.Ordinal);
        }

        public bool HasFireControlTrack(string contactId, string targetId) =>
            string.Equals(targetId, _targetId, StringComparison.Ordinal)
            && _contactIds.Contains(contactId);
    }

    private sealed class FixtureShooterSource : ISensorToShooterShooterSource
    {
        private readonly string _targetId;
        private readonly SensorToShooterShooterCandidate[] _candidates;

        public FixtureShooterSource(string targetId, params SensorToShooterShooterCandidate[] candidates)
        {
            _targetId = targetId;
            _candidates = candidates;
        }

        public IReadOnlyList<SensorToShooterShooterCandidate> GetCandidatesForTarget(string targetId) =>
            string.Equals(targetId, _targetId, StringComparison.Ordinal)
                ? _candidates
                : [];
    }

    private sealed class SliceAScenarioFile
    {
        public string ScenarioId { get; init; } = string.Empty;

        public string StoryId { get; init; } = string.Empty;

        public string ReplayScope { get; init; } = string.Empty;

        public ulong EvaluationSimTick { get; init; }

        public string ObserverId { get; init; } = string.Empty;

        public string ShooterUnitId { get; init; } = string.Empty;

        public int WeaponRounds { get; init; }

        public string Catalog { get; init; } = string.Empty;

        public string AcceptanceFingerprint { get; init; } = string.Empty;

        public List<SliceAScenarioPathFile> Paths { get; init; } = [];
    }

    private sealed class SliceAScenarioPathFile
    {
        [JsonConstructor]
        public SliceAScenarioPathFile(
            IReadOnlyList<string>? fireControlContactIds,
            SliceAAuthorityFile? authority,
            IReadOnlyList<SliceAStepFile>? steps)
        {
            FireControlContactIds = fireControlContactIds;
            Authority = authority;
            Steps = steps;
        }

        public string PathId { get; init; } = string.Empty;

        public string Kind { get; init; } = string.Empty;

        public string ContactId { get; init; } = string.Empty;

        public string TargetId { get; init; } = string.Empty;

        public IReadOnlyList<string>? FireControlContactIds { get; }

        public SliceAAuthorityFile? Authority { get; }

        public IReadOnlyList<SliceAStepFile>? Steps { get; }
    }

    private sealed class SliceAAuthorityFile
    {
        [JsonConstructor]
        public SliceAAuthorityFile(bool fireControlSatisfied, string? commandId, bool humanControlled)
        {
            FireControlSatisfied = fireControlSatisfied;
            CommandId = commandId;
            HumanControlled = humanControlled;
        }

        public string Roe { get; init; } = string.Empty;

        public string Lane { get; init; } = string.Empty;

        public string RequiredApproval { get; init; } = string.Empty;

        public string TrackSource { get; init; } = string.Empty;

        public bool FireControlSatisfied { get; }

        public string? CommandId { get; }

        public bool HumanControlled { get; }
    }

    private sealed class SliceAStepFile
    {
        [JsonConstructor]
        public SliceAStepFile(ulong sequenceId, double simTime, ulong simTick)
        {
            SequenceId = sequenceId;
            SimTime = simTime;
            SimTick = simTick;
        }

        public ulong SequenceId { get; }

        public double SimTime { get; }

        public ulong SimTick { get; }

        public string PreviousState { get; init; } = string.Empty;

        public string NewState { get; init; } = string.Empty;
    }
}
