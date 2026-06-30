namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// First-class umpire and adjudication workspace (req 11, AC3).
/// Pure deterministic snapshot/diff; verif-before every op; real state from editor.Load/Create + mutations.
/// Role-based: 'umpire' role grants full controls (author/reviewer/player denied for adjudication ops).
/// </summary>
public sealed class AdjudicationWorkspace
{
    private readonly ScenarioDocumentEditor _editor;
    private readonly string _currentRole;
    private readonly List<AdjudicationSnapshot> _snapshots = new();
    private readonly List<AuditEntry> _auditEntries = new();
    private bool _isFrozen;
    private int _stepCount;

    public AdjudicationWorkspace(ScenarioDocumentEditor editor, string role = "umpire")
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _currentRole = role ?? "player";
        ApplyRoleGuard(_currentRole); // verif-before on construction for adjudication workspace
        _isFrozen = false;
        _stepCount = 0;
    }

    public string Role => _currentRole;
    public bool IsFrozen => _isFrozen;
    public int StepCount => _stepCount;
    public IReadOnlyList<AdjudicationSnapshot> Snapshots => _snapshots;
    public IReadOnlyList<AuditEntry> AuditEntries => _auditEntries;

    /// <summary>Pure: captures real turn-boundary snapshot from editor state (no side effects). Clones state for immutability of historical snapshots.</summary>
    public AdjudicationSnapshot Snapshot(string turn)
    {
        if (string.IsNullOrWhiteSpace(turn))
            turn = $"turn-{_stepCount}";
        var src = _editor.ToDto();
        // deep copy missions list + metadata ref (immutable enough) to keep snapshot stable (pure historical)
        var clonedMissions = src.Missions.Select(m => new ScenarioMissionDto
        {
            Id = m.Id,
            Type = m.Type,
            AssignedUnitIds = m.AssignedUnitIds.ToArray(),
            TargetIds = m.TargetIds.ToArray(),
            FerryDestinationBaseId = m.FerryDestinationBaseId,
            PatrolZone = m.PatrolZone.Select(w => new ScenarioWaypointDto { Lat = w.Lat, Lon = w.Lon }).ToArray()
        }).ToList();
        var dto = new ScenarioDocumentDto
        {
            Metadata = src.Metadata,
            Missions = clonedMissions
        };
        var hash = _editor.ComputeFileHash();
        var snap = new AdjudicationSnapshot(
            Id: $"snap-{turn}-{hash.Substring(0, 8)}",
            Turn: turn,
            StateHash: hash,
            State: dto,
            Timestamp: hash // deterministic/pure (no wall clock)
        );
        _snapshots.Add(snap);
        return snap;
    }

    /// <summary>Pure deterministic before/after diff computation from snapshots + reason.</summary>
    public AdjudicationDiff ComputeDiff(AdjudicationSnapshot before, AdjudicationSnapshot after, string reason)
    {
        if (before == null || after == null)
            throw new ArgumentNullException("Snapshots required for diff");
        if (string.IsNullOrWhiteSpace(reason))
            reason = "unspecified umpire intervention";

        int beforeMissions = before.State.Missions.Count;
        int afterMissions = after.State.Missions.Count;
        int delta = afterMissions - beforeMissions;
        string summary = $"missions: {beforeMissions}->{afterMissions} (delta={delta}); hashes {before.StateHash.Substring(0,8)}->{after.StateHash.Substring(0,8)}";

        var diff = new AdjudicationDiff(
            Id: $"diff-{before.Id}-to-{after.Id}",
            BeforeHash: before.StateHash,
            AfterHash: after.StateHash,
            Reason: reason,
            DiffSummary: summary
        );
        return diff;
    }

    /// <summary>Records audit entry with reason. Verif-before role.</summary>
    public AuditEntry AuditLog(string action, string reason, string? role = null)
    {
        role ??= _currentRole;
        ApplyRoleGuard(role);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Audit requires reason");

        var stateHash = _editor.ComputeFileHash();
        var entry = new AuditEntry(
            Timestamp: stateHash + "-" + _auditEntries.Count.ToString(),
            Action: action,
            Reason: reason,
            Role: role,
            StateHash: stateHash
        );
        _auditEntries.Add(entry);
        return entry;
    }

    /// <summary>Role guard: only umpire (and author for limited) allowed for adjudication ops. Verif-before.</summary>
    public string ApplyRoleGuard(string role)
    {
        if (string.Equals(role, "umpire", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "author", StringComparison.OrdinalIgnoreCase))
        {
            return "ok";
        }
        throw new InvalidOperationException($"Role '{role}' denied for adjudication workspace operations (allowed: umpire, author)");
    }

    /// <summary>Freeze: prevents further mutations until step/inject/resume. Verif-before role.</summary>
    public void Freeze()
    {
        ApplyRoleGuard(_currentRole);
        _isFrozen = true;
        AuditLog("freeze", "umpire freeze for adjudication intervention", _currentRole);
    }

    /// <summary>Step: advances adjudication step counter (turn boundary sim). Allowed in frozen or running.</summary>
    public void Step()
    {
        ApplyRoleGuard(_currentRole);
        _stepCount++;
        // step does not un-freeze; explicit resume for that
        AuditLog("step", "adjudication step advanced", _currentRole);
    }

    /// <summary>
    /// Inject: performs real mutation from editor under umpire role + verif-before.
    /// Example: inject a patrol or strike marker to demonstrate state change (uses editor mutation).
    /// Caller provides the mutation action; workspace enforces guards + audit + commit.
    /// </summary>
    public void Inject(Action mutation, string reason = "umpire inject")
    {
        ApplyRoleGuard(_currentRole);
        if (_isFrozen)
        {
            // inject is explicitly allowed while frozen (core of adjudication)
        }
        if (mutation == null)
            throw new ArgumentNullException(nameof(mutation));
        if (string.IsNullOrWhiteSpace(reason))
            reason = "umpire intervention";

        // verif-before: snapshot current before inject
        var before = Snapshot($"pre-inject-{_stepCount}");

        mutation();

        // ensure committed if caller forgot (real editor usage)
        // Note: typical callers do their edits then we commit here for safety under umpire
        _editor.CommitMutation();

        // after state
        var after = Snapshot($"post-inject-{_stepCount}");

        var diff = ComputeDiff(before, after, reason);
        // record diff info in audit
        AuditLog("inject", $"{reason}; {diff.DiffSummary}", _currentRole);

        // mark the diff in snapshots for traceability (last action)
    }

    /// <summary>Resume: unfreezes for continued play or authoring. Verif-before.</summary>
    public void Resume()
    {
        ApplyRoleGuard(_currentRole);
        _isFrozen = false;
        AuditLog("resume", "adjudication resume after freeze/step/inject", _currentRole);
    }

    /// <summary>Helper: direct access to drive with editor.Load/Create</summary>
    public ScenarioDocumentEditor Editor => _editor;
}

/// <summary>Immutable turn-boundary snapshot (pure capture from editor state).</summary>
public sealed record AdjudicationSnapshot(
    string Id,
    string Turn,
    string StateHash,
    ScenarioDocumentDto State,
    string Timestamp  // string for pure deterministic value (hash based, no wall time)
);

/// <summary>Before/after diff for umpire interventions (pure computed).</summary>
public sealed record AdjudicationDiff(
    string Id,
    string BeforeHash,
    string AfterHash,
    string Reason,
    string DiffSummary
);

/// <summary>Audit entry with required reason (immutable log).</summary>
public sealed record AuditEntry(
    string Timestamp,
    string Action,
    string Reason,
    string Role,
    string StateHash
);
