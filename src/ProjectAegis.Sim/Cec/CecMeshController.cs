namespace ProjectAegis.Sim.Cec;

/// <summary>
/// SWARM-31 / B6a (DRG-102): CEC mesh membership/health + composite track picture.
/// Mesh is independent of C2 host/order channel — this type never references
/// <c>SwarmLinkState</c> or other Swarm C2 types. Remote engage (B6b) is separate.
/// Pure Sim surface: no Unity, no DelegationBridge.
/// </summary>
public sealed class CecMeshController
{
    private readonly double _connectedRangeDeg;
    private readonly double _degradedRangeDeg;
    private readonly Dictionary<string, NodeRuntime> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CecMeshState> _meshStates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OrganicContribution> _organics = new(StringComparer.Ordinal);
    private readonly List<CecMeshEvent> _eventLog = new();
    private ulong _eventSequence = 1;

    public CecMeshController(
        double connectedRangeDeg = CecMeshEvaluator.DefaultConnectedRangeDeg,
        double degradedRangeDeg = CecMeshEvaluator.DefaultDegradedRangeDeg)
    {
        _connectedRangeDeg = connectedRangeDeg > 0
            ? connectedRangeDeg
            : CecMeshEvaluator.DefaultConnectedRangeDeg;
        _degradedRangeDeg = degradedRangeDeg > _connectedRangeDeg
            ? degradedRangeDeg
            : CecMeshEvaluator.DefaultDegradedRangeDeg;
    }

    /// <summary>Append-only mesh join/leave/degrade events (sequence-ordered, deterministic).</summary>
    public IReadOnlyList<CecMeshEvent> MeshEventLog => _eventLog;

    /// <summary>Register or replace a CEC node registration (geometry + capability).</summary>
    public void Register(CecNodeRegistration registration)
    {
        if (registration is null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (string.IsNullOrWhiteSpace(registration.UnitId))
        {
            throw new ArgumentException("UnitId is required.", nameof(registration));
        }

        if (string.IsNullOrWhiteSpace(registration.SideId))
        {
            throw new ArgumentException("SideId is required.", nameof(registration));
        }

        var id = registration.UnitId.Trim();
        _nodes[id] = new NodeRuntime(
            id,
            registration.SideId.Trim(),
            registration.CecCapable,
            registration.LatDeg,
            registration.LonDeg,
            registration.IsAlive,
            registration.IsSwarm);

        if (!_meshStates.ContainsKey(id))
        {
            _meshStates[id] = CecMeshState.OutOfMesh;
        }
    }

    /// <summary>Update geometry and/or liveness for an existing node.</summary>
    public void UpdateNode(string unitId, double latDeg, double lonDeg, bool isAlive = true)
    {
        var node = RequireNode(unitId);
        node.LatDeg = latDeg;
        node.LonDeg = lonDeg;
        node.IsAlive = isAlive;
    }

    /// <summary>
    /// Recompute all mesh states pairwise among same-side CEC peers.
    /// Iteration order is deterministic (unit id ordinal sort). Jam drops mesh without implying C2 lost.
    /// </summary>
    public void Refresh(bool jammed = false)
    {
        var unitIds = _nodes.Keys.ToList();
        unitIds.Sort(StringComparer.Ordinal);

        foreach (var unitId in unitIds)
        {
            var node = _nodes[unitId];
            var previous = _meshStates.TryGetValue(unitId, out var existing)
                ? existing
                : CecMeshState.OutOfMesh;

            var (hasPeer, bestRange) = FindBestSameSideCecPeer(node, unitIds);
            var next = CecMeshEvaluator.EvaluateMeshState(
                node.CecCapable,
                hasPeer,
                bestRange,
                jammed,
                node.IsAlive,
                _connectedRangeDeg,
                _degradedRangeDeg);

            _meshStates[unitId] = next;
            if (previous != next)
            {
                LogStateTransition(unitId, previous, next);
            }
        }
    }

    public CecMeshState GetMeshState(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return CecMeshState.OutOfMesh;
        }

        return _meshStates.TryGetValue(unitId.Trim(), out var state)
            ? state
            : CecMeshState.OutOfMesh;
    }

    /// <summary>
    /// Contribute an organic sensor detection into the composite picture.
    /// Accepted only when the contributor is CecCapable and currently <see cref="CecMeshState.InMesh"/>.
    /// </summary>
    public bool ContributeOrganic(
        string sideId,
        string contributorUnitId,
        string targetId,
        double sensorQuality)
    {
        if (string.IsNullOrWhiteSpace(sideId) ||
            string.IsNullOrWhiteSpace(contributorUnitId) ||
            string.IsNullOrWhiteSpace(targetId))
        {
            return false;
        }

        var unitId = contributorUnitId.Trim();
        if (!_nodes.TryGetValue(unitId, out var node))
        {
            return false;
        }

        if (!node.CecCapable)
        {
            return false;
        }

        if (GetMeshState(unitId) != CecMeshState.InMesh)
        {
            return false;
        }

        if (!string.Equals(node.SideId, sideId.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        var quality = Clamp01(sensorQuality);
        var key = OrganicKey(sideId.Trim(), unitId, targetId.Trim());
        _organics[key] = new OrganicContribution(sideId.Trim(), unitId, targetId.Trim(), quality);
        return true;
    }

    /// <summary>
    /// Build composite tracks for a side from organic contributions of mesh-connected nodes.
    /// Requires ≥2 mesh-connected contributors on the same target.
    /// Fire-control quality requires ≥1 contributor currently <see cref="CecMeshState.InMesh"/>
    /// with quality ≥ 0.6 (degraded-only mesh yields FC false).
    /// </summary>
    public IReadOnlyList<CecCompositeTrack> TryGetCompositeTracks(string sideId)
    {
        if (string.IsNullOrWhiteSpace(sideId))
        {
            return Array.Empty<CecCompositeTrack>();
        }

        var side = sideId.Trim();
        var byTarget = new Dictionary<string, List<OrganicContribution>>(StringComparer.Ordinal);

        foreach (var contribution in _organics.Values)
        {
            if (!string.Equals(contribution.SideId, side, StringComparison.Ordinal))
            {
                continue;
            }

            var state = GetMeshState(contribution.ContributorUnitId);
            if (state is not (CecMeshState.InMesh or CecMeshState.Degraded))
            {
                continue;
            }

            if (!byTarget.TryGetValue(contribution.TargetId, out var list))
            {
                list = new List<OrganicContribution>();
                byTarget[contribution.TargetId] = list;
            }

            list.Add(contribution);
        }

        var targetIds = byTarget.Keys.ToList();
        targetIds.Sort(StringComparer.Ordinal);

        var tracks = new List<CecCompositeTrack>();
        foreach (var targetId in targetIds)
        {
            var contributors = byTarget[targetId];
            contributors.Sort(static (a, b) =>
                string.Compare(a.ContributorUnitId, b.ContributorUnitId, StringComparison.Ordinal));

            if (contributors.Count < 2)
            {
                continue;
            }

            var sum = 0.0;
            var bestQuality = -1.0;
            string? primary = null;
            var anyInMeshHighQuality = false;
            var anyInMesh = false;

            foreach (var c in contributors)
            {
                sum += c.SensorQuality;
                var state = GetMeshState(c.ContributorUnitId);
                if (state == CecMeshState.InMesh)
                {
                    anyInMesh = true;
                    if (c.SensorQuality >= 0.6)
                    {
                        anyInMeshHighQuality = true;
                    }
                }

                if (c.SensorQuality > bestQuality ||
                    (Math.Abs(c.SensorQuality - bestQuality) < 1e-12 &&
                     (primary is null ||
                      string.Compare(c.ContributorUnitId, primary, StringComparison.Ordinal) < 0)))
                {
                    bestQuality = c.SensorQuality;
                    primary = c.ContributorUnitId;
                }
            }

            var avgQuality = Clamp01(sum / contributors.Count);
            var fireControl = anyInMeshHighQuality && anyInMesh;

            tracks.Add(new CecCompositeTrack(
                TrackId: $"cec-{side}-{targetId}",
                TargetId: targetId,
                SideId: side,
                PrimaryContributorUnitId: primary ?? contributors[0].ContributorUnitId,
                ContributorCount: contributors.Count,
                FireControlQuality: fireControl,
                Quality: avgQuality));
        }

        return tracks;
    }

    /// <summary>Stable fingerprint of the mesh event log for determinism checks.</summary>
    public string ComputeEventLogFingerprint()
    {
        if (_eventLog.Count == 0)
        {
            return "empty";
        }

        var parts = new string[_eventLog.Count];
        for (var i = 0; i < _eventLog.Count; i++)
        {
            var e = _eventLog[i];
            parts[i] = $"{e.SequenceId}:{e.UnitId}:{e.Kind}:{e.PreviousState}->{e.NewState}";
        }

        return string.Join("|", parts);
    }

    private (bool HasPeer, double? BestRange) FindBestSameSideCecPeer(
        NodeRuntime node,
        IReadOnlyList<string> sortedUnitIds)
    {
        if (!node.CecCapable || !node.IsAlive)
        {
            return (false, null);
        }

        double? best = null;
        foreach (var peerId in sortedUnitIds)
        {
            if (string.Equals(peerId, node.UnitId, StringComparison.Ordinal))
            {
                continue;
            }

            var peer = _nodes[peerId];
            if (!peer.CecCapable || !peer.IsAlive)
            {
                continue;
            }

            if (!string.Equals(peer.SideId, node.SideId, StringComparison.Ordinal))
            {
                continue;
            }

            var range = CecMeshEvaluator.RangeDeg(node.LatDeg, node.LonDeg, peer.LatDeg, peer.LonDeg);
            if (best is null || range < best.Value)
            {
                best = range;
            }
        }

        if (best is null)
        {
            return (false, null);
        }

        // Peer counts as "in range" only when within degraded band (outer mesh envelope).
        if (best.Value > _degradedRangeDeg)
        {
            return (false, best);
        }

        return (true, best);
    }

    private void LogStateTransition(string unitId, CecMeshState previous, CecMeshState next)
    {
        var kind = next switch
        {
            CecMeshState.InMesh => CecMeshEventKind.Join,
            CecMeshState.Degraded => CecMeshEventKind.Degrade,
            _ => CecMeshEventKind.Leave,
        };

        // Leaving from Degraded or InMesh → Leave; joining from OutOfMesh → Join;
        // InMesh → Degraded → Degrade; Degraded → InMesh → Join.
        if (previous == CecMeshState.OutOfMesh && next == CecMeshState.Degraded)
        {
            kind = CecMeshEventKind.Degrade;
        }
        else if (previous != CecMeshState.OutOfMesh && next == CecMeshState.OutOfMesh)
        {
            kind = CecMeshEventKind.Leave;
        }
        else if (next == CecMeshState.InMesh)
        {
            kind = CecMeshEventKind.Join;
        }
        else if (next == CecMeshState.Degraded)
        {
            kind = CecMeshEventKind.Degrade;
        }

        _eventLog.Add(new CecMeshEvent(
            _eventSequence++,
            unitId,
            kind,
            previous,
            next));
    }

    private NodeRuntime RequireNode(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            throw new ArgumentException("UnitId is required.", nameof(unitId));
        }

        var id = unitId.Trim();
        if (!_nodes.TryGetValue(id, out var node))
        {
            throw new KeyNotFoundException($"CEC node '{id}' is not registered.");
        }

        return node;
    }

    private static string OrganicKey(string sideId, string unitId, string targetId) =>
        $"{sideId}|{unitId}|{targetId}";

    private static double Clamp01(double value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 1)
        {
            return 1;
        }

        return value;
    }

    private sealed class NodeRuntime
    {
        public NodeRuntime(
            string unitId,
            string sideId,
            bool cecCapable,
            double latDeg,
            double lonDeg,
            bool isAlive,
            bool isSwarm)
        {
            UnitId = unitId;
            SideId = sideId;
            CecCapable = cecCapable;
            LatDeg = latDeg;
            LonDeg = lonDeg;
            IsAlive = isAlive;
            IsSwarm = isSwarm;
        }

        public string UnitId { get; }
        public string SideId { get; }
        public bool CecCapable { get; }
        public double LatDeg { get; set; }
        public double LonDeg { get; set; }
        public bool IsAlive { get; set; }
        public bool IsSwarm { get; }
    }

    private sealed record OrganicContribution(
        string SideId,
        string ContributorUnitId,
        string TargetId,
        double SensorQuality);
}
